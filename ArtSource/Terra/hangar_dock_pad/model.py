import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve the output directory when invoked as a file or via Blender's -- argument.
try:
    OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUTPUT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

def material(index, color, emission=0.0):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*color, 1.0)
    shader.inputs['Roughness'].default_value = 0.75
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1.0)
        shader.inputs['Emission Strength'].default_value = emission
    return mat

hull = material(0, (0.075, 0.105, 0.115))
deck = material(1, (0.13, 0.17, 0.18))
rim = material(2, (0.21, 0.26, 0.26))
edge = material(3, (0.055, 0.075, 0.085))
brass = material(4, (0.32, 0.27, 0.15))
amber = material(5, (0.95, 0.62, 0.25), 1.5)
cyan = material(6, (0.25, 0.75, 0.85), 1.8)
podmat = material(7, (0.11, 0.15, 0.16))
parts = []

def finish(obj, mat, bevel=0.0):
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Machined bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def cylinder(name, radius, depth, z, mat, vertices=8, x=0, y=0, bevel=0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=(x, y, z), rotation=(0, 0, math.pi / 8 if vertices == 8 else 0))
    obj = bpy.context.object
    obj.name = name
    return finish(obj, mat, bevel)

def ring(name, outer, inner, z_bottom, z_top, mat, segments=8, x=0, y=0, rotation=math.pi/8, bevel=0):
    verts = []
    for z, radius in ((z_bottom, outer), (z_bottom, inner), (z_top, outer), (z_top, inner)):
        verts.extend((x + radius * math.cos(rotation + 2*math.pi*i/segments), y + radius * math.sin(rotation + 2*math.pi*i/segments), z) for i in range(segments))
    faces = []
    for i in range(segments):
        j = (i + 1) % segments
        faces.extend(((i,j,2*segments+j,2*segments+i), (segments+j,segments+i,3*segments+i,3*segments+j), (2*segments+i,2*segments+j,3*segments+j,3*segments+i), (j,i,segments+i,segments+j)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat, bevel)

# A broad, unobstructed landing surface, framed by an independently raised rim.
cylinder('Lower octagonal hull', 0.75, 0.14, -0.095, hull, bevel=0.018)
cylinder('Flat landing deck', 0.655, 0.025, -0.0125, deck, bevel=0.006)
ring('Recessed perimeter', 0.735, 0.63, -0.045, 0.004, edge)
ring('Raised bevelled rim', 0.75, 0.665, -0.012, 0.048, rim, bevel=0.012)
ring('Rim inset', 0.704, 0.691, 0.045, 0.049, brass)

# Eight discrete lamp lenses, each backed by a darker socket.
for i in range(8):
    a = math.pi / 8 + i * math.tau / 8
    x, y = 0.708 * math.cos(a), 0.708 * math.sin(a)
    cylinder('Lamp socket', 0.035, 0.009, 0.046, edge, 8, x, y)
    cylinder('Amber lamp', 0.022, 0.009, 0.052, amber, 8, x, y, 0.002)

# Four pods peek beyond the diagonal edges; their cyan annuli face downward.
for sx, sy in ((1,1), (-1,1), (-1,-1), (1,-1)):
    x, y = sx * 0.51, sy * 0.51
    cylinder('Thruster housing', 0.155, 0.105, -0.112, podmat, 12, x, y, 0.012)
    cylinder('Thruster lower cap', 0.128, 0.025, -0.173, edge, 12, x, y, 0.004)
    ring('Downward cyan thruster ring', 0.116, 0.088, -0.191, -0.181, cyan, 12, x, y, 0)
    cylinder('Thruster aperture', 0.082, 0.008, -0.185, hull, 12, x, y)
    # A thin exposed collar makes the hovering mechanism legible from overhead.
    ring('Exposed cyan collar', 0.145, 0.131, -0.106, -0.097, cyan, 12, x, y, 0)

# Consolidate the asset; its origin and coordinates remain in the requested frame.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.wm.obj_export(filepath=os.path.join(OUTPUT_DIR, 'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 32
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.06, 0.08, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

bpy.ops.object.light_add(type='SUN', location=(-3, 4, 6))
sun = bpy.context.object
sun.name = 'Upper left sun'
sun.data.energy = 2.2
sun.rotation_euler = (Vector((0, 0, 0)) - sun.location).to_track_quat('-Z', 'Y').to_euler()
sun.data.angle = math.radians(12)

bpy.ops.object.camera_add(location=(0, 0, 5))
cam = bpy.context.object
cam.name = 'Render Camera'
cam.data.type = 'ORTHO'
cam.data.ortho_scale = 1.95
cam.rotation_euler = (0, 0, 0)
scene.camera = cam
scene.render.filepath = os.path.join(OUTPUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

cam.location = (2.7, -3.3, 3.2)
cam.rotation_euler = (Vector((0, 0, -0.06)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
cam.data.ortho_scale = 2.15
scene.render.filepath = os.path.join(OUTPUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)
