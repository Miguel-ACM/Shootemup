import bpy
import math
import os
import sys
from mathutils import Vector

# Unity-oriented model: Y is the long/up axis and Z is the front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.95, 0.70, 0.14, 1), # warm scatter-cannon yellow
    (1.00, 0.86, 0.36, 1), # muzzle and edge gold
    (0.65, 0.35, 0.08, 1), # ochre structural bands
    (0.31, 0.39, 0.43, 1), # blue steel
    (0.53, 0.65, 0.68, 1), # light steel
    (0.12, 0.17, 0.19, 1), # small bore details
    (0.94, 0.39, 0.09, 1), # orange accent
    (0.96, 0.90, 0.59, 1), # ivory glint
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
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def cylinder(a, b, radius, material, vertices=8):
    a, b = Vector(a), Vector(b)
    delta = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=delta.length, end_fill_type='NGON', location=(a+b)*0.5)
    obj = bpy.context.object
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = delta.to_track_quat('Z', 'Y')
    return finish(obj, material)

def box(location, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Chunky bevels', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, material)

def hollow_muzzle(center, axis, outer, inner, length, mat_outer, mat_inner, n=8):
    # An actual recessed bore, with a thick octagonal lip and a dark well.
    c, d = Vector(center), Vector(axis).normalized()
    u = Vector((0, 0, 1)).cross(d).normalized()
    v = d.cross(u).normalized()
    verts = []
    for offset, rad in [(-length/2, outer), (length/2, outer), (length/2, inner), (-length/2, inner)]:
        for j in range(n):
            angle = 2 * math.pi * j / n + math.pi / 8
            verts.append(c + d*offset + rad*(math.cos(angle)*u + math.sin(angle)*v))
    faces, indices = [], []
    for j in range(n):
        k = (j + 1) % n
        faces.extend([(j,k,n+k,n+j), (n+j,n+k,2*n+k,2*n+j), (2*n+j,2*n+k,3*n+k,3*n+j)])
        indices.extend([0,0,1])
    faces.append(tuple(3*n+j for j in reversed(range(n))))
    indices.append(1)
    mesh = bpy.data.meshes.new('Bored muzzle')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Bored muzzle', mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[mat_outer])
    obj.data.materials.append(mats[mat_inner])
    for p, index in zip(obj.data.polygons, indices):
        p.material_index = index
        p.use_smooth = False
    parts.append(obj)

# Five splayed octagonal barrels rise from a compact receiver. Their uneven
# heights and generous widths keep the fan readable even at HUD scale.
for i in range(-2, 3):
    start = Vector((i * 0.047, -0.105, 0))
    end = Vector((i * 0.164, 0.355 - abs(i)*0.040, 0))
    direction = (end-start).normalized()
    cylinder(start, end, 0.050, 0, 8)
    cylinder(start + direction*0.045, start + direction*0.095, 0.056, 2, 8)
    hollow_muzzle(end - direction*0.012, direction, 0.062, 0.032, 0.070, 1, 5)

# Faceted drum receiver, with finished front and back hubs and a short heel.
cylinder((0,-0.235,-0.13), (0,-0.235,0.13), 0.220, 3, 12)
cylinder((0,-0.235,-0.155), (0,-0.235,-0.12), 0.175, 2, 12)
cylinder((0,-0.235,0.12), (0,-0.235,0.155), 0.175, 2, 12)
cylinder((0,-0.235,-0.174), (0,-0.235,-0.153), 0.115, 0, 10)
cylinder((0,-0.235,0.153), (0,-0.235,0.174), 0.115, 0, 10)
cylinder((0,-0.235,-0.186), (0,-0.235,-0.173), 0.050, 4, 8)
cylinder((0,-0.235,0.173), (0,-0.235,0.186), 0.050, 4, 8)
box((0, -0.393, 0), (0.29, 0.105, 0.19), 3, 0.025)
box((0, -0.405, 0.105), (0.19, 0.045, 0.025), 6, 0.009)
box((0, -0.405, -0.105), (0.19, 0.045, 0.025), 6, 0.009)

# Join the entire pickup into one exportable mesh and bake its transforms.
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
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

# Render previews only after exporting, so no camera or light enters the OBJ.
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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
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

sun = light('Upper left sun', 'SUN', (-3, 5, 5), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4, 1, 4), 110, 5.0)
cam_data = bpy.data.cameras.new('Preview camera')
cam = bpy.data.objects.new('Preview camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.85, 0.58, 1.45)
aim(cam, (0, -0.02, 0))
cam_data.type = 'PERSP'
cam_data.lens = 52
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

