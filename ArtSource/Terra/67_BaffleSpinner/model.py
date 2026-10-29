import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Outputs are always written alongside this script, including when run with --.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, rgb in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    shader = m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*rgb, 1.0)
    shader.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        shader.inputs['Emission Color'].default_value = (*rgb, 1.0)
        shader.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

body, rotor = [], []
def mesh(name, verts, faces, tile, dest=body, face_tiles=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    for m in mats:
        data.materials.append(m)
    for p in data.polygons:
        p.material_index = face_tiles[p.index] if face_tiles else tile
        p.use_smooth = False
    dest.append(ob)
    return ob

def armor(name, outline, z0, z1, inset, top, side=6, dest=body):
    # Three rings give the armor a real sloped shoulder and a faceted top.
    n = len(outline)
    cx = sum(p[0] for p in outline) / n
    cy = sum(p[1] for p in outline) / n
    upper = [(cx + (x-cx)*(1-inset), cy + (y-cy)*(1-inset)) for x,y in outline]
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1-0.055) for x,y in outline] + [(x,y,z1) for x,y in upper]
    faces = [tuple(reversed(range(n)))]
    tiles = [11]
    for ring in range(2):
        for i in range(n):
            j = (i+1)%n
            faces.append((ring*n+i, ring*n+j, (ring+1)*n+j, (ring+1)*n+i))
            tiles.append(side if ring == 0 else top)
    faces.append(tuple(range(2*n,3*n)))
    tiles.append(top)
    return mesh(name, verts, faces, top, dest, tiles)

def disc(name, center, radius, depth, tile, sides=12, dest=body):
    x,y,z = center
    verts = [(x+radius*math.cos(2*math.pi*i/sides), y+radius*math.sin(2*math.pi*i/sides), z+dz) for dz in (-depth/2, depth/2) for i in range(sides)]
    faces = [tuple(reversed(range(sides))), tuple(range(sides,2*sides))]
    faces += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
    return mesh(name, verts, faces, tile, dest)

def tube_y(name, x, y, z, outer, inner, length, tile, dest=body):
    # Annular tube: its front mouth is exactly at y, with a recessed dark bore.
    n=10
    verts=[]
    for yy,rad in ((y,outer),(y+length,outer),(y,inner),(y+length*0.65,inner)):
        verts += [(x+rad*math.cos(2*math.pi*i/n), yy, z+rad*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]; ft=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
        ft.extend((tile,6,11))
    faces.append(tuple(range(3*n,4*n))); ft.append(11)
    return mesh(name, verts, faces, tile, dest, ft)

def stripe(name, coords, z, tile, dest=body):
    return armor(name, coords, z, z+0.009, 0.0, tile, tile, dest)

# Broad, swept side baffles preserve the original six-sided footprint.
for s, suffix in ((-1,'L'),(1,'R')):
    shape=[(s*.25,-.10),(s*.60,-.51),(s*.97,-.20),(s*.80,.52),(s*.40,.73),(s*.62,.25)]
    if s < 0: shape.reverse()
    armor('Side_'+suffix, shape, -.12, .18, .16, 0, 11)
    inset=[(s*.38,-.12),(s*.61,-.39),(s*.84,-.19),(s*.73,.38),(s*.57,.47),(s*.67,.18)]
    if s < 0: inset.reverse()
    armor('RimPlate_'+suffix, inset, .165, .208, .11, 4, 6)
    # Single broad hazard slash on each shoulder, readable at gameplay size.
    hazard=[(s*.69,.13),(s*.80,.07),(s*.77,-.04),(s*.66,.02)]
    if s < 0: hazard.reverse()
    stripe('Hazard_'+suffix, hazard, .211, 13)
    # Muzzle mouth is fixed at (+/- .64, -.30, +.16).
    tube_y('Muzzle_'+suffix, s*.64, -.30, .16, .083, .048, .16, 11)
    # Rear motor housing; a recessed annular opening at the socket itself.
    motor=[(s*.22,.43),(s*.38,.43),(s*.40,.64),(s*.36,.71),(s*.24,.71),(s*.20,.64)]
    if s < 0: motor.reverse()
    armor('Motor_'+suffix, motor, -.11, .22, .15, 0, 6)
    # Opening faces aft (+Y); start at the specified socket plane.
    tube_y('Socket_'+suffix, s*.30, .55, .0, .068, .041, .13, 6)
    # A dark top slot makes the socket readable from directly above.
    slot=[(s*.26,.57),(s*.34,.57),(s*.34,.66),(s*.26,.66)]
    if s < 0: slot.reverse()
    stripe('SocketSlot_'+suffix, slot, .222, 11)

# Fixed central bearing and a distinct rotating, four-vane baffle assembly.
disc('Bearing', (0,0,.075), .38, .24, 11, 12)
disc('BearingRim', (0,0,.205), .35, .045, 6, 12)
disc('BaffleDisc', (0,0,.247), .285, .055, 8, 12, rotor)
for i in range(4):
    a=i*math.pi/2
    ca,sa=math.cos(a),math.sin(a)
    def turn(p): return (p[0]*ca-p[1]*sa, p[0]*sa+p[1]*ca)
    blade=[turn(p) for p in ((.08,-.045),(.30,-.075),(.34,.035),(.15,.075))]
    armor('BaffleVane', blade, .273, .315, .08, 6 if i%2==0 else 12, 11, rotor)
disc('BaffleHub', (0,0,.32), .105, .055, 11, 10, rotor)
disc('HubCap', (0,0,.355), .06, .018, 13, 10, rotor)

def join_objects(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for p in ob.data.polygons: p.use_smooth = False
    return ob

model = join_objects(body, 'Model')
baffle = join_objects(rotor, 'Baffle')
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([{'object':'Baffle', 'motion':'spin', 'axis':[0,0,1], 'pivot':[0,0,.247], 'speed':360}], f, indent=2)

# Export only the two mesh objects, with scene coordinates preserved.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
baffle.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, export_object_groups=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray') if scene.world is None else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    aim(ob,target)
    return ob

light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,1,4), 160, (0,0,0), 5.0)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)
cam_data.type='ORTHO'
cam_data.ortho_scale=2.28
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(2.7,-3.8,3.5)
aim(cam,(0,0,.05))
cam_data.type='PERSP'
cam_data.lens=48
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
