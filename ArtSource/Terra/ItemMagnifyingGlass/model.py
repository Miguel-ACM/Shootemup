import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Resolve the output location in both Blender's text runner and background mode.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.75, 0.49, 0.16, 1),  # warm brass outer rim
    (0.96, 0.72, 0.30, 1),  # golden bevel
    (0.57, 0.36, 0.12, 1),  # rim side
    (0.25, 0.48, 0.54, 1),  # blue-gray glass edge
    (0.37, 0.66, 0.70, 1),  # lens
    (0.54, 0.78, 0.79, 1),  # lens facets
    (0.83, 0.92, 0.86, 1),  # glint
    (0.62, 0.25, 0.19, 1),  # red-brown grip
    (0.77, 0.36, 0.24, 1),  # grip bevel
    (0.88, 0.64, 0.28, 1),  # collar
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def mesh_part(name, verts, faces, material_indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly, index in zip(mesh.polygons, material_indices):
        poly.material_index = index
        poly.use_smooth = False
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    parts.append(obj)
    return obj

# Surfaces of revolution around Z: coordinates are (radius, depth, material
# for the strip that follows). The closed cross section gives a true rim hole.
def radial(name, cx, cy, profile, segments=20):
    verts = []
    for radius, z, _ in profile:
        for i in range(segments):
            a = 2 * math.pi * i / segments
            verts.append((cx + radius * math.cos(a), cy + radius * math.sin(a), z))
    faces, indices = [], []
    for j in range(len(profile) - 1):
        for i in range(segments):
            k = (i + 1) % segments
            faces.append((j * segments + i, j * segments + k,
                          (j + 1) * segments + k, (j + 1) * segments + i))
            indices.append(profile[j][2])
    return mesh_part(name, verts, faces, indices)

# Rounded octagonal profile on the grip, along the vertical Y axis.
def grip(name, sections, material, sides=8):
    verts = []
    for y, rx, rz in sections:
        for i in range(sides):
            a = 2 * math.pi * (i + 0.5) / sides
            verts.append((rx * math.cos(a), y, rz * math.sin(a)))
    faces = [tuple(reversed(range(sides)))]
    indices = [material]
    for j in range(len(sections) - 1):
        for i in range(sides):
            k = (i + 1) % sides
            faces.append((j*sides+i, j*sides+k, (j+1)*sides+k, (j+1)*sides+i))
            indices.append(8 if i in (0, 3, 4, 7) and material == 7 else material)
    faces.append(tuple((len(sections)-1)*sides+i for i in range(sides)))
    indices.append(material)
    return mesh_part(name, verts, faces, indices)

# The outline is 0.62 wide and 0.90 high, centered on the origin.
cy = 0.14
radial('beveled brass bezel', 0, cy, [
    (0.245, -0.074, 1), (0.275, -0.082, 1),
    (0.305, -0.057, 0), (0.310, -0.038, 2),
    (0.310,  0.038, 0), (0.305,  0.057, 1),
    (0.275,  0.082, 1), (0.245,  0.074, 2),
    (0.245, -0.074, 2),
])
# Opaque, faceted lens is deliberately colored like glass; matching reverse face.
radial('convex blue lens', 0, cy, [
    (0.002, -0.084, 5), (0.125, -0.080, 4),
    (0.247, -0.060, 3), (0.247, 0.060, 4),
    (0.125, 0.080, 5), (0.002, 0.084, 5),
])
# Broad, graphic highlights read even at small sizes, on both sides of the glass.
for z in (-0.086, 0.086):
    points = [(-0.135, 0.16, z), (-0.097, 0.16, z),
              (-0.045, 0.32, z), (-0.078, 0.32, z)]
    mesh_part('lens glint', points, [(0, 1, 2, 3)], [6])

# A short ferrule joins the round head to a chunky, tapered, rounded grip.
grip('brass neck', [(-0.225, .080, .076), (-0.205, .093, .085),
                     (-0.155, .093, .085), (-0.135, .070, .072)], 9)
grip('red brown handle', [(-0.450, .055, .056), (-0.435, .076, .072),
                           (-0.240, .067, .067), (-0.215, .060, .061)], 7)
grip('bottom brass cap', [(-0.450, .055, .056), (-0.445, .070, .067),
                           (-0.423, .075, .071), (-0.416, .073, .070)], 9)

# Join and bake geometry before export. Triangulation fixes the exported budget
# and produces the same flat facets in Blender and the game.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 1, 4), 130, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.1, 0.65, 2.05)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 65
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

