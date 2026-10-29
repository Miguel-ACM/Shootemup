import bpy
import math
import os
import sys
from mathutils import Vector

# LANCER: a compact, faceted interceptor, modeled in game coordinates.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

colors = [
    (0.075, 0.115, 0.22, 1),   # royal blue shadow
    (0.14, 0.22, 0.36, 1),   # royal blue hull
    (0.20, 0.32, 0.50, 1),    # cobalt
    (0.32, 0.46, 0.62, 1),    # light cobalt
    (0.52, 0.66, 0.76, 1),    # ice blue
    (0.58, 0.65, 0.70, 1),    # steel
    (0.34, 0.42, 0.49, 1),    # dark steel
    (0.045, 0.06, 0.09, 1),   # recessed detail
    (0.40, 0.10, 0.72, 1),    # violet canopy
    (0.62, 0.28, 0.86, 1),    # canopy facets
    (0.27, 0.06, 0.48, 1),    # violet shadow
    (0.67, 0.75, 0.79, 1),    # bright steel
    (0.20, 0.25, 0.29, 1),    # blue steel
    (0.22, 0.39, 0.64, 1),    # medium blue
    (0.76, 0.62, 0.24, 1),    # trim
    (0.52, 0.20, 0.76, 1),    # lens
]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    m.diffuse_color = color
    mats.append(m)

pieces = []
def mesh(name, verts, faces, material, indices=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.clear()
    for mat in mats:
        data.materials.append(mat)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    for i, poly in enumerate(data.polygons):
        poly.material_index = indices[i] if indices else material
        poly.use_smooth = False
    pieces.append(ob)
    return ob

def loft(name, sections, material, side_mats=None):
    # Each station has a six-sided cross section, with chamfered deck and belly.
    verts = []
    for y, width, bottom, top in sections:
        verts.extend([(-width*.70,y,top), (width*.70,y,top),
                      (width,y,top-(top-bottom)*.24), (width,y,bottom+(top-bottom)*.25),
                      (width*.68,y,bottom), (-width*.68,y,bottom),
                      (-width,y,bottom+(top-bottom)*.25), (-width,y,top-(top-bottom)*.24)])
    faces = [tuple(range(7,-1,-1))]
    ids = [material]
    for j in range(len(sections)-1):
        for k in range(8):
            faces.append((j*8+k,j*8+(k+1)%8,(j+1)*8+(k+1)%8,(j+1)*8+k))
            ids.append(side_mats[k] if side_mats else material)
    faces.append(tuple((len(sections)-1)*8+k for k in range(8)))
    ids.append(material)
    return mesh(name, verts, faces, material, ids)

def plate(name, outline, z0, z1, mat, bevel=0.003):
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline]
    faces = [tuple(range(n-1,-1,-1)), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    ob = mesh(name, verts, faces, mat)
    if bevel:
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        mod = ob.modifiers.new('Armor edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.ops.object.modifier_apply(modifier=mod.name)
        ob.select_set(False)
    return ob

def ring_y(name, x, y_front, y_back, z, outer, inner, mat_outer, mat_inside, n=12):
    # Axis along Y. Outer shell and an actual inset bore.
    verts=[]
    for y,r in [(y_front,outer),(y_back,outer*.88),(y_front,inner),(y_back,inner*.82)]:
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    faces=[]; ids=[]
    for i in range(n):
        k=(i+1)%n
        faces.extend([(i,k,n+k,n+i), (i,2*n+i,2*n+k,k),
                      (2*n+i,3*n+i,3*n+k,2*n+k)])
        ids.extend([mat_outer,mat_outer,mat_inside])
    faces.append(tuple(3*n+i for i in range(n)))
    ids.append(mat_inside)
    return mesh(name,verts,faces,mat_outer,ids)

# Deep armored belly and long tapered shoulder. The point is an actual 3D taper.
loft('Keel', [(-.365,.115,-.057,.025),(-.27,.15,-.066,.043),(-.08,.145,-.067,.058),(.15,.102,-.050,.052),(.34,.056,-.034,.040),(.475,.019,-.017,.025)], 1,
     [2,1,0,0,0,0,1,2])
# Broad rear side shoulders create the sturdy, distinctive engine silhouette.
for s in (-1,1):
    def mirror(p): return [(s*x,y) for x,y in p]
    plate('Swept stabilizer', mirror([(.095,-.12),(.145,-.15),(.23,-.285),(.215,-.332),(.155,-.31),(.12,-.35),(.09,-.30)]), -.028,.014, 1, .004)
    plate('Wing inset', mirror([(.145,-.18),(.205,-.278),(.196,-.298),(.15,-.275),(.125,-.29),(.12,-.24)]), .015,.021,3,.002)
    # Engine pods are substantial volumes instead of narrow rods.
    loft('Engine nacelle', [(-.365,.042,-.045,.035),(-.32,.049,-.050,.049),(-.19,.048,-.042,.056),(-.135,.032,-.028,.049)], 1,
         [3,2,1,0,0,0,1,2]).location.x=s*.11
    plate('Nacelle cap', mirror([(.076,-.32),(.081,-.19),(.108,-.15),(.139,-.19),(.148,-.30),(.135,-.33)]), .050,.057,2,.003)
    plate('Nacelle stripe', mirror([(.095,-.305),(.097,-.205),(.108,-.186),(.117,-.205),(.119,-.305)]), .058,.061,4,.001)
    ring_y('Engine bell',s*.11,-.40,-.365,-.004,.034,.023,6,7)
    # Narrow raised seams and a few purposeful vents.
    plate('Shoulder plate', mirror([(.052,-.105),(.115,-.12),(.118,-.03),(.078,.13),(.045,.20)]), .052,.059,3,.002)
    plate('Shoulder inlay', mirror([(.075,-.07),(.102,-.075),(.094,.015),(.068,.095)]), .060,.063,4,.001)
    plate('Rear vent', mirror([(.045,-.30),(.071,-.30),(.071,-.215),(.045,-.215)]), .046,.049,7,.001)
    for y in (-.285,-.266,-.247,-.228):
        plate('Vent fin', mirror([(.047,y),(.069,y),(.069,y+.004),(.047,y+.004)]), .050,.052,5,0)
    plate('Underwing armor', mirror([(.13,-.21),(.19,-.28),(.18,-.30),(.13,-.28)]), -.035,-.029,2,.002)

# Central raised armored spine carries the long continuous laser to the tip.
loft('Laser spine', [(.045,.034,.044,.076),(.22,.029,.035,.069),(.405,.020,.018,.047),(.492,.014,.004,.030)], 2,
     [4,3,1,0,0,0,1,3])
mesh('Spine highlight', [(-.009,.18,.071),(.009,.18,.071),(.008,.405,.048),(-.008,.405,.048),(-.006,.472,.035),(.006,.472,.035)], [(0,1,2,3),(3,2,5,4)], 4)
# Ring mouth exactly at the forward tip, with the violet lens recessed behind it.
ring_y('Laser emitter',0,.52,.491,.017,.017,.011,11,15,12)
# Replace the former elongated cockpit with a flush blue armor plate.
plate('Central hull infill', [(-.046,-.17),(.046,-.17),(.050,-.075),(.033,.02),
                             (-.033,.02),(-.050,-.075)], .058,.063,2,.003)
# Compact octagonal hitbox canopy, centered precisely at (0, .08).
# A thin dark rim sits over the spine; the dome's rings form faceted glass.
cx, cy = 0.0, .08
n = 8
angles = [2*math.pi*i/n + math.pi/8 for i in range(n)]
outline = [(cx + .065*math.cos(a), cy + .065*math.sin(a)) for a in angles]
plate('Canopy rim', outline, .077, .081, 0, .001)
verts = []
for radius, z in [(.060,.081), (.052,.099), (.032,.113)]:
    verts.extend((cx + radius*math.cos(a), cy + radius*math.sin(a), z) for a in angles)
verts.append((cx, cy, .119))
faces = []
ids = []
for j in range(2):
    for i in range(n):
        faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
        ids.append(8 if (i+j)%3 else 9)
for i in range(n):
    faces.append((2*n+i, 2*n+(i+1)%n, 3*n))
    ids.append(9 if i in (2,3,4) else 8)
mesh('Violet canopy', verts, faces, 8, ids)
# Small dorsal and ventral armor facets make the silhouette work during rolls.
plate('Rear deck', [(-.042,-.355),(.042,-.355),(.055,-.31),(.045,-.19),(-.045,-.19),(-.055,-.31)], .035,.045,2,.003)
plate('Rear deck inset', [(-.020,-.331),(.020,-.331),(.026,-.24),(-.026,-.24)], .046,.049,6,.002)
plate('Belly armor', [(-.075,-.27),(.075,-.27),(.085,-.10),(.048,.16),(0,.34),(-.048,.16),(-.085,-.10)], -.071,-.065,2,.003)

# Industrial armour: chunky overlapping plates, dark channels and machinery.
for s in (-1, 1):
    def m(points):
        return [(s*x, y) for x, y in points]
    plate('Shoulder seam', m([(.085,-.105),(.114,-.115),(.105,-.015),(.076,.105),(.066,.12),(.089,-.02)]), .064,.066,12,.001)
    plate('Shoulder armor step', m([(.094,-.102),(.112,-.108),(.103,-.022),(.083,.065),(.077,.074),(.096,-.026)]), .067,.070,2,.001)
    # Dark intake beds and wide cross slats on each nacelle.
    plate('Nacelle intake recess', m([(.083,-.295),(.137,-.295),(.137,-.211),(.130,-.198),(.090,-.198),(.083,-.211)]), .062,.064,7,.001)
    for yy in (-.282,-.264,-.246,-.228,-.210):
        plate('Intake slat', m([(.087,yy),(.133,yy),(.133,yy+.005),(.087,yy+.005)]), .065,.068,6,0)
    plate('Outer rail', m([(.142,-.315),(.151,-.307),(.149,-.217),(.141,-.205)]), .044,.051,12,.001)
    plate('Rail actuator', m([(.137,-.272),(.151,-.272),(.151,-.247),(.137,-.247)]), .052,.058,5,.001)
    plate('Wing overlap', m([(.153,-.247),(.204,-.282),(.196,-.298),(.154,-.281),(.139,-.293),(.138,-.275)]), .023,.028,12,.002)
    plate('Wing armor facet', m([(.156,-.251),(.199,-.282),(.194,-.290),(.155,-.275),(.144,-.283),(.143,-.275)]), .029,.032,3,.001)
    # Compact hazard panels with three broad yellow stripes.
    plate('Hazard backing', m([(.118,-.185),(.143,-.198),(.140,-.164),(.123,-.148)]), .057,.061,7,.001)
    for y in (-.181,-.170,-.159):
        plate('Hazard stripe', m([(.122,y),(.139,y-.009),(.139,y-.004),(.122,y+.005)]), .062,.064,14,0)
    plate('Engine collar', m([(.078,-.360),(.142,-.360),(.143,-.335),(.136,-.327),(.084,-.327),(.077,-.335)]), .038,.045,12,.002)
    plate('Collar edge', m([(.083,-.357),(.137,-.357),(.137,-.351),(.083,-.351)]), .046,.049,5,.001)
    # Underside panels remain visible during a roll.
    plate('Belly service panel', m([(.049,-.23),(.079,-.23),(.078,-.12),(.054,-.10)]), -.075,-.071,12,.002)
    for yy in (-.21,-.19,-.17):
        plate('Belly vent slat', m([(.052,yy),(.075,yy),(.075,yy+.006),(.052,yy+.006)]), -.077,-.075,5,0)

# Segmented barrel shroud and channels emphasize the long laser weapon.
for s in (-1, 1):
    def m(points): return [(s*x,y) for x,y in points]
    plate('Spine channel', m([(.021,.195),(.032,.195),(.024,.35),(.016,.42),(.013,.42)]), .050,.053,12,.001)
    plate('Spine clamp aft', m([(.014,.195),(.031,.195),(.030,.214),(.014,.214)]), .070,.074,6,.001)
    plate('Spine clamp forward', m([(.011,.351),(.023,.351),(.022,.366),(.010,.366)]), .053,.057,6,.001)
    plate('Emitter collar', m([(.008,.464),(.016,.464),(.015,.486),(.008,.486)]), .029,.033,12,.001)

# Layered central service hatch and grille break up the broad aft deck.
plate('Central service well', [(-.033,-.186),(.033,-.186),(.037,-.116),(.028,-.092),(-.028,-.092),(-.037,-.116)], .064,.067,12,.002)
plate('Central grille', [(-.025,-.177),(.025,-.177),(.027,-.120),(-.027,-.120)], .068,.070,7,.001)
for y in (-.166,-.153,-.140,-.127):
    plate('Central grille rib', [(-.024,y),(.024,y),(.024,y+.004),(-.024,y+.004)], .071,.073,5,0)
plate('Aft sensor block', [(-.023,-.322),(.023,-.322),(.025,-.295),(-.025,-.295)], .050,.058,12,.002)
plate('Sensor face', [(-.017,-.316),(.017,-.316),(.017,-.303),(-.017,-.303)], .059,.061,4,.001)
# Collapse all geometry, including applied bevels and nacelle offsets, into one object.
bpy.ops.object.select_all(action='DESELECT')
for ob in pieces:
    ob.select_set(True)
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

# Render setup: camera and lights are added only after the OBJ is exported.
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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target, track='-Z', up='Y'):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat(track, up).to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    point_at(ob, target)
    return ob

light('Sun', 'SUN', (-3, 4, 6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,-2,4), 100, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, .06, 3)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.05, 1.35, 1.2)
point_at(cam, (0,.04,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)


