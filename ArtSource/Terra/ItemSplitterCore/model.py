import bpy
import math
import os
import sys
from mathutils import Vector

# The pickup is authored in game coordinates: Y is vertical, Z is the front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.95, 0.57, 0.08, 1), # golden housing
    (1.00, 0.76, 0.19, 1), # bright brass
    (0.67, 0.32, 0.07, 1), # bronze joints
    (0.24, 0.34, 0.39, 1), # blue steel
    (0.42, 0.56, 0.58, 1), # pale steel
    (0.14, 0.22, 0.25, 1), # recesses
    (1.00, 0.87, 0.40, 1), # cartridge tips
    (0.95, 0.39, 0.08, 1), # orange core
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def cylinder(a, b, radius, material, sides=10, radius2=None):
    a, b = Vector(a), Vector(b)
    direction = b - a
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius, radius2=radius if radius2 is None else radius2,
                                   depth=direction.length, end_fill_type='NGON', location=(a+b)*0.5)
    obj = bpy.context.object
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = direction.to_track_quat('Z', 'Y')
    return finish(obj, material)

def ring(a, b, outer, inner, material, sides=10):
    # A substantial hollow barrel lip, including the inset bore wall.
    a, b = Vector(a), Vector(b)
    axis = (b-a).normalized()
    u = axis.cross(Vector((0, 0, 1)))
    if u.length < 0.01:
        u = axis.cross(Vector((0, 1, 0)))
    u.normalize()
    v = axis.cross(u).normalized()
    verts = []
    for center, rad in ((a, outer), (b, outer), (b, inner), (a, inner)):
        for i in range(sides):
            t = 2 * math.pi * i / sides
            verts.append(center + rad * (math.cos(t)*u + math.sin(t)*v))
    faces = []
    for j in range(4):
        for i in range(sides):
            k = (i+1) % sides
            faces.append((j*sides+i, j*sides+k, ((j+1)%4)*sides+k, ((j+1)%4)*sides+i))
    mesh = bpy.data.meshes.new('barrel lip')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('barrel lip', mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material)

# Single feed cartridge and collar below a thick, octagonal splitter manifold.
cylinder((0,-.43,0), (0,-.14,0), .105, 3, 10)
cylinder((0,-.43,0), (0,-.38,0), .128, 1, 10)
cylinder((0,-.35,0), (0,-.30,0), .111, 1, 10)
cylinder((0,-.16,0), (0,-.10,0), .145, 2, 10)
cylinder((0,-.055,-.145), (0,-.055,.145), .202, 0, 10)
# Raised, stepped central hub on both faces reads equally well during rotation.
for s in (-1, 1):
    cylinder((0,-.055,s*.145), (0,-.055,s*.174), .155, 2, 10)
    cylinder((0,-.055,s*.174), (0,-.055,s*.195), .115, 1, 10)
    cylinder((0,-.055,s*.195), (0,-.055,s*.209), .071, 7, 8)

# Two thick diverging gun barrels make the split visible in silhouette.
for s in (-1, 1):
    start = Vector((s*.075, .035, 0))
    end = Vector((s*.285, .302, 0))
    axis = (end-start).normalized()
    cylinder(start, end, .082, 0, 10)
    cylinder(start + axis*.035, start + axis*.082, .096, 2, 10)
    cylinder(end - axis*.045, end + axis*.012, .101, 3, 10)
    ring(end, end + axis*.095, .108, .055, 1, 10)
    cylinder(end + axis*.012, end + axis*.017, .054, 5, 10)
    # Visible paired projectile tips above the muzzles, offset forward slightly.
    tip_start = end + axis*.105 + Vector((0,0,.01))
    cylinder(tip_start, tip_start + axis*.055, .046, 6, 8, .012)

# One mesh with baked transforms and flat normals.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

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
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-3, 5, 6))
sun = bpy.context.object
sun.data.energy = 2.0
sun.data.angle = math.radians(18)
point_at(sun, (0,0,0))
bpy.ops.object.light_add(type='AREA', location=(4, 1, 4))
fill = bpy.context.object
fill.data.energy = 120
fill.data.shape = 'DISK'
fill.data.size = 5
point_at(fill, (0,0,0))

bpy.ops.object.camera_add(location=(0, 0, 3))
cam = bpy.context.object
scene.camera = cam
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.22
point_at(cam, (0,0,0))
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.data.type = 'PERSP'
cam.data.lens = 50
cam.location = (0.90, 0.58, 1.62)
point_at(cam, (0,0,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

