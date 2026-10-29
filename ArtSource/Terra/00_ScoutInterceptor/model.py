import bpy, bmesh, math, os, sys
from mathutils import Vector

# Output beside this script, including when invoked with Blender's -- argument.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),(0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),(0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),(0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),(0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*rgb, 1)
    p.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*rgb, 1)
        p.inputs['Emission Strength'].default_value = 2.0
    mats.append(m)

parts = []
def mesh(name, verts, faces, material):
    bm = bmesh.new()
    vs = [bm.verts.new(v) for v in verts]
    bm.verts.ensure_lookup_table()
    for f in faces:
        bm.faces.new([vs[i] for i in f])
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    data = bpy.data.meshes.new(name)
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def layers(name, rings, mat):
    # Corresponding polygon rings create broad bevels and faceted upper surfaces.
    n = len(rings[0]); verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    for r in range(len(rings)-1):
        for i in range(n):
            j = (i+1)%n
            faces.append((r*n+i, r*n+j, (r+1)*n+j, (r+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, mat)

def plate(name, outline, z0, z1, mat, inset=0.08):
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    bottom = [(x,y,z0) for x,y in outline]
    shoulder = [(x,y,z1-0.035) for x,y in outline]
    top = [(cx+(x-cx)*(1-inset), cy+(y-cy)*(1-inset), z1) for x,y in outline]
    return layers(name, [bottom, shoulder, top], mat)

def ring_y(name, x, y, z, outer, inner, depth, mat, back_direction):
    # Open annulus with a dark recessed bore; the front plane is exactly y.
    n=12
    verts=[]
    for yy, r in [(y,outer),(y,inner),(y+back_direction*depth,inner),(y+back_direction*depth,outer)]:
        verts.extend([(x+math.cos(2*math.pi*i/n)*r, yy, z+math.sin(2*math.pi*i/n)*r) for i in range(n)])
    faces=[]
    for a,b in [(0,1),(1,2),(3,0)]:
        for i in range(n):
            j=(i+1)%n
            faces.append((a*n+i,a*n+j,b*n+j,b*n+i))
    mesh(name,verts,faces,mat)
    # The black disk is recessed inside the rim, rather than an emissive projectile.
    yy=y+back_direction*depth*0.9
    disk=[(x+math.cos(2*math.pi*i/n)*inner,yy,z+math.sin(2*math.pi*i/n)*inner) for i in range(n)]
    mesh(name+' bore',disk,[tuple(range(n))],11)

# Original gull wing envelope: 2.8 wide, nose to tail about 1.79.
wing=[(-1.4,.47),(-.46,-.09),(-.18,-.52),(0,-.94),(.18,-.52),(.46,-.09),(1.4,.47),(.64,.37),(.2,.14),(-.2,.14),(-.64,.37)]
plate('Gull wing',wing,-.065,.10,0,.035)
# Raised center body, pointed nose and a faceted spine.
plate('Fuselage',[(-.19,.53),(-.24,-.16),(0,-1.02),(.24,-.16),(.19,.53)],-.07,.25,1,.16)
plate('Nose armor',[(-.155,-.39),(0,-.99),(.155,-.39),(0,-.46)],.252,.275,6,.08)
# A raised rear motor housing ends at the gameplay engine socket.
plate('Motor housing',[(-.19,.18),(-.18,.68),(-.12,.76),(.12,.76),(.18,.68),(.19,.18),(0,.08)],.03,.27,0,.16)
plate('Motor dorsal panel',[(-.11,.29),(-.10,.65),(.10,.65),(.11,.29)],.27,.30,2,.12)
ring_y('Engine nozzle',0,.77,0,.145,.102,.11,6,-1)
# Opaque faceted canopy, its shape readable from straight above.
layers('Canopy',[[(-.135,-.52,.26),(.135,-.52,.26),(.16,-.16,.26),(.10,.02,.26),(-.10,.02,.26),(-.16,-.16,.26)],
                 [(-.075,-.42,.37),(.075,-.42,.37),(.10,-.17,.41),(.065,-.04,.38),(-.065,-.04,.38),(-.10,-.17,.41)]],8)
# Broad, simple wing armor and distinct pale leading-edge slash on each side.
for s in (-1,1):
    plate('Wing armor',[(s*.30,-.16),(s*.46,-.08),(s*1.26,.40),(s*.78,.29),(s*.42,.03)],.10,.155,2,.08)
    plate('Wing leading edge',[(s*.46,-.075),(s*1.31,.43),(s*1.19,.42),(s*.43,-.005)],.155,.172,6,.025)
    plate('Wing root',[(s*.30,-.12),(s*.42,-.08),(s*.66,.13),(s*.48,.10)],.15,.19,0,.08)
    # Muzzle lips at (+/- .66, -.16, +.08); short housings run rearwards.
    ring_y('Gun muzzle',s*.66,-.16,.08,.068,.040,.16,6,1)
    plate('Gun fairing',[(s*.60,-.02),(s*.72,-.02),(s*.72,.17),(s*.60,.17)],.07,.16,0,.1)

# Join only the model parts and bake transforms. No modifiers remain unapplied.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Export before adding the rendering rig.
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'; data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)

light('Upper left sun','SUN',(-4,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,5),180,(0,0,0),5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,7)
# Camera local up is +Y in the image, preserving screen-up convention.
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.30
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (3.7,-5.0,4.4)
point_at(cam,(0,-.10,.05))
cam_data.type = 'PERSP'
cam_data.lens = 52
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
