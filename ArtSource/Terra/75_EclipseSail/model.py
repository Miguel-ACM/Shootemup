import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Blender 5.x: run with blender --background --python model.py -- <script-directory>
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    OUT = os.path.abspath(args[0]) if args else os.getcwd()
    if os.path.isfile(OUT) or OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)

colors = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.105,0.15,0.20), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.48,0.36,0.18), (0.53,0.61,0.55),
    (0.66,0.70,0.71),
]
# Space atlas tile_07 is the solar blue tile; its grid is represented by geometry.
colors[7] = (0.16, 0.25, 0.43)
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.68
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(mat)

body = []
def mesh_obj(name, verts, faces, material, indices=None):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    if indices is None:
        ob.data.materials.append(mats[material])
    else:
        for m in mats:
            ob.data.materials.append(m)
        for p, idx in zip(ob.data.polygons, indices):
            p.material_index = idx
    body.append(ob)
    return ob

def box(name, center, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    if bevel:
        mod = ob.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(ob)
    return ob

def annulus(name, cx, cy, radii, zs, count, material_indices):
    # Four concentric profiles give the ring a sloping upper bevel and real thickness.
    verts = []
    for radius, z in zip(radii, zs):
        for i in range(count):
            a = 2 * math.pi * i / count + math.pi / count
            verts.append((cx + radius * math.cos(a), cy + radius * math.sin(a), z))
    faces, inds = [], []
    for k in range(len(radii)):
        j = (k + 1) % len(radii)
        for i in range(count):
            n = (i + 1) % count
            faces.append((k*count+i, k*count+n, j*count+n, j*count+i))
            inds.append(material_indices[k])
    return mesh_obj(name, verts, faces, 0, inds)

def cylinder(name, loc, radius, depth, mat, vertices=12, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[mat])
    body.append(ob)
    return ob

def barrel_mouth(x):
    # The front face of this open tube lies exactly at the gameplay muzzle point.
    n = 10
    verts = []
    for y, radius in [(-0.61, 0.083), (-0.61, 0.047), (-0.565, 0.047), (-0.43, 0.083)]:
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((x + radius*math.cos(a), y, 0.30 + radius*math.sin(a)))
    faces, ids = [], []
    for k, material in [(0,13), (1,11), (2,6), (3,6)]:
        j = (k+1) % 4
        for i in range(n):
            q = (i+1) % n
            faces.append((k*n+i, k*n+q, j*n+q, j*n+i))
            ids.append(material)
    mesh_obj('Muzzle lip', verts, faces, 0, ids)
    # Dark recessed bore stays visible from the front and from above at game scale.
    cylinder('Bore', (x,-0.558,0.30), 0.046, 0.008, 11, 10, (math.pi/2,0,0))

# The octagonal eclipse halo is raised and faceted, with a dark inner wall.
annulus('Eclipse halo', 0, 0, [0.56,0.59,0.86,0.92,0.92,0.56],
        [0.05,0.20,0.20,0.12,-0.10,-0.10], 16, [13,15,15,13,6,6])
# Thin gold foil along the outer crown, leaving the ivory halo broad and readable.
annulus('Outer foil', 0, 0, [0.858,0.876,0.90,0.882],
        [0.205,0.205,0.174,0.174], 16, [13,13,13,13])

# Cross-spar and two fixed, framed solar sails. Their top faces are offset to
# ensure the grid remains visible without relying on textures or z fighting.
box('Cross spar', (0,0.26,0.015), (2.04,0.105,0.13), 13, 0.025)
for s in (-1, 1):
    x = s * 0.95
    box('Panel frame', (x,0.26,0.075), (0.46,0.92,0.11), 13, 0.025)
    box('Solar cell surface', (x,0.26,0.137), (0.38,0.82,0.025), 7, 0.012)
    # Two broad divisions, not a row of tiny decorative buttons.
    for y in (0.045, 0.475):
        box('Panel division', (x,y,0.153), (0.38,0.012,0.009), 4)
    box('Panel center seam', (x,0.26,0.153), (0.012,0.82,0.009), 4)
    box('Weapon mounting', (s*0.52,-0.37,0.18), (0.22,0.31,0.19), 6, 0.035)
    barrel_mouth(s*0.52)

# Central satellite bus and raised eclipse sensor: stepped volumes create a
# clear height hierarchy while preserving the open center of the circular sail.
box('Rear satellite bus', (0,0.54,0.07), (0.27,0.48,0.32), 15, 0.045)
box('Bus gold inset', (0,0.55,0.244), (0.18,0.26,0.035), 13, 0.012)
cylinder('Sensor collar', (0,0.40,0.278), 0.18, 0.075, 6, 12)
cylinder('Sensor bezel', (0,0.40,0.324), 0.132, 0.036, 13, 12)
cylinder('Eclipse lens', (0,0.40,0.346), 0.092, 0.014, 11, 12)
cylinder('Sensor glint', (0,0.40,0.357), 0.035, 0.009, 9, 10)

# All pieces are static: the sails are structural panels, not hinged fins.
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Export triangles explicitly, for predictable in-game counts and flat facets.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert 300 <= len(model.data.polygons) <= 2500, 'Triangle budget exceeded: %d' % len(model.data.polygons)
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    point_at(ob, target)
    return ob

light('Sun upper left', 'SUN', (-3,-4,6), 2.0, (0,0,0)).data.angle = math.radians(18)
light('Soft fill', 'AREA', (4,2,5), 180, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
# Camera local Y points to +Y, so screen up matches the requested coordinate system.
cam.rotation_euler = (0,0,0)
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.80
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (3.5,-4.8,4.4)
point_at(cam, (0,0,0.05))
camdata.type = 'PERSP'
camdata.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
