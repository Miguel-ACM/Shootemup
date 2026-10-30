import bpy
import math
import os
import sys
from mathutils import Vector

# Warden: compact drone tender. +Y is forward and +Z is up.
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    ROOT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(ROOT):
        ROOT = os.path.dirname(ROOT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.25, 0.35, 0.29, 1),  # forest green
    (0.34, 0.46, 0.37, 1),  # leaf green
    (0.46, 0.57, 0.48, 1),  # sage
    (0.65, 0.78, 0.61, 1),  # mint
    (0.77, 0.79, 0.68, 1),  # cream
    (0.48, 0.57, 0.55, 1),  # pale steel
    (0.13, 0.23, 0.18, 1),  # deep green seams
    (0.08, 0.15, 0.17, 1),  # recess
    (0.10, 0.38, 0.70, 1),  # cobalt canopy
    (0.27, 0.62, 0.86, 1),  # sky blue facets
    (0.68, 0.77, 0.72, 1),  # bright steel
    (0.29, 0.43, 0.36, 1),  # underside
    (0.82, 0.69, 0.29, 1),  # small warm marking
    (0.35, 0.57, 0.51, 1),
    (0.22, 0.27, 0.28, 1),
    (0.55, 0.70, 0.58, 1),
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.8
    mats.append(mat)

body = []
def mesh_obj(name, verts, faces, indices=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if indices:
        for p, idx in zip(mesh.polygons, indices):
            p.material_index = idx
    body.append(obj)
    return obj

def loft(name, rings, side_mats, cap_top=1, cap_bottom=11):
    # rings are ordered outlines at successive heights, with matching vertex count
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    inds = [cap_bottom]
    for j in range(len(rings)-1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
            inds.append(side_mats[j] if isinstance(side_mats[j], int) else side_mats[j][i])
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    inds.append(cap_top)
    return mesh_obj(name, verts, faces, inds)

def plate(name, outline, z0, z1, material, bevel=0.008, edge=6):
    # inset top gives each armor plate a physical chamfer
    cx = sum(x for x,y in outline)/len(outline)
    cy = sum(y for x,y in outline)/len(outline)
    top = []
    for x,y in outline:
        d = Vector((cx-x, cy-y))
        if d.length: d.normalize()
        top.append((x+d.x*bevel, y+d.y*bevel, z1))
    return loft(name, [[(x,y,z0) for x,y in outline], top], [edge], material, edge)

def box(name, loc, scale, mat, bevel=0.0, rot=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    obj.rotation_euler[2] = rot
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    body.append(obj)
    return obj

def ring_y(name, x, y, z, radii, ys, material_indices, segments=12):
    # Consecutive rings form the external lip and the recessed dark bore.
    verts = []
    for radius, yy in zip(radii, ys):
        for k in range(segments):
            a = 2*math.pi*k/segments
            verts.append((x+radius*math.cos(a), yy, z+radius*math.sin(a)))
    faces, indices = [], []
    for j in range(len(radii)-1):
        for k in range(segments):
            faces.append((j*segments+k, j*segments+(k+1)%segments,
                          (j+1)*segments+(k+1)%segments, (j+1)*segments+k))
            indices.append(material_indices[j])
    faces.append(tuple((len(radii)-1)*segments+k for k in range(segments)))
    indices.append(7)
    return mesh_obj(name, verts, faces, indices)

# Narrow command wedge, separated from the engine booms by open lanes.
def scaled_ring(points, sx, sy, z):
    return [(x*sx,y*sy,z) for x,y in points]
outline = [(-.075,-.255),(.075,-.255),(.105,-.19),(.105,-.06),(.083,.13),(.045,.265),(0,.34),(-.045,.265),(-.083,.13),(-.105,-.06),(-.105,-.19)]
loft('Command hull', [scaled_ring(outline,.78,.97,-.055),scaled_ring(outline,1,1,-.025),scaled_ring(outline,1,1,.015),scaled_ring(outline,.83,.98,.047)], [11,0,1],2,11)
plate('Dorsal shell',[(-.062,-.21),(.062,-.21),(.083,-.14),(.079,.10),(.046,.25),(0,.315),(-.046,.25),(-.079,.10),(-.083,-.14)],.047,.053,1,.005,0)
plate('Forward armour',[(-.045,.155),(-.034,.25),(0,.318),(.034,.25),(.045,.155),(.032,.14),(-.032,.14)],.054,.059,3,.004,0)
plate('Nose marking',[(-.013,.26),(0,.299),(.013,.26),(.011,.248),(-.011,.248)],.060,.062,4,.002,2)
# Round, flush blue canopy: centered at (0, .08), 0.10 across, top z=.079.
def canopy_ring(radius,z):
    return [(radius*math.cos(2*math.pi*k/12),.08+radius*math.sin(2*math.pi*k/12),z) for k in range(12)]
loft('Canopy gasket',[canopy_ring(.052,.054),canopy_ring(.052,.057)],[6],6,6)
loft('Blue canopy',[canopy_ring(.05,.057),canopy_ring(.043,.069),canopy_ring(.025,.077),canopy_ring(.006,.079)],[[8,8,8,9,9,9,9,9,8,8,8,8],[8,8,9,9,9,9,9,9,9,8,8,8],[8,9,9,9,9,9,9,9,9,9,8,8]],9,8)
for s, label in [(-1,'L'),(1,'R')]:
    def mirror(poly):
        p = [(s*x,y) for x,y in poly]
        return p if s == 1 else list(reversed(p))
    # Short swept bridges leave large apertures to either side of the hull.
    plate('Swept bridge '+label,mirror([(.075,-.105),(.155,-.165),(.215,-.105),(.213,-.069),(.091,-.035)]),-.035,.022,0,.006,11)
    plate('Bridge armour '+label,mirror([(.091,-.083),(.163,-.134),(.201,-.099),(.200,-.084),(.103,-.050)]),.023,.030,3,.004,0)
    pod = mirror([(.122,-.245),(.215,-.245),(.231,-.19),(.215,.065),(.195,.176),(.17,.245),(.146,.16),(.119,.015),(.108,-.15)])
    loft('Engine boom '+label,[scaled_ring(pod,.96,1,-.052),scaled_ring(pod,1,1,-.025),scaled_ring(pod,1,1,.021),scaled_ring(pod,.98,.98,.046)],[11,0,1],2,11)
    plate('Boom armour '+label,mirror([(.126,-.205),(.207,-.205),(.216,-.16),(.202,.045),(.183,.15),(.168,.195),(.149,.125),(.129,.01)]),.047,.054,1,.005,0)
    plate('Boom forward blade '+label,mirror([(.151,.085),(.17,.215),(.185,.245),(.196,.13),(.187,.072)]),.052,.059,3,.003,0)
    # Tapered sensor vanes are flat blades, not gun barrels.
    plate('Sensor vane '+label,mirror([(.181,.15),(.194,.315),(.205,.25),(.203,.12)]),.008,.025,5,.002,0)
    plate('Vane inset '+label,mirror([(.188,.18),(.195,.277),(.199,.245),(.198,.175)]),.026,.029,4,.001,5)
    ring_y('Engine nozzle '+label,s*.17,-.28,-.008,[.039,.039,.028,.028],[-.235,-.28,-.28,-.253],[5,4,7],12)
    plate('Outrigger pylon '+label,mirror([(.205,-.12),(.275,-.17),(.303,-.13),(.218,-.065)]),-.036,.017,0,.006,11)
    plate('Pylon cap '+label,mirror([(.218,-.107),(.272,-.146),(.287,-.127),(.226,-.077)]),.018,.026,3,.003,0)
    # Open-nosed clamps around a recessed, empty drone service bed.
    plate('Bay floor '+label,mirror([(.265,-.115),(.336,-.115),(.344,-.075),(.338,.038),(.319,.075),(.273,.075),(.257,.034)]),-.025,-.010,6,.004,11)
    plate('Bay bed '+label,mirror([(.273,-.09),(.326,-.09),(.331,-.06),(.326,.038),(.275,.038),(.268,-.06)]),-.009,-.005,13,.003,6)
    plate('Inner clamp '+label,mirror([(.248,-.13),(.267,-.113),(.277,-.065),(.275,.045),(.268,.103),(.255,.15),(.245,.08),(.246,-.075)]),-.026,.041,3,.004,0)
    plate('Outer clamp '+label,mirror([(.326,-.13),(.346,-.115),(.36,-.065),(.357,.065),(.343,.132),(.330,.094),(.337,.035),(.336,-.065)]),-.026,.041,3,.004,0)
    plate('Cradle heel '+label,mirror([(.257,-.138),(.336,-.138),(.347,-.112),(.333,-.094),(.264,-.094),(.251,-.111)]),-.025,.039,2,.004,0)
    plate('Heel stripe '+label,mirror([(.269,-.128),(.325,-.128),(.33,-.115),(.27,-.115)]),.040,.043,4,.002,5)
    for x in (.278,.322):
        box('Dock guide '+label,(s*x,-.025,-.002),(.006,.095,.006),5,.001)
    box('Cradle stop '+label,(s*.300,-.075,.005),(.042,.008,.012),14,.001)
    plate('Intake rim '+label,mirror([(.137,-.16),(.199,-.16),(.201,-.045),(.139,-.045)]),.055,.058,14,.003,6)
    plate('Intake recess '+label,mirror([(.143,-.151),(.193,-.151),(.195,-.052),(.144,-.052)]),.059,.061,7,.002,14)
    for y in (-.135,-.112,-.089,-.066):
        box('Cooling slat '+label,(s*.169,y,.063),(.046,.006,.004),5,.001)
    plate('Rear engine armour '+label,mirror([(.13,-.238),(.205,-.238),(.215,-.207),(.127,-.207)]),.047,.054,2,.003,0)
    box('Hazard field '+label,(s*.168,-.221,.056),(.057,.014,.003),12,.001)
    for x in (.148,.163,.178,.193):
        box('Hazard stripe '+label,(s*x,-.221,.058),(.005,.014,.002),7,0,-.4)
    plate('Belly panel '+label,mirror([(.13,-.205),(.205,-.205),(.207,-.08),(.137,-.08)]),-.056,-.052,14,.004,11)
plate('Rear spine panel',[(-.053,-.225),(.053,-.225),(.065,-.17),(.052,-.12),(-.052,-.12),(-.065,-.17)],.054,.060,2,.004,0)
for x in (-.034,.034):
    box('Rear vent well',(x,-.188,.062),(.038,.045,.004),7,.002)
    for y in (-.199,-.187,-.175):
        box('Rear vent slat',(x,y,.065),(.031,.004,.003),5,.001)
plate('Belly keel',[(-.055,-.20),(.055,-.20),(.074,-.10),(.055,.14),(0,.265),(-.055,.14),(-.074,-.10)],-.060,-.055,14,.004,11)
bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=.028,depth=.012,location=(0,-.105,.057))
pedestal = bpy.context.object
pedestal.name = 'Radar pedestal'
pedestal.data.materials.append(mats[0])
body.append(pedestal)

# Join and triangulate, leaving exactly one fully applied mesh object.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False

# The dish is a separate mesh with its origin at the spin pivot.
rings = [(.032,.004),(.037,.011),(.025,.008),(.008,.004)]
verts = [(r*math.cos(2*math.pi*k/12), r*math.sin(2*math.pi*k/12), z)
         for r,z in rings for k in range(12)]
faces = []
for j in range(len(rings)-1):
    for k in range(12):
        faces.append ( (j*12+k, j*12+(k+1)%12, (j+1)*12+(k+1)%12, (j+1)*12+k) )
faces.append(tuple(range(36,48)))
mesh = bpy.data.meshes.new('Radar mesh')
mesh.from_pydata(verts, [], faces)
mesh.update()
radar = bpy.data.objects.new('Radar', mesh)
bpy.context.collection.objects.link(radar)
radar.location = (0,-.105,.063)
for material in mats:
    mesh.materials.append(material)
for poly in mesh.polygons:
    poly.material_index = 5 if poly.index < 24 else 4
    poly.use_smooth = False
bpy.ops.object.select_all(action='DESELECT')
radar.select_set(True)
bpy.context.view_layer.objects.active = radar
tri = radar.modifiers.new('Triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
import json
with open(os.path.join(ROOT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([{'object':'Radar', 'motion':'spin', 'axis':[0,0,1],
                'pivot':[0,-.105,.063], 'speed':90}], f, indent=2)
    f.write('\n')
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
radar.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(ROOT, 'model.obj'), export_materials=True,
                      forward_axis='Y', up_axis='Z', export_selected_objects=True)

# Neutral studio illumination; rendered after export so lights/camera never enter the OBJ.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape = 'DISK'; data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj,target)
    return obj

sun = light('Sun', 'SUN', (-3,4,6), 2.0, (0,0,0))
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (3,-2,4), 18, (0,0,0), 4)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,2)
cam.rotation_euler = (0,0,0)  # local -Z down, local +Y toward screen up
cam_data.type = 'ORTHO'
cam_data.ortho_scale = .84
scene.render.filepath = os.path.join(ROOT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.05,1.25,1.15)
point_at(cam, (0,0,.005))
cam_data.type = 'PERSP'
cam_data.lens = 52
scene.render.filepath = os.path.join(ROOT,'angle.png')
bpy.ops.render.render(write_still=True)

