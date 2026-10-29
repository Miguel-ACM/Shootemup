import bpy, math, os, sys
from mathutils import Vector

# Outputs follow the script, including when invoked using blender --python ... -- path.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

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
for i, c in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*c, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*c, 1)
    bs.inputs['Roughness'].default_value = 0.65
    bs.inputs['Metallic'].default_value = 0.18 if i not in (9,10) else 0.0
    if i in (9,10):
        bs.inputs['Emission Color'].default_value = (*c, 1)
        bs.inputs['Emission Strength'].default_value = 1.7
    mats.append(m)

parts = []
def mesh(name, vertices, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.materials.append(mats[mat])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

def plate(name, outline, z0, z1, mat, inset=0.08):
    # Inset top face gives each plate real sloped armor edges.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    inner = [(x+(cx-x)*inset, y+(cy-y)*inset) for x,y in outline]
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1-0.045) for x,y in outline] + [(x,y,z1) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
    faces.append(tuple(range(2*n,3*n)))
    return mesh(name, verts, faces, mat)

def disk(name, x, y, z, radius, depth, mat, vertices=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(x,y,z))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

def ring(name, center, outer, inner, depth, mat, axis='Z', vertices=10):
    # A true open annulus with a short inner wall, rather than a painted disk.
    x,y,z = center
    verts=[]
    for d,r in ((-depth,outer),(0,outer),(0,inner),(-depth,inner)):
        for i in range(vertices):
            a=2*math.pi*i/vertices
            if axis == 'Z': verts.append((x+r*math.cos(a),y+r*math.sin(a),z+d))
            else: verts.append((x+r*math.cos(a),y+(d if axis == 'Y+' else -d),z+r*math.sin(a)))
    faces=[]
    for k in range(3):
        for i in range(vertices):
            j=(i+1)%vertices
            faces.append((k*vertices+i,k*vertices+j,(k+1)*vertices+j,(k+1)*vertices+i))
    return mesh(name, verts, faces, mat)

# Long tapered armored keel and raised, segmented spine.
plate('Keel', [(0,-1.98),(.22,-.66),(.29,.80),(0,1.30),(-.29,.80),(-.22,-.66)], -.22,.25, 6, .11)
plate('Reactor spine', [(-.14,.80),(.14,.80),(.14,-.72),(0,-1.64),(-.14,-.72)], .22,.37, 11,.10)
plate('Spine armor', [(-.10,.19),(.10,.19),(.095,-.76),(0,-1.35),(-.095,-.76)], .36,.405, 4,.12)
plate('Reactor line', [(-.025,.10),(.025,.10),(.025,-.78),(0,-1.02),(-.025,-.78)], .405,.43,9,.08)

for s in (-1,1):
    def side(points):
        pts=[(s*x,y) for x,y in points]
        return pts if s == 1 else list(reversed(pts))
    plate('Swept violet shoulder', side([(.20,.74),(.55,1.03),(.69,.35),(.54,-.73),(.31,-1.39),(.37,-.29)]), -.12,.22,7,.10)
    plate('Forward rail blade', side([(.34,-.63),(.45,-.80),(.29,-1.65),(.25,-1.37)]), .10,.24,4,.08)
    # Three broad capacitor paddles retain the original sawtooth outline.
    for i in range(3):
        y=.46-i*.47
        plate('Capacitor paddle', side([(.45,y+.16),(.85,y+.06),(.78,y-.14),(.48,y-.22)]), .15,.29,6,.11)
        plate('Paddle violet inset', side([(.49,y+.105),(.79,y+.035),(.74,y-.105),(.52,y-.16)]), .285,.32,7,.10)
        # The first pair is the gameplay emitter; the other two are closed capacitor caps.
        x=s*.71
        if i == 0:
            ring('Shoulder emitter lip', (x,.435,.30), .089,.052,.065, 15)
            disk('Shoulder emitter dark well',x,.435,.252,.050,.012,11)
            disk('Shoulder emitter core',x,.435,.261,.025,.008,9)
        else:
            disk('Capacitor hub',x,y-.025,.335,.068,.045,11)
            disk('Capacitor top',x,y-.025,.36,.038,.012,13)
    # Rear motor fairing and open socket at the exact gameplay position.
    plate('Motor housing', side([(.17,.59),(.40,.59),(.43,1.05),(.36,1.16),(.22,1.16),(.15,1.05)]), -.13,.22,7,.12)
    ring('Engine socket', (s*.29,1.16,0), .105,.065,.13,6,axis='Y+')
    # Recessed black interior sits just forward of the rear-facing lip.
    ring('Engine inner rim', (s*.29,1.151,0), .064,.045,.055,11,axis='Y+')

plate('Command station', [(-.19,.40),(.19,.40),(.17,.72),(-.17,.72)], .34,.49,7,.15)
plate('Command slit', [(-.13,.48),(.13,.48),(.13,.54),(-.13,.54)], .49,.51,9,.08)
# Nose mouth: opening plane is exactly at the gameplay point, axis facing -Y.
# Axis Y helper points toward +Y, so its depth is behind the front plane.
ring('Lance muzzle', (0,-1.98,.05), .068,.038,.09,15,axis='Y',vertices=8)
ring('Lance inner bore', (0,-1.975,.05), .038,.022,.05,11,axis='Y',vertices=8)

# One exported mesh, with flat faces and no unapplied transforms.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Explicit triangulation makes the export budget and flat shading deterministic.
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert len(model.data.polygons) <= 6000
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
scene.world = bpy.data.worlds.new('Studio world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(obj, point):
    obj.rotation_euler = (Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape='DISK'; data.size=size
    ob = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    aim(ob,target)
    return ob
light('Upper left sun','SUN',(-4,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(3,1,5),180,(0,-.3,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,-.34,8)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=3.85
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(4.3,-6.1,5.3)
aim(cam,(0,-.32,.05))
camdata.type='PERSP'
camdata.lens=48
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

