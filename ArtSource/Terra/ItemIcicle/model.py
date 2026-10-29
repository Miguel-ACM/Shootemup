import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Output follows the script, including when Blender executes it with --python.
try:
    OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    OUTPUT_DIR = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.20, 0.53, 0.61, 1), # deep glacial teal
    (0.23, 0.72, 0.80, 1), # cyan ice
    (0.37, 0.82, 0.88, 1), # light blue
    (0.70, 0.92, 0.94, 1), # frosty white
    (0.13, 0.43, 0.52, 1), # shadowed ice
    (0.31, 0.65, 0.73, 1), # muted blue
    (0.52, 0.87, 0.91, 1), # bright facet
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    materials.append(mat)

# Ring meshes are closed solids. Each ring stores (height, x center, z center,
# x radius, z radius); their pointed ends and irregular shoulders give the ice
# a clear silhouette without relying on surface decoration.
bm = bmesh.new()
def crystal(rings, sectors, rotation, facet_colors, cap_color):
    loops = []
    for y, cx, cz, rx, rz in rings:
        loop = []
        for j in range(sectors):
            a = rotation + 2.0 * math.pi * j / sectors
            loop.append(bm.verts.new((cx + rx * math.cos(a), y, cz + rz * math.sin(a))))
        loops.append(loop)
    for k in range(len(loops) - 1):
        for j in range(sectors):
            n = (j + 1) % sectors
            # Consistent broad vertical facets, with occasional diagonal breaks.
            shade = facet_colors[j % len(facet_colors)]
            a = bm.faces.new((loops[k][j], loops[k+1][j], loops[k+1][n]))
            b = bm.faces.new((loops[k][j], loops[k+1][n], loops[k][n]))
            a.material_index = shade
            b.material_index = shade if (j + k) % 4 else min(shade + 1, 6)
    top = bm.verts.new((rings[0][1], rings[0][0] + 0.012, rings[0][2]))
    bottom = bm.verts.new((rings[-1][1], rings[-1][0] - 0.004, rings[-1][2]))
    for j in range(sectors):
        n = (j + 1) % sectors
        f = bm.faces.new((top, loops[0][j], loops[0][n]))
        f.material_index = cap_color
        f = bm.faces.new((bottom, loops[-1][n], loops[-1][j]))
        f.material_index = facet_colors[j % len(facet_colors)]

# Long main icicle: broad jagged crown and a slightly off-center tip.
crystal([
    ( .390, -.035, 0.000, .145, .133),
    ( .345, -.025, 0.000, .220, .185),
    ( .265, -.010, 0.000, .232, .195),
    ( .130,  .005, 0.000, .205, .172),
    (-.035,  .015, 0.000, .155, .130),
    (-.210,  .026, 0.000, .098, .086),
    (-.360,  .038, 0.000, .042, .039),
    (-.445,  .045, 0.000, .003, .003),
], 16, math.pi / 16, [1, 2, 6, 3, 2, 1, 5, 0, 4, 0, 5, 1, 2, 3, 6, 1], 3)

# Thick, integrated satellite shards form asymmetric shoulders and broken tips.
crystal([
    ( .320, .205, .012, .085, .095),
    ( .275, .220, .012, .123, .112),
    ( .180, .228, .012, .113, .103),
    ( .065, .228, .012, .080, .076),
    (-.070, .213, .012, .040, .040),
    (-.165, .198, .012, .003, .003),
], 12, .12, [1, 2, 6, 3, 2, 1, 5, 0, 4, 0, 5, 1], 6)
crystal([
    ( .300, -.190, -.035, .070, .075),
    ( .255, -.207, -.035, .105, .095),
    ( .165, -.215, -.035, .100, .090),
    ( .060, -.220, -.035, .072, .068),
    (-.030, -.214, -.035, .035, .034),
    (-.105, -.205, -.035, .003, .003),
], 12, .18, [2, 3, 6, 2, 1, 5, 0, 4, 0, 5, 1, 2], 3)

bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
mesh = bpy.data.meshes.new('IcicleMesh')
bm.to_mesh(mesh)
bm.free()
mesh.update()
model = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(model)
for mat in materials:
    mesh.materials.append(mat)
for polygon in mesh.polygons:
    polygon.use_smooth = False
bpy.context.view_layer.objects.active = model
model.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Export only the item; render helpers are created afterwards.
bpy.ops.wm.obj_export(filepath=os.path.join(OUTPUT_DIR, 'model.obj'),
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
world = bpy.data.worlds.new('Blue gray studio')
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 5), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, 1, 4), 110, 5.0)

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 4)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.10
scene.render.filepath = os.path.join(OUTPUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (1.0, 0.55, 1.55)
aim(camera, (0, 0, 0))
camera_data.type = 'PERSP'
camera_data.lens = 55
scene.render.filepath = os.path.join(OUTPUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

