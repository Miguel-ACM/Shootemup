import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

# Blender may execute scripts without defining __file__ (for example in its console).
if globals().get('__file__'):
    OUT = Path(__file__).resolve().parent
elif '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1:
    arg = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
    OUT = arg if arg.is_dir() else arg.parent
else:
    raise RuntimeError('Supply the script path after -- when __file__ is unavailable')

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [
    (.78,.19,.40), (.97,.85,.71), (.30,.78,.66), (1,.59,.25),
    (.39,.17,.12), (.62,.28,.18), (.45,.25,.34), (.57,.38,.79),
    (.90,.44,.65), (.27,.91,.83), (1,.82,.39), (.13,.055,.13),
    (.92,.31,.49), (.83,.66,.39), (.51,.26,.58), (.98,.96,.88)
]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = .32 if i in (4, 5, 9, 10, 11) else .46
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.7
    mats.append(m)

body = []
def mesh(name, verts, faces, tile):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[tile])
    body.append(obj)
    return obj

def layer(name, outline, z0, z1, inset, tile):
    # Polygonal slab with a chamfered upper rim and a separate broad top face.
    n = len(outline)
    cx = sum(p[0] for p in outline) / n
    cy = sum(p[1] for p in outline) / n
    inner = [(cx + (x-cx)*(1-inset), cy + (y-cy)*(1-inset)) for x,y in outline]
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1-.035) for x,y in outline] + [(x,y,z1) for x,y in inner]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i)])
    return mesh(name, verts, faces, tile)

def box(name, loc, scale, tile, bevel=.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Cut edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mats[tile])
    body.append(obj)
    return obj

def tube(name, x, front_y, z, radius, inner, length, tile, rearward=True):
    # A real open annulus in the XZ plane, with a recessed dark bore.
    n = 12
    back_y = front_y + length if rearward else front_y - length
    verts = []
    for y,r in ((front_y,radius),(front_y,inner),(back_y,radius),(back_y,inner)):
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((x+r*math.cos(a),y,z+r*math.sin(a)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),
                      (n+i,n+j,3*n+j,3*n+i),(2*n+i,3*n+i,3*n+j,2*n+j)])
    mesh(name,verts,faces,tile)
    # A dark recessed cap makes the hollow mouth legible without a bright projectile.
    verts2=[(x+inner*math.cos(2*math.pi*i/n), front_y+(0.035 if rearward else -0.035), z+inner*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh(name+'_bore',verts2,[tuple(range(n))],11)

# Sandwich construction: golden biscuit, chocolate filling and a chocolate cap.
outline=[(0,-1.05),(.36,-.24),(.28,.76),(-.28,.76),(-.36,-.24)]
layer('Biscuit lower',outline,-.13,-.025,.06,13)
layer('Chocolate filling',outline,-.025,.035,.035,5)
layer('Biscuit upper',outline,.035,.13,.06,13)
# Swept wings are individual thick, chamfered chocolate wafers, not flat decals.
for s in (-1,1):
    wing=[(s*.21,-.30),(s*.68,.41),(s*.55,.85),(s*.20,.50)]
    if s < 0: wing.reverse()
    layer('Chocolate wing',wing,-.07,.105,.09,4)
    inset=[(s*.27,-.13),(s*.59,.40),(s*.52,.69),(s*.25,.46)]
    if s < 0: inset.reverse()
    layer('Wing wafer inlay',inset,.107,.135,.08,5)
    # Finite-sized paired launchers, with their front ring at the gameplay point.
    box('Launcher fairing',(s*.27,-.535,.115),(.16,.43,.17),4,.025)
    tube('Muzzle',s*.27,-.78,.16,.074,.043,.18,13)
    # Rear-facing recessed openings align with the two gameplay engine sockets.
    tube('Socket',s*.24,.82,0,.092,.061,.16,4,False)

# Three broad, raised chocolate segments echo the original blocks; pale breaks
# between them give the central spine the appearance of a layered wafer bar.
for y in (.44,.17,-.10):
    box('Chocolate bar segment',(0,y,.175),(.42,.205,.095),4,.026)
    box('Wafer ridge',(0,y-.065,.227),(.31,.025,.014),13,.005)

# A small faceted aqua cockpit inset defines the pointed front at gameplay scale.
layer('Cockpit bezel',[(0,-.64),(.145,-.23),(0,-.13),(-.145,-.23)],.15,.225,.12,11)
layer('Cockpit',[(0,-.59),(.105,-.245),(0,-.18),(-.105,-.245)],.226,.255,.10,9)
# Two diagonal scoring grooves in each wing reinforce the wafer surface.
for s in (-1,1):
    for y in (.34,.51):
        # Thin, shallow, readable diagonal scoring rather than tiny surface noise.
        p=[(s*.32,y-.07),(s*.51,y+.07),(s*.49,y+.10),(s*.30,y-.04)]
        if s < 0: p.reverse()
        layer('Wafer scoring',p,.136,.146,.04,13)

# Join every static component; all transforms and modifiers are applied.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# This aircraft has no obvious moving element: its wings and launchers are fixed.
(OUT / 'parts.json').write_text(json.dumps([], indent=2) + '\n', encoding='utf-8')
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    aim(ob, target)
    return ob

light('Sun upper left','SUN',(-3,-4,6),2.0,(0,0,0))
light('Soft fill','AREA',(3,1,4),180,(0,0,0),5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.25
cam.location = (0,0,5)
cam.rotation_euler = (0,0,0)
scene.render.filepath = str(OUT / 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.15,-4.3,3.35)
aim(cam,(0,-.05,.03))
scene.render.filepath = str(OUT / 'angle.png')
bpy.ops.render.render(write_still=True)
