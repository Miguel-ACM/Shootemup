import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as vertical and looks at the front from +Z.
def output_dir():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    args = sys.argv
    if '--' in args and len(args) > args.index('--') + 1:
        path = os.path.abspath(args[args.index('--') + 1])
        return path if os.path.isdir(path) else os.path.dirname(path)
    raise RuntimeError('Pass the script path after -- when __file__ is unavailable')

out = output_dir()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.34, 0.43, 0.49, 1), # blue steel housing
    (0.53, 0.63, 0.65, 1), # light steel
    (0.95, 0.16, 0.12, 1), # throttle red
    (1.00, 0.35, 0.20, 1), # coral red
    (1.00, 0.66, 0.22, 1), # amber accents
    (0.21, 0.29, 0.34, 1), # inset slate
    (0.76, 0.83, 0.78, 1), # pale metal
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def finish(obj, mat, bevel=0):
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Machined corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat, bevel)

def prism(name, points, depth, mat, bevel=0):
    n = len(points)
    verts = [(x,y,-depth/2) for x,y in points] + [(x,y,depth/2) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    finish(obj, mat, bevel)
    obj.select_set(False)
    return obj

def cylinder(name, loc, radius, depth, mat, vertices=10, axis='Z'):
    rot = (math.pi/2, 0, 0) if axis == 'Y' else ((0, math.pi/2, 0) if axis == 'X' else (0,0,0))
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, mat, 0.008)

# A squat, beveled throttle console with a broad, stable footprint.
prism('Console', [(-.29,-.37),(.29,-.37),(.25,-.10),(.19,.01),(-.19,.01),(-.25,-.10)], .30, 0, .025)
box('Bottom bumper', (0,-.365,0), (.61,.065,.34), 1, .018)
box('Top collar', (0,-.045,0), (.42,.095,.33), 1, .018)
# Thick, swept fins sit on either side, visible from both directions.
for side in (-1, 1):
    p = [(side*.20,-.16),(side*.45,-.045),(side*.34,-.34),(side*.22,-.30)]
    if side < 0:
        p.reverse()
    prism('Swept red fin', p, .18, 2, .012)
    # a wide stripe supplies a second acceleration cue without tiny markings
    p = [(side*.27,-.18),(side*.39,-.12),(side*.34,-.25),(side*.27,-.27)]
    if side < 0:
        p.reverse()
    stripe = prism('Fin stripe', p, .012, 4, .003)
    stripe.location.z = .098
    stripe = prism('Rear fin stripe', p, .012, 4, .003)
    stripe.location.z = -.098

# A metal lever rises from a central pivot into a distinctive red T grip.
cylinder('Pivot barrel', (0,-.055,0), .12, .39, 5, 12)
for z in (-.205,.205):
    cylinder('Pivot cap', (0,-.055,z), .087, .035, 4, 10)
box('Lever stem', (0,.155,0), (.105,.40,.12), 6, .018)
box('Grip neck', (0,.315,0), (.16,.10,.17), 5, .015)
box('Red throttle grip', (0,.375,0), (.43,.14,.21), 2, .035)
box('Grip highlight', (0,.407,.108), (.29,.045,.018), 3, .007)
box('Rear grip highlight', (0,.407,-.108), (.29,.045,.018), 3, .007)
for x in (-.185,.185):
    box('Grip end cap', (x,.375,0), (.055,.13,.22), 3, .015)

# Front and rear inset panels and broad amber charge bars.
for side in (-1,1):
    box('Console inset', (0,-.23,side*.157), (.32,.19,.025), 5, .015)
    for y in (-.275,-.215,-.155):
        box('Charge bar', (0,y,side*.176), (.23,.030,.016), 4, .006)

# One mesh, flat faces, identity transforms, and an explicit triangle budget.
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
model.data.calc_loop_triangles()
assert 300 <= len(model.data.loop_triangles) <= 2000, 'Triangle count outside requested range: %s' % len(model.data.loop_triangles)

bpy.ops.wm.obj_export(filepath=os.path.join(out, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3,4,5), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (3,1,4), 120, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.15
scene.render.filepath = os.path.join(out, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.95,0.56,1.40)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(out, 'angle.png')
bpy.ops.render.render(write_still=True)

