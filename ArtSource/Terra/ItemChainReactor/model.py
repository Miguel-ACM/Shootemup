import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as vertical and sees the front along +Z.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.95, 0.20, 0.055, 1),  # vermilion charge casings
    (1.00, 0.36, 0.09, 1),   # orange shoulder facets
    (0.54, 0.16, 0.085, 1),  # warm copper end caps
    (0.92, 0.58, 0.18, 1),   # brass bands and terminals
    (0.40, 0.27, 0.18, 1),   # dark brown binding
    (0.88, 0.72, 0.38, 1),   # woven fuse
    (1.00, 0.72, 0.16, 1),   # hot fuse tip
    (1.00, 0.43, 0.08, 1),   # ember
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    m.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.72
    mats.append(m)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)
    return obj

def lathe_y(name, x, z, rings, segments, face_mats):
    # rings are (height, radius); flat annular facets form real bevels.
    verts = []
    for y, radius in rings:
        for i in range(segments):
            a = 2 * math.pi * i / segments + math.pi / segments
            verts.append((x + radius * math.cos(a), y, z + radius * math.sin(a)))
    faces = [tuple(reversed(range(segments)))]
    indices = [face_mats[0]]
    for j in range(len(rings) - 1):
        for i in range(segments):
            faces.append((j * segments + i, j * segments + (i + 1) % segments,
                          (j + 1) * segments + (i + 1) % segments, (j + 1) * segments + i))
            indices.append(face_mats[j + 1])
    faces.append(tuple((len(rings) - 1) * segments + i for i in range(segments)))
    indices.append(face_mats[-1])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for m in mats:
        mesh.materials.append(m)
    for p, index in zip(mesh.polygons, indices):
        p.material_index = index
        p.use_smooth = False
    parts.append(ob)
    return ob

def box(name, loc, size, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('Cut corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(ob, mat)

def rod(name, a, b, radius, mat, vertices=8):
    a, b = Vector(a), Vector(b)
    d = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=d.length, end_fill_type='NGON', location=(a+b)/2)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return finish(ob, mat)

# A central charge stands slightly higher than its two companions.
for x, offset in [(-0.255, -0.055), (0.0, 0.035), (0.255, -0.055)]:
    y0, y1 = -0.275 + offset, 0.245 + offset
    lathe_y('Powder charge', x, 0, [(y0, .115), (y0+.035, .150), (y1-.035, .150), (y1, .115)], 10, [2, 1, 0, 1, 2])
    for yy in (y0 + .045, y1 - .045):
        lathe_y('Brass collar', x, 0, [(yy-.022, .151), (yy-.015, .158), (yy+.015, .158), (yy+.022, .151)], 10, [2, 3, 3, 3, 2])
    lathe_y('Fuse socket', x, 0, [(y1, .060), (y1+.035, .060), (y1+.045, .045)], 8, [2, 3, 3, 3])

# Two broad straps bind the bundle all the way around, including the back.
for y in (-.18, .12):
    box('Binding front', (0, y, .155), (.80, .065, .040), 4, .012)
    box('Binding back', (0, y, -.155), (.80, .065, .040), 4, .012)
    for x in (-.397, .397):
        box('Binding side', (x, y, 0), (.04, .065, .31), 4, .012)
    box('Brass clasp', (0, y, .180), (.14, .080, .025), 3, .010)

# Thick linked fuse, rising from all three sockets and terminating in an ember.
fuse_paths = [
    [(-.255, .235, 0), (-.255, .305, 0), (-.12, .335, 0), (0, .325, 0)],
    [(.255, .235, 0), (.255, .305, 0), (.12, .335, 0), (0, .325, 0)],
    [(0, .325, 0), (.035, .375, 0), (.065, .415, 0)],
]
for path in fuse_paths:
    for a, b in zip(path, path[1:]):
        rod('Fuse', a, b, .026, 5)
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.026, location=b)
        finish(bpy.context.object, 5)
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.043, location=(.069, .424, 0))
finish(bpy.context.object, 6)

# Consolidate to one flat-shaded mesh and bake transforms; triangulate for export.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Studio preview setup, added after the model-only export.
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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05, .07, .09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = loc
    aim(ob)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 4), 180, 5.0)
cam_data = bpy.data.cameras.new('Preview camera')
cam = bpy.data.objects.new('Preview camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.14
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.65, 1.05, 2.65)
aim(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
