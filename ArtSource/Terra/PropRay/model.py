import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Blender executes text scripts without __file__ in some invocation modes.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    arg = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    OUT = arg if os.path.isdir(arg) else os.path.dirname(arg)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = {
    0: (0.24, 0.35, 0.40), 1: (0.78, 0.83, 0.79),
    2: (0.35, 0.43, 0.30), 3: (0.95, 0.38, 0.12),
    4: (0.32, 0.48, 0.55), 5: (0.54, 0.18, 0.16),
    6: (0.53, 0.61, 0.62), 7: (0.34, 0.31, 0.48),
    8: (0.20, 0.36, 0.40), 11: (0.055, 0.09, 0.12),
    12: (0.62, 0.25, 0.19), 13: (0.82, 0.64, 0.26),
    14: (0.53, 0.61, 0.55), 15: (0.80, 0.84, 0.83),
}
mats = {}
for n, color in palette.items():
    mat = bpy.data.materials.new('tile_%02d' % n)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.88
    mats[n] = mat

# Each station describes half of a continuous swept, cambered disc:
# x, leading edge, trailing edge, elevation of the thin outer edge.
stations = [
    (0.00, -0.67, 0.53, 0.000),
    (0.18, -0.66, 0.46, 0.000),
    (0.34, -0.56, 0.36, -0.002),
    (0.52, -0.42, 0.33, -0.004),
    (0.72, -0.25, 0.36, -0.007),
    (0.93, -0.07, 0.42, -0.010),
    (1.12,  0.08, 0.46, -0.012),
    (1.28,  0.18, 0.41, -0.010),
    (1.40,  0.24, 0.26, -0.006),
]
full = [(-x, a, b, z) for x, a, b, z in stations[:0:-1]] + stations
verts, faces, tiles = [], [], []
def vert(co):
    verts.append(co)
    return len(verts) - 1
def face(indices, tile):
    faces.append(indices)
    tiles.append(tile)

# Two surfaces and a closed edge make a real shallow solid, including for shadows.
rows = []
steps = 8
for x, lead, trail, edge_z in full:
    chord = []
    for j in range(steps + 1):
        t = j / steps
        y = lead * (1-t) + trail * t
        camber = math.sin(math.pi*t)
        body = max(0.0, 1.0 - abs(x)/1.4)
        top = edge_z + (0.006 + 0.039 * body**1.1) * camber
        bottom = edge_z - (0.004 + 0.039 * body**1.3) * camber
        chord.append((vert((x, y, top)), vert((x, y, bottom))))
    rows.append(chord)
for i in range(len(rows)-1):
    for j in range(steps):
        a, b = rows[i][j], rows[i+1][j]
        c, d = rows[i+1][j+1], rows[i][j+1]
        midx = abs((full[i][0] + full[i+1][0]) * 0.5)
        # Restrained shoulder patches define the animal without a bright outline.
        top_tile = 4 if (0.18 < midx < 0.54 and j in (1, 2)) else 8
        if midx > 1.1:
            top_tile = 0
        face((a[0], b[0], c[0]), top_tile)
        face((a[0], c[0], d[0]), top_tile)
        face((a[1], c[1], b[1]), 0)
        face((a[1], d[1], c[1]), 0)
    for j in (0, steps):
        a, b = rows[i][j], rows[i+1][j]
        face((a[0], a[1], b[1], b[0]), 0)
for i in (0, len(rows)-1):
    for j in range(steps):
        a, b = rows[i][j], rows[i][j+1]
        face((a[0], b[0], b[1], a[1]), 0)

# Slightly rolled cephalic fins extend forward on either side of the mouth.
# The fins overlap the disc to avoid gaps in silhouette and shadow.
for side in (-1, 1):
    outline = [(0.14, -0.53), (0.33, -0.55), (0.39, -0.69),
               (0.32, -0.80), (0.23, -0.77), (0.19, -0.65)]
    upper, lower = [], []
    for x, y in outline:
        upper.append(vert((side*x, y, 0.012)))
        lower.append(vert((side*x, y, -0.012)))
    center_top = vert((side*0.27, -0.66, 0.025))
    center_bottom = vert((side*0.27, -0.66, -0.020))
    for j in range(len(outline)):
        k = (j+1) % len(outline)
        if side == 1:
            face((center_top, upper[j], upper[k]), 0)
            face((center_bottom, lower[k], lower[j]), 0)
        else:
            face((center_top, upper[k], upper[j]), 0)
            face((center_bottom, lower[j], lower[k]), 0)
        face((upper[j], lower[j], lower[k], upper[k]), 0)

# Narrow tapering tail, centered on +Y and ending at the original 1.67 extent.
tail_rings = []
for y, half, z, thick in [(0.43, .050, .005, .020), (0.62, .035, .000, .015),
                          (0.95, .022, -.003, .011), (1.30, .014, -.005, .008),
                          (1.67, .003, -.006, .003)]:
    tail_rings.append([vert((-half, y, z)), vert((0, y, z+thick)),
                       vert((half, y, z)), vert((0, y, z-thick))])
for a, b in zip(tail_rings, tail_rings[1:]):
    for j in range(4):
        face((a[j], b[j], b[(j+1)%4], a[(j+1)%4]), 8 if j < 2 else 0)
face(tuple(reversed(tail_rings[0])), 0)
face(tuple(tail_rings[-1]), 0)

# Build the single mesh through bmesh; all surfaces remain deliberately flat.
mesh = bpy.data.meshes.new('RayMesh')
bm = bmesh.new()
bv = [bm.verts.new(co) for co in verts]
bm.verts.ensure_lookup_table()
used = sorted(set(tiles))
for n in used:
    mesh.materials.append(mats[n])
for indices, tile in zip(faces, tiles):
    f = bm.faces.new([bv[i] for i in indices])
    f.material_index = used.index(tile)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
bm.to_mesh(mesh)
bm.free()
mesh.update()
model = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(model)
bpy.context.view_layer.objects.active = model
model.select_set(True)
for poly in mesh.polygons:
    poly.use_smooth = False
# Triangulate prior to export, so the delivered topology and budget are explicit.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
assert 150 <= len(model.data.polygons) <= 1500
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
scene.world = bpy.data.worlds.new('Underwater studio')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()
def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj, target)
    return obj
light('Sun upper left', 'SUN', (-3, -4, 6), 2.0, (0, 0.3, 0))
light('Soft fill', 'AREA', (3, 2, 4), 120, (0, 0.3, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0.435, 5)
aim(cam, (0, 0.435, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.3
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.1, -4.4, 4.7)
aim(cam, (0, 0.35, 0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
