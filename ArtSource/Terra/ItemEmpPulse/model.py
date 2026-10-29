import bpy
import math
import os
import sys
from mathutils import Vector

# The exported object uses X right, Y up and Z forward.
def script_directory():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()

OUT = script_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.18, 0.62, 0.62, 1),  # teal housing
    (0.30, 0.87, 0.82, 1),  # turquoise armor
    (0.48, 0.95, 0.92, 1),  # pale cyan energy
    (0.17, 0.42, 0.52, 1),  # blue steel
    (0.57, 0.75, 0.78, 1),  # silver blue
    (0.98, 0.69, 0.20, 1),  # amber contacts
    (0.93, 0.98, 0.88, 1),  # pale center
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.65
    bsdf.inputs['Metallic'].default_value = 0.08
    mats.append(mat)

parts = []
def profile_disc(name, rings, segments, material, phase=math.pi/8):
    # Each ring is (radius, Z); the broad, faceted edges supply real depth.
    verts = [(r * math.cos(phase + 2 * math.pi * i / segments),
              r * math.sin(phase + 2 * math.pi * i / segments), z)
             for r, z in rings for i in range(segments)]
    faces = []
    faces.append(tuple(reversed(range(segments))))
    for j in range(len(rings) - 1):
        for i in range(segments):
            a = j * segments + i
            b = j * segments + (i + 1) % segments
            faces.append((a, b, b + segments, a + segments))
    faces.append(tuple((len(rings) - 1) * segments + i for i in range(segments)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mats[material])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

def block(name, location, scale, material, bevel=0.0, angle=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    obj.rotation_euler[2] = angle
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

# The original is a radial EMP puck with eight surrounding contacts and a mast.
# Its front and back have matching stepped emitter faces for the spin.
profile_disc('Main chassis', [(0.265,-0.155),(0.315,-0.11),(0.315,0.11),(0.265,0.155)], 16, 0)
profile_disc('Center belt', [(0.315,-0.048),(0.326,-0.036),(0.326,0.036),(0.315,0.048)], 16, 3)
for side in (-1, 1):
    # Ring profiles are ordered back to front, even on the back of the device.
    def face_disc(name, radius, base, top, mat, segments=16):
        a, b = sorted((side * base, side * top))
        lip = min(0.012, (b-a)*0.35)
        profile_disc(name, [(radius*0.91,a),(radius,a+lip),(radius,b-lip),(radius*0.91,b)], segments, mat)
    face_disc('Raised bezel', 0.235, 0.150, 0.192, 4)
    face_disc('Emitter field', 0.188, 0.190, 0.219, 1)
    face_disc('Central socket', 0.118, 0.217, 0.245, 3, 12)
    face_disc('Pulse core', 0.085, 0.243, 0.268, 2, 12)
    face_disc('Core cap', 0.038, 0.267, 0.278, 6, 8)
    # Four bold, inset contact marks on both faces.
    for i in range(4):
        a = math.pi / 4 + i * math.pi / 2
        block('Emitter contact', (0.157*math.cos(a), 0.157*math.sin(a), side*0.224),
              (0.050, 0.025, 0.014), 5, 0.004, a)

# Eight chunky radial teeth echo the original square nodes and make the outline distinctive.
for i in range(8):
    a = i * math.pi / 4
    block('Radial lug', (0.355*math.cos(a), 0.355*math.sin(a), 0),
          (0.135, 0.105, 0.23), 1, 0.018, a)
    block('Lug contact', (0.414*math.cos(a), 0.414*math.sin(a), 0),
          (0.035, 0.065, 0.13), 5, 0.008, a)

# The short aerial is a recognizable asymmetry in the source silhouette.
block('Aerial base', (0, 0.400, 0), (0.115, 0.080, 0.14), 3, 0.015)
block('Aerial stem', (0, 0.455, 0), (0.058, 0.100, 0.075), 4, 0.009)
block('Aerial tip', (0, 0.510, 0), (0.10, 0.045, 0.10), 2, 0.012)

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
# Explicit triangulation makes the exported budget and the faceted surface predictable.
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
    export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

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

def point_at(obj, target):
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
    point_at(obj, (0, 0, 0))
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 5), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 1, 4), 180, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0.04, 4)
point_at(cam, (0, 0.04, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.28
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.85, 0.68, 1.55)
point_at(cam, (0, 0.035, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

