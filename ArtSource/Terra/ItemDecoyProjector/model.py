import bpy
import math
import os
import sys
from mathutils import Vector

# Run with Blender 5.x. The model uses Y as its vertical axis.
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    HERE = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.19, 0.30, 0.35, 1),  # blue-charcoal housing
    (0.32, 0.46, 0.49, 1),  # upper casing
    (0.48, 0.60, 0.59, 1),  # edge metal
    (0.58, 0.28, 0.21, 1),  # copper emitter
    (0.82, 0.52, 0.25, 1),  # warm brass
    (0.15, 0.69, 0.77, 1),  # spectral cyan
    (0.35, 0.86, 0.90, 1),  # spectral light
    (0.69, 0.96, 0.94, 1),  # inset glint
    (0.88, 0.69, 0.31, 1),  # indicator
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    materials.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(materials[mat])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def drum(loc, radius, depth, mat, vertices=12):
    # Blender cylinders start on Z; rotate onto the item's Y axis.
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                         location=loc, rotation=(math.pi / 2, 0, 0))
    return finish(bpy.context.object, mat)

def prism(points, depth, mat):
    # X/Y profile extruded along Z, with real thickness for the spinning view.
    n = len(points)
    verts = [(x, y, z) for z in (-depth / 2, depth / 2) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new('Projection mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Projection', mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

# Broad, stepped chassis: a recognisable physical source for the hologram.
box((0, -0.285, 0), (0.68, 0.23, 0.42), 0, 0.045)
box((0, -0.405, 0), (0.62, 0.055, 0.38), 2, 0.025)
box((0, -0.155, 0), (0.59, 0.065, 0.36), 1, 0.025)
# Contrasting end cheeks and visible indicator bars on both faces.
for side in (-1, 1):
    box((side * 0.294, -0.277, 0), (0.075, 0.15, 0.43), 2, 0.018)
    for face in (-1, 1):
        box((side * 0.23, -0.287, face * 0.216), (0.085, 0.055, 0.028), 8, 0.008)
# Layered, faceted projection emitter, equally legible from all azimuths.
drum((0, -0.105, 0), 0.225, 0.075, 3)
drum((0, -0.055, 0), 0.193, 0.040, 4)
drum((0, -0.026, 0), 0.151, 0.026, 5)
# A thick, faceted arrow/ghost-like spectral copy, echoing the original prism.
prism([(-0.24, 0.115), (-0.14, 0.285), (0, 0.445),
       (0.14, 0.285), (0.24, 0.115), (0.105, 0.155),
       (0, 0.075), (-0.105, 0.155)], 0.115, 5)
# Raised inset facets on front and rear give the projection an unmistakable core.
# These are solid and opaque, as required by the game's material pipeline.
for z in (-0.063, 0.063):
    obj = prism([(-0.10, 0.205), (0, 0.365), (0.10, 0.205),
                 (0, 0.15)], 0.016, 6)
    obj.location.z = z
    obj = prism([(-0.033, 0.235), (0, 0.30), (0.033, 0.235),
                 (0, 0.205)], 0.008, 7)
    obj.location.z = z + (0.013 if z > 0 else -0.013)

# Join and bake all transforms before exporting; triangulate for predictable output.
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
assert 300 <= len(model.data.polygons) <= 2000, 'Triangle budget exceeded'
bpy.ops.wm.obj_export(filepath=os.path.join(HERE, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 5), 180, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.15
scene.render.filepath = os.path.join(HERE, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.8, 0.55, 1.45)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(HERE, 'angle.png')
bpy.ops.render.render(write_still=True)

