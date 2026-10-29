import bpy
import math
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- directory-or-script-path]
def output_directory():
    path = globals().get('__file__')
    if path:
        return os.path.dirname(os.path.abspath(path))
    args = sys.argv
    if '--' in args and len(args) > args.index('--') + 1:
        path = os.path.abspath(args[args.index('--') + 1])
        return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()

OUT = output_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, rgb in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    mats.append(mat)

parts = []
def mesh(name, verts, faces, face_tiles):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for material in mats:
        data.materials.append(material)
    for face, tile in zip(data.polygons, face_tiles):
        face.material_index = tile
        face.use_smooth = False
    parts.append(obj)
    return obj

def armor(name, outline, bottom, shoulder, crown, scale, tile, side=11):
    # Inset upper ring creates a faceted, sloping armored rim.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    verts = [(x, y, bottom) for x, y in outline]
    verts += [(x, y, shoulder) for x, y in outline]
    verts += [(cx + (x-cx)*scale, cy + (y-cy)*scale, crown) for x, y in outline]
    faces = [tuple(reversed(range(n)))]
    tiles = [side]
    for ring in range(2):
        for i in range(n):
            j = (i + 1) % n
            faces.append((ring*n+i, ring*n+j, (ring+1)*n+j, (ring+1)*n+i))
            tiles.append(side if ring == 0 else tile)
    faces.append(tuple(range(2*n, 3*n)))
    tiles.append(tile)
    return mesh(name, verts, faces, tiles)

def tube(name, x, mouth_y, z, radius, depth, direction, outside, inner=11):
    # Axis along Y; the annular face lies precisely on the gameplay point.
    n = 8
    verts = []
    for y, r in ((mouth_y - direction*depth, radius*0.88),
                 (mouth_y, radius), (mouth_y, radius*0.58),
                 (mouth_y - direction*depth*0.55, radius*0.53)):
        for i in range(n):
            a = 2*math.pi*i/n + math.pi/8
            verts.append ( (x + r*math.cos(a), y, z + r*math.sin(a)) )
    faces, tiles = [], []
    for ring in range(3):
        for i in range(n):
            j = (i+1) % n
            faces.append((ring*n+i, ring*n+j, (ring+1)*n+j, (ring+1)*n+i))
            tiles.append(outside if ring < 2 else inner)
    faces.append(tuple(range(3*n, 4*n)))
    tiles.append(inner)
    mesh(name, verts, faces, tiles)

# The dark central keel, pointed in -Y, has a raised and faceted spine.
armor('Fuselage', [(-.19,.67),(-.28,.22),(-.25,-.40),(0,-1.36),(.25,-.40),(.28,.22),(.19,.67)], -.12, .10, .25, .72, 0)
armor('Nose armor', [(-.16,-.42),(0,-1.30),(.16,-.42),(.13,-.16),(-.13,-.16)], .12, .20, .27, .77, 4, 0)

for s in (-1, 1):
    def mirror(points):
        result = [(s*x,y) for x,y in points]
        return result if s == 1 else list(reversed(result))
    # Cranked, swept delta wing retains the characteristic original outline.
    armor('Swept wing', mirror([(.12,-.65),(.39,-.18),(1.13,.60),(1.03,.77),(.68,.56),(.25,.09)]), -.10, .06, .15, .88, 0)
    armor('Wing shoulder', mirror([(.27,-.25),(.43,-.12),(.96,.54),(.82,.53),(.38,.05)]), .145, .155, .18, .94, 4, 0)
    # Large single painted slash remains readable in the top-down game view.
    armor('Wing stripe', mirror([(.42,-.06),(.50,.04),(.89,.53),(.81,.52)]), .181, .184, .19, .98, 1, 1)
    armor('Wing tip cap', mirror([(.94,.52),(1.13,.60),(1.03,.77),(.89,.67)]), .04, .11, .16, .88, 6, 0)
    # Engine cowls terminate at the fixed sockets; the dark annular mouths are unlit.
    x = s*.43
    armor('Engine nacelle', [(x-.13,.24),(x-.16,.64),(x-.12,.88),(x+.12,.88),(x+.16,.64),(x+.13,.24)], -.11, .10, .26, .77, 4, 11)
    armor('Cowl dorsal panel', [(x-.07,.34),(x-.09,.69),(x+.09,.69),(x+.07,.34)], .25, .26, .285, .86, 0, 0)
    tube('Engine socket', x, .91, 0.0, .115, .14, 1, 6)
    # Wing-root cannon housing leads to the fixed barrel mouth.
    armor('Cannon fairing', [(x-.09,-.68),(x-.11,-.35),(x+.11,-.35),(x+.09,-.68)], .04, .14, .22, .75, 0)
    tube('Wing muzzle', x, -.79, .20, .064, .18, -1, 6)

# The nose muzzle's center is exactly (0, -1.38, .15).
tube('Nose muzzle', 0, -1.38, .15, .055, .14, -1, 6)
# Faceted opaque canopy: dark surround and two steel-blue glazing facets.
armor('Canopy frame', [(-.15,-.54),(-.16,-.16),(-.10,.01),(0,.10),(.10,.01),(.16,-.16),(.15,-.54),(0,-.61)], .235, .28, .34, .82, 11, 11)
mesh('Canopy glazing', [(-.105,-.48,.345),(-.115,-.17,.35),(0,.025,.38),(.115,-.17,.35),(.105,-.48,.345),(0,-.53,.37),(0,-.17,.425)],
     [(0,1,6,5),(1,2,6),(2,3,6),(3,4,5,6)], [8,4,6,4])
armor('Rear spine', [(-.12,.14),(-.13,.48),(0,.59),(.13,.48),(.12,.14)], .20, .25, .32, .78, 0)
armor('Spine marking', [(-.035,.20),(-.035,.44),(.035,.44),(.035,.20)], .321, .323, .33, .95, 1, 1)

# Merge before export; every piece is real geometry and all faces stay flat shaded.
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
scene.world = bpy.data.worlds.new('Studio world') if scene.world is None else scene.world
scene.world.use_nodes = True
world_bg = scene.world.node_tree.nodes.get('Background')
world_bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world_bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

light('Sun', 'SUN', (-3, -4, 7), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 5), 180, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.75
cam.location = (0, -.235, 7)
cam.rotation_euler = (0, 0, 0)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.5, -5.0, 4.0)
aim(cam, (0, -.20, .08))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
