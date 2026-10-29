import bpy
import math
import os
import sys
from mathutils import Vector

# Rail Driver: twin accelerator rails held in three chunky inductive collars.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT_DIR = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT_DIR) or OUT_DIR.lower().endswith('.py'):
        OUT_DIR = os.path.dirname(OUT_DIR)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.48, 0.43, 0.94, 1),  # violet body
    (0.29, 0.28, 0.64, 1),  # rail side panels
    (0.72, 0.68, 1.00, 1),  # lavender collars
    (0.47, 0.77, 0.96, 1),  # cool conductor accents
    (0.78, 0.84, 0.96, 1),  # pale metal
    (0.24, 0.25, 0.46, 1),  # small recessed details
    (0.98, 0.67, 0.27, 1),  # warm power indicator
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    mats.append(mat)

parts = []
def block(name, center, size, material, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

# The broad lower breech supports a forked pair of rails. All geometry is
# symmetric in depth so the pickup remains legible while turning.
block('Breech', (0, -0.335, 0), (0.40, 0.20, 0.27), 0, 0.035)
block('Breech lower cap', (0, -0.425, 0), (0.32, 0.05, 0.22), 1, 0.012)
for side in (-1, 1):
    x = side * 0.125
    block('Main rail', (x, 0.045, 0), (0.115, 0.72, 0.18), 1, 0.018)
    block('Rail outer armour', (x + side * 0.037, 0.09, 0), (0.055, 0.59, 0.21), 0, 0.012)
    block('Muzzle shoulder', (x, 0.378, 0), (0.15, 0.115, 0.23), 0, 0.018)
    block('Muzzle cap', (x, 0.43, 0), (0.125, 0.04, 0.19), 4, 0.009)
    # Bright conductor faces show the split barrel from both front and back.
    for face in (-1, 1):
        block('Conductor strip', (x, 0.19, face * 0.105), (0.045, 0.29, 0.025), 3, 0.006)

# Three collars surround the rails with a small central aperture. Separate
# front and rear bars make the components visibly volumetric in side views.
for y in (-0.20, -0.005, 0.19):
    for side in (-1, 1):
        block('Coil outer end', (side * 0.235, y, 0), (0.10, 0.095, 0.28), 2, 0.016)
    for face in (-1, 1):
        block('Coil crossbar', (0, y, face * 0.105), (0.44, 0.075, 0.07), 2, 0.012)
        block('Coil centre conductor', (0, y, face * 0.147), (0.105, 0.046, 0.025), 3, 0.006)

# Large, simple identifier inset on both faces of the breech.
for face in (-1, 1):
    block('Breech inset', (0, -0.335, face * 0.142), (0.23, 0.115, 0.025), 1, 0.012)
    block('Power indicator', (0, -0.335, face * 0.159), (0.10, 0.065, 0.02), 6, 0.008)

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

# Only the finished pickup is selected for export; scene lighting is added later.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, type=kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 4), 90, (0, 0, 0), 5)

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 3)
point_at(camera, (0, 0, 0))
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (0.95, 0.56, 1.51)
point_at(camera, (0, 0, 0))
camera_data.type = 'PERSP'
camera_data.lens = 50
camera_data.sensor_width = 36
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

