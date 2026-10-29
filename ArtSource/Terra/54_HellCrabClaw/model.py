import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

# The input hull is an asymmetric claw, not a complete crab.  Coordinates are
# kept in hull space, including the inherited weapon and engine socket positions.
if '__file__' in globals():
    OUT = Path(__file__).resolve().parent
else:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = Path(args[args.index('--') + 1]).resolve().parent

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [(.24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),
          (.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),
          (.20,.36,.40),(.16,.83,.91),(1.,.77,.27),(.055,.09,.12),
          (.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = .78
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

body = []
jaw = []
def mesh(name, verts, faces, material, group=body, face_materials=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    for mat in mats:
        me.materials.append(mat)
    for p in me.polygons:
        p.material_index = material if face_materials is None else face_materials[p.index]
        p.use_smooth = False
    group.append(ob)
    return ob

def shell(name, outline, zbottom, zedge, ztop, material, group=body, inset=.16):
    # Inset top ring produces large, angular bevel facets and a raised crown.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    vs = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline]
    vs += [(x+(cx-x)*inset, y+(cy-y)*inset, ztop) for x,y in outline]
    fs = [tuple(reversed(range(n)))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    fs += [(i+n,(i+1)%n+n,(i+1)%n+2*n,i+2*n) for i in range(n)]
    fs += [tuple(range(2*n,3*n))]
    return mesh(name, vs, fs, material, group)

def ellipsoid(name, location, scale, mat, group=body, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    for p in ob.data.polygons:
        p.use_smooth = False
    group.append(ob)
    return ob

def tube_y(name, x, yface, z, radius, length, mat, group=body):
    # Annular end and inset dark bore: the opening really is recessed, not a disc on a rod.
    n = 10
    verts = []
    for y, r in [(yface+length,radius*.85),(yface,radius),(yface,radius*.58),(yface+length*.48,radius*.58)]:
        verts += [(x+r*math.cos(2*math.pi*i/n), y, z+r*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]
    ids=[]
    for ring in range(3):
        for i in range(n):
            faces.append((ring*n+i,ring*n+(i+1)%n,(ring+1)*n+(i+1)%n,(ring+1)*n+i))
            ids.append(mat if ring < 2 else 11)
    faces.append(tuple(3*n+i for i in reversed(range(n))))
    ids.append(11)
    return mesh(name, verts, faces, mat, group, ids)

def join_group(group, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = group[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob

# Armored wrist and its dark articulated knuckles.
ellipsoid('Wrist', (0,.30,0), (.56,.68,.36), 5)
shell('Carapace', [(-.48,.16),(-.48,.53),(-.32,.83),(0,.96),(.33,.83),(.49,.52),(.45,.15),(.22,-.09),(-.23,-.09)], -.12,.22,.38,12)
shell('Crown', [(-.33,.30),(-.30,.66),(0,.82),(.31,.66),(.33,.30),(0,.06)], .34,.38,.46,5, inset=.20)
shell('Bone crest', [(-.17,.32),(-.21,.57),(0,.72),(.21,.57),(.17,.32),(0,.20)], .445,.455,.49,1, inset=.22)
shell('Crest shadow', [(-.11,.36),(-.11,.52),(0,.61),(.11,.52),(.11,.36),(0,.30)], .49,.495,.50,11, inset=.15)
shell('Ember seam', [(-.045,.37),(-.05,.50),(0,.55),(.05,.50),(.045,.37),(0,.34)], .501,.504,.51,10, inset=.1)
ellipsoid('Left knuckle', (-.38,.02,.04), (.27,.24,.27), 11)
ellipsoid('Right knuckle', (.35,.02,.04), (.25,.23,.25), 11)

# Long fixed finger: broad shell, contrasting edge and inward-facing bone teeth.
shell('Fixed talon', [(-.46,.12),(-.67,-.43),(-.58,-.76),(-.35,-1.13),(.03,-1.40),(-.06,-.76),(-.17,-.22)], -.20,.11,.30,12, inset=.13)
shell('Fixed raised scute', [(-.47,-.14),(-.57,-.43),(-.48,-.72),(-.26,-1.05),(-.18,-1.12),(-.22,-.72),(-.27,-.29)], .24,.29,.35,5, inset=.15)
shell('Fixed tip', [(-.35,-1.12),(.03,-1.40),(-.07,-.95),(-.18,-.98)], .08,.22,.27,1, inset=.12)
shell('Fixed tooth', [(-.20,-.49),(-.07,-.57),(-.19,-.68),(-.27,-.66)], .19,.25,.29,1, inset=.12)

# Smaller hinged finger is independent of the fixed weapon for game animation.
shell('Moving talon', [(.30,.13),(.58,-.09),(.67,-.31),(.59,-.62),(.48,-.88),(.12,-1.17),(.25,-.48),(.12,-.12)], -.18,.10,.28,12, jaw, .13)
shell('Moving raised scute', [(.35,-.09),(.54,-.26),(.53,-.54),(.38,-.84),(.27,-.90),(.33,-.53),(.27,-.20)], .23,.27,.33,5, jaw, .16)
shell('Moving tip', [(.48,-.88),(.12,-1.17),(.23,-.78),(.33,-.77)], .08,.20,.25,1, jaw, .12)
shell('Moving tooth', [(.24,-.40),(.11,-.49),(.23,-.60),(.31,-.59)], .17,.24,.28,1, jaw, .12)

# Fixed forward-facing weapon, with its lip centered exactly on the gameplay point.
ellipsoid('Gun cradle', (0,-.59,.19), (.14,.22,.13), 11)
tube_y('Muzzle lip', 0,-.75,.30,.085,.20,13)
# The inherited engine sockets are widely spaced. Compact outriggers support the
# rear-facing annular mouths without altering the pincer or adding any flames.
for side in (-1,1):
    x=side*.874
    shell('Socket outrigger', [(side*.35,.69),(side*.77,.78),(side*.86,.89),(side*.80,.95),(side*.39,.85)], -.09,.07,.13,11, inset=.12)
    # Rear-facing rings extend toward the center; the mouth is exactly at y=.93.
    # Reverse the y coordinates of the standard forward-facing tube afterwards.
    ob=tube_y('Engine socket', x,-.93,0,.055,.11,6)
    for v in ob.data.vertices:
        v.co.y = -v.co.y
    ob.data.update()

model = join_group(body, 'Model')
part = join_group(jaw, 'Jaw_R')
# Geometry remains in world hull coordinates; pivot is explicitly recorded.
parts = [{'object':'Jaw_R','motion':'swing','axis':[0,0,1],
          'pivot':[.35,.02,.04],'speed':.7,'amplitude':14}]
(OUT / 'parts.json').write_text(json.dumps(parts, indent=2)+'\n', encoding='utf-8')

# Export only the mesh objects, retaining their object names for animation.
bpy.ops.object.select_all(action='DESELECT')
for ob in (model, part): ob.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=str(OUT / 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape='DISK'; data.size=size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    point_at(ob, target)
    return ob
light('Sun', 'SUN', (-3,-4,7), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 250, (0,-.15,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,-.22,6)
cam.rotation_euler = (0,0,0)
point_at(cam,(0,-.22,0))
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.78
scene.render.filepath = str(OUT / 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (3.6,-5.0,4.3)
point_at(cam,(0,-.20,.05))
camdata.type = 'PERSP'
camdata.lens = 50
scene.render.filepath = str(OUT / 'angle.png')
bpy.ops.render.render(write_still=True)
