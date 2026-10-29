import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve output beside this script, including Blender's -- fallback invocation.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

def material(name, rgb):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    return mat

ivory = material('tile_00', (236/255, 232/255, 222/255))
blue = material('tile_01', (40/255, 96/255, 230/255))
cream = material('tile_02', (236/255, 228/255, 200/255))
ink = material('tile_03', (30/255, 70/255, 220/255))
objects = []

def lathe(name, profile, mat, segments=20):
    # A series of circular cross sections, around the vertical Y axis.
    verts = []
    for y, radius in profile:
        for i in range(segments):
            a = 2 * math.pi * i / segments
            verts.append((radius * math.sin(a), y, radius * math.cos(a)))
    faces = []
    faces.append(tuple(range(segments - 1, -1, -1)))
    for row in range(len(profile) - 1):
        for i in range(segments):
            j = (i + 1) % segments
            faces.append((row*segments+i, row*segments+j, (row+1)*segments+j, (row+1)*segments+i))
    faces.append(tuple((len(profile)-1)*segments+i for i in range(segments)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    objects.append(ob)
    return ob

def box(name, center, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mat)
    if bevel:
        mod = ob.modifiers.new('Small molded edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    objects.append(ob)
    return ob

# Original dimensions: 0.68-wide body, 0.72-wide lid, and 0.74 tall overall.
lathe('Tub', [(-.36,.305),(-.35,.325),(-.33,.34),(.19,.34),(.22,.33),(.24,.31)], ivory)
lathe('Screw lid', [(.235,.337),(.245,.355),(.265,.36),(.35,.36),(.375,.345),(.38,.325)], blue)
# A shallow cream label, with its edges embedded into the round tub.
box('Label', (0,-.06,.319), (.36,.36,.065), cream, .009)
# Blocky P reproduces the atlas glyph: a stem, two bars and a right upright.
box('P stem', (-.060,-.055,.355), (.044,.225,.008), ink)
box('P top', (.002,.038,.355), (.145,.043,.008), ink)
box('P middle', (.002,-.052,.355), (.145,.043,.008), ink)
box('P upright', (.053,-.007,.355), (.043,.09,.008), ink)

bpy.ops.object.select_all(action='DESELECT')
for ob in objects:
    ob.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

# Export only the item, before adding the presentation rig.
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
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = location
    point_at(ob, target)
    return ob

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0,0,0))
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 1, 4), 120, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0.01, 4)
point_at(cam, (0, 0.01, 0))
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.06
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.82, 0.53, 1.16)
point_at(cam, (0, 0.01, 0))
camdata.type = 'PERSP'
camdata.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

