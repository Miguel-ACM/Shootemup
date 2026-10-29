import bpy
import math
import os
import sys
from mathutils import Vector

# Output lives beside this script, including when launched with Blender's -- option.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.95, 0.22, 0.055, 1), # enamel orange red
    (1.00, 0.40, 0.075, 1), # bright orange
    (0.98, 0.67, 0.16, 1), # brass yellow
    (0.37, 0.40, 0.42, 1), # warm steel
    (0.18, 0.23, 0.25, 1), # hose and recess
    (0.68, 0.71, 0.66, 1), # pale metal
    (0.72, 0.12, 0.045, 1), # red-orange bands
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

def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj

def box(loc, dims, mat, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad chamfers', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def cylinder(loc, radius, depth, mat, vertices=12, axis='Y', radius2=None):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius if radius2 is None else radius2, depth=depth, location=loc, rotation=(math.pi/2, 0, 0) if axis == 'Y' else (0, 0, 0))
    return finish(bpy.context.object, mat)

def segment(a, b, radius, mat):
    a, b = Vector(a), Vector(b)
    d = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=d.length, location=(a+b)*0.5)
    obj = bpy.context.object
    obj.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return finish(obj, mat)

def ring_y(cx, cy, cz, outer, inner, depth, mat, n=12):
    verts = []
    for y, r in [(cy-depth/2,outer), (cy+depth/2,outer), (cy+depth/2,inner), (cy-depth/2,inner)]:
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((cx+r*math.cos(a), y, cz+r*math.sin(a)))
    faces = []
    for j in range(4):
        k = (j+1)%4
        for i in range(n):
            ni = (i+1)%n
            faces.append((j*n+i, j*n+ni, k*n+ni, k*n+i))
    mesh = bpy.data.meshes.new('Muzzle ring mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Muzzle ring', mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, mat)

# Faceted pressurised fuel cylinder, shoulder and foot, all upright on Y.
cylinder((-.235, -.105, 0), .175, .49, 0)
cylinder((-.235, .155, 0), .175, .07, 1, radius2=.125)
cylinder((-.235, -.365, 0), .175, .055, 6)
cylinder((-.235, .22, 0), .092, .07, 3)
cylinder((-.235, .265, 0), .065, .035, 2)
# Wide bright hoops remain legible on all sides.
cylinder((-.235, -.25, 0), .182, .055, 2)
cylinder((-.235, .035, 0), .182, .055, 2)
# Heavy supporting bridge and upright delivery pipe.
box((.005, -.29, 0), (.40, .105, .16), 3, .025)
cylinder((.205, -.055, 0), .065, .47, 3, vertices=10)
cylinder((.205, -.24, 0), .082, .07, 2, vertices=10)
# A flared projector head with an actual open, recessed bore.
# Cone rotation reverses local Z, so radius1 is the upper, flared end.
cylinder((.205, .225, 0), .205, .31, 1, vertices=12, radius2=.085)
ring_y(.205, .397, 0, .213, .145, .065, 2)
cylinder((.205, .38, 0), .142, .012, 4, vertices=12)
# A chunky bent feed hose bridges the canister and projector on both sides.
for z in (-.10, .10):
    path = [(-.19, -.31, z), (-.11, -.39, z), (.04, -.39, z), (.13, -.30, z)]
    for a, b in zip(path, path[1:]):
        segment(a, b, .033, 4)
    for p in path[1:-1]:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.034, location=p)
        finish(bpy.context.object, 4)
# Raised, simple flame glyph on the front and back of the tank.
def badge(z, reverse=False):
    pts = [(-.31,-.15),(-.30,-.04),(-.245,.055),(-.23,-.005),(-.19,.035),(-.16,-.075),(-.18,-.15)]
    verts = [(x,y,z) for x,y in pts] + [(x,y,z + (-.012 if reverse else .012)) for x,y in pts]
    n=len(pts)
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new('Flame badge mesh')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new('Flame badge',mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj,2)
badge(.174)
badge(-.174, True)

# Join, bake transforms and triangulate the exported geometry.
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_materials=True, forward_axis='Y', up_axis='Z', export_selected_objects=True)

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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point(obj, target=(0, 0, 0)):
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
    point(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 4), 130, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.81, 0.52, 1.47)
point(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

