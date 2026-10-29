import bpy
import math
import os
import sys
from mathutils import Vector

# Baby You: the original player-ship silhouette, enlarged to pickup scale.
def output_dir():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except NameError:
        args = sys.argv
        return os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

OUT = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.115, 0.135, 0.16, 1),  # black armour, lifted for legibility
    (0.23, 0.26, 0.29, 1),     # dark structural edges
    (0.42, 0.46, 0.48, 1),     # grey machined rims
    (0.95, 0.66, 0.055, 1),    # yellow identity bands
    (1.0, 0.82, 0.12, 1),      # yellow core
    (0.76, 0.34, 0.055, 1),    # orange engine caps
    (0.31, 0.34, 0.36, 1),     # engine/vent grey
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.75
    mats.append(mat)

parts = []
# Coordinates follow the source sprite, keeping the same relative positions.
S = 0.0275
def P(x, y):
    return ((x - 16.5) * S, (17.0 - y) * S)
def outline(points, mirror=False):
    pts = [P(x, y) for x, y in points]
    if mirror:
        pts = [(-x, y) for x, y in reversed(pts)]
    # Ensure a consistent counterclockwise winding.
    if sum(pts[i][0]*pts[(i+1)%len(pts)][1] - pts[(i+1)%len(pts)][0]*pts[i][1] for i in range(len(pts))) < 0:
        pts.reverse()
    return pts

def plate(points, back, front, surface=0, edge=1, rim=2, bevel=0.07, mirror=False):
    pts = outline(points, mirror)
    cx = sum(x for x, y in pts) / len(pts)
    cy = sum(y for x, y in pts) / len(pts)
    # Four perimeter rings form a solid with shallow, visibly faceted bevels.
    rings = []
    for z, fac in ((back, 1-bevel), (back+0.012, 1), (front-0.012, 1), (front, 1-bevel)):
        rings.append([(cx+(x-cx)*fac, cy+(y-cy)*fac, z) for x, y in pts])
    n = len(pts)
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n))), tuple(3*n+i for i in range(n))]
    indices = [surface, surface]
    for j in range(3):
        for i in range(n):
            k = (i+1)%n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
            indices.append(rim if j in (0, 2) else edge)
    mesh = bpy.data.meshes.new('plate')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('plate', mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for f, idx in zip(mesh.polygons, indices):
        f.material_index = idx
        f.use_smooth = False
    parts.append(obj)
    return obj

def rect(cx, cy, w, h, z0, z1, surface, edge=1, rim=2):
    return plate([(cx-w/2,cy-h/2),(cx+w/2,cy-h/2),(cx+w/2,cy+h/2),(cx-w/2,cy+h/2)], z0, z1, surface, edge, rim, 0.12)

def tube(px, py0, py1, z, radius, material, endmat=None, vertices=8):
    x, y0 = P(px, py0)
    _, y1 = P(px, py1)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=abs(y1-y0), location=(x,(y0+y1)/2,z), rotation=(math.pi/2,0,0))
    obj = bpy.context.object
    for mat in mats:
        obj.data.materials.append(mat)
    for f in obj.data.polygons:
        f.material_index = material if len(f.vertices) == 4 or endmat is None else endmat
        f.use_smooth = False
    parts.append(obj)

# The body, center spike, swept wing supports and tall outer blades.
plate([(9,9.5),(24,9.5),(26,12),(26,19),(22,21.5),(19,25.5),(14,25.5),(11,21.5),(7,19),(7,12)], -0.085, 0.085, 0, 1, 2, 0.10)
plate([(14.5,9.6),(16.5,5),(18.5,9.6)], -0.045, 0.075, 0, 1, 2, 0.08)
for mirror in (False, True):
    plate([(3.8,11),(11,3),(13,9.6),(9,12),(7.5,19.5),(3.8,21)], -0.065, 0.065, 0, 1, 2, 0.08, mirror)
    plate([(0.5,8),(2,4.5),(3.5,4),(3.8,12),(3.8,26),(3.5,30),(2,30),(0.5,26)], -0.11, 0.11, 0, 1, 2, 0.10, mirror)
    plate([(9,12.5),(12.5,11),(12.5,18),(14,21),(10,20),(8.5,17)], 0.065, 0.135, 0, 1, 2, 0.10, mirror)
    plate([(8.9,12.5),(11,11.7),(11,12.6),(8.9,13.4)], 0.132, 0.143, 3, 3, 3, 0.05, mirror)

# Original yellow hexagonal cockpit and its small central hitbox hexagon.
plate([(16.5,9.5),(19.5,12),(19.5,17),(16.5,19.5),(13.5,17),(13.5,12)], 0.09, 0.16, 4, 3, 3, 0.08)
hexpts = [(16.5 + 1.5*math.cos(math.pi/2+i*math.tau/6), 14.5 + 1.5*math.sin(math.pi/2+i*math.tau/6)) for i in range(6)]
plate(hexpts, 0.158, 0.175, 4, 4, 4, 0.04)

# Paired gun barrels, engine pods, and the original small blade markings.
for px in (5.6, 27.4):
    tube(px, 28.5, 19.5, 0.035, 0.036, 6, 1)
    tube(px, 29.3, 28.5, 0.035, 0.044, 1, 5)
    rect(px, 23, 2.7, 3.7, 0.09, 0.13, 6, 1, 2)
for px in (10, 23):
    tube(px, 9, 3.5, 0.085, 0.021, 2, 1, 6)
    rect(px, 8, 2.1, 1.6, 0.09, 0.12, 3, 1, 3)
for px in (2.2, 30.8):
    rect(px, 7.5, 1.3, 2.1, 0.105, 0.13, 3, 1, 3)
    for py in (10, 17, 24):
        rect(px, py, 2.0, 0.8, 0.105, 0.132, 2, 1, 2)
for py, width in ((21.7,5.1),(23,4.2),(24.3,3.3)):
    rect(16.5, py, width, 0.7, 0.09, 0.118, 2, 1, 2)
plate([(14.5,25),(18.5,25),(17.5,28),(15.5,28)], -0.06, 0.08, 0, 1, 2, 0.1)

# Join, center on the bounding box, and bake all object transforms.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
coords = [v.co for v in model.data.vertices]
center = Vector(tuple((min(v[i] for v in coords)+max(v[i] for v in coords))/2 for i in range(3)))
for v in model.data.vertices:
    v.co -= center
for poly in model.data.polygons:
    poly.use_smooth = False
model.data.update()
triangles = sum(len(p.vertices)-2 for p in model.data.polygons)
assert 300 <= triangles <= 2000, 'Triangle budget: %d' % triangles
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True, export_materials=True, export_triangulated_mesh=True, forward_axis='Y', up_axis='Z')

# Presentation: cameras look toward the origin with +Y as image up.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue grey studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    point_at(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 1, 5), 100, 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (1.6, 1.05, 2.65)
point_at(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
