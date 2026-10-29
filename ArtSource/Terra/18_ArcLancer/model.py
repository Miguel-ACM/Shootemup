import bpy
import math
import os
import sys
from mathutils import Vector

# Output follows the script, including when launched with blender --python ... -- path.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script or output directory after --')
    p = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    OUT = p if os.path.isdir(p) else os.path.dirname(p)

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
for i, c in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*c, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1)
    bs.inputs['Roughness'].default_value = 0.68
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*c, 1)
        bs.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

parts = []
def mesh(name, verts, faces, mat):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

def plate(name, outline, zbottom, zedge, ztop, inset, mat):
    # Three rings make the edge bevel and a raised, faceted upper surface.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    v = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline]
    v += [(cx+(x-cx)*inset, cy+(y-cy)*inset, ztop) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    for j in range(2):
        for i in range(n):
            k = (i+1)%n
            faces.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    faces.append(tuple(range(2*n,3*n)))
    return mesh(name,v,faces,mat)

def tube(name, x, y_front, y_back, z, outer, inner, mat, inside=11, segments=10):
    # Annular front lip, inner bore, and dark recessed back; oriented along Y.
    v=[]
    for y,r in [(y_front,outer),(y_back,outer),(y_front,inner),(y_front+(y_back-y_front)*0.62,inner)]:
        for i in range(segments):
            a=2*math.pi*i/segments
            v.append((x+math.cos(a)*r,y,z+math.sin(a)*r))
    f=[]
    for i in range(segments):
        j=(i+1)%segments
        f.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                  (2*segments+i,3*segments+i,3*segments+j,2*segments+j)])
    f.append(tuple(range(3*segments,4*segments)))
    ob=mesh(name,v,f,mat)
    ob.data.materials.append(mats[inside])
    for poly in ob.data.polygons:
        if poly.index % 3 == 2 or poly.index == len(f)-1:
            poly.material_index=1
    return ob

# Main fuselage and its raised dorsal facets.
plate('violet keel', [(-.38,.70),(.20,.68),(.34,.20),(.30,-.27),(.06,-.84),(-.43,-.57),(-.49,.10)], -.16,.08,.25,.78,7)
plate('spine', [(-.28,.55),(.13,.52),(.20,.04),(.07,-.48),(-.13,-.66),(-.32,-.42)], .20,.25,.34,.73,4)
plate('cockpit surround', [(-.28,.12),(-.19,-.43),(-.06,-.58),(.09,-.35),(.11,.06),(-.05,.19)], .29,.32,.39,.80,11)
plate('cockpit glass', [(-.22,.06),(-.14,-.32),(-.06,-.43),(.045,-.30),(.055,.035),(-.06,.12)], .375,.39,.43,.76,8)
# Swept counterweight wing and inset inlet, with a substantial tapered profile.
plate('port wing', [(-.32,.32),(-.95,.53),(-.86,-.17),(-.49,-.49),(-.31,-.32)], -.12,.07,.19,.79,7)
plate('wing leading armor', [(-.90,.43),(-.40,.26),(-.40,.13),(-.82,.20)], .13,.17,.205,.85,6)
plate('inlet rim', [(-.76,.12),(-.53,.04),(-.48,-.25),(-.68,-.23)], .18,.205,.235,.82,11)
plate('inlet inner', [(-.70,.055),(-.57,.01),(-.54,-.16),(-.66,-.16)], .23,.235,.245,.90,4)
# The off-centre steel rail forms the defining long lance silhouette.
plate('lance chassis', [(.16,.17),(.70,.40),(.84,-.48),(.54,-1.48),(.34,-1.18),(.38,-.42)], -.13,.08,.22,.84,6)
plate('lance upper armor', [(.30,.16),(.67,.29),(.74,-.43),(.57,-1.08),(.40,-1.16),(.46,-.38)], .18,.22,.29,.84,4)
plate('rail channel', [(.43,.12),(.63,.13),(.66,-.47),(.54,-1.16),(.44,-1.03)], .285,.30,.315,.90,11)
plate('rail conductor', [(.505,.04),(.565,.045),(.58,-.50),(.52,-.91),(.49,-.89)], .31,.318,.327,.93,9)
# Two restrained capacitor collars straddle the rail, rather than tiny surface buttons.
for y in (-.28,-.65):
    plate('capacitor bridge', [(.42,y+.055),(.66,y+.055),(.65,y-.055),(.43,y-.055)], .31,.33,.36,.88,7)
    plate('bridge contact', [(.465,y+.028),(.615,y+.028),(.61,y-.028),(.47,y-.028)], .355,.36,.37,.92,13)
# Twin rear motor pods terminate in the gameplay socket planes at y=.91, z=0.
for x in (-.42,.42):
    plate('motor housing', [(x-.17,.30),(x+.17,.30),(x+.17,.77),(x+.11,.89),(x-.11,.89),(x-.17,.77)], -.14,.08,.21,.78,7)
    plate('motor cap', [(x-.12,.36),(x+.12,.36),(x+.12,.70),(x+.08,.79),(x-.08,.79),(x-.12,.70)], .19,.21,.26,.82,6)
    tube('engine socket',x,.91,.75,0,.115,.073,6,11)
# The muzzle opening is centred exactly on the weapon point (.52,-1.45,+.20).
tube('rail muzzle', .52,-1.45,-1.22,.20,.105,.064,6,11,12)
# A small top-facing sight marks the weapon tip without resembling a projectile.
plate('muzzle sight', [(.49,-1.38),(.55,-1.38),(.55,-1.30),(.49,-1.30)], .285,.29,.30,.9,9)

# Consolidate and triangulate for a predictable, flat-shaded exported asset.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri=model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth=False
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
scene.world=bpy.data.worlds.new('Dark blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def aim(obj, point):
    obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None:
        data.shape='DISK'
        data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=loc
    aim(obj,target)

light('Sun upper left','SUN',(-4,-4,7),2.0,(0,-.25,0))
light('Soft fill','AREA',(4,1,5),280,(0,-.25,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
camdata.type='ORTHO'
camdata.ortho_scale=2.82
cam.location=(0,-.28,6)
cam.rotation_euler=(0,0,0)
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(3.5,-5.2,4.6)
aim(cam,(0,-.28,.05))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
