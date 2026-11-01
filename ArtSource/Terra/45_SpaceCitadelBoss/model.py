import bpy
import bmesh
import math
import json
import os
import sys
from mathutils import Vector

# Run with Blender 5.x: blender --background --python model.py
try:
    ROOT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    ROOT = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))
os.makedirs(ROOT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

COLORS = {
    '03': (0.95, 0.38, 0.12), '05': (0.54, 0.18, 0.16),
    '06': (0.105, 0.15, 0.20), '09': (0.16, 0.83, 0.91),
    '10': (1.0, 0.77, 0.27), '11': (0.055, 0.09, 0.12),
    '12': (0.62, 0.25, 0.19), '13': (0.48, 0.36, 0.18),
    '15': (0.66, 0.70, 0.71), '01': (0.78, 0.83, 0.79),
    '04': (0.32, 0.48, 0.55),
}
MATS = {}
for code, rgb in COLORS.items():
    mat = bpy.data.materials.new('tile_' + code)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = 0.68
    if code == '11':
        # Keep the horizon near-black regardless of preview lighting.
        dark = mat.node_tree.nodes.new('ShaderNodeEmission')
        dark.inputs['Color'].default_value = (0.004, 0.006, 0.009, 1)
        dark.inputs['Strength'].default_value = 1
        mat.node_tree.links.new(dark.outputs['Emission'], mat.node_tree.nodes.get('Material Output').inputs['Surface'])
    if code in ('09', '10'):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    MATS[code] = mat


def finish(obj, material):
    obj.data.materials.append(MATS[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj


def make_mesh(name, vertices, faces, material):
    # Construct polygonal forms with bmesh, retaining intentionally flat facets.
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    vs = [bm.verts.new(v) for v in vertices]
    for face in faces:
        bm.faces.new([vs[i] for i in face])
    bm.normal_update()
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material)


def annulus(name, inner, outer, z, depth, material, sy=1.0, segments=48):
    vertices = []
    for zz, radius in ((z-depth/2, inner), (z-depth/2, outer),
                       (z+depth/2, inner), (z+depth/2, outer)):
        for i in range(segments):
            a = 2 * math.pi * i / segments
            vertices.append((radius * math.cos(a), sy * radius * math.sin(a), zz))
    faces = []
    for i in range(segments):
        j = (i+1) % segments
        faces.extend([(i, j, segments+j, segments+i),
                      (2*segments+i, 3*segments+i, 3*segments+j, 2*segments+j),
                      (i, 2*segments+i, 2*segments+j, j),
                      (segments+i, segments+j, 3*segments+j, 3*segments+i)])
    return make_mesh(name, vertices, faces, material)


def spiral(name, phase, material, sy=0.73):
    vertices, faces = [], []
    count = 35
    for i in range(count):
        t = i / (count-1)
        a = phase + t * 3.2
        radius = 0.76 + 1.44*t
        width = 0.008 + 0.052 * t
        for r in (radius-width, radius+width):
            vertices.append((r*math.cos(a), sy*r*math.sin(a), -0.12 + 0.025*t))
    for i in range(count-1):
        faces.append((2*i, 2*i+1, 2*i+3, 2*i+2))
    return make_mesh(name, vertices, faces, material)


def sphere(name, radius, location, material, scale=(1, 1, 1), segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material)


def torus(name, major, minor, location, material, rotation=(0, 0, 0), scale=(1, 1, 1), major_segments=24, minor_segments=4):
    bpy.ops.mesh.primitive_torus_add(major_segments=major_segments, minor_segments=minor_segments,
                                    location=location, rotation=rotation,
                                    major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material)


def cylinder(name, radius, depth, location, material, rotation=(0, 0, 0), vertices=12, radius2=None):
    if radius2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius2, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, material)


def join(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    # All objects have the common local origin even when individual primitives were translated.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    return obj


def export(filename, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.wm.obj_export(filepath=os.path.join(ROOT, filename), export_selected_objects=True,
                          forward_axis='Y', up_axis='Z', export_materials=True,
                          export_triangulated_mesh=True)

# Raised, alternating fragments of infalling matter; their shared origin is the spin pivot.
disk_parts = []
for inner, outer, z, depth, mat in [
    (0.67, 0.77, -0.20, 0.08, '10'),
    (0.79, 0.98, -0.22, 0.10, '03'),
    (1.00, 1.18, -0.25, 0.09, '12'),
    (1.20, 1.38, -0.27, 0.08, '05'),
    (1.40, 1.58, -0.30, 0.08, '12'),
    (1.60, 1.78, -0.32, 0.07, '05'),
    (1.80, 2.01, -0.34, 0.07, '06'),
    (2.03, 2.25, -0.36, 0.06, '06'),
]:
    disk_parts.append(annulus('Matter band', inner, outer, z, depth, mat, sy=0.73))
for i, mat in enumerate(('12', '05', '12', '05')):
    disk_parts.append(spiral('Infall streamer', i*math.pi/2 + 0.3, mat))
disk = join(disk_parts, 'Disk')
# Static edge of the well: circular opening is clear to radius 0.61.
body = join([annulus('Photon ring', 0.61, 0.635, -0.09, 0.035, '10', segments=48),
             annulus('Dark well lip', 0.64, 0.70, -0.18, 0.08, '11', segments=48)], 'Model')
export('hull.obj', [body, disk])
body.name = 'Hull preview'
with open(os.path.join(ROOT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([{'object': 'Disk', 'motion': 'spin', 'axis': [0, 0, 1],
                'pivot': [0, 0, 0], 'speed': 32}], f, indent=2)
    f.write('\n')

# Compact, bright captured star with three differently tilted gravitational lens cages.
p = [sphere('Pulsar heart', .205, (0, 0, .12), '09', segments=16, rings=8),
     sphere('Pulsar bright centre', .115, (0, 0, .28), '01', scale=(1, 1, .6), segments=12, rings=6),
     torus('Equatorial lens', .31, .037, (0, 0, .12), '13', scale=(1, 1.24, 1)),
     torus('Tilted lens', .32, .032, (0, 0, .12), '15', rotation=(.75, .35, .35), scale=(1, 1.32, 1)),
     torus('Crossed lens', .31, .026, (0, 0, .12), '04', rotation=(-.72, .3, -.45), scale=(1, 1.32, 1)),
     sphere('Upper polar knot', .09, (0, .47, .12), '13', segments=12, rings=6),
     sphere('Lower polar knot', .08, (0, -.47, .12), '13', segments=12, rings=6)]
module0 = join(p, 'Model')
export('module0.obj', [module0])
module0.name = 'Generator source'

# Funnel axis points down the screen; two tips terminate at the registered muzzle coordinates.
p = [cylinder('Magnetic funnel', .20, .67, (0, .04, .03), '06', rotation=(math.pi/2, 0, 0), radius2=.32),
     sphere('Emitter cap', .29, (0, .32, .04), '13', scale=(1, .65, .62), segments=12, rings=6)]
for y, radius in ((.24, .29), (.06, .255), (-.12, .22), (-.28, .19)):
    p.append(torus('Field coil', radius, .035, (0, y, .03), '15', rotation=(math.pi/2, 0, 0), major_segments=16))
for side in (-1, 1):
    p.append(cylinder('Twin muzzle', .095, .43, (side*.16, -.46, .08), '11', rotation=(math.pi/2, 0, 0), vertices=10))
    p.append(torus('Muzzle rim', .077, .018, (side*.16, -.675, .08), '13', rotation=(math.pi/2, 0, 0), major_segments=12))
module1 = join(p, 'Model')
export('module1.obj', [module1])
module1.name = 'Jet source'

# Black sphere and a narrow lens around its silhouette, leaving the centre unadorned.
p = [sphere('Event horizon', .50, (0, 0, 0), '11', segments=24, rings=12),
     torus('Photon filament', .518, .018, (0, 0, .02), '10', major_segments=48),
     torus('Lensed rim', .498, .008, (0, 0, .12), '09', major_segments=48)]
module2 = join(p, 'Model')
export('module2.obj', [module2])
module2.name = 'Core source'

# Solid faceted crescent, occupying the left half of the horizon in the closed position.
def shutter():
    n = 16
    verts = []
    for z in (.60, .76):
        for i in range(n+1):
            t = -math.pi/2 + math.pi*i/n
            y = .67*math.sin(t)
            outer = -.045 - .56*math.cos(t)
            inner = -.018 - .025*math.cos(t)
            verts.extend([(outer, y, z), (inner, y, z)])
    layer = 2*(n+1)
    faces = []
    for i in range(n):
        a = 2*i
        faces.extend([(a, a+2, a+3, a+1),
                      (layer+a, layer+a+1, layer+a+3, layer+a+2),
                      (a, layer+a, layer+a+2, a+2),
                      (a+1, a+3, layer+a+3, layer+a+1)])
    faces.extend([(0, 1, layer+1, layer),
                  (2*n, layer+2*n, layer+2*n+1, 2*n+1)])
    return make_mesh('Crescent lens fragment', verts, faces, '05')

p = [shutter()]
# An inset arc follows the outer lip; it is matter, not an illuminated UI element.
verts, faces = [], []
for i in range(17):
    t = -math.pi/2 + math.pi*i/16
    y = .62*math.sin(t)
    x = -.045 - .51*math.cos(t)
    verts.extend([(x, y, .77), (x+.045, y, .77)])
for i in range(16):
    faces.append((2*i, 2*i+1, 2*i+3, 2*i+2))
p.append(make_mesh('Crescent striation', verts, faces, '12'))
# A narrow hot inner edge follows the side nearest the horizon.
verts, faces = [], []
for i in range(17):
    t = -math.pi/2 + math.pi*i/16
    y = .67*math.sin(t)
    x = -.018 - .025*math.cos(t)
    verts.extend([(x-.025, y, .771), (x, y, .771)])
for i in range(16):
    faces.append((2*i, 2*i+1, 2*i+3, 2*i+2))
p.append(make_mesh('Hot inner edge', verts, faces, '03'))
module3 = join(p, 'Model')
export('module3.obj', [module3])
module3.name = 'Shutter source'

# Preview assembly: duplicate mesh data through object links, leaving exports untouched.
for obj in (module0, module1, module2, module3):
    obj.hide_render = True

def instance(source, name, location, mirror=False):
    obj = source.copy()
    obj.data = source.data
    bpy.context.collection.objects.link(obj)
    obj.name = name
    obj.hide_render = False
    obj.location = location
    if mirror:
        obj.scale.x = -1
    return obj

for x in (-1.95, 1.95):
    instance(module0, 'Shield anchor', (x, .25, 0))
for x in (-1.13, 1.13):
    instance(module1, 'Jet emitter', (x, -1.0, 0))
instance(module2, 'Event horizon', (0, 0, 0))
instance(module3, 'Left shutter open', (-.72, .12, 0))
instance(module3, 'Right shutter open', (.72, .12, 0), mirror=True)

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = scene.render.resolution_y = 768
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
world = bpy.data.worlds.new('Deep space') if bpy.data.worlds.get('Deep space') is None else bpy.data.worlds['Deep space']
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.02, 0.025, 0.04, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1


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

light('Upper left sun', 'SUN', (-4, 5, 7), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (3, -4, 6), 350, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Preview camera')
cam = bpy.data.objects.new('Preview camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 10)
aim(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 5.35
scene.render.filepath = os.path.join(ROOT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 48
cam.location = (5.8, -7.8, 8.7)
aim(cam, (0, 0, 0))
scene.render.filepath = os.path.join(ROOT, 'angle.png')
bpy.ops.render.render(write_still=True)

