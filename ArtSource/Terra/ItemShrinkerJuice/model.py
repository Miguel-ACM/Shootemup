import bpy
import math
import os
import sys
from mathutils import Vector

# Blender's background runner may omit __file__; the argument after -- is the script path.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.35, 0.48, 0.23, 1),  # olive bottle
    (0.48, 0.62, 0.30, 1),  # shoulder and bevels
    (0.27, 0.39, 0.19, 1),  # base glass
    (0.86, 0.69, 0.30, 1),  # golden cap
    (0.68, 0.50, 0.20, 1),  # cap collar
    (0.86, 0.88, 0.74, 1),  # pale label
    (0.32, 0.51, 0.24, 1),  # shrink symbol
    (0.64, 0.78, 0.42, 1),  # glass glint
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.65
    mats.append(mat)

parts = []
def mesh_obj(name, verts, faces, material, face_materials=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for p in mesh.polygons:
        p.material_index = material if face_materials is None else face_materials[p.index]
        p.use_smooth = False
    parts.append(ob)
    return ob

def ring(y, width, depth, cut):
    # Eight corners of a bevelled rectangle, clockwise viewed from above.
    w, d = width / 2, depth / 2
    return [(-w+cut,y,-d), (w-cut,y,-d), (w,y,-d+cut), (w,y,d-cut),
            (w-cut,y,d), (-w+cut,y,d), (-w,y,d-cut), (-w,y,-d+cut)]

def vessel(name, levels, ids):
    verts = sum((ring(*level) for level in levels), [])
    faces = [tuple(range(8))]
    mids = [ids[0]]
    for j in range(len(levels)-1):
        for k in range(8):
            faces.append((j*8+k, (j+1)*8+k, (j+1)*8+(k+1)%8, j*8+(k+1)%8))
            mids.append(ids[j])
    faces.append(tuple(reversed([(len(levels)-1)*8+k for k in range(8)])))
    mids.append(ids[-1])
    return mesh_obj(name, verts, faces, 0, mids)

def box(name, loc, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[material])
    if bevel:
        mod = ob.modifiers.new('Broad bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in ob.data.polygons:
        poly.use_smooth = False
    parts.append(ob)
    return ob

def triangle(name, z, material, back=False):
    # Raised, chunky down-pointing emblem, repeated on the rear.
    front = [( -0.105, 0.035), (0.105, 0.035), (0, -0.105)]
    verts = [(x,y,z) for x,y in front] + [(x,y,z + (-0.008 if back else 0.008)) for x,y in front]
    faces = [(0,2,1), (3,4,5), (0,1,4,3), (1,2,5,4), (2,0,3,5)]
    if not back:
        faces = [tuple(reversed(face)) for face in faces]
    mesh_obj(name, verts, faces, material)

# The glass is opaque, with a generous depth and stepped, chamfered shoulder.
vessel('Bottle', [(-0.445, .39, .29, .045), (-0.415, .44, .33, .055),
                  (-0.35, .46, .34, .055), (0.105, .46, .34, .055),
                  (0.15, .43, .32, .05), (0.255, .22, .22, .035),
                  (0.335, .20, .20, .03)], [2, 1, 0, 0, 1, 1, 0])
# Raised base strip and a broad label wrapped around all four sides.
vessel('Foot band', [(-0.427, .438, .335, .055), (-0.388, .458, .345, .055)], [2, 2])
vessel('Paper label', [(-0.175, .466, .346, .057), (0.065, .466, .346, .057)], [5, 5])
triangle('Front symbol', .174, 6)
triangle('Back symbol', -.174, 6, True)
# Gold screw cap, with substantial beveled rim and broad flutes.
box('Neck collar', (0, .322, 0), (.235, .055, .235), 4, .012)
box('Cap', (0, .382, 0), (.255, .125, .255), 3, .018)
box('Cap top', (0, .447, 0), (.215, .018, .215), 3, .008)
for x in (-.083, -.042, 0, .042, .083):
    box('Cap flute front', (x, .383, .129), (.015, .075, .012), 4, .003)
    box('Cap flute back', (x, .383, -.129), (.015, .075, .012), 4, .003)
for z in (-.083, -.042, 0, .042, .083):
    box('Cap flute side', (.129, .383, z), (.012, .075, .015), 4, .003)
    box('Cap flute side', (-.129, .383, z), (.012, .075, .015), 4, .003)
# A pair of chunky highlights echo the original bottle's vertical glint.
for z in (-.174, .174):
    box('Glass glint', (-.15, .105, z), (.032, .125, .012), 7, .006)

bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Triangulate the exported object so its faceted appearance is deterministic.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2000, len(model.data.polygons)
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    point_at(ob, (0, 0, 0))
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 140, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.18
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.55, 1.10, 2.8)
point_at(cam, (0, 0, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)


