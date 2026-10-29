import bpy
import math
import os
import sys
from mathutils import Vector

# The model uses the game's axes directly: X right, Y up, Z toward the viewer.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.40, 0.52, 0.60, 1),  # steel housing
    (0.67, 0.78, 0.81, 1),  # bright collar bevel
    (0.22, 0.31, 0.40, 1),  # recessed steel
    (0.92, 0.95, 0.89, 1),  # light polarity
    (0.17, 0.22, 0.32, 1),  # shade polarity, readable without a black fill
    (0.86, 0.62, 0.22, 1),  # brass fittings
    (0.45, 0.66, 0.71, 1),  # cool edge
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
def mesh_part(name, verts, faces, material_indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        mesh.materials.append(m)
    for p, index in zip(mesh.polygons, material_indices):
        p.material_index = index
        p.use_smooth = False
    parts.append(obj)
    return obj

# Closed faceted housing, with stepped shoulders and a prominent metal bezel.
def housing():
    n = 32
    profile = [(-.145, .365), (-.135, .403), (-.105, .425),
               (.105, .425), (.135, .403), (.145, .365)]
    verts = [(r * math.cos(2 * math.pi * i / n),
              r * math.sin(2 * math.pi * i / n), z)
             for z, r in profile for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple((len(profile)-1)*n+i for i in range(n))]
    ids = [2, 2]
    band_colors = [1, 1, 0, 1, 1]
    for j in range(len(profile)-1):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
            ids.append(band_colors[j])
    mesh_part('Faceted steel collar', verts, faces, ids)

housing()

# Two complementary, raised, interlocking lobes. Their boundary follows the
# two half-circles of the original symbol, rather than a painted-on texture.
def polarity_half(side, z, mat_index):
    radius = .348
    steps = 16
    pts = []
    for i in range(steps + 1):
        a = math.pi/2 + i*math.pi/steps
        pts.append((radius*math.cos(a), radius*math.sin(a)))
    small = radius/2
    for i in range(1, steps + 1):
        a = -math.pi/2 + i*math.pi/steps
        pts.append((small*math.cos(a), -small + small*math.sin(a)))
    for i in range(1, steps):
        a = -math.pi/2 - i*math.pi/steps
        pts.append((small*math.cos(a), small + small*math.sin(a)))
    pts = [(side*x, side*y) for x, y in pts]
    # A shallow extrusion ensures the symbol has actual dimensional edges.
    inner_z = z - math.copysign(.018, z)
    verts = [(x, y, inner_z) for x,y in pts] + [(x, y, z) for x,y in pts]
    n = len(pts)
    faces = [tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    obj = mesh_part('Polarity lobe', verts, faces, [mat_index]*len(faces))
    # Triangulation resolves the concave S-shaped face for predictable export.
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Triangulate curved face', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)

def cylinder(name, radius, depth, loc, material, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    for p in ob.data.polygons:
        p.use_smooth = False
    parts.append(ob)
    return ob

for face in (-1, 1):
    z = face * .151
    polarity_half(1, z, 3)
    polarity_half(-1, z, 4)
    # Contrasting seeds sit in opposite lobes on both faces.
    cylinder('Shade seed', .064, .018, (0, -.174, face*.159), 4)
    cylinder('Light seed', .064, .018, (0, .174, face*.159), 3)

# Substantial side lugs echo the original's twin tabs. A bevel supplies broad,
# light-catching facets without fragile thin geometry.
for side in (-1, 1):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(side*.421, 0, 0))
    ob = bpy.context.object
    ob.name = 'Collar lug'
    ob.dimensions = (.105, .205, .24)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[6])
    bevel = ob.modifiers.new('Machined corners', 'BEVEL')
    bevel.width = .022
    bevel.segments = 1
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    for p in ob.data.polygons:
        p.use_smooth = False
    parts.append(ob)
    for face in (-1, 1):
        cylinder('Brass lug stud', .028, .012, (side*.421, 0, face*.125), 5, 8)

# Join, freeze transforms and triangulate the final mesh for an explicit budget.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2000, 'Triangle budget exceeded'
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Presentation: render only after export so cameras and lights never enter the OBJ.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05, .07, .09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def point_at(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    point_at(ob)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 80, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (.75, .50, 1.35)
point_at(cam)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

