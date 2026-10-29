import bpy, math, os, sys
from mathutils import Vector

# Blender --background --python model.py [-- /path/to/output-directory]
def output_dir():
    if '__file__' in globals():
        return os.path.dirname(os.path.abspath(__file__))
    args = sys.argv
    if '--' in args and len(args) > args.index('--') + 1:
        p = os.path.abspath(args[args.index('--') + 1])
        return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()
OUT = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

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
    bs.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*c, 1)
        bs.inputs['Emission Strength'].default_value = 1.4
    mats.append(m)

parts = []
def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[mat])
    parts.append(ob)
    return ob

def layered(name, outline, z0, z1, inset, mat):
    # A faceted bevel around an arbitrary convex planform.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    top = [(cx+(x-cx)*inset, cy+(y-cy)*inset, z1) for x,y in outline]
    lower = [(x,y,z0) for x,y in outline]
    shoulder = [(x,y,z1-(z1-z0)*0.38) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    faces += [(i+n,(i+1)%n+n,(i+1)%n+2*n,i+2*n) for i in range(n)]
    faces += [tuple(range(2*n,3*n))]
    return mesh(name, lower+shoulder+top, faces, mat)

def ellipsoid(name, pos, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, location=pos)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mats[mat])
    parts.append(ob)
    return ob

def annulus_y(name, x, y, z, outer, inner, depth, mat, back_mat=11):
    # Mouth plane is exactly (x,y,z); the lip recedes into the hull in +Y.
    n=12
    v=[]
    for yy,r in [(y,outer),(y,inner),(y+depth,inner),(y+depth,outer)]:
        for i in range(n):
            a=2*math.pi*i/n
            v.append((x+math.cos(a)*r, yy, z+math.sin(a)*r))
    f=[]
    for j in range(3):
        for i in range(n):
            k=(i+1)%n
            f.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    f.append(tuple(range(3*n,4*n)))
    ob=mesh(name,v,f,mat)
    ob.data.materials.append(mats[back_mat])
    for poly in ob.data.polygons:
        if poly.index >= n and poly.index < 2*n or poly.index == len(f)-1:
            poly.material_index=1
    return ob

def top_ring(name,x,y,z,ro,ri,depth,mat):
    n=12
    v=[]
    for zz,r in [(z,ro),(z,ri),(z-depth,ri)]:
        for i in range(n):
            a=2*math.pi*i/n
            v.append((x+r*math.cos(a),y+r*math.sin(a),zz))
    f=[]
    for j in range(2):
        for i in range(n):
            k=(i+1)%n
            f.append((j*n+i,j*n+k,(j+1)*n+k,(j+1)*n+i))
    f.append(tuple(range(2*n,3*n)))
    ob=mesh(name,v,f,mat)
    ob.data.materials.append(mats[11])
    for p in ob.data.polygons:
        if p.index>=n: p.material_index=1
    return ob

# The broad octagonal keel and raised, pointed centerline retain the source outline.
layered('armored keel',[(-1.22,.42),(-1.08,-.46),(-.56,-.76),(.56,-.76),(1.08,-.46),(1.22,.42),(.57,.70),(-.57,.70)],-.12,.22,.88,4)
layered('central spine',[(-.24,.65),(.24,.65),(.25,-.67),(0,-.98),(-.25,-.67)],.19,.43,.84,1)
layered('nose armor',[(-.20,-.59),(.20,-.59),(0,-.91)],.405,.47,.80,15)
# Angular opaque cockpit panes, ice-blue like the original glass.
layered('cockpit frame',[(-.19,-.26),(-.16,-.55),(0,-.68),(.16,-.55),(.19,-.26)],.40,.51,.80,11)
layered('cockpit glass',[(-.145,-.29),(-.12,-.53),(0,-.61),(.12,-.53),(.145,-.29)],.485,.525,.86,8)
layered('rear spine hatch',[(-.15,.50),(.15,.50),(.17,-.05),(-.17,-.05)],.42,.46,.83,6)
layered('hatch inset',[(-.105,.40),(.105,.40),(.105,.04),(-.105,.04)],.46,.475,.88,11)

for s in (-1,1):
    # Bulging pressure vessels dominate the plan view; layered shoulder plates make them armored.
    ellipsoid('payload vessel',(s*.69,-.17,.22),(.43,.61,.30),15)
    panel=[(s*.40,.28),(s*.45,.48),(s*.99,.49),(s*1.13,.31),(s*1.06,.13),(s*.50,.13)]
    if s < 0: panel.reverse()
    layered('vessel shoulder',panel,.34,.43,.88,4)
    band=[(s*.43,.10),(s*.97,.13),(s*.97,.22),(s*.43,.19)]
    if s < 0: band.reverse()
    layered('retaining band',band,.476,.495,.94,6)
    # Recessed shell-loading hatches visible from above, with a warm inset.
    top_ring('shell hatch',s*.69,-.38,.505,.175,.115,.035,6)
    top_ring('shell hatch inner',s*.69,-.38,.482,.077,.047,.008,12)
    # Launcher rims terminate at the exact gameplay muzzle positions.
    annulus_y('launcher lip',s*.88,-.72,.32,.115,.068,.18,6)
    # Twin motor housings reach the original sockets at y=.86.
    pod=[(s*.66,.43),(s*.66,.76),(s*.75,.86),(s*1.01,.86),(s*1.10,.75),(s*1.10,.43)]
    if s < 0: pod.reverse()
    layered('motor pod',pod,-.12,.22,.86,1)
    # Rear-facing nozzle; its outer face is exactly at the socket, with no flame geometry.
    # Reverse the geometry along Y so the recess extends toward the nose.
    ob=annulus_y('engine socket',s*.88,.86,0,.115,.073,-.10,6)

# One export object, with flat face normals and baked object transforms.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts: ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons: poly.use_smooth=False
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
scene.world=bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def point_at(ob, target):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, typ, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,typ)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data)
    scene.collection.objects.link(ob)
    ob.location=loc
    point_at(ob,target)
    return ob
light('Sun upper left','SUN',(-4,-5,8),2.0,(0,0,0))
light('Soft fill','AREA',(4,2,5),250,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
scene.collection.objects.link(cam)
scene.camera=cam
camdata.type='ORTHO'
camdata.ortho_scale=2.90
cam.location=(0,0,8)
cam.rotation_euler=(0,0,0)
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(3.6,-5.5,4.7)
point_at(cam,(0,0,.1))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
