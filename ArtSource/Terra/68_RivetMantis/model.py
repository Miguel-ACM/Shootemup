import bpy
import math
import json
import os
import sys
from mathutils import Vector

def output_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            p = os.path.abspath(args[0])
            return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()

OUT = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.materials):
    bpy.data.materials.remove(block)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.76
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body = []
parts = []
def mesh_obj(name, verts, faces, tile, group=body):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[tile])
    for face in obj.data.polygons:
        face.use_smooth = False
    group.append(obj)
    return obj

def plate(name, outline, z0, z1, inset, tile, group=body):
    if sum(outline[i][0] * outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0] * outline[i][1] for i in range(len(outline))) < 0:
        outline = list(reversed(outline))
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    top = [(cx + (x-cx)*(1-inset), cy + (y-cy)*(1-inset), z1) for x,y in outline]
    verts = [(x,y,z0) for x,y in outline] + top
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh_obj(name, verts, faces, tile, group)

def tube_y(name, x, y_front, y_back, z, outer, inner, tile, inner_tile=11, sides=10):
    # The mouth plane is at y_front. Its bore recesses toward y_back.
    direction = 1 if y_back > y_front else -1
    verts = []
    for y, radius in ((y_front, outer), (y_back, outer), (y_front-direction*.001, inner), (y_front+direction*.065, inner)):
        verts.extend((x + radius*math.cos(2*math.pi*i/sides), y,
                      z + radius*math.sin(2*math.pi*i/sides)) for i in range(sides))
    faces = []
    for i in range(sides):
        j = (i+1)%sides
        faces.extend([(i,j,sides+j,sides+i), (i,2*sides+i,2*sides+j,j),
                      (2*sides+i,3*sides+i,3*sides+j,2*sides+j)])
    faces.append(tuple(range(3*sides, 4*sides)))
    if direction > 0:
        faces = [tuple(reversed(face)) for face in faces]
    obj = mesh_obj(name, verts, faces, tile)
    obj.data.materials.append(mats[inner_tile])
    for poly in obj.data.polygons:
        if poly.index % 3 == 2 or poly.index == len(obj.data.polygons)-1:
            poly.material_index = 1
    return obj

def disk(name, x, y, z, radius, depth, tile, group=body, sides=8):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=(x,y,z))
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[tile])
    group.append(ob)
    return ob

# Long central thorax and raised, faceted carapace.
plate('Thorax', [(0,-.80),(-.27,-.31),(-.20,.74),(.20,.74),(.27,-.31)], -.07,.25,.17,6)
plate('Dark_carapace', [(0,-.66),(-.18,-.27),(-.15,.53),(0,.67),(.15,.53),(.18,-.27)], .235,.355,.16,0)
plate('Head', [(0,-.78),(-.19,-.40),(-.15,-.25),(.15,-.25),(.19,-.40)], .24,.34,.12,6)
plate('Visor', [(-.14,-.24),(-.12,.15),(0,.25),(.12,.15),(.14,-.24),(0,-.37)], .34,.405,.20,11)
for s in (-1,1):
    plate('Eye', [(s*.025,-.23),(s*.12,-.17),(s*.10,-.09),(s*.025,-.12)], .402,.414,.05,10)
    outline = [(s*.17,.30),(s*.98,.58),(s*1.10,-.24),(s*.78,-.83),
               (s*.65,-.75),(s*.76,-.08),(s*.39,.08)]
    if s < 0:
        outline.reverse()
    plate('Claw', outline, -.04,.19,.10,0)
    shoulder = [(s*.25,.29),(s*.95,.52),(s*.99,.16),(s*.73,.08),(s*.39,.15)]
    if s < 0: shoulder.reverse()
    plate('Shoulder_armor', shoulder, .17,.255,.12,6)
    forearm = [(s*.78,.02),(s*1.04,-.20),(s*.80,-.69),(s*.68,-.67)]
    if s < 0: forearm.reverse()
    plate('Forearm_armor', forearm, .175,.26,.12,4)
    stripe = [(s*.70,.27),(s*.80,.31),(s*.88,.16),(s*.79,.12)]
    if s < 0: stripe.reverse()
    plate('Hazard_stripe', stripe, .253,.263,.02,13)
    stripe2 = [(s*.53,.22),(s*.62,.25),(s*.70,.11),(s*.61,.09)]
    if s < 0: stripe2.reverse()
    plate('Hazard_stripe', stripe2, .253,.263,.02,13)
    disk('Shoulder_rivet', s*.87,.34,.27,.052,.035,13)
    disk('Elbow_rivet', s*.94,-.19,.275,.047,.035,12)
    # Mouth centers lie at the exact gameplay coordinates.
    tube_y('Rivet_gun', s*.78, -.77, -.39, .16, .105, .058, 6)
    tube_y('Engine_socket', s*.30, .69, .48, 0.0, .105, .063, 6)
    fin = [(s*.25,.43),(s*.49,.48),(s*.53,.73),(s*.36,.64)]
    if s < 0: fin.reverse()
    plate('Fin_L' if s < 0 else 'Fin_R', fin, .09,.17,.10,12,parts)

# Join the static geometry only; keep named fins independent for the game.
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
for ob in [model] + parts:
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for poly in ob.data.polygons:
        poly.use_smooth = False

part_data = [
    {'object':'Fin_L','motion':'swing','axis':[0,1,0], 'pivot':[-.25,.43,.13], 'speed':1.4, 'amplitude':14},
    {'object':'Fin_R','motion':'swing','axis':[0,-1,0], 'pivot':[.25,.43,.13], 'speed':1.4, 'amplitude':14},
]
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump(part_data, f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
for ob in [model] + parts:
    ob.select_set(True)
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
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    aim(ob, target)
    return ob

sun = light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
sun.data.angle = math.radians(18)
light('Fill', 'AREA', (3,2,4), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.62
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.15,-4.1,3.8)
aim(cam, (0,0,.08))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

