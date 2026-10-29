import bpy
import math
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- output-directory]
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    ROOT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(ROOT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    mats.append(mat)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def sphere(loc, scale, material, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, material)

def bevel_box(loc, scale, material, bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mod = obj.modifiers.new('Cut corners', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, material)

def cylinder(loc, radius, depth, material, vertices=12, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc, rotation=rotation)
    return finish(bpy.context.object, material)

def ring_z(cx, cy, z, outer, inner, height, material, n=16):
    # An actual thick annulus, leaving the window visibly recessed.
    verts = []
    for zz, rad in ((z, outer), (z+height, outer), (z+height, inner), (z, inner)):
        verts.extend((cx+rad*math.cos(2*math.pi*i/n), cy+rad*math.sin(2*math.pi*i/n), zz) for i in range(n))
    faces = []
    for j in range(4):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, ((j+1)%4)*n+k, ((j+1)%4)*n+i))
    mesh = bpy.data.meshes.new('Annulus')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Rim', mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material)

def muzzle(x):
    # Mouth plane is exactly y=-0.4; its center is at the gameplay x coordinate.
    # The annular lip surrounds a dark, recessed bore pointing toward -Y.
    n = 12
    z = 0.235
    verts = []
    for y, rad in ((-0.31, .108), (-0.40, .108), (-0.40, .069), (-0.365, .069)):
        verts.extend((x+rad*math.cos(2*math.pi*i/n), y, z+rad*math.sin(2*math.pi*i/n)) for i in range(n))
    faces = []
    for j in range(3):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    mesh = bpy.data.meshes.new('Launcher lip')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Launcher lip', mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, 13)
    cylinder((x,-.362,z), .068, .008, 11, 12, (math.pi/2,0,0))
    # Small sight on top of each launcher makes the fixed mouths legible from above.
    cylinder((x,-.345,.341), .025, .009, 10, 8)

# A faceted pressure vessel with a broad dark keel and raised brass equator.
sphere((0,0,0), (.64,.66,.45), 13, 16, 8)
cylinder((0,0,-.10), .635, .13, 8, 16)
# Restore the rounded crown over the equator; these overlapping forms read as armor.
sphere((0,0,.015), (.61,.63,.44), 13, 16, 8)
# Viewport: concentric cast-metal rim, dark gasket and inset cyan glass.
cylinder((0,-.13,.423), .325, .07, 11, 16)
ring_z(0,-.13,.452,.315,.235,.048,13,16)
cylinder((0,-.13,.465), .231, .018, 8, 16)
cylinder((0,-.13,.477), .195, .012, 9, 16)
# Broad window reflection, a geometric highlight rather than a texture.
bevel_box((-.058,-.16,.488), (.055,.21,.012), 6, .015)
# Flanking ballast tanks, with tapered-looking beveled ends and distinct steel caps.
for side in (-1, 1):
    x = side * .70
    bevel_box((x,.025,.00), (.23,.91,.25), 8, .065)
    bevel_box((x,.20,.145), (.20,.43,.075), 4, .035)
    bevel_box((x,-.28,.145), (.22,.23,.095), 11, .035)
    cylinder((x,.34,.17), .078, .055, 13, 10)
    muzzle(x)
# Rear mounting lug and an armored dorsal ridge make the front/rear direction clear.
bevel_box((0,.715,-.01), (.20,.25,.22), 6, .035)
bevel_box((0,.48,.305), (.14,.25,.08), 8, .025)

# Collapse all primitive transforms and modifiers into a single export mesh.
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
bpy.ops.wm.obj_export(filepath=os.path.join(ROOT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world') if scene.world is None else scene.world
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(kind, loc, energy, size=None):
    data = bpy.data.lights.new(kind, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(kind, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj, (0,0,0))
    return obj

light('SUN', (-3,4,6), 2.0)
light('AREA', (3,-1,4), 180, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,5)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.95
scene.render.filepath = os.path.join(ROOT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (2.8,-3.8,3.2)
aim(cam, (0,0,.03))
scene.render.filepath = os.path.join(ROOT,'angle.png')
bpy.ops.render.render(write_still=True)
