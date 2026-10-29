import bpy
import math
import os
import sys
from mathutils import Vector

# Outputs are placed beside this script, including when invoked with -- <directory>.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*rgb, 1)
    bs.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*rgb, 1)
        bs.inputs['Emission Strength'].default_value = 1.4
    mats.append(m)

parts = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.append(mats[material])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

def layered(name, lower, upper, z0, z1, material):
    # Corresponding contours make chamfered, genuinely volumetric armor.
    n = len(lower)
    verts = [(x,y,z0) for x,y in lower] + [(x,y,z1) for x,y in upper]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, material)

def plate(name, outline, z, thick, material, inset=0.88):
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    top = [(cx+(x-cx)*inset,cy+(y-cy)*inset) for x,y in outline]
    return layered(name, outline, top, z, z+thick, material)

def ring_y(name, x, y, z, radius, inner, depth, material, interior=11, segments=10, direction=-1):
    # The lip is exactly on y; the tube extends into the craft.
    vs=[]
    for yy, rr in [(y,radius),(y,inner),(y-direction*depth,radius*0.92),(y-direction*depth,inner*0.85)]:
        for i in range(segments):
            a=2*math.pi*i/segments + math.pi/segments
            vs.append((x+rr*math.cos(a), yy, z+rr*math.sin(a)))
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                      (segments+i,segments+j,3*segments+j,3*segments+i)])
    obj=mesh(name,vs,faces,material)
    obj.data.materials.append(mats[interior])
    for i,p in enumerate(obj.data.polygons):
        if i%3==2: p.material_index=1
    # A dark recessed back gives each opening an actual visible bore.
    yy=y-direction*depth*0.88
    disc=[(x,yy,z)]+[(x+inner*.85*math.cos(2*math.pi*i/segments+math.pi/segments),yy,z+inner*.85*math.sin(2*math.pi*i/segments+math.pi/segments)) for i in range(segments)]
    mesh(name+' bore',disc,[(0,i+1,(i+1)%segments+1) for i in range(segments)],interior)

def mirror(points, side):
    p=[(side*x,y) for x,y in points]
    return p if side>0 else list(reversed(p))

# Main cranked wing, with a thick dark lower edge and chamfered upper armor.
wing=[(-1.52,.72),(-1.22,-.40),(-.48,-.82),(.48,-.82),(1.22,-.40),(1.52,.72),(.35,.28),(-.35,.28)]
wingtop=[(-1.39,.61),(-1.12,-.34),(-.43,-.72),(.43,-.72),(1.12,-.34),(1.39,.61),(.32,.23),(-.32,.23)]
layered('Wing structure',wing,wingtop,-.12,.20,2)
# Distinct raised spine and pointed armored nose.
body=[(-.30,.91),(-.40,-.50),(0,-1.20),(.40,-.50),(.30,.91)]
bodytop=[(-.23,.80),(-.30,-.46),(0,-1.07),(.30,-.46),(.23,.80)]
layered('Armored fuselage',body,bodytop,-.08,.42,0)
plate('Nose shield',[(-.25,-.50),(0,-1.10),(.25,-.50),(.20,-.33),(-.20,-.33)],.421,.035,6,.90)
# Faceted opaque cockpit, black surround and blue-green glazing.
plate('Canopy frame',[(-.24,-.48),(-.22,-.10),(-.14,.10),(.14,.10),(.22,-.10),(.24,-.48),(0,-.72)],.44,.09,11,.83)
plate('Canopy glass',[(-.16,-.44),(-.15,-.12),(-.10,.035),(.10,.035),(.15,-.12),(.16,-.44),(0,-.61)],.535,.055,4,.83)
plate('Dorsal spine',[(-.15,.12),(-.18,.70),(0,.83),(.18,.70),(.15,.12)],.43,.075,6,.78)
plate('Spine intake',[(-.11,.31),(-.12,.61),(.12,.61),(.11,.31)],.51,.025,11,.92)

for s in (-1,1):
    # Swept inset panels accentuate the broad wing and preserve the original olive scheme.
    plate('Wing inset',mirror([(.44,-.38),(1.14,-.28),(1.38,.53),(.49,.20)],s),.205,.055,14,.90)
    plate('Wing tip cap',mirror([(1.13,-.31),(1.28,-.36),(1.52,.72),(1.34,.62)],s),.21,.05,0,.90)
    plate('Gun shoulder',mirror([(.43,-.79),(.92,-.68),(.98,.05),(.46,-.08)],s),.25,.13,6,.83)
    # Long, heavy pods retain the source's twin-engine aircraft identity.
    pod=mirror([(.65,-.29),(1.03,-.29),(1.08,.52),(.98,.68),(.72,.68),(.62,.48)],s)
    podtop=mirror([(.70,-.20),(.98,-.20),(1.02,.48),(.94,.60),(.75,.60),(.68,.45)],s)
    layered('Motor nacelle',pod,podtop,.08,.37,0)
    plate('Motor upper armor',mirror([(.71,.02),(.98,.02),(.98,.47),(.91,.55),(.77,.55),(.71,.45)],s),.372,.055,2,.86)
    plate('Forward intake',mirror([(1.08,-.04),(1.27,-.03),(1.30,.35),(1.12,.35)],s),.255,.045,11,.85)
    plate('Intake lip',mirror([(1.09,-.06),(1.29,-.04),(1.29,.02),(1.09,.02)],s),.31,.025,6,.9)
    # Recessed muzzle front is centered on the gameplay point.
    ring_y('Cannon muzzle',s*.68,-1.20,.30,.115,.068,.22,6,11,10,-1)
    ring_y('Engine socket',s*.85,.67,0,.145,.098,.18,6,11,10,1)
    plate('Gun housing',mirror([(.55,-.93),(.81,-.93),(.83,-.42),(.53,-.42)],s),.27,.11,0,.80)
    plate('Gun marking',mirror([(.57,-.47),(.79,-.47),(.79,-.40),(.57,-.40)],s),.39,.018,3,.92)
    # One angular tail fin on each shoulder; silhouette remains inside wing span.
    layered('Tail fin',mirror([(1.11,.31),(1.26,.34),(1.31,.65),(1.16,.62)],s),
            mirror([(1.15,.38),(1.22,.39),(1.25,.62),(1.18,.59)],s),.27,.48,2)

# Join before introducing render-only camera and lights.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Explicit triangulation guarantees a predictable game mesh and budget.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert len(model.data.polygons) <= 4000, 'Triangle budget exceeded'
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def aim(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None and kind=='AREA': data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj,target)
    return obj

sun=light('Upper left sun','SUN',(-4,-4,7),2.0,(0,0,0))
sun.data.angle=math.radians(18)
light('Soft fill','AREA',(4,2,5),350,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,8)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=3.58
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=48
cam.location=(4.0,-5.2,4.5)
aim(cam,(0,-.12,.08))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
