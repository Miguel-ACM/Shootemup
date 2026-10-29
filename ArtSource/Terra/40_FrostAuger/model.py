import bpy, math, os, sys, json
from mathutils import Vector

# Run with: blender --background --python model.py [-- output_directory]
def output_dir():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except NameError:
        if '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv):
            return os.path.abspath(sys.argv[sys.argv.index('--') + 1])
        return os.getcwd()
OUT = output_dir()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
 (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
 (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
 (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
 (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.75
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(mat)

body = []
def mesh(name, vertices, faces, material, group=body, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        data.materials.append(mat)
    for i, p in enumerate(data.polygons):
        p.material_index = face_mats[i] if face_mats else material
        p.use_smooth = False
    group.append(obj)
    return obj

def slab(name, outline, z0, z1, inset, mat, side):
    # Inset upper outline creates broad, deliberately faceted armor bevels.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    low = [(x,y,z0) for x,y in outline]
    rim = [(x,y,z1-inset) for x,y in outline]
    top = [(cx+(x-cx)*(1-inset*1.3), cy+(y-cy)*(1-inset*1.3), z1) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    faces += [(i+n,(i+1)%n+n,(i+1)%n+2*n,i+2*n) for i in range(n)]
    faces += [tuple(range(2*n,3*n))]
    return mesh(name, low+rim+top, faces, mat, face_mats=[side]+[side]*n+[mat]*n+[mat])

def box(name, loc, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object; o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(mats[mat])
    if bevel:
        mod = o.modifiers.new('Armor chamfer', 'BEVEL'); mod.width = bevel; mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(o)
    return o

def cylinder(name, loc, radius, depth, mat, vertices=10, rotation=(0,0,0), group=body):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc, rotation=rotation)
    o = bpy.context.object; o.name = name; o.data.materials.append(mats[mat]); group.append(o)
    return o

# The low central chassis retains the original six-sided, snowplow-like outline.
slab('Main armored chassis', [(-.35,-.80),(.35,-.80),(.45,-.47),(.41,.69),(-.41,.69),(-.45,-.47)], -.09,.25,.09,1,4)
slab('Raised forward glacis', [(-.29,-.69),(.29,-.69),(.25,-.31),(-.25,-.31)], .25,.32,.045,6,4)
slab('Dark inset on glacis', [(-.21,-.62),(.21,-.62),(.18,-.39),(-.18,-.39)], .321,.335,.008,4,11)
# Angled outer armor links shelter the screw noses without covering their spiral.
for s in (-1,1):
    outline = [(s*.17,-.61),(s*.79,-.92),(s*.82,-.71),(s*.29,-.43)]
    if s == -1: outline.reverse()
    slab('Forward ice deflector', outline, .02,.17,.035,1,6)
    box('Rear screw bearing', (s*.43,.48,.10), (.25,.22,.26), 6,.035)
    box('Inner chassis rail', (s*.39,.08,.19), (.10,.83,.12), 4,.025)
# Broad rear deck and turret seat; the separately supplied mortar is not part of this chassis.
slab('Rear deck', [(-.32,.16),(.32,.16),(.31,.64),(-.31,.64)], .25,.30,.035,15,6)
box('Rear recessed grille', (0,.51,.307), (.40,.20,.025),11,.018)
for y in (.46,.53,.60):
    box('Grille slat', (0,y,.327), (.33,.025,.018),6,.005)
cylinder('Turret mounting collar', (0,.04,.29), .235,.07,4,12)
cylinder('Turret mounting top', (0,.04,.335), .18,.025,11,12)

# Each screw is a continuous faceted core and a thick, genuine helical flight.
# Axis is Y and the screw's actual origin is placed at the animation pivot.
def make_auger(x, sign, label):
    group = []
    cylinder('Screw core', (x,0,.10), .155,1.66,4,10,(math.pi/2,0,0),group)
    # Thick radial ribbon; its inner edge intersects the core for a solid appearance.
    verts=[]; faces=[]; steps=72; turns=3.0
    for i in range(steps+1):
        t=i/steps; y=-.80+1.60*t; a=sign*2*math.pi*turns*t
        for radius, dy in ((.145,-.025),(.235,-.025),(.235,.025),(.145,.025)):
            verts.append((x+radius*math.cos(a),y+dy,.10+radius*math.sin(a)))
    for i in range(steps):
        for j in range(4):
            faces.append((4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j))
    faces.extend([(3,2,1,0),tuple(4*steps+j for j in range(4))])
    mesh('Helical flight',verts,faces,6,group)
    # Tapered front tip and rear end cap make the rotating gear visibly grounded.
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=.155, radius2=0.025, depth=.22, location=(x,-.91,.10), rotation=(math.pi/2,0,0))
    tip=bpy.context.object; tip.name='Screw nose'; tip.data.materials.append(mats[6]); group.append(tip)
    cylinder('Screw rear cap', (x,.84,.10), .17,.075,11,10,(math.pi/2,0,0),group)
    bpy.ops.object.select_all(action='DESELECT')
    for o in group: o.select_set(True)
    bpy.context.view_layer.objects.active=group[0]
    bpy.ops.object.join()
    o=bpy.context.object; o.name=label
    # Move the object origin to the hub while keeping the mesh in world coordinates.
    bpy.context.scene.cursor.location=(x,0,.10)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return o

left=make_auger(-.59,1,'Auger_L')
right=make_auger(.59,-1,'Auger_R')
bpy.ops.object.select_all(action='DESELECT')
for o in body: o.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join()
model=bpy.context.object; model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Screw origins are the pivots; their geometry is already in its rest position.
parts=[{'object':'Auger_L','motion':'spin','axis':[0,1,0],'pivot':[-.59,0,.10],'speed':260},
       {'object':'Auger_R','motion':'spin','axis':[0,-1,0],'pivot':[.59,0,.10],'speed':260}]
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump(parts,f,indent=2)
bpy.ops.object.select_all(action='DESELECT')
for o in (model,left,right): o.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1); bg.inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'
def point_at(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, loc, energy, target, size=1):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if kind=='AREA': data.shape='DISK'; data.size=size
    o=bpy.data.objects.new(name,data); scene.collection.objects.link(o); o.location=loc; point_at(o,target)
light('Sun upper left','SUN',(-4,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,5),350,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata)
scene.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,6); cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'; camdata.ortho_scale=2.50
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
cam.location=(3.2,-4.2,3.2); point_at(cam,(0,0,.1)); camdata.type='PERSP'; camdata.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)
