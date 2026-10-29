import bpy
import math
import os
import sys
from mathutils import Vector

# The pickup uses game coordinates: Y is vertical and Z is the front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)

palette = [
    (0.48, 0.57, 0.58, 1),  # main steel
    (0.70, 0.77, 0.75, 1),  # light steel
    (0.25, 0.35, 0.39, 1),  # blue steel
    (0.95, 0.38, 0.12, 1),  # orange side armour
    (1.00, 0.61, 0.22, 1),  # orange bevel accents
    (0.16, 0.83, 0.91, 1),  # cyan center
    (0.09, 0.15, 0.18, 1),  # small muzzle recesses
]
mats = []
for i, colour in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = colour
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = colour
    p.inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Broad machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, mat)

def cylinder(name, x, y, z, radius, depth, mat, vertices=8):
    # Blender cylinders start along Z; turn their axis to Y.
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       end_fill_type='NGON', location=(x, y, z),
                                       rotation=(math.pi / 2, 0, 0))
    o = bpy.context.object
    o.name = name
    return finish(o, mat)

def hull(name, outline, depth, mat):
    n = len(outline)
    verts = [(x, y, z) for z in (-depth/2, depth/2) for x, y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    return finish(o, mat)

# Low, broad receiver with a clipped silhouette and an armored rear plate.
hull('Six sided receiver', [(-.255,-.405),(.255,-.405),(.31,-.12),(.245,.025),(-.245,.025),(-.31,-.12)], .26, 0)
box('Rear receiver casing', (0,-.19,-.145), (.43,.34,.11), 2, .035)
box('Lower mounting lug', (0,-.405,0), (.25,.09,.32), 1, .025)

for side in (-1, 1):
    x = side * .175
    # Broad octagonal barrels read as a paired cannon from every side.
    cylinder('Barrel', x, .145, 0, .072, .48, 2)
    cylinder('Barrel sleeve', x, .075, 0, .086, .30, 0)
    cylinder('Upper barrel', x, .30, 0, .068, .20, 1)
    cylinder('Muzzle collar', x, .383, 0, .092, .115, 0)
    cylinder('Muzzle lip', x, .442, 0, .080, .025, 1)
    cylinder('Muzzle bore', x, .456, 0, .043, .004, 6)
    # Two chunky cooling rings instead of small fragile fins.
    for y in (-.025, .095):
        cylinder('Cooling ring', x, y, 0, .096, .035, 1)
    box('Orange side pod', (side*.315,-.235,0), (.13,.265,.29), 3, .026)
    box('Pod upper edge', (side*.315,-.12,0), (.14,.055,.30), 4, .015)
    # Both faces of the mount stay finished throughout the spin.
    for z in (-.151, .151):
        box('Pod inset', (side*.315,-.245,z), (.075,.12,.016), 0, .008)

for z in (-.142, .142):
    box('Center raised plate', (0,-.205,z), (.155,.255,.035), 2, .02)
    box('Cyan status panel', (0,-.17,z + (0.021 if z > 0 else -0.021)), (.095,.14,.018), 5, .012)
    box('Plate foot', (0,-.33,z + (0.021 if z > 0 else -0.021)), (.12,.04,.02), 1, .008)

# Join, apply transforms, triangulate for predictable game/export topology.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for p in model.data.polygons:
    p.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Preview scene: cameras use local -Z as viewing direction and local Y as up.
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
scene.view_settings.view_transform = 'AgX'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, target)

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 180, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.20
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.85, 0.57, 1.4)
point_at(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

