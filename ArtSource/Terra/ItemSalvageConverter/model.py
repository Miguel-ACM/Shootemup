import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as the vertical axis and looks toward the front (+Z).
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials:
    bpy.data.materials.remove(data)

palette = [
    (0.95, 0.59, 0.09, 1),  # warm golden hopper
    (1.00, 0.76, 0.20, 1),  # bright coin gold
    (0.67, 0.34, 0.08, 1),  # bronze structure
    (0.95, 0.32, 0.11, 1),  # missile nose
    (0.31, 0.48, 0.52, 1),  # blue steel
    (0.60, 0.72, 0.69, 1),  # pale steel
    (0.27, 0.32, 0.30, 1),  # recessed opening
    (1.00, 0.88, 0.40, 1),  # coin face accents
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.78
    materials.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(materials[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

def box(name, center, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def cylinder(name, center, radius, depth, mat, axis='Z', vertices=12):
    rot = (0, 0, 0) if axis == 'Z' else (math.pi / 2, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=center, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, mat)

def mesh(name, vertices, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

# A deep, genuinely open tapered hopper, with a broad solid rim. The inner
# walls descend to a dark floor, so its opening reads from above and behind.
def rect_loop(w, y, d):
    return [(-w, y, -d), (w, y, -d), (w, y, d), (-w, y, d)]
verts = (rect_loop(.39, .23, .22) + rect_loop(.19, -.10, .14) +
         rect_loop(.32, .23, .15) + rect_loop(.14, -.045, .09))
faces = []
for i in range(4):
    j = (i + 1) % 4
    faces.extend([(i, j, 4+j, 4+i), (i, 8+i, 8+j, j),
                  (8+i, 12+i, 12+j, 8+j)])
faces.extend([(4, 5, 6, 7), (12, 15, 14, 13)])
hopper = mesh('Hollow tapered hopper', verts, faces, 0)
hopper.data.materials.append(materials[2])
hopper.data.materials.append(materials[6])
for i, poly in enumerate(hopper.data.polygons):
    if i < 12 and i % 3 == 1:
        poly.material_index = 1
    elif i < 12 and i % 3 == 2 or i == 13:
        poly.material_index = 2

# Raised rim on all four sides and sturdy shoulder lugs.
for z in (-.205, .205):
    box('Hopper lip', (0, .245, z), (.82, .055, .075), 2, .014)
for x in (-.375, .375):
    box('Hopper side lip', (x, .245, 0), (.075, .055, .38), 2, .014)
    box('Side hinge', (x, .035, 0), (.10, .13, .29), 4, .018)
    cylinder('Hinge cap', (x * 1.14, .035, 0), .055, .025, 5, 'Z', 10)

# Three thick upright coins emerge from the hopper, visible from both sides.
for x, y, z, r in [(-.23, .265, -.035, .108), (0, .32, .005, .115), (.23, .265, -.035, .108)]:
    cylinder('Coin edge', (x, y, z), r, .065, 2, vertices=12)
    for side in (-1, 1):
        cylinder('Coin face', (x, y, z + side*.035), r*.84, .012, 1, vertices=12)
        box('Coin stamp', (x, y, z + side*.043), (.095, .026, .012), 7, .005)

# Compact converter block and downward-pointing missile chute.
box('Converter body', (0, -.16, 0), (.40, .19, .31), 4, .045)
for z in (-.165, .165):
    box('Converter front and rear band', (0, -.16, z), (.29, .10, .035), 5, .012)
box('Missile cradle', (.105, -.275, 0), (.24, .22, .25), 2, .035)
cylinder('Missile body', (.105, -.29, 0), .085, .25, 5, 'Y', 10)
# Cone primitives are Z-aligned: rotate so their tip points down along Y.
bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=.085, radius2=0, depth=.16,
                                location=(.105, -.455, 0), rotation=(math.pi/2, 0, 0))
finish(bpy.context.object, 3)
# Small thick fins, duplicated front and rear, make the projectile legible in a spin.
for z in (-.075, .075):
    box('Missile fin', (.105, -.34, z), (.21, .11, .06), 3, .012)

# Combine the geometry and bake transforms; export only this mesh.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = hopper
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False

try:
    output_dir = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    output_dir = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(output_dir):
        output_dir = os.path.dirname(output_dir)
os.makedirs(output_dir, exist_ok=True)
bpy.ops.wm.obj_export(filepath=os.path.join(output_dir, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05, .07, .09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
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
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 4), 2.0)
light('Soft fill', 'AREA', (3, 1, 4), 80, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.18
scene.render.filepath = os.path.join(output_dir, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.90, 0.58, 1.65)
aim(cam, (0, -.02, 0))
scene.render.filepath = os.path.join(output_dir, 'angle.png')
bpy.ops.render.render(write_still=True)

