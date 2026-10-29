import bpy, math, os, sys, json
from mathutils import Vector

# Blender 5.x: run with blender --background --python model.py [-- OUTPUT_DIR]
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1:
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
else:
    OUT = os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in list(bpy.data.materials):
    bpy.data.materials.remove(datablock)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.105,0.15,0.20),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.48,0.36,0.18),(0.53,0.61,0.55),
          (0.66,0.70,0.71)]
# The Space atlas's solar-cell tile is blue; grid is suggested geometrically.
colors[7] = (0.18, 0.25, 0.42)
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 2.0
    mats.append(mat)

body = []
def mesh(name, verts, faces, mat, group=body):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[mat])
    group.append(obj)
    return obj

def tier(name, outline, lower, upper, inset, mat, group=body):
    # Beveled polygon with a contracted raised top.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    verts = [(x,y,lower) for x,y in outline]
    verts += [(cx+(x-cx)*inset, cy+(y-cy)*inset, upper) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, mat, group)

def box(name, loc, scale, mat, group=body, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Edge chamfers', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    group.append(obj)
    return obj

def cylinder(name, loc, radius, depth, mat, vertices=12, rotation=(0,0,0), group=body):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[mat])
    group.append(ob)
    return ob

def ring_y(name, x, y, z, outer, inner, depth, mat):
    # Annular muzzle: the forward face is precisely at the gameplay point.
    n=12
    verts=[]
    for yy, rad in [(y,outer),(y,inner),(y+depth,outer),(y+depth,inner)]:
        verts += [(x+rad*math.cos(2*math.pi*i/n), yy, z+rad*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),
                      (n+i,n+j,3*n+j,3*n+i),(2*n+i,3*n+i,3*n+j,2*n+j)])
    mesh(name, verts, faces, mat)

# A deep, faceted core and raised armored crown.
outline=[(0,-.51),(-.27,-.10),(-.22,.30),(.09,.52),(.31,.19),(.22,-.20)]
tier('Lower keel', outline, -.13, .08, .99, 6)
tier('Ivory armored body', outline, .065, .24, .76, 15)
tier('Crown plate', [(-.17,-.10),(-.15,.22),(.08,.39),(.20,.17),(.14,-.10),(0,-.28)], .235, .285, .88, 1)
# Small navy weapon saddle, with mouth exactly at y=-.34.
box('Weapon saddle', (0,-.205,.27), (.16,.27,.10), 6, bevel=.025)
ring_y('Muzzle lip', 0,-.34,.30,.075,.043,.075,13)
cylinder('Recessed bore', (0,-.332,.30), .042, .008, 11, 12, (math.pi/2,0,0))
# Raised sensor core: concentric, distinct but compact.
cylinder('Sensor base', (0,.13,.295), .145, .06, 13)
cylinder('Sensor bezel', (0,.13,.33), .108, .035, 6)
cylinder('Sensor lens', (0,.13,.35), .070, .012, 9)

parts=[]
for s, suffix in [(-1,'L'),(1,'R')]:
    # Gold root spars sit under the hinged blue solar fins.
    box('Root spar '+suffix, (s*.40,.16,.055), (.43,.075,.085), 13, bevel=.018)
    cylinder('Hinge '+suffix, (s*.34,.21,.10), .065, .12, 6, 10, (math.pi/2,0,0))
    group=[]
    # Swept four-corner fins maintain the original asymmetric kite planform.
    poly=[(s*.31,-.10),(s*.82,.37),(s*.55,.69),(s*.37,.40)]
    tier('Fin frame '+suffix, poly, .06, .13, .98, 13, group)
    inset=[(s*.36,-.025),(s*.75,.38),(s*.55,.61),(s*.405,.39)]
    tier('Solar surface '+suffix, inset, .13, .155, .98, 7, group)
    # One broad diagonal divider and an ivory tip are readable at game scale.
    strip=[(s*.48,.145),(s*.505,.17),(s*.645,.47),(s*.62,.46)]
    tier('Cell division '+suffix, strip, .155, .161, 1.0, 4, group)
    tip=[(s*.73,.38),(s*.79,.37),(s*.55,.66),(s*.53,.61)]
    tier('Fin tip '+suffix, tip, .13, .165, .98, 15, group)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group: ob.select_set(True)
    bpy.context.view_layer.objects.active = group[0]
    bpy.ops.object.join()
    fin=bpy.context.object
    fin.name='Fin_'+suffix
    # Leave object origin at the actual hinge for engines that use it.
    pivot=Vector((s*.34,.21,.10))
    for v in fin.data.vertices: v.co -= pivot
    fin.location = pivot
    parts.append((fin, {'object':fin.name,'motion':'swing','axis':[0,s,0],
                        'pivot':list(pivot),'speed':1.4,'amplitude':12}))

bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for obj, _ in parts:
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    # Geometry remains at rest in world space; origin is the animation pivot.
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
for obj in [model]+[p[0] for p in parts]:
    for face in obj.data.polygons: face.use_smooth=False

with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([p[1] for p in parts],f,indent=2)
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
for obj,_ in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=model
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
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'

def aim(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=1):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind=='AREA': data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=loc
    aim(obj,target)
    return obj
light('Sun','SUN',(-3,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(3,2,4),100,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,5)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=1.92
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(2.5,-3.3,3.0)
aim(cam,(0,.05,.07))
camdata.type='PERSP'
camdata.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
