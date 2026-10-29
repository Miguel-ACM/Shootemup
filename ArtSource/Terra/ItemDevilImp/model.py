import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as vertical and looks at the front from +Z.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.82, 0.105, 0.095, 1),  # vermilion head
    (0.99, 0.24, 0.16, 1),    # cheek and brow
    (0.48, 0.075, 0.11, 1),   # wing membranes
    (0.72, 0.16, 0.16, 1),    # wing leading edges
    (0.93, 0.68, 0.27, 1),    # warm horn
    (1.0, 0.78, 0.16, 1),     # gold eyes
    (0.23, 0.045, 0.07, 1),   # mouth and pupils
    (1.0, 0.87, 0.58, 1),     # fang
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = color
    shader.inputs['Roughness'].default_value = 0.8
    mats.append(mat)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def ico(name, loc, scale, material, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    return finish(obj, material)

def mesh_part(name, verts, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material)

def tube(name, points, radii, material, sides=8):
    # Polygonal tapered segments; the perpendicular frames follow the bend.
    verts = []
    for i, (point, radius) in enumerate(zip(points, radii)):
        p = Vector(point)
        tangent = Vector(points[min(i + 1, len(points)-1)]) - Vector(points[max(i - 1, 0)])
        tangent.normalize()
        u = tangent.cross(Vector((0, 0, 1)))
        if u.length < 0.01:
            u = tangent.cross(Vector((0, 1, 0)))
        u.normalize()
        v = tangent.cross(u).normalized()
        for j in range(sides):
            angle = 2 * math.pi * j / sides
            verts.append(tuple(p + radius * (math.cos(angle)*u + math.sin(angle)*v)))
    faces = [tuple(reversed(range(sides)))]
    for i in range(len(points)-1):
        for j in range(sides):
            k = (j + 1) % sides
            faces.append((i*sides+j, i*sides+k, (i+1)*sides+k, (i+1)*sides+j))
    faces.append(tuple((len(points)-1)*sides+j for j in range(sides)))
    return mesh_part(name, verts, faces, material)

def wing(side):
    # Thick, ridged bat wing with a scalloped trailing edge and full reverse.
    outline = [(0.19, 0.09), (0.31, 0.19), (0.48, 0.22),
               (0.43, 0.08), (0.47, -0.02), (0.37, 0.015),
               (0.34, -0.115), (0.27, -0.07), (0.20, -0.14)]
    n = len(outline)
    verts = [(side*x, y, z) for z in (0.015, -0.065) for x, y in outline]
    verts += [(side*0.315, 0.055, 0.06), (side*0.315, 0.055, -0.10)]
    faces = []
    for i in range(n):
        j = (i+1) % n
        faces.append((2*n, i, j))
        faces.append((2*n+1, n+j, n+i))
        faces.append((i, n+i, n+j, j))
    if side < 0:
        faces = [tuple(reversed(f)) for f in faces]
    mesh_part('wing', verts, faces, 2)
    tube('wing leading edge', [(side*.21,.09,.025), (side*.31,.19,.025), (side*.47,.22,.025)], [.045,.035,.012], 3, 6)

# Main head and broad cheeks give the imp a rounded, faceted 3D mass.
ico('head', (0, -0.035, 0), (.255, .275, .225), 0, 2)
for s in (-1, 1):
    wing(s)
    # Horns lean outwards and curl inwards at the tips, with a broad root.
    tube('horn', [(s*.145,.155,-.005), (s*.19,.265,-.005),
                  (s*.225,.365,.005), (s*.205,.435,.015)],
         [.077,.063,.040,.006], 4, 8)
    # Eyes are large, angular gold gems, backed by red brows for expression.
    ico('eye', (s*.101,.035,.206), (.079,.060,.035), 5, 1)
    ico('pupil', (s*.101,.035,.237), (.020,.037,.012), 6, 1)
    tube('brow', [(s*.035,.095,.207), (s*.12,.12,.195), (s*.18,.135,.155)],
         [.027,.031,.020], 1, 6)
    ico('cheek', (s*.157,-.075,.165), (.071,.064,.045), 1, 1)

# A short dark grin with two chunky ivory fangs.
tube('grin', [(-.105,-.145,.177), (0,-.166,.204), (.105,-.145,.177)], [.018,.021,.018], 6, 6)
for s in (-1, 1):
    tube('fang', [(s*.065,-.14,.201), (s*.055,-.182,.211)], [.020,.003], 7, 6)

# Join and bake transforms; retain material slots and flat faces.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for polygon in model.data.polygons:
    polygon.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Rendering setup, created after export so the OBJ contains only the model.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 5), 100, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.30
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.95, 0.58, 1.65)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

