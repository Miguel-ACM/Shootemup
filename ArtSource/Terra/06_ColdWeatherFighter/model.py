import bpy
import math
import os
import sys
from mathutils import Vector

# Locate the deliverables even when run from Blender's text editor or CLI.
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    arg = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    ROOT = arg if os.path.isdir(arg) else os.path.dirname(arg)
os.makedirs(ROOT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

colors = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*rgb, 1.0)
    bs.inputs['Roughness'].default_value = 0.67
    bs.inputs['Metallic'].default_value = 0.12 if i not in (9, 10) else 0.0
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bs.inputs['Emission Strength'].default_value = 1.4
    mats.append(m)

parts = []
def mesh_obj(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def loft(name, rings, material):
    # Corresponding polygon rings make beveled, faceted armor with real depth.
    n = len(rings[0])
    assert all(len(r) == n for r in rings)
    verts = [p for ring in rings for p in ring]
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh_obj(name, verts, faces, material)

def armor(name, outline, z0, z1, inset, material):
    cx = sum(x for x,y in outline)/len(outline)
    cy = sum(y for x,y in outline)/len(outline)
    low = [(x,y,z0) for x,y in outline]
    edge = [(x,y,z1-0.045) for x,y in outline]
    top = [(cx+(x-cx)*inset, cy+(y-cy)*inset,z1) for x,y in outline]
    return loft(name, [low,edge,top], material)

def tube(name, x, front_y, back_y, z, outer, inner, material, sides=10):
    # Hollow annulus at the fixed front/rear endpoint; interior walls are real geometry.
    verts=[]
    for y,r in ((front_y,outer),(back_y,outer),(front_y,inner),(back_y,inner)):
        verts += [(x+r*math.cos(2*math.pi*i/sides), y,
                   z+r*math.sin(2*math.pi*i/sides)) for i in range(sides)]
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.extend([(i,j,sides+j,sides+i), (i,2*sides+i,2*sides+j,j),
                      (2*sides+i,3*sides+i,3*sides+j,2*sides+j),
                      (sides+i,sides+j,3*sides+j,3*sides+i)])
    return mesh_obj(name,verts,faces,material)

def disk(name,x,y,z,r,material,sides=10):
    verts=[(x+r*math.cos(2*math.pi*i/sides),y,z+r*math.sin(2*math.pi*i/sides)) for i in range(sides)]
    return mesh_obj(name,verts,[tuple(range(sides))],material)

# Stout central keel and snow-white, forward-hooked wings.
armor('keel', [(-.26,.61),(.26,.61),(.40,-.25),(.23,-.84),(0,-1.02),(-.23,-.84),(-.40,-.25)], -.15,.23,.88, 4)
for s in (-1,1):
    wing=[(s*.22,.26),(s*.78,.68),(s*1.25,.48),(s*1.01,-.43),
          (s*.78,-.61),(s*.81,-.07),(s*.35,-.30)]
    armor('hooked wing',wing,-.13,.13,.93,1)
    armor('outer ice blue inlay',[(s*.89,.30),(s*1.12,.34),(s*.98,-.20),(s*.84,-.34),(s*.87,-.04)],.125,.16,.92,4)
    armor('wing shoulder plate',[(s*.34,.20),(s*.75,.49),(s*.89,.41),(s*.73,.14),(s*.42,-.10)],.13,.185,.91,15)
    # Raised engine nacelle, terminating at the precise rear socket.
    armor('engine nacelle',[(s*.40,.10),(s*.70,.10),(s*.72,.69),(s*.65,.80),(s*.45,.80),(s*.38,.69)],-.16,.20,.91,6)
    armor('nacelle upper armor',[(s*.43,.22),(s*.67,.22),(s*.67,.65),(s*.61,.72),(s*.49,.72),(s*.43,.65)],.20,.245,.91,1)
    tube('rear nozzle',s*.55,.70,.80,0,.135,.086,6)
    disk('nozzle dark interior',s*.55,.765,0,.084,11)
    # Front face of each launcher sits exactly at the gameplay muzzle point.
    armor('gun housing',[(s*.43,-.67),(s*.67,-.67),(s*.69,-.28),(s*.61,-.19),(s*.47,-.19),(s*.41,-.28)],.06,.22,.92,6)
    tube('muzzle lip',s*.55,-.70,-.49,.20,.095,.057,11)
    disk('muzzle bore',s*.55,-.685,.20,.055,11)
    # Small pale cap along the wing's leading hook.
    armor('wing tip cap',[(s*.79,-.57),(s*1.01,-.40),(s*1.04,-.30),(s*.87,-.40)],.13,.16,.9,15)

# Faceted, dark-edged pressure canopy, broad enough to read at gameplay scale.
armor('canopy gasket',[(-.23,-.57),(.23,-.57),(.28,-.05),(.19,.22),(-.19,.22),(-.28,-.05)],.23,.275,.92,11)
loft('ice blue pressure cabin',[
    [(-.19,-.53,.27),(.19,-.53,.27),(.23,-.06,.27),(.15,.17,.27),(-.15,.17,.27),(-.23,-.06,.27)],
    [(-.12,-.45,.39),(.12,-.45,.39),(.15,-.08,.43),(.10,.10,.40),(-.10,.10,.40),(-.15,-.08,.43)]],4)
armor('dorsal spine',[(-.17,.53),(.17,.53),(.20,.22),(.14,.15),(-.14,.15),(-.20,.22)],.23,.31,.88,15)
armor('nose armor',[(-.18,-.84),(0,-.99),(.18,-.84),(.21,-.66),(-.21,-.66)],.23,.275,.86,1)

# Join only geometry; leave camera and lights out of the exported asset.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
# Triangulate explicitly for predictable budget and OBJ topology.
mod = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=mod.name)
triangles = len(model.data.polygons)
assert 300 <= triangles <= 2500, 'Triangle count outside budget: %d' % triangles
bpy.ops.wm.obj_export(filepath=os.path.join(ROOT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray') if scene.world is None else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind == 'AREA':
        data.shape='DISK'
        data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj,target)
    return obj

sun=light('Upper left sun','SUN',(-4,-5,8),2.0,(0,0,0))
sun.data.angle=math.radians(12)
light('Soft fill','AREA',(4,2,5),180,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,7)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=2.95
scene.render.filepath=os.path.join(ROOT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(3.5,-4.8,4.2)
aim(cam,(0,-.10,.05))
camdata.type='PERSP'
camdata.lens=52
scene.render.filepath=os.path.join(ROOT,'angle.png')
bpy.ops.render.render(write_still=True)
print('Exported Model: %d triangles' % triangles)
