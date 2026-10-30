import bpy
import math
import os
import sys
from mathutils import Vector

# LANCER: game-space axes are X right, Y forward, Z up.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

colors = [
    (0.045, 0.085, 0.26, 1), (0.075, 0.16, 0.46, 1),
    (0.12, 0.27, 0.65, 1), (0.25, 0.43, 0.76, 1),
    (0.53, 0.72, 0.87, 1), (0.62, 0.70, 0.75, 1),
    (0.30, 0.39, 0.49, 1), (0.045, 0.065, 0.10, 1),
    (0.47, 0.08, 0.78, 1), (0.69, 0.26, 0.95, 1),
    (0.29, 0.055, 0.49, 1), (0.76, 0.83, 0.86, 1),
    (0.18, 0.25, 0.33, 1), (0.17, 0.35, 0.68, 1),
    (0.89, 0.68, 0.27, 1), (0.56, 0.22, 0.84, 1),
]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    mat.diffuse_color = color
    mats.append(mat)

pieces = []
def mesh(name, verts, faces, material, indices=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    for mat in mats:
        data.materials.append(mat)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for i, poly in enumerate(data.polygons):
        poly.material_index = indices[i] if indices else material
        poly.use_smooth = False
    pieces.append(obj)
    return obj

def plate(name, outline, bottom, top, material):
    # Ensure the top always faces up, including mirrored outlines.
    area = sum(outline[i][0] * outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0] * outline[i][1] for i in range(len(outline)))
    if area < 0:
        outline = list(reversed(outline))
    n = len(outline)
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,top) for x,y in outline]
    faces = [tuple(range(n-1,-1,-1)), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name, verts, faces, material)

def loft(name, x, stations, material):
    # Chamfered octagonal sections produce real sloping sides and belly.
    verts = []
    for y, w, bottom, top in stations:
        h = top-bottom
        verts.extend([(x-w*.68,y,top), (x+w*.68,y,top), (x+w,y,top-h*.25),
                      (x+w,y,bottom+h*.25), (x+w*.68,y,bottom),
                      (x-w*.68,y,bottom), (x-w,y,bottom+h*.25), (x-w,y,top-h*.25)])
    faces = [tuple(range(7,-1,-1))]
    ids = [material]
    for j in range(len(stations)-1):
        for k in range(8):
            faces.append((j*8+k,j*8+(k+1)%8,(j+1)*8+(k+1)%8,(j+1)*8+k))
            ids.append([material,material,0,0,0,0,0,material][k])
    faces.append(tuple((len(stations)-1)*8+k for k in range(8)))
    ids.append(material)
    return mesh(name, verts, faces, material, ids)

def ring_y(name, x, mouth, back, z, outer, inner, shell, bore, n=12):
    # A true annular lip and recessed end face; mouth is at the stated point.
    verts=[]
    for y,r in [(mouth,outer),(back,outer*.90),(mouth,inner),(back,inner*.85)]:
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((x+r*math.cos(a),y,z+r*math.sin(a)))
    faces=[]; ids=[]
    for i in range(n):
        k=(i+1)%n
        faces.extend([(i,k,n+k,n+i),(i,2*n+i,2*n+k,k),(2*n+i,3*n+i,3*n+k,2*n+k)])
        ids.extend([shell,shell,bore])
    faces.append(tuple(3*n+i for i in range(n))); ids.append(bore)
    return mesh(name,verts,faces,shell,ids)

def mirrored(points, s):
    return [(s*x,y) for x,y in points]

# Narrow central armoured wedge; the front weapon projects well beyond it.
loft('Central keel', 0, [(-.355,.068,-.056,.022),(-.25,.088,-.065,.040),
     (-.075,.080,-.058,.050),(.13,.060,-.045,.052),(.25,.038,-.028,.042),
     (.335,.018,-.018,.032)], 1)
plate('Dorsal aft shield', [(-.058,-.30),(.058,-.30),(.068,-.22),(.054,-.115),(.038,-.075),(-.038,-.075),(-.054,-.115),(-.068,-.22)], .040,.049,2)
plate('Aft center recess', [(-.026,-.275),(.026,-.275),(.033,-.225),(.025,-.15),(-.025,-.15),(-.033,-.225)], .050,.052,12)
plate('Reactor grille', [(-.022,-.26),(.022,-.26),(.022,-.18),(-.022,-.18)], .053,.055,7)
for y in (-.247,-.229,-.211,-.193):
    plate('Grille vane', [(-.021,y),(.021,y),(.021,y+.005),(-.021,y+.005)], .056,.058,5)
plate('Belly keel armor', [(-.045,-.31),(.045,-.31),(.063,-.20),(.049,.04),(0,.26),(-.049,.04),(-.063,-.20)], -.068,-.060,3)

# Twin detached engine masses: a short rear bridge leaves clear, long open slots.
for s in (-1,1):
    m = lambda p: mirrored(p,s)
    loft('Engine pod',s*.11,[(-.365,.038,-.043,.031),(-.31,.043,-.050,.048),
         (-.18,.040,-.043,.048),(-.095,.028,-.030,.040),(.005,.008,-.014,.018)],2)
    ring_y('Engine nozzle',s*.11,-.40,-.357,-.006,.033,.022,6,7)
    plate('Swept bridge',m([(.052,-.29),(.10,-.32),(.157,-.295),(.23,-.245),(.224,-.208),(.15,-.245),(.075,-.23)]),-.025,.012,1)
    plate('Wing armor',m([(.145,-.278),(.219,-.232),(.212,-.218),(.15,-.251),(.119,-.262)]),.013,.020,3)
    # Long separate forward-swept armor blades make the slots visible from above.
    plate('Outer blade',m([(.143,-.25),(.165,-.24),(.190,-.095),(.165,.095),(.145,.15),(.153,-.09)]),-.018,.016,1)
    plate('Blade top facet',m([(.158,-.216),(.172,-.204),(.182,-.09),(.166,.064),(.160,-.08)]),.017,.022,3)
    plate('Nacelle armor',m([(.079,-.31),(.14,-.31),(.145,-.205),(.132,-.14),(.11,-.105),(.085,-.17)]),.048,.054,3)
    plate('Nacelle intake',m([(.088,-.294),(.132,-.294),(.132,-.215),(.088,-.215)]),.055,.057,7)
    for y in (-.28,-.262,-.244,-.226):
        plate('Intake rib',m([(.09,y),(.13,y),(.13,y+.005),(.09,y+.005)]),.058,.061,6)
    plate('Pod forward spear',m([(.092,-.17),(.111,-.13),(.124,-.055),(.111,-.006),(.103,-.085)]),.049,.055,4)
    plate('Engine collar',m([(.078,-.359),(.142,-.359),(.142,-.335),(.078,-.335)]),.032,.039,12)
    plate('Collar highlight',m([(.084,-.355),(.136,-.355),(.136,-.349),(.084,-.349)]),.040,.043,5)
    plate('Hazard backing',m([(.150,-.25),(.181,-.232),(.180,-.214),(.149,-.232)]),.023,.025,7)
    for x in (.153,.163,.173):
        plate('Hazard slash',m([(x,-.237+(x-.153)*.58),(x+.005,-.234+(x-.153)*.58),(x+.005,-.222+(x-.153)*.58),(x,-.225+(x-.153)*.58)]),.026,.028,14)
    plate('Belly pod armor',m([(.082,-.31),(.137,-.31),(.139,-.20),(.116,-.15),(.088,-.20)]),-.052,-.048,13)
    plate('Inner shoulder',m([(.042,-.10),(.071,-.09),(.060,.10),(.038,.18),(.028,.13)]),.051,.055,3)
    plate('Shoulder inset',m([(.048,-.06),(.061,-.055),(.049,.075),(.039,.11)]),.056,.058,4)

# Lance barrel begins ahead of the dome and projects to the single laser muzzle.
loft('Emitter spine',0,[(.145,.026,-.019,.048),(.25,.026,-.020,.048),(.37,.018,-.014,.035),(.495,.014,-.010,.027)],2)
plate('Spine top rail',[(-.009,.16),(.009,.16),(.009,.37),(.006,.482),(-.006,.482),(-.009,.37)],.048,.051,4)
for y in (.22,.34,.45):
    plate('Barrel coupling',[(-.021,y),(.021,y),(.021,y+.012),(-.021,y+.012)],.041,.050,6)
ring_y('Laser emitter',0,.52,.49,.008,.017,.010,11,15)

# Small, centred round violet dome. Highest point is exactly z=.079.
n=12
angles=[2*math.pi*i/n for i in range(n)]
outline=[(.052*math.cos(a),.08+.052*math.sin(a)) for a in angles]
plate('Canopy rim',outline,.052,.056,12)
verts=[]
for radius,z in [(.049,.056),(.042,.068),(.026,.076)]:
    verts.extend((radius*math.cos(a),.08+radius*math.sin(a),z) for a in angles)
verts.append((0,.08,.079))
faces=[]; ids=[]
for j in range(2):
    for i in range(n):
        faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
        ids.append(9 if i in (3,4,5) else 8)
for i in range(n):
    faces.append((2*n+i,2*n+(i+1)%n,3*n))
    ids.append(9 if i in (3,4,5) else 8)
mesh('Violet canopy',verts,faces,8,ids)

# Collapse all body geometry into one mesh with applied transforms.
bpy.ops.object.select_all(action='DESELECT')
for obj in pieces:
    obj.select_set(True)
bpy.context.view_layer.objects.active = pieces[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    f.write('[]\n')

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj,target)

light('Sun','SUN',(-3,4,6),2.0,(0,0,0))
light('Soft fill','AREA',(3,-2,4),100,(0,0,0),5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,.06,3)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.05,1.35,1.2)
point_at(cam,(0,.05,0))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

