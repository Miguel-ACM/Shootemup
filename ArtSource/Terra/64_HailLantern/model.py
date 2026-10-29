import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Blender may run this file through --python or execute it from a text block.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    candidate = os.path.abspath(args[args.index('--') + 1])
    OUT = candidate if os.path.isdir(candidate) else os.path.dirname(candidate)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.00, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, rgb in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.76
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(mat)

body = []
def mesh(name, verts, faces, mat, indices=None):
    data = bpy.data.meshes.new(name + '_mesh')
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    if indices is None:
        obj.data.materials.append(mats[mat])
    else:
        for m in mats:
            obj.data.materials.append(m)
        for p, index in zip(obj.data.polygons, indices):
            p.material_index = index
    body.append(obj)
    return obj

def rings(name, levels, sides, material, center=(0, 0), phase=math.pi/8):
    # Consecutive horizontal rings form a faceted, genuinely volumetric shell.
    verts = [(center[0] + rx * math.cos(phase + 2*math.pi*i/sides),
              center[1] + ry * math.sin(phase + 2*math.pi*i/sides), z)
             for z, rx, ry in levels for i in range(sides)]
    faces = [tuple(reversed(range(sides)))]
    for j in range(len(levels)-1):
        for i in range(sides):
            a = j*sides+i
            b = j*sides+(i+1)%sides
            faces.append((a, b, b+sides, a+sides))
    faces.append(tuple((len(levels)-1)*sides+i for i in range(sides)))
    return mesh(name, verts, faces, material)

def plate(name, xy, bottom, top, material):
    n = len(xy)
    verts = [(x, y, bottom) for x, y in xy] + [(x, y, top) for x, y in xy]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, verts, faces, material)

def box(name, location, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Cut edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(obj)
    return obj

def tube_y(name, x, y_front, z, radius, inner, length, mat_outer, mat_inner, direction=-1):
    # The first annulus is exactly at the gameplay socket/muzzle coordinate.
    sides = 8
    verts = []
    for yy, rr in [(y_front, radius), (y_front, inner),
                   (y_front-direction*length, radius),
                   (y_front-direction*length*0.46, inner)]:
        for i in range(sides):
            a = 2*math.pi*i/sides + math.pi/8
            verts.append((x + rr*math.cos(a), yy, z + rr*math.sin(a)))
    faces, idx = [], []
    for i in range(sides):
        j = (i+1)%sides
        faces.extend([(i, j, sides+j, sides+i),
                      (i, 2*sides+i, 2*sides+j, j),
                      (sides+i, sides+j, 3*sides+j, 3*sides+i)])
        idx.extend([mat_outer, mat_outer, mat_inner])
    faces.append(tuple(3*sides+i for i in range(sides)))
    idx.append(mat_inner)
    return mesh(name, verts, faces, mat_outer, idx)

# Broad octagonal bell: dark lower rim, icy sloping skirt and raised cap.
rings('Lower bell', [(-0.16, .39, .39), (-0.10, .52, .50),
                     (0.04, .54, .52), (0.17, .47, .45)], 8, 6, (0, .10))
rings('Dark lantern band', [(0.11, .472, .452), (0.18, .472, .452)], 8, 11, (0, .10))
rings('Pale crown', [(0.18, .46, .44), (0.27, .37, .36),
                     (0.32, .28, .27)], 8, 1, (0, .10))
rings('Inset glass', [(0.32, .245, .24), (0.355, .21, .205)], 8, 4, (0, .10))
rings('Cap bezel', [(0.355, .105, .10), (0.385, .095, .09)], 8, 11, (0, .10))
rings('Small ice core', [(0.385, .055, .053), (0.39, .055, .053)], 8, 9, (0, .10))

# Two swept shoulder blades retain the distinctive pointed hanging-crown outline.
for s in (-1, 1):
    points = [(s*.20, .45), (s*.68, 1.00), (s*.58, .17), (s*.47, -.12)]
    if s < 0:
        points.reverse()
    plate('Swept shoulder', points, -.08, .10, 15)
    inset = [(s*.31, .50), (s*.61, .88), (s*.53, .28), (s*.47, .20)]
    if s < 0:
        inset.reverse()
    plate('Shoulder inset', inset, .101, .135, 4)
    box('Side guard', (s*.51, -.17, .06), (.13, .71, .20), 6, .035)
    box('Guard inset', (s*.51, -.17, .167), (.075, .48, .025), 1, .012)
    # Rear openings are centered on the gameplay engine sockets, with no flame.
    tube_y('Engine socket', s*.33, .66, 0.0, .115, .072, .16, 6, 11, direction=1)

# A short armored neck terminates in the actual forward-facing muzzle mouth.
box('Emitter neck', (0, -.45, .16), (.30, .48, .22), 11, .045)
box('Neck armor', (0, -.47, .283), (.23, .36, .065), 6, .025)
tube_y('Emitter mouth', 0, -.76, .32, .145, .092, .18, 1, 11)
# A small recessed glow remains inside the hollow lip.
tube_y('Emitter inner rim', 0, -.737, .32, .055, .038, .025, 9, 11)

# All stationary geometry is exported as one flat-shaded body.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)

light('Upper left sun', 'SUN', (-3, 4, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (3, -3, 4), 130, (0, 0, 0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, .10, 5)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (2.6, -3.6, 3.0)
aim(cam, (0, .08, .08))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
