import bpy
import math
import json
import os
import sys
from mathutils import Vector

# RibbonGlider: a faceted candy bow with a raised central fuselage.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for datablock in list(datablocks):
        if datablock.users == 0:
            datablocks.remove(datablock)

colors = [(0.78,0.19,0.40),(0.97,0.85,0.71),(0.30,0.78,0.66),
          (1.0,0.59,0.25),(0.39,0.17,0.12),(0.62,0.28,0.18),
          (0.45,0.25,0.34),(0.57,0.38,0.79),(0.90,0.44,0.65),
          (0.27,0.91,0.83),(1.0,0.82,0.39),(0.13,0.055,0.13),
          (0.92,0.31,0.49),(0.83,0.66,0.39),(0.51,0.26,0.58),
          (0.98,0.96,0.88)]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1.0)
    bs.inputs['Roughness'].default_value = 0.32
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*color, 1.0)
        bs.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

body = []
def mesh_obj(name, verts, faces, material, indices=None):
    mesh = bpy.data.meshes.new(name + 'Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mats[material])
    if indices:
        for idx in sorted(set(indices)):
            if idx != material:
                mesh.materials.append(mats[idx])
        slot = {m.name: n for n, m in enumerate(mesh.materials)}
        for poly, idx in zip(mesh.polygons, indices):
            poly.material_index = slot[mats[idx].name]
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    body.append(ob)
    return ob

def faceted_plate(name, outline, z0, zedge, ztop, inset, top, side):
    # Three rings create bevelled edge and a broad, raised faceted surface.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    inner = [(cx + (x-cx)*inset, cy + (y-cy)*inset) for x,y in outline]
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,zedge) for x,y in outline] + [(x,y,ztop) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    ids = [side]
    for layer in range(2):
        for i in range(n):
            j = (i+1)%n
            faces.append((layer*n+i, layer*n+j, (layer+1)*n+j, (layer+1)*n+i))
            ids.append(side if layer == 0 else top)
    faces.append(tuple(range(2*n,3*n)))
    ids.append(top)
    return mesh_obj(name, verts, faces, top, ids)

def ellipsoid(name, loc, scale, material, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mats[material])
    body.append(ob)
    return ob

def tube_y(name, x, y_front, y_back, z, outer, inner, material, dark=11, n=12):
    # Annular lip with an inset dark bore, open toward -Y (or rear for engines).
    verts = []
    for y, radius in ((y_front,outer),(y_back,outer),(y_front,inner),(y_front+(y_back-y_front)*0.35,inner)):
        verts.extend((x + radius*math.cos(2*math.pi*i/n), y, z + radius*math.sin(2*math.pi*i/n)) for i in range(n))
    faces, ids = [], []
    for i in range(n):
        j=(i+1)%n
        faces.extend(((i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)))
        ids.extend((material,material,dark))
    faces.append(tuple(range(3*n,4*n)))
    ids.append(dark)
    return mesh_obj(name, verts, faces, material, ids)

# The footprint follows the original 2.1 x 1.56 silhouette. Thick beveled
# wing lobes read as a pair of folded ribbon loops, rather than flat triangles.
for s, label in ((-1,'L'),(1,'R')):
    wing = [(s*.12,.06),(s*.90,.65),(s*1.05,.23),(s*.49,-.44)]
    if s < 0: wing.reverse()
    faceted_plate('RibbonWing_'+label, wing, -.035, .075, .19, .81, 8, 14)
    fold = [(s*.26,.11),(s*.73,.48),(s*.87,.27),(s*.52,-.18)]
    if s < 0: fold.reverse()
    faceted_plate('Fold_'+label, fold, .185, .195, .225, .86, 12, 0)
    # A broad cream diagonal stripe makes the ribbon folds legible at game scale.
    stripe = [(s*.36,.035),(s*.47,.11),(s*.79,.34),(s*.75,.43),(s*.64,.35),(s*.31,.15)]
    if s < 0: stripe.reverse()
    faceted_plate('CreamInlay_'+label, stripe, .225, .229, .237, .97, 1, 1)
    # Forward-facing launcher mouth is exactly at the fixed gameplay point.
    tube_y('Muzzle_'+label, s*.49, -.44, -.27, .16, .075, .043, 13)
    # Rear-facing engine opening centered at the gameplay socket; no flames.
    tube_y('Socket_'+label, s*.22, .65, .49, 0.0, .085, .052, 5)

# Long chocolate fuselage and raised sugar-white nose accents.
hull = [(0,-.83),(.19,-.38),(.17,.55),(.13,.73),(-.13,.73),(-.17,.55),(-.19,-.38)]
faceted_plate('Fuselage', hull, -.08, .12, .29, .78, 4, 6)
nose = [(0,-.77),(.115,-.39),(.10,-.21),(-.10,-.21),(-.115,-.39)]
faceted_plate('NoseIcing', nose, .275, .283, .31, .83, 1, 5)
# A round wrapped-candy knot joins both wings, with a contrasting inset.
ellipsoid('BowKnot', (0,.10,.31), (.205,.23,.12), 8)
ellipsoid('KnotInset', (0,.10,.413), (.115,.13,.035), 15, 10, 5)
faceted_plate('Spine', [(-.075,.34),(.075,.34),(.09,.65),(-.09,.65)], .265,.28,.31,.85, 5, 4)

# All pieces are static: the weapon-bearing bow wings must not animate.
for ob in body:
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.select_set(False)
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for face in model.data.polygons:
    face.use_smooth = False
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
scene.world = bpy.data.worlds.new('Blue Gray World') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA': data.shape = 'DISK'; data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    point_at(ob, (0,0,0))
    return ob

light('Sun', 'SUN', (-3,-4,6), 2.0)
light('Soft Fill', 'AREA', (4,1,5), 170, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.48
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (3.1,-4.2,3.6)
point_at(cam, (0,-.02,.08))
cam_data.type = 'PERSP'
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
