import bpy
import math
import os
import sys
from mathutils import Vector

# Output beside this script, including when Blender is launched with --python.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(data):
        if block.users == 0:
            data.remove(block)

def material(name, color):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.8
    return m

steel = material('steel_dark', (0.055, 0.078, 0.082))
edge = material('steel_edge', (0.12, 0.16, 0.16))
plate = material('steel_plate', (0.085, 0.115, 0.12))
recess = material('recess', (0.03, 0.045, 0.05))
lightsteel = material('steel_worn', (0.18, 0.22, 0.22))
rust = material('rust', (0.22, 0.13, 0.09))
brass = material('brass', (0.27, 0.23, 0.13))
amber = material('glow_amber', (0.95, 0.62, 0.25))
cyan = material('glow_cyan', (0.25, 0.75, 0.85))
parts = []

def finish(obj, mat):
    obj.data.materials.append(mat)
    parts.append(obj)
    return obj

def box(name, loc, size, mat, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, mat)

def cylinder(name, loc, radius, depth, mat, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    o = bpy.context.object
    o.name = name
    return finish(o, mat)

def ring(name, x, y, inner, outer, z, depth, mat, sides=8):
    verts = []
    for zz, rad in ((z, outer), (z, inner), (z + depth, outer), (z + depth, inner)):
        for i in range(sides):
            a = 2 * math.pi * i / sides + math.pi / sides
            verts.append((x + rad * math.cos(a), y + rad * math.sin(a), zz))
    faces = []
    for i in range(sides):
        j = (i + 1) % sides
        faces.extend(((i,j,2*sides+j,2*sides+i), (sides+j,sides+i,3*sides+i,3*sides+j),
                      (2*sides+i,2*sides+j,3*sides+j,3*sides+i), (j,i,sides+i,sides+j)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

def beam_between(name, a, b, width, height, mat):
    midpoint = (Vector(a) + Vector(b)) / 2
    length = (Vector(b) - Vector(a)).length
    o = box(name, midpoint, (length, width, height), mat, 0.015)
    o.rotation_euler.z = math.atan2(b[1] - a[1], b[0] - a[0])
    return o

# Docking platforms are shallow, with empty, dark top surfaces.
for y in (4.2, 1.4, -1.4, -4.2):
    box('Dock wall anchor', (-4.82, y, 0.13), (0.34, 1.18, 0.26), steel, 0.045)
    box('Dock arm', (-4.38, y, 0.075), (0.95, 0.25, 0.15), edge, 0.025)
    for dy in (-0.32, 0.32):
        beam_between('Dock fork', (-4.60, y + dy, 0.07), (-4.02, y + dy * 0.65, 0.07), 0.10, 0.13, plate)
    cylinder('Octagonal cradle', (-3.4, y, 0.055), 0.80, 0.11, steel, 8)
    ring('Cradle rim', -3.4, y, 0.67, 0.78, 0.108, 0.012, edge)
    cylinder('Empty landing surface', (-3.4, y, 0.112), 0.66, 0.012, recess, 8)
    for i in range(12):
        a = 2 * math.pi * i / 12
        cylinder('Amber rim lamp', (-3.4 + 0.72 * math.cos(a), y + 0.72 * math.sin(a), 0.126), 0.022, 0.012, amber, 8)

# Narrow rack hugs the right wall; the item surfaces are deliberately clear.
box('Shop rack spine', (4.72, 0.6, 0.18), (0.43, 7.6, 0.36), steel, 0.05)
for y in (-3.05, -0.8, 2.0, 4.25):
    box('Rack upright', (4.65, y, 0.27), (0.53, 0.13, 0.54), edge, 0.025)
for y in (3.4, 0.6, -2.2):
    box('Plinth mount', (4.10, y, 0.12), (1.0, 0.36, 0.24), plate, 0.04)
    cylinder('Display pedestal', (3.4, y, 0.15), 0.56, 0.30, steel, 8)
    ring('Cyan display rim', 3.4, y, 0.47, 0.51, 0.301, 0.012, cyan)
    cylinder('Empty display top', (3.4, y, 0.303), 0.46, 0.012, recess, 8)
# Wall equipment fits between the display positions.
box('Vending cabinet', (4.53, 1.99, 0.28), (0.64, 0.73, 0.56), plate, 0.045)
box('Cabinet inset', (4.53, 1.99, 0.566), (0.47, 0.53, 0.015), recess, 0.015)
box('Cabinet latch', (4.42, 1.99, 0.58), (0.035, 0.17, 0.025), brass, 0.005)
for y in (-0.78, 4.02):
    box('Storage shelf', (4.45, y, 0.13), (0.78, 0.56, 0.26), steel, 0.03)
    box('Small supply case', (4.42, y, 0.30), (0.48, 0.37, 0.12), edge, 0.025)

# Open blast doors: both leaves are retracted into the wall pockets.
for side in (-1, 1):
    x = side * 3.82
    box('Door pocket', (side * 4.30, 7.85, 0.16), (1.36, 2.05, 0.32), steel, 0.06)
    box('Retracted blast leaf', (x, 7.84, 0.21), (1.42, 1.78, 0.38), plate, 0.07)
    box('Door armor inset', (x, 7.84, 0.408), (1.14, 1.49, 0.025), steel, 0.035)
    for y in (7.16, 7.50, 7.84, 8.18, 8.52):
        box('Interlocking door tooth', (side * 3.04, y, 0.22), (0.23, 0.19, 0.30), edge, 0.025)
    box('Door sill side', (side * 3.55, 6.90, 0.09), (1.17, 0.17, 0.18), edge, 0.02)
    cylinder('Beacon mount', (side * 2.96, 6.78, 0.13), 0.13, 0.26, steel, 8)
    cylinder('Warning beacon', (side * 2.96, 6.78, 0.27), 0.065, 0.04, amber, 8)
# The unfilled central opening reveals the existing tunnel/shaft underneath.
for y in (6.5, 4.8, 3.1, 1.4, -0.3, -2.0, -3.7, -5.4, -6.0):
    cylinder('Inset guide dot', (0, y, 0.012), 0.035, 0.024, amber, 8)

# Workshop stays below the clear approach at (0, -5.8).
box('Workshop ledge', (0, -8.52, 0.12), (9.5, 0.68, 0.24), steel, 0.045)
box('Tool bench', (-2.55, -7.80, 0.22), (2.15, 0.65, 0.44), plate, 0.045)
box('Bench top inset', (-2.55, -7.80, 0.448), (1.94, 0.48, 0.025), recess, 0.015)
box('Tool tray', (-2.95, -7.79, 0.48), (0.48, 0.25, 0.04), edge, 0.01)
beam_between('Spanner handle', (-2.85, -7.80, 0.51), (-2.62, -7.64, 0.51), 0.035, 0.025, brass)
box('Bench vise', (-1.90, -7.81, 0.51), (0.28, 0.24, 0.12), edge, 0.025)
for x, y, size in ((1.45, -8.0, 0.64), (2.12, -8.15, 0.52)):
    box('Workshop crate', (x, y, 0.23), (size, size * 0.8, 0.46), plate, 0.045)
    box('Crate top panel', (x, y, 0.468), (size * 0.75, size * 0.58, 0.016), steel, 0.012)
    box('Crate strap', (x, y, 0.48), (0.065, size * 0.78, 0.018), brass, 0.005)
for x, y in ((3.0, -7.96), (3.55, -8.25)):
    cylinder('Workshop barrel', (x, y, 0.25), 0.24, 0.50, steel, 12)
    ring('Barrel rim', x, y, 0.18, 0.235, 0.50, 0.025, edge, 12)
    cylinder('Barrel lid', (x, y, 0.50), 0.18, 0.018, plate, 12)
# Crane boom and a small open hook silhouette.
box('Crane base', (-4.38, -8.15, 0.24), (0.58, 0.82, 0.48), edge, 0.04)
beam_between('Crane boom', (-4.30, -8.10, 0.48), (-3.80, -7.30, 0.48), 0.17, 0.16, steel)
cylinder('Crane cable', (-3.80, -7.30, 0.37), 0.035, 0.22, brass, 8)
ring('Crane hook', -3.80, -7.30, 0.10, 0.16, 0.25, 0.06, rust, 8)

# Consolidate all geometry, retaining material slots and flat face normals.
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

# Orthographic framing compensates for the small difference in output aspect ratio.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 32
scene.render.resolution_x = 450
scene.render.resolution_y = 800
scene.render.resolution_percentage = 100
scene.render.pixel_aspect_x = (10.0 / 17.8) / (450.0 / 800.0)
scene.render.pixel_aspect_y = 1.0
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.film_transparent = True
scene.world = bpy.data.worlds.new('Tunnel ambient') if scene.world is None else scene.world
scene.world.color = (0.06, 0.08, 0.09)
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.06, 0.08, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
bpy.ops.object.camera_add(location=(0, 0, 30))
cam = bpy.context.object
cam.name = 'Top camera'
cam.rotation_euler = (0, 0, 0)
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 17.8
scene.camera = cam

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.light_add(type='SUN', location=(-6, 8, 12))
sun = bpy.context.object
sun.name = 'Upper left sun'
sun.data.energy = 1.6
sun.data.angle = math.radians(18)
point_at(sun, (0, 0, 0))
bpy.ops.object.light_add(type='AREA', location=(4, -2, 10))
fill = bpy.context.object
fill.name = 'Soft fill'
fill.data.energy = 550
fill.data.shape = 'DISK'
fill.data.size = 12
point_at(fill, (0, 0, 0))

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

