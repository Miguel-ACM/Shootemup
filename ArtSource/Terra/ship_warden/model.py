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
    (0.24, 0.39, 0.25, 1),  # forest green
    (0.34, 0.52, 0.31, 1),  # leaf green
    (0.46, 0.64, 0.43, 1),  # sage
    (0.65, 0.78, 0.61, 1),  # mint
    (0.77, 0.79, 0.68, 1),  # cream
    (0.48, 0.57, 0.55, 1),  # pale steel
    (0.13, 0.23, 0.18, 1),  # deep green seams
    (0.08, 0.15, 0.17, 1),  # recess
    (0.10, 0.38, 0.70, 1),  # cobalt canopy
    (0.27, 0.62, 0.86, 1),  # sky blue facets
    (0.68, 0.77, 0.72, 1),  # bright steel
    (0.29, 0.43, 0.36, 1),  # underside
    (0.73, 0.67, 0.40, 1),  # small warm marking
    (0.35, 0.57, 0.51, 1),
    (0.19, 0.31, 0.27, 1),
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

# Faceted chisel keel with swept shoulders and a pointed forward prow.
outline = [(-.12,-.255),(.12,-.255),(.215,-.195),(.235,-.065),(.225,.115),(.15,.225),(.065,.305),(0,.34),(-.065,.305),(-.15,.225),(-.225,.115),(-.235,-.065),(-.215,-.195)]
def scaled_ring(points, sx, sy, z):
    return [(x*sx,y*sy,z) for x,y in points]
loft('Armoured keel', [scaled_ring(outline,.82,.92,-.055), scaled_ring(outline,1,1,-.020), scaled_ring(outline,.97,.99,.025), scaled_ring(outline,.84,.94,.055)], [11,0,1], 2, 11)
# Center dorsal shell and nose layers.
plate('Dorsal armor', [(-.105,-.22),(.105,-.22),(.15,-.14),(.142,.09),(.10,.195),(.045,.285),(0,.315),(-.045,.285),(-.10,.195),(-.142,.09),(-.15,-.14)], .056,.068,1,.006,0)
plate('Raised spine', [(-.055,-.19),(.055,-.19),(.074,-.11),(.068,.13),(.048,.23),(0,.29),(-.048,.23),(-.068,.13),(-.074,-.11)], .069,.084,2,.006,0)
plate('Nose shield', [(-.068,.205),(-.045,.275),(0,.318),(.045,.275),(.068,.205),(.048,.184),(-.048,.184)], .069,.078,3,.004,0)
plate('Nose chevron', [(-.027,.262),(0,.297),(.027,.262),(.021,.252),(-.021,.252)], .079,.082,4,.002,2)
# Raised blue faceted canopy, distinctly visible from gameplay view.
canopy_base = [(-.067,.065),(-.073,.145),(-.048,.205),(.048,.205),(.073,.145),(.067,.065),(.038,.036),(-.038,.036)]
plate('Canopy gasket', canopy_base, .080,.087,6,.003,6)
loft('Blue canopy', [[(x*.90,y,.087) for x,y in canopy_base], [(-.045,.080,.111),(-.050,.141,.116),(-.032,.184,.106),(.032,.184,.106),(.050,.141,.116),(.045,.080,.111),(.026,.054,.100),(-.026,.054,.100)]], [[8,8,9,9,9,8,8,8]], 9, 8)
# A short transverse canopy frame reads as a structural detail, not a weapon.
box('Canopy frame', (0,.067,.108), (.092,.009,.006), 5, .002)

# Outboard support wings and U-shaped drone cradles, open toward the nose.
for s, label in [(-1,'L'),(1,'R')]:
    def mirror(poly):
        p = [(s*x,y) for x,y in poly]
        return p if s == 1 else list(reversed(p))
    plate('Wing '+label, mirror([(.175,-.18),(.285,-.19),(.335,-.125),(.35,-.015),(.335,.105),(.275,.155),(.205,.115)]), -.012,.026,1,.012,0)
    # Dark bay floor, edged by separated arms and rear sill. Empty central area signals docking.
    plate('Bay recess '+label, mirror([(.255,-.103),(.331,-.103),(.342,-.045),(.338,.065),(.319,.092),(.264,.092),(.250,.065)]), .027,.030,6,.003,6)
    plate('Bay deck '+label, mirror([(.263,-.080),(.319,-.080),(.327,-.040),(.323,.052),(.267,.052),(.258,-.040)]), .031,.033,13,.003,6)
    plate('Inner clamp '+label, mirror([(.247,-.105),(.264,-.093),(.270,.045),(.282,.081),(.271,.103),(.251,.082),(.241,.035)]), .034,.058,3,.004,2)
    plate('Outer clamp '+label, mirror([(.323,-.103),(.341,-.092),(.356,-.041),(.355,.059),(.339,.096),(.325,.080),(.333,.043),(.334,-.037)]), .034,.058,3,.004,2)
    plate('Cradle heel '+label, mirror([(.261,-.105),(.325,-.105),(.334,-.083),(.322,-.072),(.265,-.072),(.253,-.084)]), .034,.053,4,.004,5)
    # Wing root armor and a single stripe distinguish the silhouette without busy detail.
    plate('Shoulder '+label, mirror([(.149,-.12),(.218,-.13),(.245,-.09),(.241,.092),(.210,.16),(.154,.13)]), .038,.056,2,.006,0)
    plate('Shoulder stripe '+label, mirror([(.199,.065),(.219,.065),(.216,.115),(.204,.132)]), .057,.060,4,.002,4)
    # Swept leading edge and a raised sensor fin on each shoulder.
    plate('Leading shoulder '+label, mirror([(.174,.16),(.205,.20),(.258,.15),(.286,.12),(.269,.098),(.222,.113)]), .025,.043,0,.004,11)
    plate('Fin base '+label, mirror([(.185,.075),(.204,.078),(.214,.14),(.203,.16),(.189,.145)]), .057,.061,4,.002,2)
    loft('Sensor fin '+label, [[(s*.19,.08,.060),(s*.205,.08,.060),(s*.211,.147,.060),(s*.196,.15,.060)], [(s*.193,.09,.090),(s*.202,.09,.090),(s*.207,.137,.078),(s*.199,.14,.078)]], [5], 4, 2)
    # Rear engine housings blend into the hull; openings end exactly at y=-.28.
    plate('Engine pod '+label, mirror([(.112,-.255),(.215,-.255),(.232,-.205),(.218,-.133),(.125,-.133),(.107,-.205)]), -.025,.048,0,.007,11)
    ring_y('Nozzle '+label, s*.17, -.28, -.004, [.041,.041,.030,.030], [-.245,-.28,-.28,-.255], [5,4,7], 12)
    # Underside plate is visible in rolls.
    plate('Underwing '+label, mirror([(.15,-.16),(.275,-.15),(.31,-.09),(.29,.075),(.22,.11),(.15,.08)]), -.055,-.050,11,.006,14)

# Fixed socket for the rotating radar dish.
bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=.028, depth=.013, location=(0,-.105,.085))
pedestal = bpy.context.object
pedestal.name = 'Radar pedestal'
pedestal.data.materials.append(mats[0])
body.append(pedestal)
# Rear deck panels and small vents, arranged as a single assembly.
plate('Rear deck', [(-.087,-.235),(-.103,-.205),(-.092,-.15),(.092,-.15),(.103,-.205),(.087,-.235)], .058,.068,2,.005,0)
for x in (-.051, .051):
    box('Cooling recess', (x,-.199,.070), (.042,.033,.004), 6, .003)
    for y in (-.207,-.197,-.187):
        box('Cooling vane', (x,y,.073), (.032,.003,.003), 5, .001)

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
radar.location = (0,-.105,.092)
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
                'pivot':[0,-.105,.092], 'speed':90}], f, indent=2)
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

