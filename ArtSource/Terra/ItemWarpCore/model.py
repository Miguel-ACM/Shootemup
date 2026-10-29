import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve the output folder even when launched with Blender's -- argument convention.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials:
    bpy.data.materials.remove(data)

palette = [
    (0.48, 0.61, 0.64, 1),  # blue-gray metal
    (0.72, 0.80, 0.78, 1),  # pale metal bevels
    (0.30, 0.43, 0.48, 1),  # metal recesses
    (0.15, 0.78, 0.86, 1),  # cyan crystal
    (0.36, 0.94, 0.94, 1),  # bright crystal facets
    (0.10, 0.56, 0.69, 1),  # deeper crystal facets
    (0.48, 0.39, 0.66, 1),  # violet warp ring
    (0.70, 0.61, 0.85, 1),  # violet bevel
    (1.00, 0.72, 0.22, 1),  # golden core
    (1.00, 0.88, 0.48, 1),  # golden highlight
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def mesh(name, vertices, faces, indices):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vertices, [], faces)
    me.materials.clear()
    for mat in mats:
        me.materials.append(mat)
    me.update()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    for poly, index in zip(me.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    parts.append(obj)
    return obj

def lathe(name, profile, n, side_mats, phase=math.pi / 8):
    # A surface of revolution around Y. Profiles are ordered from bottom to top.
    verts = [(r * math.cos(phase + 2 * math.pi * i / n), y,
              r * math.sin(phase + 2 * math.pi * i / n))
             for y, r in profile for i in range(n)]
    faces = [tuple(reversed(range(n)))]
    indices = [side_mats[0]]
    for j in range(len(profile) - 1):
        for i in range(n):
            k = (i + 1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
            indices.append(side_mats[j])
    faces.append(tuple((len(profile)-1)*n+i for i in range(n)))
    indices.append(side_mats[-1])
    return mesh(name, verts, [tuple(reversed(f)) for f in faces], indices)

def rod(name, a, b, radius, material, n=8):
    a, b = Vector(a), Vector(b)
    axis = (b-a).normalized()
    u = axis.cross(Vector((0, 0, 1)))
    if u.length < 0.01:
        u = axis.cross(Vector((0, 1, 0)))
    u.normalize()
    v = axis.cross(u).normalized()
    verts = [tuple(p + radius * (math.cos(2*math.pi*i/n)*u + math.sin(2*math.pi*i/n)*v))
             for p in (a, b) for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, verts, faces, [material]*len(faces))

def torus_y(name, major, minor, y, major_segments, minor_segments):
    verts = []
    for i in range(major_segments):
        a = 2*math.pi*i/major_segments
        for j in range(minor_segments):
            b = 2*math.pi*j/minor_segments
            r = major + minor*math.cos(b)
            verts.append((r*math.cos(a), y + minor*math.sin(b), r*math.sin(a)))
    faces, inds = [], []
    for i in range(major_segments):
        for j in range(minor_segments):
            faces.append((i*minor_segments+j, ((i+1)%major_segments)*minor_segments+j,
                          ((i+1)%major_segments)*minor_segments+(j+1)%minor_segments,
                          i*minor_segments+(j+1)%minor_segments))
            inds.append(7 if j in (1, 2) else 6)
    return mesh(name, verts, [tuple(reversed(f)) for f in faces], inds)

# Beveled, layered metal end plates, mirrored above and below the crystal.
for side in (-1, 1):
    profile = [(side*y, r) for y, r in [(0.315, 0.205), (0.327, 0.255),
               (0.350, 0.285), (0.392, 0.285), (0.410, 0.247), (0.420, 0.205)]]
    if side < 0:
        profile.reverse()
    lathe('Armored end plate', profile, 12, [1, 0, 0, 1, 2, 1])
    profile = sorted([(side*0.420, 0.137), (side*0.435, 0.137),
                      (side*0.445, 0.108)])
    lathe('Raised end boss', profile, 12, [2, 1, 0])
    profile = sorted([(side*0.300, 0.095), (side*0.321, 0.135),
                      (side*0.335, 0.135)])
    lathe('Crystal socket', profile, 10, [8, 9, 8])

# Three substantial cage posts, positioned so the crystal reads clearly from +Z.
for i in range(3):
    angle = math.radians(30 + i*120)
    x, z = 0.232*math.cos(angle), 0.232*math.sin(angle)
    rod('Cage strut', (x, -0.34, z), (x, 0.34, z), 0.029, 0)
    for s in (-1, 1):
        rod('Strut collar', (x, s*0.278, z), (x, s*0.325, z), 0.039, 1)

# A faceted double-point crystal; alternating broad faces retain a readable cyan mass.
n = 8
verts = [(0, -0.31, 0)]
for y, radius in [(-0.11, 0.145), (0.11, 0.145)]:
    for i in range(n):
        a = 2*math.pi*i/n + math.pi/8
        verts.append((radius*math.cos(a), y, radius*math.sin(a)))
verts.append((0, 0.31, 0))
faces, inds = [], []
for i in range(n):
    j = (i+1)%n
    faces.extend([(0, 1+j, 1+i), (1+i, 1+j, 1+n+j),
                  (1+i, 1+n+j, 1+n+i), (1+n+i, 1+n+j, 17)])
    inds.extend([3 if i%3 else 5, 4 if i%3 == 1 else 3,
                 3 if i%3 == 1 else 5, 4 if i%3 == 0 else 3])
mesh('Warp crystal', verts, [tuple(reversed(f)) for f in faces], inds)

# Segmented, solid purple equatorial ring and a small golden central node on each face.
torus_y('Warp field ring', 0.29, 0.025, 0, 16, 6)
# A gold waist hints at the energetic heart without obscuring the large crystal.
lathe('Golden heart', [(-0.035, 0.151), (-0.020, 0.164),
                       (0.020, 0.164), (0.035, 0.151)], 8, [9, 8, 9, 8])

# Join, bake transforms, and triangulate explicitly to keep the export deterministic.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Neutral studio setup. Cameras look toward the origin, with +Y as image up.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, point=(0, 0, 0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, position, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = position
    point_at(obj)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 5), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 65, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.05
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.35, 0.88, 2.07)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

