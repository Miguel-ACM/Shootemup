import bpy
import math
import os
import sys
from mathutils import Vector

# Blender may execute a script without defining __file__ in some launch modes.
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    ROOT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.12, 0.70, 0.79, 1),  # cyan energy shell
    (0.31, 0.91, 0.94, 1),  # pale cyan energy facets
    (0.33, 0.48, 0.52, 1),  # blue steel braces
    (0.18, 0.29, 0.34, 1),  # blue steel cap
    (0.70, 0.79, 0.76, 1),  # pale ceramic
    (0.85, 0.59, 0.20, 1),  # gold terminals
    (0.98, 0.79, 0.35, 1),  # terminal highlights
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

def box(location, dimensions, material, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, material)

def cylinder(location, radius, depth, material, axis='Y', vertices=8):
    # Blender's cylinders start on Z; rotate them for a vertical Y axis.
    rotation = (math.pi / 2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       location=location, rotation=rotation)
    return finish(bpy.context.object, material)

# A faceted, opaque energy chamber, visible from every direction.
bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=8, location=(0, 0, 0))
core = bpy.context.object
core.scale = (0.235, 0.312, 0.235)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
finish(core, 0)
# Select a few substantial facets for a readable, non-emissive glint.
core.data.materials.append(mats[1])
for p in core.data.polygons:
    if p.center.x < -0.08 and p.center.y > 0.02 and p.center.z > 0.10:
        p.material_index = 1

# Two broad retaining jaws recall the original side rails.  Their chamfered
# edges and smaller ceramic inset break up the outline without fine detail.
for side in (-1, 1):
    box((side * 0.302, 0, 0), (0.115, 0.48, 0.32), 2, 0.035)
    box((side * 0.316, 0, 0.166), (0.074, 0.29, 0.025), 4, 0.012)
    box((side * 0.316, 0, -0.166), (0.074, 0.29, 0.025), 4, 0.012)
    # Octagonal top and bottom housings wrap around the chamber in depth.
    cylinder((0, side * 0.315, 0), 0.275, 0.105, 3)
    cylinder((0, side * 0.374, 0), 0.215, 0.045, 2)
    cylinder((0, side * 0.408, 0), 0.115, 0.035, 5)
    cylinder((0, side * 0.433, 0), 0.082, 0.018, 6)

# Broad concentric front and rear terminals tie the cyan chamber to the frame.
for side in (-1, 1):
    cylinder((0, 0, side * 0.222), 0.116, 0.050, 2, axis='Z', vertices=8)
    cylinder((0, 0, side * 0.254), 0.079, 0.022, 4, axis='Z', vertices=8)
    cylinder((0, 0, side * 0.269), 0.047, 0.010, 1, axis='Z', vertices=8)

# Bake every part into a single flat-shaded mesh in world coordinates.
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
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio')
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
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 110, 5.0)

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 4)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.16
scene.render.filepath = os.path.join(ROOT, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (1.65, 1.05, 2.7)
aim(camera)
camera_data.type = 'PERSP'
camera_data.lens = 54
scene.render.filepath = os.path.join(ROOT, 'angle.png')
bpy.ops.render.render(write_still=True)
