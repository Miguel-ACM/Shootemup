import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as up and +Z as the front of the pickup.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    return mat

ivory = material(0, (0.78, 0.84, 0.78))
light = material(1, (0.92, 0.94, 0.84))
orange = material(2, (0.96, 0.31, 0.075))
slate = material(3, (0.37, 0.48, 0.48))
cap = material(4, (0.58, 0.68, 0.66))

parts = []
def block(name, loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Broad chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

# Two separate, slightly staggered hard-shell medical packs.
for side in (-1, 1):
    x, y, z = side * 0.205, side * 0.075, side * 0.055
    block('Pack shell', (x, y, z), (0.36, 0.50, 0.25), ivory, 0.035)
    # Raised light face panels provide a clean field for the bold orange cross.
    for face in (-1, 1):
        fz = z + face * 0.132
        block('Face panel', (x, y, fz), (0.292, 0.410, 0.026), light, 0.018)
        cross_z = z + face * 0.151
        block('Cross vertical', (x, y, cross_z), (0.075, 0.245, 0.023), orange, 0.006)
        block('Cross horizontal', (x, y, cross_z), (0.225, 0.075, 0.023), orange, 0.006)
    # A broad neck and cap give the packs their original bottle-like outline.
    block('Neck', (x, y + 0.259, z), (0.15, 0.050, 0.145), slate, 0.010)
    block('Cap', (x, y + 0.290, z), (0.18, 0.055, 0.17), cap, 0.012)
    # Side grips remain readable when the item turns edge-on.
    for edge in (-1, 1):
        block('Side grip', (x + edge * 0.181, y, z), (0.024, 0.25, 0.145), cap, 0.008)

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

# Export only the model, before creating the presentation rig.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light_obj(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light_obj('Upper left sun', 'SUN', (-3, 5, 4), 2.0)
sun.data.angle = math.radians(15)
light_obj('Soft fill', 'AREA', (4, 1, 4), 45, 5)

cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.04
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (0.9, 0.56, 1.37)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam_data.sensor_width = 36
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

