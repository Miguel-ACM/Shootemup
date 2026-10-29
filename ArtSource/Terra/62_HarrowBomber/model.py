import bpy
import math
import json
import os
import sys
from mathutils import Vector

# The command-line fallback accepts the output directory or this script's path.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    p = os.path.abspath(args[0]) if args else os.getcwd()
    OUT = p if os.path.isdir(p) else os.path.dirname(p)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(mat)

body = []
def mesh(name, verts, faces, material, indices=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        data.materials.append(m)
    for poly in data.polygons:
        poly.material_index = material if indices is None else indices[poly.index]
        poly.use_smooth = False
    body.append(obj)
    return obj

def loft(name, rings, material, side=None):
    # Rings have matching perimeter vertices; winding corrected below via bmesh.
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    obj = mesh(name, verts, faces, material)
    if side is not None:
        for p in obj.data.polygons[1:-1]: p.material_index = side
    import bmesh
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data); bm.free()
    return obj

def plate(name, outline, bottom, shoulder, top, mat, side):
    cx = sum(x for x,y in outline)/len(outline)
    cy = sum(y for x,y in outline)/len(outline)
    def ring(scale,z):
        return [(cx+(x-cx)*scale, cy+(y-cy)*scale, z) for x,y in outline]
    return loft(name, [ring(.88,bottom),ring(1,shoulder),ring(.83,top)], mat, side)

def box(name, loc, scale, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object; o.name = name
    o.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Cut corners', 'BEVEL'); mod.width = bevel; mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.append(mats[mat]); body.append(o)
    return o

def tube(name, x, y, z, radius, length, material, bore=11):
    # Visible open annulus at the forward end, with a recessed dark cavity.
    n=8; inner=radius*.62; front=y-length/2; back=y+length/2
    verts=[]
    for yy, rr in ((back,radius*.90),(front,radius),(front,inner),(front+length*.38,inner)):
        verts += [(x+rr*math.cos(2*math.pi*i/n), yy, z+rr*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]; ids=[]
    for j in range(3):
        for i in range(n):
            faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i)); ids.append(material if j<2 else bore)
    faces.append(tuple(3*n+i for i in range(n))); ids.append(bore)
    return mesh(name,verts,faces,material,ids)

# Two separate armored pontoons: characteristic swept, tapered harrow shape.
for s, label in ((-1,'L'),(1,'R')):
    outline=[(s*.24,-.48),(s*.53,-.75),(s*.92,-.55),
             (s*1.06,.46),(s*.73,.88),(s*.34,.54)]
    plate('Pod_'+label, outline, -.10, .035, .25, 2, 0)
    inset=[(s*.36,-.40),(s*.55,-.62),(s*.84,-.48),
           (s*.94,.38),(s*.73,.68),(s*.43,.46)]
    plate('Upper armor_'+label, inset, .235,.255,.30, 14, 2)
    # Broad dark bomb bay / gun channel, integrated into the nose.
    box('Launcher housing_'+label,(s*.72,-.345,.245),(.29,.47,.16),0,.045)
    box('Launcher inset_'+label,(s*.72,-.37,.331),(.19,.31,.018),11,.025)
    # Mouth front is exactly at the gameplay muzzle position (-.58, z .23).
    tube('Muzzle_'+label,s*.72,-.505,.23,.082,.15,6)
    # Rear motor cowl, with opening centered on the specified socket.
    box('Motor cowl_'+label,(s*.70,.63,.14),(.32,.37,.27),0,.055)
    # Annulus faces the stern (+Y): reverse the tube along its Y axis.
    o=tube('Socket nozzle_'+label,s*.70,.64,.04,.105,.14,6)
    # Reflect in local Y around the socket so the mouth faces rearward.
    for v in o.data.vertices: v.co.y = 1.28-v.co.y
    o.data.flip_normals()
    # Characterful diagonal shoulder panel, not rows of small greebles.
    slash=[(s*.83,.05),(s*.91,.20),(s*.87,.43),(s*.79,.32)]
    plate('Shoulder stripe_'+label,slash,.301,.305,.315,0,0)

# Structural crossbar and raised central glazed cockpit.
box('Bridge',(0,.22,.08),(1.12,.29,.24),6,.045)
box('Bridge spine',(0,.22,.22),(.52,.32,.16),0,.045)
cockpit_bottom=[(-.23,-.06,.23),(.23,-.06,.23),(.28,.30,.23),(.17,.49,.23),(-.17,.49,.23),(-.28,.30,.23)]
cockpit_top=[(-.15,.02,.39),(.15,.02,.39),(.17,.27,.39),(.11,.37,.39),(-.11,.37,.39),(-.17,.27,.39)]
loft('Cockpit', [cockpit_bottom,cockpit_top], 8, 11)
# A single light-colored brow reinforces the front-facing direction.
plate('Cockpit brow',[(-.20,-.06),(.20,-.06),(.15,.015),(-.15,.015)],.25,.27,.295,6,0)

# Join all stationary geometry, apply transforms and keep an empty part list.
bpy.ops.object.select_all(action='DESELECT')
for obj in body: obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons: poly.use_smooth = False
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([], f, indent=2)

bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if scene.world is None else scene.world
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1

def aim(obj, point):
    obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if kind=='AREA': data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    obj.location=location; aim(obj,target)
    return obj
sun=light('Sun','SUN',(-4,-5,7),2.0,(0,0,0)); sun.data.angle=math.radians(18)
light('Soft fill','AREA',(4,2,5),160,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,6); cam.rotation_euler=(0,0,0) # camera local -Z, +Y remains screen up
camdata.type='ORTHO'; camdata.ortho_scale=2.55
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50
cam.location=(3.25,-4.1,3.5); aim(cam,(0,.05,.10))
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)


