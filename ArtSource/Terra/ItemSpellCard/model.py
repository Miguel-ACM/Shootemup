import bpy
import math
import os
import sys
from mathutils import Vector

# This is an ofuda: a small, thick-edged paper charm with a seal on each face.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    ('tile_00', (0.94, 0.83, 0.60, 1)),  # warm paper edge
    ('tile_01', (1.00, 0.94, 0.78, 1)),  # ivory paper
    ('tile_02', (0.77, 0.12, 0.10, 1)),  # vermilion border
    ('tile_03', (0.96, 0.27, 0.20, 1)),  # seal red
    ('tile_04', (1.00, 0.72, 0.25, 1)),  # seal gold
    ('tile_05', (0.27, 0.19, 0.19, 1)),  # ink
]
mats = []
for name, color in palette:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.82
    mats.append(mat)

parts = []
def clipped_plate(name, width, height, clip, z0, z1, material):
    x, y = width / 2, height / 2
    outline = [(-x + clip, -y), (x - clip, -y), (x, -y + clip),
               (x, y - clip), (x - clip, y), (-x + clip, y),
               (-x, y - clip), (-x, -y + clip)]
    verts = [(a, b, z) for z in (z0, z1) for a, b in outline]
    faces = [tuple(reversed(range(8))), tuple(range(8, 16))]
    faces += [(i, (i + 1) % 8, (i + 1) % 8 + 8, i + 8) for i in range(8)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def box(name, location, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Crisp bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def disk(name, x, y, z, radius, depth, material, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(x, y, z))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

# The red inset is a second octagonal layer; the ivory inner panel leaves a
# bold, uninterrupted frame visible from both sides and in the HUD.
clipped_plate('Paper stock', .50, .80, .045, -.032, .032, 0)
for side in (-1, 1):
    def layer(name, w, h, clip, inner, outer, mat):
        a, b = sorted((side * inner, side * outer))
        clipped_plate(name, w, h, clip, a, b, mat)
    layer('Vermilion printed frame', .468, .765, .038, .032, .038, 2)
    layer('Ivory inset', .386, .676, .027, .038, .044, 1)
    # A bold circular seal with a pale center and four-point gold spark.
    disk('Seal', 0, .105, side * .050, .126, .012, 3)
    disk('Seal inset', 0, .105, side * .058, .085, .006, 1)
    disk('Golden crest', 0, .105, side * .064, .057, .006, 4, 4)
    disk('Crest center', 0, .105, side * .069, .022, .005, 3, 8)
    # Graphic, abstract inscription: broad strokes survive at icon size.
    for name, x, y, w, h in [
        ('Ink stem', 0, -.190, .032, .183),
        ('Ink upper stroke', 0, -.145, .145, .030),
        ('Ink lower stroke', 0, -.237, .108, .030),
        ('Ink top mark', 0, .292, .105, .024),
    ]:
        box(name, (x, y, side * .049), (w, h, .010), 5, .004)
    # A short gold bar reinforces the layered, ceremonial look.
    box('Gold header', (0, .335, side * .047), (.21, .022, .008), 4, .003)

# Join geometry and bake transforms. All polygons intentionally remain flat shaded.
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
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
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
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05, .07, .09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1

# Point a light's local -Z axis toward the card; Y is the model's up axis.
def aim(obj, target=(0, 0, 0)):
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
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 4, 5), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 4), 250, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
scene.view_settings.view_transform = 'Standard'

cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.15
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (0.82, 0.45, 1.43)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 55
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

