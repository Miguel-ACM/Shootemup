import bpy
import math
import os
import sys
from mathutils import Vector

# The exporter runs this file either as a script or with its path following --.
if globals().get('__file__'):
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.68, 0.77, 0.82, 1), # steel body
    (0.90, 0.94, 0.94, 1), # cutting edge
    (0.48, 0.60, 0.66, 1), # blade spine facets
    (0.77, 0.52, 0.18, 1), # brass
    (0.97, 0.73, 0.32, 1), # brass highlight
    (0.16, 0.19, 0.22, 1), # iron guard
    (0.29, 0.34, 0.36, 1), # guard rim
    (0.48, 0.30, 0.20, 1), # grip core
    (0.20, 0.22, 0.23, 1), # dark cloth wrap
    (0.76, 0.61, 0.40, 1), # grip diamonds
]
mats = []
for i, col in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = col
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = col
    bsdf.inputs['Roughness'].default_value = 0.75
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, index in zip(mesh.polygons, indices):
        poly.material_index = index
        poly.use_smooth = False
    parts.append(obj)
    return obj

def cylinder(name, radius, depth, y, material, vertices=12, scale=(1, 1, 1)):
    # Blender cylinders start along Z; the item axis is Y.
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                       end_fill_type='NGON', location=(0, y, 0),
                                       rotation=(math.pi / 2, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

# Four-sided cross section: a high central ridge and tapered cutting edges.
# The gradual offset supplies the katana's characteristic curve from both faces.
rings = [(-0.055, -0.008, 0.043, 0.020),
         ( 0.055, -0.006, 0.044, 0.019),
         ( 0.250,  0.002, 0.043, 0.017),
         ( 0.405,  0.014, 0.039, 0.014),
         ( 0.483,  0.025, 0.026, 0.010),
         ( 0.520,  0.040, 0.002, 0.002)]
verts = []
for y, center, width, thick in rings:
    verts.extend([(center-width, y, 0), (center, y, thick),
                  (center+width, y, 0), (center, y, -thick)])
faces, inds = [(3, 2, 1, 0)], [2]
for j in range(len(rings)-1):
    for k in range(4):
        faces.append((j*4+k, j*4+(k+1)%4, (j+1)*4+(k+1)%4, (j+1)*4+k))
        inds.append((2, 1, 1, 2)[k])
faces.append(tuple((len(rings)-1)*4+k for k in range(4)))
inds.append(1)
mesh_part('Faceted blade', verts, faces, inds)

# Layered collar and a thick, octagonal iron tsuba, finished on both sides.
cylinder('Blade collar', 0.054, 0.060, -0.060, 3, 8, (1, 1, 0.70))
cylinder('Collar lip', 0.057, 0.012, -0.030, 4, 8, (1, 1, 0.70))
cylinder('Guard rim', 0.149, 0.040, -0.105, 6, 12, (1, 1, 0.78))
cylinder('Guard body', 0.138, 0.046, -0.105, 5, 12, (1, 1, 0.78))
cylinder('Guard boss top', 0.064, 0.014, -0.076, 3, 10)
cylinder('Guard boss underside', 0.064, 0.014, -0.134, 3, 10)

# Tapered eight-sided handle with broad crossing cloth bands. Each band wraps
# fully around the handle so the item reads from every angle.
cylinder('Grip core', 0.048, 0.275, -0.280, 7, 8)
cylinder('Grip upper ferrule', 0.052, 0.025, -0.150, 3, 8)
cylinder('Pommel', 0.054, 0.034, -0.429, 3, 8)
cylinder('Pommel end', 0.048, 0.010, -0.450, 4, 8)

# A series of opposing diagonal ribbons follows the octagonal circumference.
# These are deliberately broad enough to remain visible at HUD scale.
def ribbon(name, y_center, slope, material):
    n = 8
    vs = []
    for i in range(n+1):
        a = 2*math.pi*i/n + math.pi/8
        # Repeated sinusoidal displacement gives a zigzag wrap on all sides.
        yy = y_center + slope * math.cos(a)
        x, z = 0.0505*math.cos(a), 0.0505*math.sin(a)
        vs.extend([(x, yy-0.016, z), (x, yy+0.016, z)])
    fs = [(2*i, 2*i+1, 2*i+3, 2*i+2) for i in range(n)]
    mesh_part(name, vs, fs, [material]*n)
for i in range(5):
    y = -0.176 - i*0.052
    ribbon('Wrap rising %d' % i, y, 0.020, 8)
    ribbon('Wrap falling %d' % i, y, -0.020, 8)

# Small warm diamonds on both faces emphasize the classic wrapped handle.
for side in (-1, 1):
    for i in range(4):
        y = -0.202 - i*0.052
        z = side*0.051
        mesh_part('Grip diamond', [(0, y+0.012, z), (0.010, y, z),
                                  (0, y-0.012, z), (-0.010, y, z)],
                  [(0, 1, 2, 3)], [9])

# Apply every object transform before joining. Triangulate to guarantee the
# exported mesh's budget and consistent flat-shaded facets.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2000, 'Triangle budget: %d' % len(model.data.polygons)

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0.03, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 5), 120, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0.035, 3)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.14
aim(cam, (0, 0.035, 0))
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.84, 0.56, 1.72)
aim(cam, (0, 0.03, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

