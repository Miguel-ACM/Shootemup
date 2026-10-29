import bpy
import math
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- /path/to/model.py]
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    path = os.path.abspath(args[args.index('--') + 1])
    HERE = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.68
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

parts = []
def mesh(name, verts, faces, material, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.clear()
    for mat in mats:
        data.materials.append(mat)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for p in data.polygons:
        p.material_index = material if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    parts.append(obj)
    return obj

def armor(name, outline, bottom, shoulder, top, inset, material, side=11):
    # Inset upper ring gives each plate broad, planar chamfers and a raised crown.
    area = sum(outline[i][0]*outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0]*outline[i][1] for i in range(len(outline)))
    if area < 0:
        outline = list(reversed(outline))
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    lower = [(x,y,bottom) for x,y in outline]
    rim = [(x,y,shoulder) for x,y in outline]
    crown = [(cx+(x-cx)*inset, cy+(y-cy)*inset, top) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    ids = [side, material]
    for i in range(n):
        j = (i+1)%n
        faces.append((i,j,n+j,n+i)); ids.append(side)
        faces.append((n+i,n+j,2*n+j,2*n+i)); ids.append(material)
    return mesh(name, lower+rim+crown, faces, material, ids)

def ring_y(name, x, y, z, outer, inner, depth, mat=6, inside=11, segments=10, direction=-1):
    # Mouth lies exactly at y; open recessed dark bore points into the body.
    verts=[]
    for yy, radius in ((y,outer),(y,inner),(y-direction*depth,inner)):
        for i in range(segments):
            a=2*math.pi*i/segments
            verts.append((x+math.cos(a)*radius, yy, z+math.sin(a)*radius))
    faces=[]; ids=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i), (segments+i,segments+j,2*segments+j,2*segments+i)])
        ids.extend([mat,inside])
    faces.append(tuple(range(2*segments,3*segments))); ids.append(inside)
    return mesh(name,verts,faces,mat,ids)

def stripe(name, points, z, material):
    area = sum(points[i][0]*points[(i+1)%len(points)][1] - points[(i+1)%len(points)][0]*points[i][1] for i in range(len(points)))
    if area < 0:
        points = list(reversed(points))
    return mesh(name, [(x,y,z) for x,y in points], [tuple(range(len(points)))], material)

# Broad faceted central fuselage, the original six-sided plan with a high spine.
armor('keel', [(-.47,.56),(.47,.56),(.67,-.22),(.28,-.86),(-.28,-.86),(-.67,-.22)], -.16,.14,.37,.78, 0,11)
armor('bridge foundation', [(-.36,.29),(.36,.29),(.43,-.20),(.25,-.57),(-.25,-.57),(-.43,-.20)], .30,.39,.49,.80, 12,0)
# Broad dark command visor and its cyan slit, both set into the top, face forward.
armor('visor surround', [(-.31,-.13),(.31,-.13),(.29,-.32),(-.29,-.32)], .485,.505,.525,.92,11,11)
stripe('cyan command slit',[(-.235,-.197),(.235,-.197),(.23,-.258),(-.23,-.258)],.527,9)
armor('rear dorsal intake', [(-.27,.51),(.27,.51),(.24,.30),(-.24,.30)], .32,.39,.44,.80,6,0)
stripe('intake recess',[(-.18,.435),(.18,.435),(.17,.365),(-.17,.365)],.442,11)

for s in (-1,1):
    # Reverse left-hand loops so outward-facing sides remain consistent.
    def mirrored(points):
        out=[(s*x,y) for x,y in points]
        return out if s == 1 else list(reversed(out))
    armor('swept outrigger', mirrored([(.43,.28),(1.29,.65),(1.48,.05),(1.16,-.61),(.79,-.79),(.65,-.20)]), -.13,.08,.23,.82,12,0)
    armor('outer shoulder', mirrored([(.80,.20),(1.28,.41),(1.36,.04),(1.13,-.40),(.91,-.43)]), .19,.24,.31,.79,4,0)
    armor('gun saddle', mirrored([(.81,.05),(1.17,.15),(1.13,-.44),(.84,-.54)]), .25,.29,.34,.80,11,0)
    # Bold painted identification chevron, large enough to read from above.
    stripe('wing marking', mirrored([(.74,.19),(.84,.22),(1.18,.38),(1.21,.30),(.88,.12)]), .295,1)
    # Motor pod: wide faceted shoulders and a real rear opening at the socket.
    armor('motor cowling', mirrored([(.82,.25),(.84,.67),(.92,.79),(1.12,.79),(1.22,.66),(1.22,.25),(1.13,.12),(.91,.12)]), -.11,.16,.30,.83,6,0)
    armor('motor cap', mirrored([(.90,.30),(.90,.64),(.97,.72),(1.09,.72),(1.15,.64),(1.15,.30)]), .29,.31,.36,.84,0,11)
    stripe('motor marking', mirrored([(.95,.39),(1.09,.39),(1.09,.46),(.95,.46)]), .362,13)
    # Short side gun, its visible front mouth is at the gameplay coordinate.
    armor('gun housing', mirrored([(.89,-.29),(1.15,-.29),(1.15,-.64),(.89,-.64)]), .18,.27,.34,.88,4,11)
    ring_y('side muzzle',s*1.02,-.72,.27,.105,.060,.11,6,11,10,-1)
    ring_y('engine socket',s*1.02,.81,0,.145,.095,.12,6,11,10,1)

# The central weapon's prescribed z is +0.30. Its muzzle lies at y=-0.70.
armor('central cannon housing', [(-.16,-.38),(.16,-.38),(.16,-.65),(-.16,-.65)], .22,.30,.38,.80,0,11)
ring_y('central muzzle',0,-.70,.30,.125,.073,.12,6,11,10,-1)

# Join the authored pieces and bake transforms and triangulation before export.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
if not 300 <= len(model.data.polygons) <= 2500:
    raise RuntimeError('Triangle budget exceeded: %d' % len(model.data.polygons))

bpy.ops.wm.obj_export(filepath=os.path.join(HERE,'model.obj'), export_selected_objects=True,
    export_materials=True, forward_axis='Y', up_axis='Z', apply_modifiers=True)

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=1):
    data = bpy.data.lights.new(name,kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj,(0,0,0))
    return obj

sun=light('Upper left sun','SUN',(-4,3,6),2.0)
sun.data.angle=math.radians(12)
light('Soft fill','AREA',(4,-2,5),220,5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,6)
cam.rotation_euler=(0,0,0)
cam_data.type='ORTHO'
cam_data.ortho_scale=3.5
scene.render.filepath=os.path.join(HERE,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(3.4,-4.6,4.0)
aim(cam,(0,0,0.12))
cam_data.type='PERSP'
cam_data.lens=50
scene.render.filepath=os.path.join(HERE,'angle.png')
bpy.ops.render.render(write_still=True)
