import bpy, math, os, sys, json
from mathutils import Vector

# Run with Blender's Python; the optional argument after -- is the output directory.
def output_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    args = sys.argv
    if '--' in args and args.index('--') + 1 < len(args):
        p = os.path.abspath(args[args.index('--') + 1])
        return p if os.path.isdir(p) else os.path.dirname(p)
    raise RuntimeError('Provide the script path or output directory after --')

out = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.105,0.15,0.20),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.48,0.36,0.18),(0.53,0.61,0.55),(0.66,0.70,0.71)]
# The solar-cell tile is represented as blue, with restrained geometric cell divisions.
colors[7] = (0.16,0.24,0.43)
mats = []
for i, c in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*c, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1)
    bs.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*c, 1)
        bs.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

body = []
def mesh(name, verts, faces, tile):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[tile])
    body.append(ob)
    return ob

def plate(name, outline, z0, z1, tile, inset=0.0):
    # A sloping rim provides a real bevel without adding a modifier.
    n = len(outline)
    cx = sum(p[0] for p in outline)/n; cy = sum(p[1] for p in outline)/n
    inner = [(cx+(x-cx)*(1-inset), cy+(y-cy)*(1-inset)) for x,y in outline]
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in inner]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, tile)

def box(name, loc, scale, tile, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object; ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[tile]); body.append(ob)
    if bevel:
        mod = ob.modifiers.new('Chamfer', 'BEVEL'); mod.width = bevel; mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return ob

def ring(name, center, outer, inner, depth, axis, tile, n=10):
    # Open tube with an actual dark bore visible along its axis.
    c = Vector(center); a = Vector(axis).normalized()
    u = Vector((1,0,0)) if abs(a.x) < 0.9 else Vector((0,1,0))
    v = a.cross(u).normalized(); u = v.cross(a).normalized()
    verts = []
    for offset, radius in [(-depth,outer),(0,outer),(0,inner),(-depth,inner)]:
        for i in range(n):
            angle = 2*math.pi*i/n
            p = c + a*offset + radius*(u*math.cos(angle)+v*math.sin(angle))
            verts.append(tuple(p))
    faces=[]
    for j in range(3):
        for i in range(n):
            k=(i+1)%n; faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    ob=mesh(name, verts, faces, tile)
    ob.data.materials.append(mats[11])
    for poly in ob.data.polygons:
        if poly.index >= 2*n: poly.material_index=1
    return ob

def disc(name, center, radius, depth, tile, n=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=radius, depth=depth, location=center)
    ob=bpy.context.object; ob.name=name; ob.data.materials.append(mats[tile]); body.append(ob)
    return ob

# Central armored bus, faceted crown, and a tapered forward keel.
hull=[(0,-1.52),(-.57,-.22),(-.38,1.13),(.38,1.13),(.57,-.22)]
plate('Armored bus', hull, -.19,.22, 15, .12)
plate('Navy crown',[(0,-1.30),(-.39,-.19),(-.29,.96),(.29,.96),(.39,-.19)],.21,.32,6,.13)
plate('Nose shield',[(0,-1.44),(-.31,-.62),(-.24,-.38),(.24,-.38),(.31,-.62)],.25,.35,15,.1)
plate('Spine', [(-.14,.93),(-.18,.49),(-.13,-.47),(0,-.73),(.13,-.47),(.18,.49),(.14,.93)],.30,.38,15,.12)
# Distinct gold collar and small cyan sensor on the raised satellite core.
disc('Core base',(0,.16,.38),.41,.16,13,10)
disc('Core housing',(0,.16,.48),.29,.10,6,10)
disc('Core sensor',(0,.16,.545),.15,.035,9,10)

for s in (-1,1):
    # Solar wings occupy the same 3.24 by 2.65 footprint as the source.
    x=s*.98
    box('Solar frame',(x,.12,.02),(1.28,1.78,.15),13,.045)
    box('Solar field',(x,.12,.115),(1.16,1.64,.06),7,.025)
    # Broad cell seams, legible at gameplay scale.
    for y in (-.39,.12,.63):
        box('Solar cell seam',(x,y,.151),(1.14,.018,.012),6)
    box('Solar center seam',(x,.12,.153),(.018,1.60,.012),6)
    box('Wing root',(s*.48,.24,.11),(.28,.54,.24),15,.035)
    # Gun housings straddle the forward edge of the panels. Barrel face is
    # exactly at the gameplay point, with its axis facing -Y.
    box('Gun pod',(s*.95,-.64,.22),(.32,.48,.30),6,.045)
    box('Gun shoulder',(s*.95,-.57,.385),(.23,.25,.055),15,.025)
    ring('Muzzle lip',(s*.95,-.85,.30),.125,.073,.15,(0,-1,0),13,10)
    # Fallback engine sockets for hull 60: (+/- .874, .93, 0).
    # The unlit hollow opening faces toward the rear; no exhaust is modeled.
    ring('Engine socket',(s*.874,.93,0),.15,.095,.14,(0,1,0),6,10)

# Join static geometry; all transforms and modifiers are baked before export.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=bpy.context.object; model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons: poly.use_smooth=False
with open(os.path.join(out,'parts.json'),'w',encoding='utf-8') as f: json.dump([],f,indent=2)
bpy.ops.object.select_all(action='DESELECT'); model.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(out,'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background'); bg.inputs['Color'].default_value=(.05,.07,.09,1); bg.inputs['Strength'].default_value=1
scene.view_settings.view_transform='AgX'

def point_at(ob, target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if size is not None and kind=='AREA': data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob); ob.location=loc; point_at(ob,target)
light('Sun','SUN',(-4,-5,8),2.0,(0,0,0))
light('Soft fill','AREA',(4,2,5),350,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,8); cam.rotation_euler=(0,0,0); point_at(cam,(0,0,0)); camdata.type='ORTHO'; camdata.ortho_scale=3.85
scene.render.filepath=os.path.join(out,'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50; cam.location=(4.4,-5.8,5.2); point_at(cam,(0,-.1,.1))
scene.render.filepath=os.path.join(out,'angle.png'); bpy.ops.render.render(write_still=True)
