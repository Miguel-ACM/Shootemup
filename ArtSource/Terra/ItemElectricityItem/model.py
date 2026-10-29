import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# The original seven-point silhouette, uniformly scaled to 0.936 units high.
SCALE = 0.9
OUTLINE = [(0.10, 0.52), (-0.22, 0.02), (-0.02, 0.02),
           (-0.14, -0.52), (0.24, 0.06), (0.03, 0.06), (0.20, 0.52)]

if '__file__' in globals():
    OUTPUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv):
    OUTPUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    if OUTPUT.lower().endswith('.py'):
        OUTPUT = os.path.dirname(OUTPUT)
else:
    OUTPUT = os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.65
    return mat

# Yellow lamp face and gold edge, matching the original atlas palette.
yellow = material('tile_00', (1.0, 0.91, 0.38))
gold = material('tile_01', (0.925, 0.729, 0.157))

n = len(OUTLINE)
verts = [(x * SCALE, y * SCALE, z) for z in (-0.075, 0.075)
         for x, y in OUTLINE]
faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
mesh = bpy.data.meshes.new('Lightning bolt mesh')
mesh.from_pydata(verts, [], faces)
mesh.materials.append(yellow)
mesh.materials.append(gold)
mesh.update()
obj = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(obj)
for poly in mesh.polygons:
    poly.material_index = 0 if poly.index < 2 else 1
    poly.use_smooth = False

# Establish outward normals before beveling the closed, concave prism.
bm = bmesh.new()
bm.from_mesh(mesh)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(mesh)
bm.free()
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bevel = obj.modifiers.new('Rounded bolt edges', 'BEVEL')
bevel.width = 0.012
bevel.segments = 2
bevel.limit_method = 'ANGLE'
bevel.material = 1
bpy.ops.object.modifier_apply(modifier=bevel.name)
# Triangulate and subdivide without smoothing: PS1-style planar facets remain flat.
tri = obj.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
sub = obj.modifiers.new('Subdivide planar facets', 'SUBSURF')
sub.subdivision_type = 'SIMPLE'
sub.levels = 1
sub.render_levels = 1
bpy.ops.object.modifier_apply(modifier=sub.name)
for poly in obj.data.polygons:
    poly.use_smooth = False
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Export only the joined model, before adding the render rig.
bpy.ops.wm.obj_export(filepath=os.path.join(OUTPUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(ob, target=(0, 0, 0)):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()


def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = location
    aim(ob)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
light('Soft fill', 'AREA', (4, 1, 5), 200, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.22
scene.render.filepath = os.path.join(OUTPUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.9, 0.42, 1.65)
aim(cam)
scene.render.filepath = os.path.join(OUTPUT, 'angle.png')
bpy.ops.render.render(write_still=True)

