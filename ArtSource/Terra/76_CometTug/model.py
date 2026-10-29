import bpy, math, os, sys, json
from mathutils import Vector

# Blender --background --python model.py [-- output_directory_or_script_path]
def script_dir():
    p = globals().get('__file__')
    if p:
        return os.path.dirname(os.path.abspath(p))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            p = os.path.abspath(args[0])
            return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()

OUT = script_dir()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
           (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
           (0.105,0.15,0.20),(0.34,0.31,0.48),(0.20,0.36,0.40),
           (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
           (0.62,0.25,0.19),(0.48,0.36,0.18),(0.53,0.61,0.55),
           (0.66,0.70,0.71)]
# Solar cells are flat blue: the game supplies the atlas grid.
palette[7] = (0.19,0.27,0.46)
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = 1.4
    mats.append(m)

body = []
def add(obj, material, group=body):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    group.append(obj)
    return obj

def box(loc, size, material, bevel=0.0, group=body):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Cut corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return add(obj, material, group)

def cylinder(loc, radius, depth, material, vertices=12, rotation=(0,0,0), group=body):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    return add(bpy.context.object, material, group)

def mesh(name, verts, faces, material, group=body, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    add(ob, material, group)
    if face_mats:
        for mat in face_mats[0]:
            ob.data.materials.append(mats[mat])
        for poly, idx in zip(ob.data.polygons, face_mats[1]):
            poly.material_index = idx
    return ob

def tapered_hull(outline, bottom_z, shoulder_z, top_z, inset, topmat, side=6):
    n = len(outline)
    cx = sum(p[0] for p in outline)/n
    cy = sum(p[1] for p in outline)/n
    rings = [[(x,y,bottom_z) for x,y in outline],
             [(x,y,shoulder_z) for x,y in outline],
             [(cx+(x-cx)*inset,cy+(y-cy)*inset,top_z) for x,y in outline]]
    verts = sum(rings, [])
    faces = [tuple(reversed(range(n)))]
    for j in range(2):
        faces += [(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for i in range(n)]
    faces.append(tuple(range(2*n,3*n)))
    return mesh('Faceted hull', verts, faces, side, face_mats=([topmat], [0]*(len(faces)-1)+[1]))

def ring_y(x, y, z, outer, inner, depth, group=body):
    # Annular forward-facing rim: the front opening is exactly at the gameplay muzzle.
    n=12
    verts=[]
    for yy, rad in ((y,outer),(y,inner),(y+depth,outer),(y+depth,inner)):
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((x+rad*math.cos(a), yy, z+rad*math.sin(a)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),
                      (n+i,n+j,3*n+j,3*n+i)])
    mesh('Muzzle lip',verts,faces,15,group)
    cylinder((x,y+depth*0.8,z), inner*0.98, 0.012, 11, 12, (math.pi/2,0,0),group)

# Asymmetric spacecraft chassis, stepped and chamfered rather than a flat plate.
tapered_hull([(-.28,-.60),(.28,-.60),(.38,.50),(-.18,.80),(-.38,.40)],
             -.10,.06,.24,.78,15)
tapered_hull([(-.19,-.48),(.19,-.48),(.27,.35),(-.13,.59),(-.27,.32)],
             .23,.25,.31,.83,6)
box((0,-.16,.32),(.27,.40,.045),15,.035)
box((0,-.18,.35),(.13,.20,.025),13,.012)
# Broad transverse arm and two staggered armored weapon outriggers.
box((.15,.19,.04),(1.89,.12,.16),13,.025)
box((-.45,-.25,.02),(.25,1.42,.23),6,.045)
box((-.45,-.17,.15),(.18,1.13,.06),13,.025)
box((.68,-.10,.02),(.24,1.20,.23),6,.045)
box((.68,-.03,.15),(.17,.95,.06),13,.025)
for x,y in [(-.45,-.98),(.68,-.71)]:
    cylinder((x,y+.16,.20),.085,.25,6,12,(math.pi/2,0,0))
    ring_y(x,y,.20,.115,.068,.075)
# Left solar wing: thick framed panel, three large blue cells with subtle separators.
box((-.95,.20,.09),(.52,1.20,.10),13,.025)
box((-.95,.20,.153),(.43,1.08,.035),7,.012)
for y in (-.16,.20,.56):
    box((-.95,y,.175),(.42,.018,.012),4)
box((-.95,.20,.177),(.018,1.07,.012),4)
for y in (-.39,.79):
    box((-.95,y,.16),(.54,.055,.08),15,.012)
# Towing winch, hexagonal drum and substantial circular collar.
cylinder((0,.16,.32),.245,.12,13,12)
cylinder((0,.16,.395),.185,.07,11,12)
cylinder((0,.16,.438),.12,.025,6,12)
box((0,.16,.455),(.14,.045,.022),15,.01)
# Dish mounting pedestal is fixed; the reflector can rotate around its vertical hub.
cylinder((.88,.58,.13),.105,.22,6,10)
dish=[]
n=12
verts=[(.88,.58,.23)]
for i in range(n):
    a=2*math.pi*i/n
    verts.append((.88+.27*math.cos(a),.58+.27*math.sin(a),.39))
faces=[(0,1+i,1+(i+1)%n) for i in range(n)]
mesh('Reflector',verts,faces,15,dish)
# A gold outer rim gives the shallow bowl a clean, readable outline.
verts=[]
for radius,z in ((.27,.39),(.235,.375)):
    for i in range(n):
        a=2*math.pi*i/n
        verts.append((.88+radius*math.cos(a),.58+radius*math.sin(a),z))
mesh('Dish rim',verts,[(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)],13,dish)
cylinder((.88,.58,.38),.035,.20,13,8,group=dish)
box((.93,.58,.49),(.12,.04,.035),6,.01,dish)

# Join independently, preserving an object for the rotating dish.
def join_objects(objects, name, pivot=None):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    ob=bpy.context.object
    ob.name=name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if pivot is not None:
        # Move the mesh relative to its origin so origin is the animation hub.
        for vertex in ob.data.vertices:
            vertex.co -= Vector(pivot)
        ob.location = pivot
    return ob

model=join_objects(body,'Model')
reflector=join_objects(dish,'Dish',(.88,.58,.23))
parts=[{'object':'Dish','motion':'spin','axis':[0,0,1],
        'pivot':[.88,.58,.23],'speed':40}]
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump(parts,f,indent=2)

# Export only ship meshes, with named objects preserved for runtime animation.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
reflector.select_set(True)
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
scene.world=bpy.data.worlds.new('Space backdrop') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'

def point_at(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None and kind=='AREA': data.shape='DISK'; data.size=size
    if kind=='SUN': data.angle=math.radians(18)
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    point_at(ob,target)
    return ob
light('Key sun','SUN',(-3,4,6),2.0,(0,0,0))
light('Soft fill','AREA',(4,-2,5),180,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,6)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=2.85
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(3.5,-4.8,4.2)
point_at(cam,(0,0,.12))
camdata.type='PERSP'
camdata.lens=52
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
