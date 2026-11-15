import bpy, math, os, sys
from mathutils import Matrix, Vector

try:
    directory = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--')+1:]:
        raise RuntimeError('Pass the script path after --')
    directory = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--')+1]))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.8,.115,.13,1),(.95,.83,.61,1),(.77,.48,.1,1),(.97,.72,.22,1),(.13,.46,.48,1),(.24,.68,.67,1),(.23,.41,.75,1),(.72,.32,.70,1),(.93,.42,.14,1),(.24,.25,.29,1),(.5,.82,.91,1),(.9,.65,.46,1),(.52,.71,.89,1),(.95,.60,.63,1),(.69,.27,.18,1),(.93,.88,.76,1)]
materials = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = color
    shader.inputs['Roughness'].default_value = .65
    materials.append(mat)
parts = []
def add(obj, material, name):
    obj.name = name
    obj.data.materials.append(materials[material])
    parts.append(obj)
    return obj

def mesh(name, vertices, faces, indices):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in materials: data.materials.append(m)
    for p, i in zip(data.polygons, indices): p.material_index = i
    parts.append(obj)
    return obj

def cyl(name, loc, radius, depth, material, axis=(0,1,0), sides=8, end=None):
    if end is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=loc)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=radius, radius2=end, depth=depth, location=loc)
    obj = bpy.context.object
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = Vector(axis).normalized().to_track_quat('Z','Y')
    return add(obj, material, name)

def box(name, loc, size, material, direction, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Chamfer', 'BEVEL')
        mod.width, mod.segments = bevel, 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.rotation_euler[1] = math.atan2(-direction.z, direction.x)
    return add(obj, material, name)

def stack(name, rings, material, n=12):
    vs = [(r*math.cos(i*2*math.pi/n),y,r*math.sin(i*2*math.pi/n)) for y,r in rings for i in range(n)]
    fs = []
    for j in range(len(rings)-1):
        for i in range(n):
            k = (i+1)%n
            fs.append((j*n+i,(j+1)*n+i,(j+1)*n+k,j*n+k))
    fs += [tuple(range(n)), tuple(reversed([(len(rings)-1)*n+i for i in range(n)]))]
    return mesh(name,vs,fs,[material]*len(fs))

stack('Turntable', [(-.45,.325),(-.43,.39),(-.35,.39),(-.33,.36)], 4)
stack('Lower edging', [(-.435,.385),(-.425,.398),(-.407,.398),(-.400,.39)], 2)
stack('Upper edging', [(-.352,.39),(-.337,.397),(-.323,.38),(-.318,.35)], 3)
stack('Raised step', [(-.32,.31),(-.30,.335),(-.275,.335),(-.263,.31)], 14)
stack('Step trim', [(-.302,.335),(-.294,.341),(-.281,.341),(-.273,.33)], 3)
cyl('Centre spindle',(0,-.055,0),.028,.42,2)
cyl('Spindle collar',(0,-.245,0),.047,.035,3)
# Alternating triangular roof panels with a deep, individually scalloped valance.
vs, fs, ids = [], [], []
for i in range(16):
    a, b = i*math.tau/16, (i+1)*math.tau/16
    def point(rad, height, theta): return (rad*math.cos(theta),height,rad*math.sin(theta))
    j = len(vs)
    vs += [point(.398,.105,a),point(.398,.105,b),(0,.365,0),point(.398,.105,a),point(.398,.105,b),point(.405,.045,(a+b)/2)]
    fs += [(j,j+2,j+1),(j+3,j+4,j+5)]
    ids += [i%2,i%2]
    # The game culls back faces: back each scallop with a reversed face and close the roof
    # with a shallow striped ceiling, so the canopy is solid when seen from below.
    vs += [(0,.15,0)]
    fs += [(j+3,j+5,j+4),(j+1,j+6,j)]
    ids += [i%2,(i+1)%2]
mesh('Striped canopy',vs,fs,ids)
stack('Gold eave',[(.098,.388),(.105,.405),(.117,.405),(.125,.387)],3)
cyl('Canopy hub',(0,.1,0),.11,.025,2,sides=12)
cyl('Finial stem',(0,.376,0),.019,.045,2)
bpy.ops.mesh.primitive_uv_sphere_add(segments=10, ring_count=5, radius=.033, location=(0,.417,0))
add(bpy.context.object,3,'Finial')

def offset(c,t,r,along=0,out=0,up=0): return c+t*along+r*out+Vector((0,up,0))
for i in range(4):
    a = math.pi/4+i*math.pi/2
    r, t = Vector((math.cos(a),0,math.sin(a))), Vector((-math.sin(a),0,math.cos(a)))
    pole = r*.248
    cyl('Ride pole',pole+Vector((0,-.082,0)),.019,.37,2)
    for y in (-.235,-.13,-.025,.075): cyl('Pole band',pole+Vector((0,y,0)),.024,.016,3,sides=6)
    c = pole+Vector((0,[-.145,-.09,-.16,-.105][i],0))
    first_gun_part = len(parts)
    if i == 0:
        box('Scatter receiver',c,(.135,.085,.087),5,t,.012)
        cyl('Scatter flare',offset(c,t,r,.094),.032,.105,5,t,10,.055)
        cyl('Scatter rim',offset(c,t,r,.15),.057,.018,3,t,10)
        cyl('Scatter bore',offset(c,t,r,.16),.041,.004,9,t,10)
    elif i == 1:
        box('Rail receiver',c,(.125,.075,.08),6,t,.01)
        box('Long barrel',offset(c,t,r,.105),(.16,.034,.042),6,t,.005)
        for side in (-1,1): box('Rail fin',offset(c,t,r,.105,side*.029,.018),(.14,.024,.014),12,t)
        box('Rail muzzle',offset(c,t,r,.185),(.018,.057,.063),3,t,.004)
    elif i == 2:
        box('Beam housing',c,(.13,.08,.085),7,t,.012)
        cyl('Beam dish',offset(c,t,r,.092),.036,.065,7,t,10,.065)
        cyl('Beam rim',offset(c,t,r,.128),.066,.014,3,t,10)
        cyl('Beam lens',offset(c,t,r,.137),.052,.006,10,t,10)
    else:
        box('Rocket rack',c,(.15,.104,.10),8,t,.009)
        box('Rocket rack face',offset(c,t,r,.079),(.012,.098,.094),3,t,.003)
        for up in (-.025,.025):
            for side in (-.024,.024): cyl('Rocket nose',offset(c,t,r,.089,side,up),.017,.023,11,t,8,.010)
    # Enlarge each gun about its pole mount so it still reads as a gun at icon size.
    bpy.context.view_layer.update()
    grow = Matrix.Translation(c) @ Matrix.Scale(1.35, 4) @ Matrix.Translation(-c)
    for obj in parts[first_gun_part:]:
        obj.matrix_world = grow @ obj.matrix_world

bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
# Compact all merged slots to the shared sixteen named materials.
face_materials = [model.data.materials[p.material_index] for p in model.data.polygons]
model.data.materials.clear()
for material in materials: model.data.materials.append(material)
material_indices = {material.name: i for i, material in enumerate(materials)}
for poly, material in zip(model.data.polygons, face_materials):
    poly.material_index = material_indices[material.name]
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for p in model.data.polygons: p.use_smooth = False
model.data.calc_loop_triangles()
triangles = len(model.data.loop_triangles)
print('Model triangles:',triangles)
if not 300 <= triangles <= 2000: raise RuntimeError('Triangle budget: %d' % triangles)
bpy.ops.wm.obj_export(filepath=os.path.join(directory,'model.obj'),export_selected_objects=True,export_materials=True,forward_axis='Y',up_axis='Z',apply_modifiers=True)

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05,.07,.09,1)
background.inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'
def aim(obj):
    # Look at the origin keeping world +Y (the item's up) as the image's up.
    forward = (Vector((0,0,0))-obj.location).normalized()
    right = forward.cross(Vector((0,1,0))).normalized()
    rotation = Matrix((right, right.cross(forward), -forward)).transposed()
    obj.rotation_euler = rotation.to_euler()
def light(name,kind,position,energy,size=1):
    data = bpy.data.lights.new(name,kind)
    data.energy = energy
    if kind == 'AREA': data.shape, data.size = 'DISK', size
    obj = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location = position
    aim(obj)
light('Upper left sun','SUN',(-3,5,5),2)
light('Soft fill','AREA',(3,1,4),90,5)
camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera',camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location = (0,0,4)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.06
scene.render.filepath = os.path.join(directory,'top.png')
bpy.ops.render.render(write_still=True)
camera.location = (1.4,1.05,2.3)
aim(camera)
camera_data.type = 'PERSP'
camera_data.lens = 80
scene.render.filepath = os.path.join(directory,'angle.png')
bpy.ops.render.render(write_still=True)



