import bpy
import math
import os
import sys
from mathutils import Vector

# Save all deliverables alongside this script, including in Blender's --python mode.
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    ROOT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)


def material(name, color, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = emission
    return mat

shell = material('tile_00', (0.09, 0.13, 0.14))
edge = material('tile_01', (0.17, 0.22, 0.22))
underside = material('tile_02', (0.05, 0.07, 0.08))
lens = material('tile_03', (0.055, 0.085, 0.09))
brass = material('tile_04', (0.32, 0.27, 0.15))
cyan = material('tile_05', (0.25, 0.75, 0.85), 1.8)
black = material('tile_06', (0.035, 0.055, 0.06))

parts = []

# A revolved, faceted profile makes the octagonal housing and its sloping bottom.
# Octagon vertices at the cardinal axes: maximum X and Y are exactly 0.5.
def profile_mesh(name, profile, sides, mats, bands, phase=0.0, caps=True):
    verts = []
    for radius, z in profile:
        verts.extend((radius * math.cos(phase + 2 * math.pi * i / sides),
                      radius * math.sin(phase + 2 * math.pi * i / sides), z)
                     for i in range(sides))
    faces, indices = [], []
    if caps:
        faces.append(tuple(reversed(range(sides))))
        indices.append(bands[0])
    for j in range(len(profile) - 1):
        for i in range(sides):
            faces.append((j*sides+i, j*sides+(i+1)%sides,
                          (j+1)*sides+(i+1)%sides, (j+1)*sides+i))
            indices.append(bands[j])
    if caps:
        faces.append(tuple((len(profile)-1)*sides+i for i in range(sides)))
        indices.append(bands[-1])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

# Ordered bottom to top; uppermost face is the z=0 display surface.
profile_mesh('Housing', [(0.23,-0.155), (0.36,-0.135), (0.47,-0.095),
                         (0.50,-0.070), (0.50,-0.026), (0.472,-0.002)],
             8, [underside, shell, edge], [0,0,1,1,2])

# Shallow concentric inset gives the projector a framed, recessed appearance.
# Components remain just below the top plane, so the disc's top is at z=0.
profile_mesh('Lens surround', [(0.355,-0.0018), (0.340,-0.001)], 32,
             [black], [0], caps=False)
profile_mesh('Projector ring', [(0.340,-0.0009), (0.322,-0.0009)], 32,
             [cyan], [0], caps=False)
profile_mesh('Inner bezel', [(0.322,-0.0008), (0.306,-0.0008)], 32,
             [edge], [0], caps=False)
profile_mesh('Centre lens', [(0.306,-0.0007), (0.282,-0.0007)], 32,
             [lens], [0], caps=True)
# Center lens cap covers the housing face and uses a subtle inset octagonal core.
profile_mesh('Lens core', [(0.19,-0.0005), (0.19,-0.0004)], 8,
             [black], [0])

# Four small inset brass registration blocks, placed within the octagonal outline.
def block(name, loc, scale, mat, angle=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    obj.rotation_euler[2] = angle
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    bevel = obj.modifiers.new('Small machined edges', 'BEVEL')
    bevel.width = 0.004
    bevel.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(obj)

for a in (math.pi/4, 3*math.pi/4, 5*math.pi/4, 7*math.pi/4):
    block('Brass index', (0.405*math.cos(a), 0.405*math.sin(a), -0.002),
          (0.046, 0.012, 0.004), brass, a + math.pi/2)

# Single downward emitter, integrated into the tapered underside.
profile_mesh('Emitter collar', [(0.105,-0.169), (0.125,-0.155)], 16,
             [edge], [0])
profile_mesh('Emitter aperture', [(0.073,-0.178), (0.10,-0.168)], 16,
             [black], [0])
profile_mesh('Emitter light', [(0.052,-0.180), (0.052,-0.179)], 16,
             [cyan], [0])

# One exportable mesh, with transforms baked and all faces kept flat.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for polygon in model.data.polygons:
    polygon.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(ROOT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Tunnel ambient') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.06, 0.08, 0.09, 1.0)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

bpy.ops.object.light_add(type='SUN', location=(-3, 4, 6))
sun = bpy.context.object
sun.name = 'Upper left sun'
sun.data.energy = 2.0
sun.data.angle = math.radians(12)
sun.rotation_euler = (Vector((0,0,0)) - sun.location).to_track_quat('-Z','Y').to_euler()

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.name = 'Camera'
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.35
scene.camera = camera

def render_view(position, target, path):
    camera.location = position
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = os.path.join(ROOT, path)
    bpy.ops.render.render(write_still=True)

render_view((0, 0, 4), (0, 0, -0.07), 'top.png')
render_view((2.4, -3.2, 2.6), (0, 0, -0.075), 'angle.png')

