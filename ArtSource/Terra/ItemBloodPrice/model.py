import bpy
import math
import os
import sys
from mathutils import Vector

# This model uses the game's coordinates: Y up, Z toward the viewer.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    OUT_DIR = os.path.abspath(args[0]) if args else os.getcwd()
    if os.path.splitext(OUT_DIR)[1]:
        OUT_DIR = os.path.dirname(OUT_DIR)
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.68, 0.10, 0.16, 1),  # rich blood red
    (0.90, 0.20, 0.23, 1),  # bright red facets
    (0.47, 0.055, 0.10, 1), # deep red facets
    (0.73, 0.79, 0.80, 1),  # cool steel
    (0.92, 0.91, 0.82, 1),  # blade edge
    (0.51, 0.62, 0.65, 1),  # steel facets
    (0.83, 0.52, 0.20, 1),  # antique gold
    (0.98, 0.72, 0.32, 1),  # gold highlight
    (0.37, 0.14, 0.14, 1),  # oxblood leather
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    materials.append(mat)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in materials:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

def bevel_box(name, loc, scale, mat, bevel=0.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[mat])
    mod = obj.modifiers.new('Broad carved edges', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    mod.affect = 'EDGES'
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def ring_form(name, center_x, rings, sides, mat_ids, phase=0):
    # rings are (y, x radius, z radius), all axisymmetric about Y
    verts = []
    for y, rx, rz in rings:
        for i in range(sides):
            a = 2 * math.pi * i / sides + phase
            verts.append((center_x + rx * math.cos(a), y, rz * math.sin(a)))
    faces = [tuple(reversed(range(sides)))]
    ids = [mat_ids[0]]
    for row in range(len(rings) - 1):
        for i in range(sides):
            faces.append((row*sides+i, row*sides+(i+1)%sides,
                          (row+1)*sides+(i+1)%sides, (row+1)*sides+i))
            ids.append(mat_ids[(row + i % 3) % len(mat_ids)])
    faces.append(tuple((len(rings)-1)*sides+i for i in range(sides)))
    ids.append(mat_ids[-1])
    return mesh_part(name, verts, faces, ids)

# A thick diamond-section blade: a strong ridge on both faces, bright cutting edges.
# Broad at the guard, tapering to a sharply pointed crown.
blade_verts = [
    (-.105, .055, 0), (0, .055, .060), (.105, .055, 0), (0, .055, -.060),
    (-.070, .335, 0), (0, .335, .048), (.070, .335, 0), (0, .335, -.048),
    (0, .49, 0),
]
blade_faces = [(0,1,5,4), (1,2,6,5), (2,3,7,6), (3,0,4,7),
               (4,5,8), (5,6,8), (6,7,8), (7,4,8), (3,2,1,0)]
mesh_part('Diamond blade', blade_verts, blade_faces, [4,3,5,3,4,3,5,3,5])

# Swept, faceted crossguard with thick ends. The shape is mirrored front and back.
outline = [(-.205,.015),(-.225,.070),(-.165,.065),(-.105,.045),
           (.105,.045),(.165,.065),(.225,.070),(.205,.015),
           (.145,-.005),(-.145,-.005)]
verts = [(x,y,z) for z in (-.052,.052) for x,y in outline]
n = len(outline)
faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
ids = [6,7]
for i in range(n):
    faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
    ids.append(6)
guard = mesh_part('Swept gold guard', verts, faces, ids)
bevel = guard.modifiers.new('Guard edge bevel', 'BEVEL')
bevel.width = .012
bevel.segments = 1
bpy.context.view_layer.objects.active = guard
guard.select_set(True)
bpy.ops.object.modifier_apply(modifier=bevel.name)
guard.select_set(False)

# A thick leather handle and gold collars maintain a readable profile from the side.
ring_form('Leather grip', 0, [(-.245,.050,.050),(-.225,.055,.055),
                            (-.045,.046,.046),(-.025,.050,.050)], 10, [8,0])
ring_form('Upper collar', 0, [(-.053,.058,.058),(-.025,.061,.061),
                             (-.012,.055,.055)], 10, [6,7])
ring_form('Lower collar', 0, [(-.250,.057,.057),(-.225,.058,.058)], 10, [6,7])
ring_form('Faceted pommel', 0, [(-.305,.025,.025),(-.292,.055,.055),
                              (-.255,.060,.060),(-.242,.047,.047)], 10, [6,7])
# A raised red jewel identifies both faces of the guard.
for z in (-.065, .065):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=(0,.025,z))
    obj = bpy.context.object
    obj.name = 'Guard ruby'
    obj.scale = (.042,.036,.025)
    obj.data.materials.append(materials[1])
    parts.append(obj)

# One large, solid blood drop, with a pointed crown and rounded, faceted belly.
ring_form('Blood drop', .245, [(-.405,.028,.028),(-.391,.071,.063),
                              (-.355,.099,.083),(-.305,.103,.087),
                              (-.255,.084,.073),(-.205,.056,.051),
                              (-.155,.027,.025),(-.110,.002,.002)],
          12, [0,1,0,2], phase=math.pi/12)

# Join only the model parts, apply all transforms, and force flat faces.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

# Export before adding the render-only camera and lights.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4, 1, 4), 120, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0.04, 4)
aim(cam, (0, 0.04, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.19
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (1.5, 1.0, 2.8)
aim(cam, (0.04, 0.035, 0))
cam_data.type = 'PERSP'
cam_data.lens = 70
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

