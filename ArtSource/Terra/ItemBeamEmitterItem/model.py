import bpy
import math
import os
import sys
from mathutils import Vector

# The model uses Y as its vertical axis and Z as its front axis.
def output_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    args = sys.argv
    if '--' in args and args.index('--') + 1 < len(args):
        p = os.path.abspath(args[args.index('--') + 1])
        return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()

out = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.22, 0.68, 0.74, 1),  # turquoise body
    (0.40, 0.88, 0.91, 1),  # pale cyan armor
    (0.15, 0.38, 0.46, 1),  # blue steel
    (0.14, 0.78, 0.88, 1),  # optical cyan
    (0.62, 0.98, 0.96, 1),  # lens highlight
    (0.88, 0.72, 0.32, 1),  # warm brass contacts
    (0.35, 0.56, 0.61, 1),  # intermediate metal
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.65
    materials.append(mat)

parts = []
def finish(obj, material, bevel=0):
    obj.data.materials.append(materials[material])
    if bevel:
        mod = obj.modifiers.new('Machined bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, location, size, material, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material, bevel)

def prism(name, points, depth, material, bevel=0.012):
    # points in XY; duplicate on both sides so the fork has real depth.
    verts = [(x, y, z) for z in (-depth / 2, depth / 2) for x, y in points]
    n = len(points)
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    finish(obj, material, bevel)
    obj.select_set(False)
    return obj

def cylinder(name, location, radius, depth, material, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    return finish(bpy.context.object, material, 0.006)

# Broad base and twin tapered prongs give the emitter its open-fork silhouette.
box('lower housing', (0, -0.315, 0), (0.56, 0.23, 0.30), 2, 0.035)
box('base collar', (0, -0.405, 0), (0.46, 0.09, 0.34), 6, 0.018)
for side in (-1, 1):
    p = [(0.11, -0.30), (0.31, -0.25), (0.34, -0.07),
         (0.30, 0.34), (0.26, 0.44), (0.15, 0.44), (0.17, 0.02)]
    if side < 0:
        p = [(-x, y) for x, y in reversed(p)]
    prism('fork arm', p, 0.25, 0, 0.018)
    # Raised caps and inset-looking conductors are mirrored on front and back.
    box('arm tip', (side * 0.225, 0.375, 0), (0.13, 0.13, 0.29), 1, 0.018)
    for front in (-1, 1):
        box('prong conductor', (side * 0.235, 0.16, front * 0.133),
            (0.055, 0.30, 0.025), 3, 0.008)
        box('base contact', (side * 0.19, -0.33, front * 0.158),
            (0.065, 0.085, 0.026), 5, 0.008)

# The optical channel has depth; the layered faceted lenses are visible from both sides.
box('channel spine', (0, -0.04, 0), (0.17, 0.43, 0.16), 2, 0.025)
box('channel core', (0, 0.065, 0), (0.10, 0.30, 0.19), 3, 0.015)
for front in (-1, 1):
    cylinder('lens bezel', (0, -0.205, front * 0.166), 0.137, 0.045, 6)
    cylinder('cyan optic', (0, -0.205, front * 0.195), 0.100, 0.028, 3)
    cylinder('optic center', (0, -0.205, front * 0.213), 0.055, 0.014, 4)
    box('channel glint', (0, 0.075, front * 0.101), (0.045, 0.21, 0.018), 4, 0.006)

# Join the exportable parts, apply all transforms, and explicitly triangulate.
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
bpy.ops.wm.obj_export(filepath=os.path.join(out, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Preview setup: the exported model is the only mesh in the scene.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Preview world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 5), 180, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.17
scene.render.filepath = os.path.join(out, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 85
cam.location = (1.45, 1.0, 2.6)
point_at(cam)
scene.render.filepath = os.path.join(out, 'angle.png')
bpy.ops.render.render(write_still=True)

