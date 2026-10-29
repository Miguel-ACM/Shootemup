import bpy
import math
import os
import sys
from mathutils import Vector

# The asset lives in the XY plane; +Z is the visible face.
if globals().get('__file__'):
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Supply the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = {
    0: (0.78, 0.19, 0.40), 1: (0.97, 0.85, 0.71),
    8: (0.90, 0.44, 0.65), 12: (0.92, 0.31, 0.49),
    15: (0.98, 0.96, 0.88),
}
materials = {}
for index, color in palette.items():
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.8
    materials[index] = mat

parts = []
def mesh_part(name, vertices, faces, face_tiles):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    used = sorted(set(face_tiles))
    for tile in used:
        mesh.materials.append(materials[tile])
    for poly, tile in zip(mesh.polygons, face_tiles):
        poly.material_index = used.index(tile)
        poly.use_smooth = False
    parts.append(obj)
    return obj

# A lightly bevelled, polygonal candy disc. Rings create a thick edge and
# a broad, gently crowned face, with the back kept inside the original depth.
n = 24
rings = [(0.64, -0.11), (0.72, -0.075), (0.72, 0.025),
         (0.675, 0.075), (0.61, 0.092)]
verts = []
for radius, z in rings:
    for i in range(n):
        a = 2 * math.pi * i / n
        verts.append((radius * math.cos(a), 0.18 + radius * math.sin(a), z))
faces, tiles = [], []
faces.append(tuple(reversed(range(n))))
tiles.append(0)
for ring in range(len(rings) - 1):
    for i in range(n):
        j = (i + 1) % n
        faces.append((ring*n+i, ring*n+j, (ring+1)*n+j, (ring+1)*n+i))
        tiles.append([0, 0, 12, 8][ring])
faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
tiles.append(8)
mesh_part('Candy', verts, faces, tiles)

# A continuous, shallow raised cream ribbon follows an Archimedean spiral.
# The narrow edge is real geometry, readable even in the three-quarter view.
steps = 96
verts = []
for i in range(steps + 1):
    t = i / steps
    a = t * 4.8 * math.pi
    radius = 0.055 + 0.55 * t
    # Taper the two ends of the stripe so they finish neatly.
    taper = min(1.0, 0.25 + i / 3, 0.25 + (steps-i) / 3)
    half = 0.034 * taper
    for z in (0.093, 0.115):
        for r in (radius-half, radius+half):
            verts.append((math.cos(a)*r, 0.18+math.sin(a)*r, z))
faces, tiles = [], []
for i in range(steps):
    a, b = i*4, (i+1)*4
    faces.extend([(a+2, a+3, b+3, b+2),
                  (a, a+2, b+2, b),
                  (a+3, a+1, b+1, b+3)])
    tiles.extend([15, 1, 1])
faces.extend([(0, 1, 3, 2), (steps*4, steps*4+2, steps*4+3, steps*4+1)])
tiles.extend([1, 1])
mesh_part('Spiral', verts, faces, tiles)

# Octagonal paper stick, behind the sweet, with a clipped lower end.
verts = []
sides = 8
for y, radius in [(-1.85, 0.052), (-1.82, 0.065), (-0.40, 0.065)]:
    for i in range(sides):
        a = 2 * math.pi * i / sides + math.pi / 8
        verts.append((radius * math.cos(a), y, -0.09 + radius * math.sin(a)))
faces = [tuple(reversed(range(sides))), tuple(2*sides+i for i in range(sides))]
tiles = [1, 1]
for ring in range(2):
    for i in range(sides):
        j = (i+1) % sides
        faces.append((ring*sides+i, ring*sides+j, (ring+1)*sides+j, (ring+1)*sides+i))
        tiles.append(15 if i in (1, 2, 3) else 1)
mesh_part('Stick', verts, faces, tiles)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Triangulate explicitly so the budget and exported geometry are predictable.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert 150 <= len(model.data.polygons) <= 1500
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
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

target = (0, -0.475, 0)
light('Upper left sun', 'SUN', (-3, 4, 6), 2.0, target)
light('Soft fill', 'AREA', (3, -2, 4), 120, target, 5)
camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 3.17
camera.location = (0, -0.475, 6)
aim(camera, target)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

camera_data.type = 'PERSP'
camera_data.lens = 50
camera.location = (3.0, -3.5, 5.2)
aim(camera, target)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
