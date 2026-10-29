import bpy
import math
import os
import sys
from mathutils import Vector

# Snow mortar: a low, heavy artillery sled with armored stabilizer pods.
# The gun mouth and rear nozzle mouths are registered to the gameplay sockets.
def script_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            return os.path.dirname(os.path.abspath(args[0]))
    return os.getcwd()

OUT = script_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

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
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.8
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

parts = []
def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

def layered(name, outline, levels, mat):
    # Similar outlines at several heights produce substantial chamfered armor.
    n = len(outline)
    verts = [(x * scale, y * scale, z) for z, scale in levels for x, y in outline]
    faces = [tuple(reversed(range(n)))]
    for j in range(len(levels) - 1):
        for i in range(n):
            k = (i + 1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    faces.append(tuple((len(levels)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, mat)

def ellipsoid(name, center, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

def tube_y(name, x, z, front, back, outer, inner, mat, n=12):
    # A genuine annular opening and recessed inner bore, not a painted cap.
    verts = []
    for y, r in ((back, outer * .88), (front, outer), (front, inner), (front + (back-front)*.42, inner)):
        verts.extend((x + r*math.cos(2*math.pi*i/n), y,
                      z + r*math.sin(2*math.pi*i/n)) for i in range(n))
    faces = []
    for j in range(3):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    return mesh(name, verts, faces, mat)

def disk_y(name, x, y, z, radius, mat, n=12):
    verts = [(x, y, z)] + [(x + radius*math.cos(2*math.pi*i/n), y,
                                z + radius*math.sin(2*math.pi*i/n)) for i in range(n)]
    return mesh(name, verts, [(0, i+1, (i+1)%n+1) for i in range(n)], mat)

# Broad six-sided foundation and raised ice-colored central gun cradle.
base = [(-.49,.83),(.49,.83),(.73,.27),(.57,-.34),(-.57,-.34),(-.73,.27)]
layered('armored sled', base, [(-.10,.88),(.05,1.0),(.28,.93),(.36,.76)], 4)
cradle = [(-.28,.58),(.28,.58),(.38,-.53),(.31,-1.23),(-.31,-1.23),(-.38,-.53)]
layered('mortar cradle', cradle, [(.04,.97),(.20,1.0),(.43,.87),(.49,.70)], 1)

# Rounded side pontoons and a pair of distinct swept armor caps.
for s in (-1, 1):
    ellipsoid('stabilizer pod', (s*.72,.02,.15), (.39,.57,.25), 15)
    poly = [(s*.54,-.06),(s*.88,-.06),(s*.91,-.38),(s*.62,-.55)]
    if s < 0: poly.reverse()
    layered('front shoulder plate', poly, [(.24,.98),(.33,1.0),(.39,.89)], 6)
    poly = [(s*.53,.23),(s*.96,.21),(s*.97,.42),(s*.84,.57),(s*.58,.53)]
    if s < 0: poly.reverse()
    layered('rear pod armor', poly, [(.26,1.0),(.36,.94),(.40,.80)], 1)
    # Large inset on each pod, with a simple transverse brace.
    poly = [(s*.64,-.03),(s*.88,-.03),(s*.88,.24),(s*.64,.24)]
    if s < 0: poly.reverse()
    layered('recessed intake', poly, [(.385,1.0),(.395,1.0)], 11)
    poly = [(s*.67,.07),(s*.85,.07),(s*.85,.12),(s*.67,.12)]
    if s < 0: poly.reverse()
    layered('intake bridge', poly, [(.398,1.0),(.415,1.0)], 4)
    # Socket centers are at y=.70, z=0; mouths point toward +Y.
    tube_y('engine socket rim', s*.66, 0, .70, .53, .16, .105, 6)
    disk_y('engine socket shadow', s*.66, .635, 0, .104, 11)

# Elevated breech and an intentionally broad, short mortar barrel.
poly = [(-.22,.36),(.22,.36),(.25,-.48),(-.25,-.48)]
layered('breech shroud', poly, [(.45,1.0),(.54,.92),(.59,.68)], 4)
poly = [(-.15,.22),(.15,.22),(.16,-.38),(-.16,-.38)]
layered('breech inset', poly, [(.585,1.0),(.602,.94)], 11)
poly = [(-.11,.16),(.11,.16),(.11,-.25),(-.11,-.25)]
layered('breech armor strip', poly, [(.603,1.0),(.62,.90)], 6)
# Outer collar ends exactly at the projectile origin, with a dark recessed bore.
tube_y('mortar barrel', 0, .30, -1.46, -.43, .235, .145, 6, 12)
tube_y('muzzle lip', 0, .30, -1.46, -1.34, .26, .145, 1, 12)
disk_y('mortar bore shadow', 0, -1.37, .30, .144, 11)
# Upper barrel armor keeps the silhouette broad and shield-like in top view.
poly = [(-.34,-.91),(.34,-.91),(.40,-1.20),(.28,-1.43),(-.28,-1.43),(-.40,-1.20)]
layered('gun shield', poly, [(.07,1.0),(.15,1.0),(.30,.90),(.37,.70)], 6)
# A restrained cyan sight at the aft end of the central hull.
poly = [(-.15,.69),(.15,.69),(.15,.75),(-.15,.75)]
layered('sight housing', poly, [(.34,1.0),(.38,1.0)], 11)
poly = [(-.09,.70),(.09,.70),(.09,.735),(-.09,.735)]
layered('sight glass', poly, [(.381,1.0),(.39,1.0)], 9)

# Combine only the mesh parts, bake object transforms, and preserve flat faces.
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
# Export before adding presentation-only objects.
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA' and size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3,-4,7), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 180, (0,-.2,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,-.30,7)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.15
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.7,-5.1,4.0)
point_at(cam, (0,-.24,.18))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
