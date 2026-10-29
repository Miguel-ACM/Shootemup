import bpy
import math
import os
import sys
from mathutils import Vector

# Blender's background mode supplies the script path after -- in some launchers.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT_DIR = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT_DIR.lower().endswith('.py'):
        OUT_DIR = os.path.dirname(OUT_DIR)
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

def material(index, color):
    m = bpy.data.materials.new('tile_%02d' % index)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    return m

ivory = material(0, (0.78, 0.84, 0.79))
light = material(1, (0.91, 0.94, 0.87))
orange = material(2, (0.96, 0.32, 0.10))
orange_light = material(3, (1.0, 0.47, 0.16))
slate = material(4, (0.25, 0.35, 0.37))
metal = material(5, (0.53, 0.64, 0.63))
parts = []

def box(name, loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    if bevel:
        mod = ob.modifiers.new('Broad molded corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

def cross(name, z, depth, mat):
    # A single solid plus-shaped prism: no internal seam on the broad face.
    a, b = 0.068, 0.183
    outline = [(-a,-b),(a,-b),(a,-a),(b,-a),(b,a),(a,a),
               (a,b),(-a,b),(-a,a),(-b,a),(-b,-a),(-a,-a)]
    verts = [(x, y-0.025, z-depth/2) for x,y in outline] + [(x, y-0.025, z+depth/2) for x,y in outline]
    faces = [tuple(reversed(range(12))), tuple(range(12,24))]
    faces += [(i, (i+1)%12, (i+1)%12+12, i+12) for i in range(12)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    parts.append(ob)

# Rugged clamshell: wide, softly chamfered body and an inset face on either side.
box('Case body', (0,-0.045,0), (0.76,0.58,0.30), ivory, 0.055)
box('Front panel', (0,-0.045,0.151), (0.66,0.48,0.045), light, 0.038)
box('Back panel', (0,-0.045,-0.151), (0.66,0.48,0.045), light, 0.038)
cross('Front medical cross', 0.186, 0.030, orange)
cross('Back medical cross', -0.186, 0.030, orange)
# Chunky protective corner caps make the profile unmistakably a case.
for x in (-0.335, 0.335):
    for y in (-0.275, 0.185):
        box('Corner guard', (x,y,0), (0.10,0.11,0.32), metal, 0.018)
# A real open handle with two molded sockets, visible from both sides.
for x in (-0.15, 0.15):
    box('Handle socket', (x,0.257,0), (0.105,0.065,0.19), metal, 0.012)
    box('Handle upright', (x,0.314,0), (0.065,0.14,0.12), slate, 0.012)
box('Handle grip', (0,0.378,0), (0.36,0.075,0.13), slate, 0.018)
box('Grip inset', (0,0.392,0), (0.22,0.055,0.135), metal, 0.01)
# Large, simple side markings read while the pickup spins.
for side in (-1,1):
    box('Side badge', (side*0.383,-0.045,0), (0.035,0.24,0.18), orange, 0.012)
    box('Side badge stripe', (side*0.402,-0.045,0), (0.012,0.075,0.16), orange_light, 0.004)

bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Explicitly triangulate so exported geometry has a predictable budget.
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)

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
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target=(0,0,0)):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light_object(name, kind, location, energy, size=None):
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

light_object('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light_object('Soft fill', 'AREA', (4, 1, 5), 180, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.03
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.76,0.50,1.34)
aim(cam, (0,0,0))
cam_data.type = 'PERSP'
cam_data.lens = 50
cam_data.sensor_width = 36
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

