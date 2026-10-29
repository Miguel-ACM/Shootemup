import bpy, math, os, sys
from mathutils import Vector

# Output beside this script; -- may supply either the script path or its directory.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    p = os.path.abspath(args[args.index('--') + 1])
    OUT = p if os.path.isdir(p) else os.path.dirname(p)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
materials = []
for i, c in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*c, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1)
    bs.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*c, 1)
        bs.inputs['Emission Strength'].default_value = 1.5
    materials.append(m)

parts = []
def mesh(name, verts, faces, mat, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in materials:
        data.materials.append(m)
    for p in data.polygons:
        p.material_index = mat if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    parts.append(obj)
    return obj

def loft(name, rings, mat):
    # Each ring has the same vertex count; closed angular sections make real volume.
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, mat)

def section(y, w, bottom, shoulder, top):
    return [(-w*.65,y,bottom),(w*.65,y,bottom),(w,y,shoulder),
            (w*.62,y,top),(0,y,top+.025),(-w*.62,y,top),(-w,y,shoulder)]

def plate(name, outline, z0, z1, mat):
    n = len(outline)
    return loft(name, [[(x,y,z0) for x,y in outline], [(x,y,z1) for x,y in outline]], mat)

def tube_y(name, x, yfront, yback, z, outer, inner, mat, inside=11, segments=12):
    # Annular, open-ended lip with a dark recessed bore, axis along Y.
    verts=[]
    for y, r in [(yfront,outer),(yback,outer),(yfront,inner),(yback,inner)]:
        for i in range(segments):
            a=2*math.pi*i/segments
            verts.append((x+r*math.cos(a),y,z+r*math.sin(a)))
    faces=[]; mids=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i),
                      (i,2*segments+i,2*segments+j,j),
                      (2*segments+i,3*segments+i,3*segments+j,2*segments+j)])
        mids.extend([mat,mat,inside])
    faces.append(tuple(3*segments+i for i in range(segments)))
    mids.append(inside)
    return mesh(name, verts, faces, mat, mids)

# Long tapered courier body; angular cross sections preserve the pointed silhouette.
loft('Keel', [section(-1.29,.012,-.025,.015,.07),
              section(-.92,.095,-.09,.035,.13),
              section(-.48,.16,-.12,.045,.18),
              section(-.10,.19,-.13,.05,.22),
              section(.40,.205,-.13,.05,.21),
              section(.68,.18,-.11,.045,.17)], 12)
# Contrasting lower cheek armor makes the slim fuselage read as faceted, not a flat wedge.
for s in (-1,1):
    plate('Swept stabilizer', [(s*.15,.12),(s*.70,.59),(s*.53,.64),(s*.17,.38)], -.035,.045,6)
    plate('Stabilizer inset', [(s*.23,.22),(s*.58,.56),(s*.50,.58),(s*.23,.40)], .047,.056,4)
    plate('Wing marking', [(s*.39,.40),(s*.45,.45),(s*.55,.53),(s*.49,.52)], .058,.063,15)
    # Raised root fairing gives the wings thickness and a clear transition into the body.
    loft('Root fairing', [[(s*.16,.08,.035),(s*.27,.18,.035),(s*.40,.49,.035),(s*.17,.43,.035)],
                           [(s*.16,.08,.13),(s*.23,.18,.10),(s*.31,.45,.085),(s*.17,.43,.14)]], 0)
# Narrow dorsal spine and a framed dark canopy.
plate('Dorsal panel', [(-.14,-.52),(.14,-.52),(.18,.42),(-.18,.42)], .185,.205,0)
loft('Visor frame', [[(-.10,-.29,.21),(.10,-.29,.21),(.11,.14,.21),(-.11,.14,.21)],
                    [(-.065,-.23,.27),(.065,-.23,.27),(.075,.10,.29),(-.075,.10,.29)]], 6)
plate('Visor', [(-.060,-.215),(.060,-.215),(.069,.09),(-.069,.09)], .293,.297,11)
plate('Visor glint', [(-.045,-.17),(.045,-.17),(.045,-.11),(-.045,-.11)], .299,.302,9)
# Pale tapered nose stripe, interrupted by the raised muzzle fairing.
plate('Nose stripe', [(-.053,-.40),(.053,-.40),(.025,-.72),(0,-1.15),(-.025,-.72)], .153,.163,6)
# The opening center is exactly the gameplay point (0,-.90), above the hull.
tube_y('Nose muzzle lip', 0, -.90, -.75, .19, .062, .037, 6)
# Central propulsion housing ends at the fixed socket (0,.86,0).
loft('Motor housing', [section(.39,.18,-.11,.035,.21), section(.62,.22,-.13,.045,.24),
                       section(.79,.19,-.12,.035,.20)], 11)
plate('Motor armor', [(-.14,.44),(.14,.44),(.16,.72),(.11,.78),(-.11,.78),(-.16,.72)], .225,.245,4)
plate('Motor center stripe', [(-.035,.48),(.035,.48),(.035,.73),(-.035,.73)], .247,.252,15)
tube_y('Engine socket', 0, .86, .72, 0, .13, .085, 6)

# Join and apply transformations before export. Geometry remains flat shaded.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts: ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons: p.use_smooth = False
# Export only the model, before introducing render equipment.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if kind == 'AREA': data.shape='DISK'; data.size=size
    if kind == 'SUN': data.angle=math.radians(12)
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob)
    ob.location=loc; aim(ob,target)

light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,4), 220, (0,0,0), 5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,-.215,5)
cam.rotation_euler=(0,0,0)
# Camera local -Z points straight down and local +Y points toward screen-up +Y.
camdata.type='ORTHO'; camdata.ortho_scale=2.53
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50
cam.location=(3.1,-4.4,3.3)
aim(cam,(0,-.20,.02))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
