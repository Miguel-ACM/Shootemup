import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as the vertical axis and looks at the +Z face.
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    ROOT = os.path.abspath(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1 else os.getcwd()
os.makedirs(ROOT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials:
    bpy.data.materials.remove(data)

palette = [
    (0.95, 0.29, 0.075, 1),  # orange shell
    (1.00, 0.52, 0.14, 1),   # warm orange inset
    (0.68, 0.15, 0.055, 1),  # red-orange edge
    (1.00, 0.76, 0.29, 1),   # brass cartridge
    (0.72, 0.39, 0.12, 1),   # brass ends
    (0.23, 0.32, 0.36, 1),   # blue-gray hardware
    (0.50, 0.70, 0.72, 1),   # cool hardware highlight
    (1.00, 0.88, 0.54, 1),   # cartridge band
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
def finish(obj, material, bevel=0):
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def prism(points, depth, z, material, bevel=0):
    n = len(points)
    verts = [(x, y, z - depth / 2) for x, y in points] + [(x, y, z + depth / 2) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new('Solid')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Solid', mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material, bevel)

def box(location, scale, material, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material, bevel)

def cylinder(location, radius, depth, material, axis='Y', vertices=12, bevel=0):
    rotation = (math.pi/2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material, bevel)

# Broad, swept boomerang: the inner notch is open and readable at pickup size.
outline = [(-.32,-.46), (-.39,-.31), (.055,0), (-.39,.31), (-.32,.46), (.315,.105), (.39,0), (.315,-.105)]
prism(outline, .17, 0, 0, .025)
# Raised inset panels follow the sweep on both faces; broad enough to survive downscaling.
for s in (-1, 1):
    for face in (-1, 1):
        panel = [(-.295,s*.385), (-.325,s*.325), (.255,s*.035), (.305,s*.085)]
        if s < 0:
            panel.reverse()
        prism(panel, .018, face*.094, 1, .007)
        cylinder((-.30, s*.335, face*.108), .041, .024, 5, 'Z', 8, .004)
        cylinder((-.30, s*.335, face*.123), .023, .008, 6, 'Z', 8)

# Cartridge and its mounting saddle bridge the boomerang's central notch.
box((-.13, 0, 0), (.30, .22, .23), 5, .028)
for face in (-1, 1):
    z = face*.158
    cylinder((-.17, 0, z), .085, .36, 3, 'Y', 12, .012)
    for y in (-.155, .155):
        cylinder((-.17, y, z), .089, .055, 4, 'Y', 12, .008)
    cylinder((-.17, 0, z), .089, .055, 7, 'Y', 12, .005)
    # Two stout clips visually connect the cartridge to the shell.
    for y in (-.105, .105):
        box((-.04, y, face*.13), (.13, .055, .08), 6, .009)

# Join and explicitly triangulate so the exported mesh has a predictable budget.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
print('Model triangles:', len(model.data.polygons))

bpy.ops.wm.obj_export(filepath=os.path.join(ROOT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not scene.world else scene.world
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj)
    return obj

light('Sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 160, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.32
scene.render.filepath = os.path.join(ROOT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.2, 0.72, 2.05)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 55
scene.render.filepath = os.path.join(ROOT, 'angle.png')
bpy.ops.render.render(write_still=True)

