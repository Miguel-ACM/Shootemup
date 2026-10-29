import bpy
import math
import os
import sys
from mathutils import Vector

# The original item is a side-on AK, with the stock on the left and muzzle on the right.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv else os.getcwd()
    if os.path.isfile(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.18, 0.19, 0.21, 1),  # receiver: black plastic, lifted for readability
    (0.28, 0.30, 0.33, 1),  # dark steel
    (0.48, 0.275, 0.16, 1), # rust-brown wood
    (0.60, 0.36, 0.21, 1),  # wood bevel and inset
    (0.74, 0.77, 0.81, 1),  # chrome barrel
    (0.38, 0.40, 0.43, 1),  # steel trim
    (0.10, 0.105, 0.12, 1), # small muzzle aperture
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.8
    mats.append(m)

parts = []
def finish(obj, mat, bevel=0):
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Small flat bevels', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for f in obj.data.polygons:
        f.use_smooth = False
    parts.append(obj)
    return obj

def box(center, size, mat, bevel=0.01):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(ob, mat, bevel)

def prism(points, depth, mat, bevel=0.008):
    n = len(points)
    verts = [(x, y, z) for z in (-depth/2, depth/2) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new('shape')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new('part', mesh)
    bpy.context.collection.objects.link(ob)
    return finish(ob, mat, bevel)

def rod(a, b, radius, mat, vertices=8):
    a, b = Vector(a), Vector(b)
    d = b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=d.length, location=(a+b)/2)
    ob = bpy.context.object
    ob.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return finish(ob, mat)

# Receiver and gently sloping wooden stock retain the original layout.
box((0, 0, 0), (0.70, 0.16, 0.12), 0, 0.015)
prism([(-0.35,-0.06),(-0.35,0.07),(-0.72,0.02),(-0.72,-0.16)], 0.10, 2, 0.012)
box((-0.714,-0.068,0), (0.025,0.18,0.11), 1, 0.006)
# Long silver barrel, dark upper gas tube, and rust-brown handguard.
rod((0.32,0.03,0), (0.86,0.03,0), 0.03, 4)
rod((0.34,0.10,0), (0.62,0.10,0), 0.025, 1)
box((0.42,-0.01,0), (0.16,0.08,0.10), 2, 0.012)
rod((0.823,0.03,0), (0.863,0.03,0), 0.034, 5)
rod((0.863,0.03,0), (0.864,0.03,0), 0.018, 6)
# The original tilted magazine and wood pistol grip, given tapered silhouettes.
prism([(0.055,-0.065),(0.18,-0.065),(0.23,-0.20),(0.235,-0.33),
       (0.20,-0.36),(0.105,-0.34),(0.09,-0.23)], 0.09, 1, 0.009)
prism([(-0.23,-0.06),(-0.14,-0.06),(-0.155,-0.25),(-0.18,-0.275),
       (-0.255,-0.26)], 0.08, 2, 0.008)
# Subtle mirrored receiver cover and wood panels make the reverse equally finished.
for side in (-1, 1):
    z = side * 0.062
    box((-0.065,0.012,z), (0.39,0.095,0.012), 1, 0.006)
    box((-0.52,-0.065,side*0.052), (0.23,0.075,0.008), 3, 0.004)
    box((0.42,-0.01,side*0.052), (0.105,0.045,0.008), 3, 0.004)
    box((0.165,-0.27,side*0.046), (0.085,0.012,0.008), 5, 0.003)
    rod((-0.22,0.012,side*0.069),(-0.22,0.012,side*0.074),0.012,5,8)

# Join, center on the actual bounding box, and scale the original proportions to 0.90 wide.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
coords = [v.co for v in model.data.vertices]
lo = Vector(tuple(min(v[i] for v in coords) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in coords) for i in range(3)))
center = (lo + hi) / 2
scale = 0.90 / (hi.x - lo.x)
for v in model.data.vertices:
    v.co = (v.co - center) * scale
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target=(0, 0, 0)):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = loc
    point_at(ob)
    return ob

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, 1, 4), 120, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.70, 0.42, 1.45)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

