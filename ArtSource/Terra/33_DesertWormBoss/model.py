import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector

# Run with: blender --background --python model.py [-- output_directory]
try:
    OUT = Path(__file__).resolve().parent
except NameError:
    args = sys.argv
    OUT = Path(args[args.index('--') + 1]).resolve() if '--' in args and len(args) > args.index('--') + 1 else Path.cwd()
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = 0.78
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)
body, jaw_l, jaw_r = [], [], []

def mesh(name, verts, faces, mat, group=body, indices=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.clear()
    for m in mats: me.materials.append(m)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    for i, p in enumerate(me.polygons):
        p.material_index = indices[i] if indices else mat
        p.use_smooth = False
    group.append(ob)
    return ob

def plate(name, outline, bottom, rim, top, mat, group=body, side=6, inset=0.13):
    # Three contour levels make the plate a real, chamfered volume.
    n = len(outline)
    cx = sum(p[0] for p in outline)/n
    cy = sum(p[1] for p in outline)/n
    inner = [(x + (cx-x)*inset, y + (cy-y)*inset) for x,y in outline]
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,rim) for x,y in outline] + [(x,y,top) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    indices = [side]
    for j in range(n):
        k = (j+1)%n
        faces.append((j,k,n+k,n+j)); indices.append(side)
        faces.append((n+j,n+k,2*n+k,2*n+j)); indices.append(mat)
    faces.append(tuple(range(2*n,3*n))); indices.append(mat)
    return mesh(name, verts, faces, mat, group, indices)

def ring_y(name, x, front, z, radius, inner, length, material, group=body):
    # Annular front face and recessed dark bore, axis along Y.
    n = 12
    verts=[]
    for y,r in [(front,radius),(front,inner),(front+length,radius),(front+length*.65,inner)]:
        verts.extend([(x+math.cos(2*math.pi*i/n)*r,y,z+math.sin(2*math.pi*i/n)*r) for i in range(n)])
    faces=[]; ids=[]
    for i in range(n):
        j=(i+1)%n
        faces += [(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)]
        ids += [material, material, 11]
    faces.append(tuple(range(3*n,4*n))); ids.append(11)
    return mesh(name, verts, faces, material, group, ids)

# Deep recessed body gives the jaws a dark mouth and a robust cross-section.
plate('Inner chassis', [(-.63,.75),(.63,.75),(.88,.18),(.64,-.66),(0,-.90),(-.64,-.66),(-.88,.18)], -.12,.16,.30,11,side=11,inset=.16)
# Broad pale side carapaces, with beveled surfaces and asymmetric material facets.
for s in (-1,1):
    def mir(poly):
        return [(s*x,y) for x,y in (poly if s==1 else list(reversed(poly)))]
    plate('Ceramic shoulder', mir([(.43,.72),(.80,.51),(.98,-.03),(.77,-.71),(.50,-.43),(.48,.12)]), .05,.29,.43,1,side=6,inset=.12)
    plate('Shoulder inset', mir([(.62,.53),(.78,.39),(.89,-.03),(.78,-.36),(.69,-.20)]), .405,.43,.46,15,side=14,inset=.12)
    plate('Rust seam', mir([(.50,.40),(.57,.36),(.65,-.34),(.59,-.42),(.54,-.17)]), .40,.415,.426,12,side=11,inset=.08)
    # Separate jaw and its teeth all rotate around the rear root.
    group = jaw_l if s < 0 else jaw_r
    plate('Jaw', mir([(.35,-.39),(.73,-.53),(.77,-.70),(.55,-1.34),(.18,-1.11),(.27,-.86)]), -.02,.20,.31,1,group,6,.10)
    plate('Jaw crown', mir([(.48,-.54),(.69,-.65),(.52,-1.22),(.37,-1.12),(.46,-.88)]), .30,.32,.36,15,group,14,.12)
    plate('Jaw brass edge', mir([(.35,-.56),(.43,-.62),(.31,-1.03),(.22,-1.10),(.26,-.85)]), .22,.29,.32,13,group,12,.08)
    for j in range(3):
        y=-.66-j*.17
        plate('Tooth', mir([(.39,y+.08),(.17,y-.025),(.39,y-.12)]), .12,.22,.27,6,group,11,.03)
    # A single restrained orange slit per side, inset into the cheek.
    plate('Eye socket', mir([(.68,.02),(.78,-.04),(.75,-.27),(.66,-.20)]), .435,.445,.455,11,side=11,inset=.05)
    plate('Amber eye', mir([(.695,-.015),(.748,-.055),(.728,-.205),(.68,-.18)]), .455,.458,.466,10,side=13,inset=.06)
# Rear collar echoes the separate body segments without adding one to this asset.
plate('Rear collar', [(-.48,.73),(.48,.73),(.55,.57),(.40,.48),(-.40,.48),(-.55,.57)], .24,.37,.43,1,side=6,inset=.13)
plate('Collar ridge', [(-.38,.69),(.38,.69),(.39,.61),(-.39,.61)], .43,.45,.48,15,side=6,inset=.08)
# The oversized red weakpoint is framed in dark metal and inset into the head.
plate('Weakpoint socket', [(-.46,.47),(.46,.47),(.53,.16),(.42,-.38),(0,-.65),(-.42,-.38),(-.53,.16)], .28,.37,.43,11,side=11,inset=.10)
plate('Red head weakpoint', [(-.37,.40),(.37,.40),(.44,.13),(.34,-.31),(0,-.53),(-.34,-.31),(-.44,.13)], .40,.46,.57,5,side=12,inset=.18)
plate('Weakpoint raised facet', [(-.23,.30),(.23,.30),(.28,.09),(.21,-.19),(0,-.32),(-.21,-.19),(-.28,.09)], .565,.575,.60,12,side=5,inset=.20)
# Muzzle front is exactly at the gameplay point; its annular lip is visible from above.
ring_y('Central muzzle',0,-.92,.18,.115,.068,.25,13)
plate('Muzzle upper guard',[(-.13,-.88),(.13,-.88),(.16,-.71),(.10,-.64),(-.10,-.64),(-.16,-.71)],.24,.265,.29,6,side=11,inset=.12)

def join_group(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob
model = join_group(body, 'Model')
left = join_group(jaw_l, 'Jaw_L')
right = join_group(jaw_r, 'Jaw_R')
parts = [
    {'object':'Jaw_L','motion':'swing','axis':[0,0,1],'pivot':[-.53,-.39,.20],'speed':0.65,'amplitude':10},
    {'object':'Jaw_R','motion':'swing','axis':[0,0,-1],'pivot':[.53,-.39,.20],'speed':0.65,'amplitude':10},
]
(OUT / 'parts.json').write_text(json.dumps(parts, indent=2)+'\n', encoding='utf-8')
# Export the full model with separate objects and no camera or lighting.
bpy.ops.object.select_all(action='DESELECT')
for ob in (model,left,right): ob.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=str(OUT / 'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not scene.world else scene.world
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def point_at(ob, target):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name, kind); data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=location; point_at(ob,target)
light('Sun','SUN',(-3,-4,6),2.0,(0,-.25,0))
light('Soft fill','AREA',(4,1,5),350,(0,-.25,0),5)
camdata=bpy.data.cameras.new('Camera'); camera=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(camera); scene.camera=camera
camdata.type='ORTHO'; camdata.ortho_scale=2.48
camera.location=(0,-.295,6); camera.rotation_euler=(0,0,0)
scene.render.filepath=str(OUT / 'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50
camera.location=(3.4,-4.7,4.0); point_at(camera,(0,-.27,.15))
scene.render.filepath=str(OUT / 'angle.png'); bpy.ops.render.render(write_still=True)
