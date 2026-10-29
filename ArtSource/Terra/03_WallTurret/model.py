import bpy, math, os, sys
from mathutils import Vector

# Outputs go beside this script, even when launched from another directory.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)
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
for i, rgb in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = 0.72
    bsdf.inputs['Metallic'].default_value = 0.18 if i not in (9,10) else 0.0
    if i in (9,10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    mats.append(mat)

parts = []
def mesh(name, vertices, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def armor(name, outline, bottom, shoulder, top, inset, material):
    # A broad, sloping perimeter creates facets around a smaller raised deck.
    cx = sum(p[0] for p in outline)/len(outline)
    cy = sum(p[1] for p in outline)/len(outline)
    n = len(outline)
    inner = [(cx+(x-cx)*inset, cy+(y-cy)*inset) for x,y in outline]
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,shoulder) for x,y in outline] + [(x,y,top) for x,y in inner]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
    return mesh(name, verts, faces, material)

def box(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def tube_y(name, x, front, rear, z, outer, inner, material, n=10):
    # Hollow ring and inner wall: the front aperture is exactly at front.
    verts = []
    for y,r in ((rear,outer*.86),(front,outer),(front,inner),(front + (0.075 if rear > front else -0.075),inner)):
        verts += [(x+r*math.cos(2*math.pi*i/n), y, z+r*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]
    for k in range(3):
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
    faces.append(tuple(range(3*n,4*n)))
    obj=mesh(name,verts,faces,material)
    obj.data.materials.append(mats[11])
    for poly in obj.data.polygons:
        if poly.index >= 2*n: poly.material_index=1
    return obj

def disc(name, x, y, z, radius, depth, material, n=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=radius, depth=depth, location=(x,y,z))
    obj=bpy.context.object
    obj.name=name
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

# Squared wall shoe and raised, tapered battery: width 1.79, length about 2.0.
armor('Mounting shoe', [(-.78,.65),(-.86,.27),(-.66,-.52),(.66,-.52),(.86,.27),(.78,.65)], -.16,.05,.19,.88,11)
for s in (-1,1):
    pts=[(s*.46,.59),(s*.76,.54),(s*.69,-.40),(s*.44,-.32)]
    if s == 1: pts.reverse()
    armor('Side armor',pts,.08,.22,.31,.84,0)
    box('Mounting lug', (s*.78,.22,.16),(.23,.20,.19),6,.035)
    box('Forward lug', (s*.70,-.28,.16),(.20,.16,.16),6,.025)
    # Socket opens toward the rear at the exact game engine location.
    tube_y('Rear socket', s*.53, .76, .54, 0.0, .115, .066, 6)
    disc('Socket shoulder',s*.53,.54,.23,.19,.12,11,10)
    disc('Socket cap',s*.53,.54,.31,.13,.06,4,10)

armor('Armored mantlet',[(-.40,.43),(-.50,-.09),(-.28,-.46),(.28,-.46),(.50,-.09),(.40,.43)], .12,.34,.48,.76,0)
# A broad rust-colored ridge and inset sight are legible in the overhead view.
armor('Crown plate',[(-.30,.34),(-.36,-.02),(-.21,-.33),(.21,-.33),(.36,-.02),(.30,.34)], .47,.49,.52,.88,4)
box('Sight recess',(0,.14,.535),(.40,.16,.045),11,.02)
box('Amber sight',(0,.14,.562),(.25,.055,.018),10,.006)
# Two short gun housings lead into hollow muzzles at the gameplay points.
for x in (-.22,.22):
    box('Breech',(x,-.42,.27),(.28,.34,.23),11,.035)
    box('Gun shroud',(x,-.62,.28),(.24,.31,.22),6,.025)
    tube_y('Muzzle lip',x,-1.115,-.70,.26,.102,.057,11,10)
    tube_y('Muzzle collar',x,-1.115,-1.035,.26,.112,.060,6,10)
# Large diagonal warning marks on the rear shoulders.
for s in (-1,1):
    pts=[(s*.49,.43),(s*.55,.47),(s*.68,.21),(s*.62,.17)]
    if s == 1: pts.reverse()
    armor('Hazard stripe',pts,.309,.311,.314,1.0,13)
    box('Rust hinge',(s*.61,-.15,.33),(.12,.17,.045),12,.012)

# One exportable object, with all modifiers already baked and flat faces.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth=False
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
scene.world=bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes=True
background=scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value=(.05,.07,.09,1)
background.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def aim(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj,target)
    return obj

light('Upper left sun','SUN',(-4,5,8),2.0,(0,0,0))
light('Soft fill','AREA',(4,-3,5),200,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,-.16,6)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=2.68
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=48
cam.location=(3.6,-4.9,4.4)
aim(cam,(0,-.12,.12))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

