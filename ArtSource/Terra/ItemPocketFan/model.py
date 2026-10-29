import bpy
import math
import os
import sys
from mathutils import Vector

# Outputs accompany this script even when Blender is launched from another directory.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.76, 0.84, 0.81, 1),  # warm pale casing
    (0.40, 0.56, 0.57, 1),  # casing edge
    (0.12, 0.25, 0.29, 1),  # recessed fan interior
    (0.12, 0.78, 0.86, 1),  # cyan blades
    (0.31, 0.92, 0.94, 1),  # blade facets
    (0.90, 0.95, 0.86, 1),  # hub and highlights
    (0.25, 0.60, 0.63, 1),  # grip and switch surround
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
def mesh_part(name, verts, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def box(name, loc, scale, material, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Rounded corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def cylinder(name, loc, radius, depth, material, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def ring(name, cy, profile, material, n=20):
    # Each profile point is (radius, depth); build a closed faceted annulus.
    verts = [(r*math.cos(2*math.pi*i/n), cy+r*math.sin(2*math.pi*i/n), z)
             for r,z in profile for i in range(n)]
    faces = []
    for j in range(len(profile)):
        k = (j+1) % len(profile)
        for i in range(n):
            ni = (i+1) % n
            faces.append((j*n+i, j*n+ni, k*n+ni, k*n+i))
    return mesh_part(name, verts, faces, material)

# Rounded, hand-sized white handle and its broad neck.
box('Handle', (0, -0.307, 0), (0.19, 0.286, 0.17), 0, 0.032)
box('Grip inset', (0, -0.338, 0.087), (0.105, 0.16, 0.015), 6, 0.014)
box('Rear grip', (0, -0.338, -0.087), (0.105, 0.16, 0.015), 6, 0.014)
box('Neck', (0, -0.179, 0), (0.16, 0.14, 0.16), 1, 0.024)
# A circular dark recess makes the three light blue paddles unmistakable.
cy = 0.13
cylinder('Recess', (0, cy, 0), 0.287, 0.115, 2, 20)
ring('Thick sculpted rim', cy, [(0.270, -0.075), (0.293, -0.092), (0.315, -0.065), (0.320, 0.0), (0.315, 0.065), (0.293, 0.092), (0.270, 0.075)], 0)
ring('Front lip', cy, [(0.267, 0.074), (0.276, 0.092), (0.288, 0.092), (0.288, 0.075)], 5)
ring('Back lip', cy, [(0.267, -0.074), (0.276, -0.092), (0.288, -0.092), (0.288, -0.075)], 5)

# Thick faceted, swept blades on both faces; raised ridge suggests pitch.
def blade(angle, side):
    # In the XY plane, all three blades share the same broad curved silhouette.
    outline = [(0.035, -0.018), (0.130, -0.050), (0.247, -0.027), (0.259, 0.034), (0.224, 0.105), (0.146, 0.118), (0.064, 0.061)]
    ca, sa = math.cos(angle), math.sin(angle)
    xy = [(x*ca-y*sa, cy+x*sa+y*ca) for x,y in outline]
    z0, z1 = side*0.060, side*0.085
    verts = [(x,y,z0) for x,y in xy] + [(x,y,z1) for x,y in xy]
    verts.append((0.145*ca-0.035*sa, cy+0.145*sa+0.035*ca, side*0.100))
    faces = []
    n = len(xy)
    for i in range(n):
        j = (i+1)%n
        faces.append((i,j,n+j,n+i))
        faces.append((n+i,n+j,2*n))
    obj = mesh_part('Swept blade', verts, faces, 3)
    obj.data.materials.append(mats[4])
    for poly in obj.data.polygons:
        if poly.index % 2 and poly.index % 4 == 1:
            poly.material_index = 1
for side in (-1, 1):
    for i in range(3):
        blade(math.radians(20 + 120*i), side)
    cylinder('Hub base', (0, cy, side*0.092), 0.079, 0.040, 1, 12)
    cylinder('Hub cap', (0, cy, side*0.116), 0.061, 0.024, 5, 12)
    cylinder('Power button', (0, -0.245, side*0.092), 0.032, 0.020, 3, 12)

# Consolidate and triangulate prior to export; keep all faces flat shaded.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for polygon in model.data.polygons:
    polygon.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2000, 'Triangle budget exceeded: %d' % len(model.data.polygons)
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
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
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj
light('Sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 30, 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
aim(cam)
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.7, 0.85, 2.8)
aim(cam)
camdata.type = 'PERSP'
camdata.lens = 95
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

