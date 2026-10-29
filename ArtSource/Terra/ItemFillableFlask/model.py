import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Blender runs this file directly, or receives its path after -- in the command line.
def script_dir():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            return os.path.dirname(os.path.abspath(args[0]))
    raise RuntimeError('Provide the script path after -- when __file__ is unavailable')

OUT = script_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.12, 0.34, 0.22, 1), # bottle green
    (0.22, 0.48, 0.30, 1), # glass edges
    (0.90, 0.32, 0.055, 1), # ember liquid
    (1.00, 0.57, 0.12, 1), # golden liquid
    (0.49, 0.25, 0.10, 1), # cork sides
    (0.74, 0.46, 0.20, 1), # cork top
    (0.72, 0.48, 0.16, 1), # brass collar
    (0.96, 0.72, 0.27, 1), # brass highlight
    (1.00, 0.76, 0.30, 1), # flame mark
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.68
    mats.append(mat)

parts = []
# Revolve horizontal rings around the Y axis. Adjacent ring pairs receive
# individual colours, so the opaque flask remains legible from every angle.
def lathe(name, rings, sections, segments=12, phase=math.pi / 12):
    verts = []
    for y, radius in rings:
        for i in range(segments):
            a = 2 * math.pi * i / segments + phase
            verts.append((radius * math.cos(a), y, radius * math.sin(a)))
    faces = []
    indices = []
    faces.append(tuple(reversed(range(segments))))
    indices.append(sections[0])
    for j in range(len(rings) - 1):
        for i in range(segments):
            faces.append((j * segments + i, j * segments + (i + 1) % segments,
                          (j + 1) * segments + (i + 1) % segments, (j + 1) * segments + i))
            indices.append(sections[j])
    faces.append(tuple((len(rings) - 1) * segments + i for i in range(segments)))
    indices.append(sections[-1])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for poly, index in zip(mesh.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    parts.append(ob)
    return ob

# Broad bottom, rounded amber belly and green shoulders taper to a short neck.
lathe('Flask', [(-.44, .19), (-.415, .255), (-.36, .292),
                (-.27, .315), (-.10, .315), (.015, .285),
                (.10, .225), (.17, .13), (.205, .105),
                (.29, .105)],
      [0, 1, 0, 2, 3, 2, 1, 0, 1], 16, math.pi / 16)
# Substantial green foot and horizontal glass bands frame the amber interior.
lathe('Foot', [(-.425, .248), (-.41, .285), (-.37, .302), (-.35, .302)],
      [0, 1, 0], 16, math.pi / 16)
lathe('Shoulder rim', [(.025, .282), (.045, .27), (.08, .242), (.095, .225)],
      [0, 1, 0], 16, math.pi / 16)
# Thick green vertical ribs give the impression of reinforced glass and make
# the amber belly read as broad inset windows as the item rotates.
for i in range(8):
    a = 2 * math.pi * i / 8
    bpy.ops.mesh.primitive_cube_add(size=1, location=(.300 * math.cos(a), -.175, .300 * math.sin(a)))
    rib = bpy.context.object
    rib.name = 'Glass rib'
    rib.dimensions = (.058, .34, .040)
    rib.rotation_euler[1] = -a
    # Local X is tangential; local Z is radial.
    rib.rotation_euler[1] = math.pi / 2 - a
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = rib.modifiers.new('Soft bevel', 'BEVEL')
    bevel.width = .012
    bevel.segments = 1
    bevel.affect = 'EDGES'
    rib.data.materials.append(mats[1])
    parts.append(rib)

lathe('Neck ring', [(.205, .108), (.22, .132), (.265, .132), (.28, .105)],
      [6, 7, 6], 12)
lathe('Cork', [(.275, .080), (.39, .095), (.42, .085)], [4, 5], 12)
lathe('Cork top', [(.405, .086), (.42, .085)], [5], 12)

# A bold, angular flame badge on both broad faces, raised above the windows.
# The silhouette is deliberately simple enough to survive at HUD scale.
def flame(name, side):
    outline = [(-.080, -.25), (-.105, -.19), (-.085, -.12), (-.045, -.075),
               (-.040, -.145), (.005, -.09), (.025, -.025), (.075, -.10),
               (.105, -.18), (.085, -.245), (.04, -.28), (-.04, -.28)]
    z0, z1 = side * .312, side * .328
    verts = [(x, y, z0) for x, y in outline] + [(x, y, z1) for x, y in outline]
    n = len(outline)
    faces = [tuple(range(n - 1, -1, -1)), tuple(range(n, n * 2))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mats[8])
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    parts.append(ob)
flame('Front flame', 1)
flame('Back flame', -1)

# Apply all modifiers, consolidate, triangulate, and bake every transform.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    for mod in list(ob.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
bm = bmesh.new()
bm.from_mesh(model.data)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(model.data)
bm.free()
model.data.update()
for face in model.data.polygons:
    face.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05, .07, .09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(ob, target=(0, 0, 0)):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    aim(ob)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 4), 2.2)
light('Soft fill', 'AREA', (4, 1, 4), 70, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.13
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.99, 0.60, 1.68)
aim(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 65
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

