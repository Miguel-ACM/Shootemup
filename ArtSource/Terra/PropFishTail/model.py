import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

# Blender can execute this file either directly or via a launcher after --.
try:
    OUT = Path(__file__).resolve().parent
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = Path(args[0]).resolve().parent

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = {
    0: (0.24, 0.35, 0.40), 1: (0.78, 0.83, 0.79),
    2: (0.35, 0.43, 0.30), 3: (0.95, 0.38, 0.12),
    4: (0.32, 0.48, 0.55), 5: (0.54, 0.18, 0.16),
    6: (0.53, 0.61, 0.62), 7: (0.34, 0.31, 0.48),
    8: (0.20, 0.36, 0.40), 11: (0.055, 0.09, 0.12),
    12: (0.62, 0.25, 0.19), 13: (0.82, 0.64, 0.26),
    14: (0.53, 0.61, 0.55), 15: (0.80, 0.84, 0.83),
}
materials = {}
for tile, rgb in palette.items():
    mat = bpy.data.materials.new('tile_%02d' % tile)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.82
    materials[tile] = mat

# A solid, forked caudal fin: radiating facets run from the attachment point
# to the two tips and central notch. Its outline and 0.035 depth match the
# original prism; shallow ridges give it a fleshy low-poly cross section.
segments, rings = 16, 5
vertices, faces, face_tiles = [], [], []
def vertex(co):
    vertices.append(co)
    return len(vertices) - 1
def face(indices, tile):
    faces.append(indices)
    face_tiles.append(tile)
def outline(i):
    u = i / segments
    return (-0.34 + 0.68 * u, 0.43 + 0.13 * abs(2.0 * u - 1.0))

surfaces = []
for side in (1, -1):
    root = vertex((0.0, 0.0, side * 0.006))
    rows = []
    for j in range(1, rings + 1):
        t = j / rings
        row = []
        for i in range(segments + 1):
            x, y = outline(i)
            # Ridge relief is part of the surface, not overlapping decoration.
            ridge = (1.0 if i % 2 == 0 else 0.54)
            height = 0.006 + 0.0115 * math.sin(math.pi * t * 0.72) * ridge
            row.append(vertex((x * t, y * t, side * height)))
        rows.append(row)
    surfaces.append((root, rows))
    for i in range(segments):
        # Row order is left-to-right; reverse it for outward top normals.
        tri = [root, rows[0][i + 1], rows[0][i]]
        if side < 0:
            tri.reverse()
        face(tri, 13 if side > 0 else 0)
    for j in range(rings - 1):
        for i in range(segments):
            a, b = rows[j][i], rows[j][i + 1]
            c, d = rows[j + 1][i + 1], rows[j + 1][i]
            if side > 0:
                # Narrow radial bands evoke rays, while the dark outer band
                # makes the fork readable under the game's blue grade.
                tile = (8 if i % 4 == 0 else 4) if j == 3 else (6 if i % 2 == 0 else 13)
                if j == 2:
                    tile = 13 if i % 2 == 0 else 14
            else:
                tile = 0 if i % 2 else 8
            tris = ([a, b, c], [a, c, d])
            for tri in tris:
                tri = list(tri)
                if side < 0:
                    tri.reverse()
                face(tri, tile)

# Seal the outer edge and the two leading edges.
top_root, top = surfaces[0]
bot_root, bot = surfaces[1]
for i in range(segments):
    face([top[-1][i], top[-1][i + 1], bot[-1][i + 1], bot[-1][i]], 6)
for edge in (0, segments):
    t = [top_root] + [row[edge] for row in top]
    b = [bot_root] + [row[edge] for row in bot]
    for j in range(rings):
        quad = [t[j], b[j], b[j + 1], t[j + 1]]
        if edge == segments:
            quad.reverse()
        face(quad, 6)

mesh = bpy.data.meshes.new('Fish tail mesh')
mesh.from_pydata(vertices, [], faces)
mesh.update()
obj = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(obj)
for tile in palette:
    mesh.materials.append(materials[tile])
tile_index = {tile: i for i, tile in enumerate(palette)}
for poly, tile in zip(mesh.polygons, face_tiles):
    poly.material_index = tile_index[tile]
    poly.use_smooth = False
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# Export before adding the rendering rig; only the single model is selected.
bpy.ops.wm.obj_export(filepath=str(OUT / 'model.obj'), export_selected_objects=True,
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
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1.0)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    lamp = bpy.data.objects.new(name, data)
    scene.collection.objects.link(lamp)
    lamp.location = location
    aim(lamp, target)
    return lamp

light('Upper left sun', 'SUN', (-3, 4, 6), 2.0, (0, 0.28, 0))
light('Soft fill', 'AREA', (2, -1, 3), 60.0, (0, 0.28, 0), 4.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
center = (0, 0.28, 0)
cam.location = (0, 0.28, 3)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 0.80
scene.render.filepath = str(OUT / 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (0.82, -0.71, 1.15)
aim(cam, center)
scene.render.filepath = str(OUT / 'angle.png')
bpy.ops.render.render(write_still=True)
