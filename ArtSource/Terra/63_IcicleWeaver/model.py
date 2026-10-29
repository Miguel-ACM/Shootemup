import bpy
import math
import os
import sys
import json
from mathutils import Vector

# Icicle Weaver: faceted winter siege craft, dimensions in gameplay units.
def script_dir():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            p = os.path.abspath(args[0])
            return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()

OUT = script_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body = []
def mesh(name, verts, faces, mat, face_mats=None, collection=body):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.materials.clear()
    for m in mats:
        me.materials.append(m)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    for p in me.polygons:
        p.material_index = mat if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    collection.append(ob)
    return ob

def plate(name, outline, inset, zbottom, zedge, ztop, side, bevel, top):
    # Matching perimeter and inset make a closed, sloping armored shell.
    n = len(outline)
    verts = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline] + [(x,y,ztop) for x,y in inset]
    faces = [tuple(reversed(range(n)))]
    indices = [side]
    for k in range(n):
        j = (k+1)%n
        faces.append((k,j,n+j,n+k)); indices.append(side)
        faces.append((n+k,n+j,2*n+j,2*n+k)); indices.append(bevel)
    faces.append(tuple(range(2*n,3*n))); indices.append(top)
    return mesh(name, verts, faces, side, indices)

def ring_y(name, x, y, z, radius, inner, depth, outermat, innermat=11):
    # Front rim is exactly at y; the dark recessed bore faces that end.
    n = 10
    direction = 1 if depth > 0 else -1
    rings = [(radius,y), (radius*0.93,y+depth), (inner,y), (inner*0.85,y+direction*0.065)]
    v = []
    for r, yy in rings:
        for i in range(n):
            a = 2*math.pi*i/n
            v.append((x+r*math.cos(a), yy, z+r*math.sin(a)))
    f=[]; mi=[]
    for i in range(n):
        j=(i+1)%n
        f.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)])
        mi.extend([outermat,6,innermat])
    f.append(tuple(3*n+i for i in range(n))); mi.append(innermat)
    return mesh(name,v,f,outermat,mi)

# Deep central keel, crowned with a smaller ridged dorsal plate.
plate('Keel', [(-.20,-.45),(0,-.77),(.20,-.45),(.25,.68),(-.25,.68)],
      [(-.13,-.40),(0,-.63),(.13,-.40),(.17,.57),(-.17,.57)],
      -.10,.12,.28, 0,4,7)
plate('Spine armor', [(-.14,-.42),(0,-.68),(.14,-.42),(.15,.44),(-.15,.44)],
      [(-.08,-.38),(0,-.56),(.08,-.38),(.09,.36),(-.09,.36)],
      .24,.27,.34, 4,6,15)
# Symmetric swept plates preserve the original wide rear shoulders and forward tines.
for s in (-1,1):
    def mirror(poly):
        pts=[(s*x,y) for x,y in poly]
        return pts if s==1 else list(reversed(pts))
    plate('Swept wing', mirror([(.15,.12),(.85,.73),(.62,-.73),(.46,-.78),(.38,-.06)]),
          mirror([(.22,.13),(.75,.58),(.56,-.62),(.49,-.66),(.43,-.04)]),
          -.075,.075,.17, 0,6,15)
    # Raised ice-blue inset follows the wing rather than scattering small details.
    plate('Wing inset', mirror([(.28,.13),(.70,.48),(.55,-.48),(.49,-.52),(.46,-.04)]),
          mirror([(.32,.13),(.64,.37),(.53,-.39),(.51,-.41),(.49,-.03)]),
          .169,.175,.205, 4,4,1)
    # A single diagonal ridge gives the loom its interwoven structure.
    plate('Loom rib', mirror([(.27,.18),(.32,.22),(.64,-.42),(.59,-.47)]),
          mirror([(.29,.18),(.32,.19),(.62,-.42),(.60,-.44)]),
          .20,.21,.245, 4,6,6)
    # Muzzle front plane centered exactly on the gameplay muzzle.
    ring_y('Launcher', s*.55, -.78, .18, .105, .063, .30, 4)
    # Engine opening centered at the specified socket, facing rearward (+Y).
    ring_y('Engine socket', s*.37, .59, 0.0, .115, .071, -.20, 6)
    # Small raised rear armor around the motor.
    plate('Motor shroud', mirror([(.26,.35),(.47,.39),(.48,.57),(.37,.67),(.27,.59)]),
          mirror([(.29,.38),(.44,.41),(.44,.55),(.37,.61),(.30,.56)]),
          .03,.12,.20, 4,6,1)

# Dark faceted canopy with a pale armored frame: entirely opaque atlas colors.
plate('Canopy frame', [(-.145,-.13),(0,-.23),(.145,-.13),(.13,.27),(0,.37),(-.13,.27)],
      [(-.105,-.10),(0,-.17),(.105,-.10),(.095,.23),(0,.30),(-.095,.23)],
      .32,.34,.39, 4,6,11)
plate('Canopy glass', [(-.09,-.08),(0,-.14),(.09,-.08),(.08,.20),(0,.26),(-.08,.20)],
      [(-.055,-.055),(0,-.105),(.055,-.055),(.05,.17),(0,.22),(-.05,.17)],
      .39,.40,.45, 8,4,4)

# The craft has no naturally moving mechanism; the wings and weapons are fixed.
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Explicit triangulation ensures the exported low-poly facets remain stable.
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, location, energy, target, size=1):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if kind == 'AREA': data.shape='DISK'; data.size=size
    if kind == 'SUN': data.angle=math.radians(18)
    ob=bpy.data.objects.new(name,data); scene.collection.objects.link(ob)
    ob.location=location; aim(ob,target)
    return ob
light('Sun', 'SUN', (-4,-5,7), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 240, (0,0,0), 5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
scene.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=2.05
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(2.7,-3.7,3.2)
aim(cam,(0,0,.09))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
