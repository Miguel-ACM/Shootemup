import bpy
import math
import os
import sys
from mathutils import Vector

# The pickup uses Y as its vertical axis and +Z as its front.
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script directory or script path after --')
    supplied = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    HERE = supplied if os.path.isdir(supplied) else os.path.dirname(supplied)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.92, 0.27, 0.075, 1),  # enamel orange
    (0.66, 0.16, 0.075, 1),  # deeper orange edging
    (1.00, 0.43, 0.12, 1),   # raised panel
    (0.93, 0.67, 0.24, 1),  # ochre cross and fuel
    (0.64, 0.72, 0.71, 1),  # steel handle
    (0.25, 0.32, 0.33, 1),  # cap and recesses
    (1.00, 0.78, 0.36, 1),  # fuel highlight
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
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('Broad flat bevels', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(ob, material)

def rod(name, a, b, width, depth, material):
    # Rectangular bar in the XY plane, with bevels that remain visible at icon size.
    a, b = Vector(a), Vector(b)
    mid = (a + b) * 0.5
    ob = box(name, mid, (width, (b-a).length, depth), material, min(width, depth) * 0.18)
    ob.rotation_euler[2] = math.atan2(-(b.x-a.x), b.y-a.y)
    return ob

def cylinder_y(name, loc, radius, depth, material, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                        location=loc, rotation=(math.pi/2, 0, 0))
    ob = bpy.context.object
    ob.name = name
    return finish(ob, material)

# A compact stamped metal can, with a continuous inset body and rolled edge bands.
box('Can body', (0, -0.045, 0), (0.61, 0.60, 0.33), 0, 0.055)
box('Lower rolled seam', (0, -0.315, 0), (0.59, 0.075, 0.345), 1, 0.022)
box('Upper shoulder', (0, 0.215, 0), (0.59, 0.105, 0.335), 0, 0.035)
# Substantial arched handle: the hole is real, visible from both faces.
rod('Handle left', (0.015, 0.235, 0), (0.055, 0.365, 0), 0.065, 0.13, 4)
rod('Handle top', (0.055, 0.365, 0), (0.245, 0.365, 0), 0.065, 0.13, 4)
rod('Handle right', (0.245, 0.365, 0), (0.27, 0.235, 0), 0.065, 0.13, 4)
# Stubby, dark screw cap and a warm metal collar on the opposite shoulder.
cylinder_y('Filler collar', (-0.185, 0.274, 0), 0.085, 0.075, 3)
cylinder_y('Filler cap', (-0.185, 0.329, 0), 0.083, 0.055, 5)
cylinder_y('Cap top', (-0.185, 0.359, 0), 0.064, 0.012, 4)

# Embossed inset faces and a bold golden plus mark on both sides of the can.
for side in (-1, 1):
    box('Stamped face', (0, -0.052, side*0.174), (0.48, 0.44, 0.028), 1, 0.035)
    box('Face center', (0, -0.052, side*0.191), (0.425, 0.385, 0.022), 2, 0.027)
    box('Cross vertical', (0, -0.052, side*0.207), (0.071, 0.31, 0.018), 3, 0.009)
    box('Cross horizontal', (0, -0.052, side*0.208), (0.31, 0.071, 0.020), 3, 0.009)

# A single large faceted drop, connected to the bottom edge so the leak survives rotation.
rod('Leak neck', (-0.235, -0.326, 0), (-0.255, -0.365, 0), 0.055, 0.07, 3)
bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=6, location=(-0.255, -0.403, 0))
drop = bpy.context.object
drop.name = 'Fuel drop'
drop.scale = (0.060, 0.074, 0.060)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
finish(drop, 6)

# Bake the construction into one flat-shaded, triangulated mesh.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(HERE, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Render setup is added only after export, keeping the OBJ model-only.
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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = loc
    aim(ob)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 110, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.20
scene.render.filepath = os.path.join(HERE, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (1.55, 1.05, 2.7)
aim(cam, (0, -0.02, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(HERE, 'angle.png')
bpy.ops.render.render(write_still=True)
