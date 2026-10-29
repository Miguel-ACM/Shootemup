import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as the vertical axis and sees the front along +Z.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [
    (0.35, 0.46, 0.29, 1),  # olive bottle
    (0.44, 0.56, 0.35, 1),  # shoulder facets
    (0.29, 0.39, 0.25, 1),  # base and neck
    (0.82, 0.64, 0.29, 1),  # golden stopper
    (0.96, 0.84, 0.57, 1),  # bone
    (0.11, 0.17, 0.13, 1),  # eye sockets
    (0.61, 0.72, 0.51, 1),  # glass glint
    (0.55, 0.42, 0.20, 1),  # stopper band
]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.78
    mats.append(m)

parts = []
def mesh_object(name, verts, faces, mat, indices=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for m in mats:
        mesh.materials.append(m)
    for poly in mesh.polygons:
        poly.material_index = mat if indices is None else indices[poly.index]
        poly.use_smooth = False
    parts.append(ob)
    return ob

def bevel_box(name, loc, scale, material, bevel=0.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[material])
    mod = ob.modifiers.new('Broad flat bevels', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

def ellipsoid(name, loc, scale, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mats[material])
    parts.append(ob)
    return ob

def disc(name, loc, radius, depth, material, vertices=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    parts.append(ob)
    return ob

def rod(name, a, b, radius, material):
    a, b = Vector(a), Vector(b)
    direction = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=direction.length, location=(a + b) / 2)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_euler = direction.to_track_quat('Z', 'Y').to_euler()
    ob.data.materials.append(mats[material])
    parts.append(ob)
    return ob

# Eight-sided, flattened bottle: broad foot and sloping shoulders into a narrow neck.
def outline(w, d, cut=0.28):
    return [(-w*(1-cut), d), (w*(1-cut), d), (w, d*(1-cut)), (w, -d*(1-cut)),
            (w*(1-cut), -d), (-w*(1-cut), -d), (-w, -d*(1-cut)), (-w, d*(1-cut))]
rings = [(-.42,.265,.145), (-.385,.31,.18), (-.22,.33,.19),
         (-.16,.315,.185), (.205,.115,.115), (.34,.115,.115), (.365,.125,.125)]
verts = [(x, y, z) for y,w,d in rings for x,z in outline(w,d)]
faces = [tuple(reversed(range(8)))]
inds = [2]
for j in range(len(rings)-1):
    for k in range(8):
        faces.append((j*8+k, j*8+(k+1)%8, (j+1)*8+(k+1)%8, (j+1)*8+k))
        inds.append(2 if j == 0 else (1 if j == 3 else 0))
faces.append(tuple((len(rings)-1)*8+k for k in range(8)))
inds.append(2)
mesh_object('Faceted bottle', verts, faces, 0, inds)
bevel_box('Stopper', (0,.413,0), (.255,.134,.27), 3, .025)
bevel_box('Stopper lower band', (0,.358,0), (.263,.035,.278), 7, .008)

# A bold skull on both broad faces, so it remains legible during the spin.
for side in (1, -1):
    z = side * .192
    # Crossed bones are short and thick enough to survive icon scale.
    for slope in (-1, 1):
        a = (-.155, -.25 if slope == 1 else -.035, z + side*.008)
        b = (.155, -.035 if slope == 1 else -.25, z + side*.008)
        rod('Crossbone', a, b, .022, 4)
        for p in (a, b):
            ellipsoid('Bone end', p, (.032,.032,.018), 4)
    ellipsoid('Skull', (0,-.115,z + side*.035), (.122,.112,.038), 4)
    bevel_box('Jaw', (0,-.204,z + side*.038), (.135,.065,.045), 4, .012)
    for x in (-.045,.045):
        disc('Eye socket', (x,-.105,z + side*.074), .027, .012, 5, 8)
    disc('Nasal cavity', (0,-.157,z + side*.074), .012, .012, 5, 3)
    for x in (-.035, 0, .035):
        bevel_box('Tooth groove', (x,-.208,z + side*.063), (.009,.029,.009), 5, .002)
# Thick, subtle glints set on the outer shoulder facets.
for side in (1,-1):
    rod('Glass glint', (-.195,.012,side*.158), (-.154,.088,side*.142), .012, 6)

# Join and bake every transform before exporting only the model.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
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
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target=(0,0,0)):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    aim(ob)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 45, 5)
camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location = (0,0,4)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.30
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camera.location = (0.85,0.60,1.43)
aim(camera, (0,0,0))
camera_data.type = 'PERSP'
camera_data.lens = 50
camera_data.sensor_width = 36
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

