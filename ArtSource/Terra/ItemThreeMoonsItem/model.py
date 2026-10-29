import bpy
import math
import os
import sys
from mathutils import Vector

# Three original gray moons in their original triangular arrangement.
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    HERE = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(HERE):
        HERE = os.path.dirname(HERE)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [(0.61, 0.61, 0.63, 1), (0.46, 0.46, 0.48, 1),
           (0.70, 0.70, 0.72, 1), (0.54, 0.54, 0.56, 1)]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.9
    mats.append(mat)

# Shallow, polygonal impact basins are part of the sphere surface, not decals.
# Distributed on both hemispheres so the moons retain their lunar character in a spin.
craters = [
    (Vector((-0.42, 0.40, 0.82)).normalized(), 0.38),
    (Vector((0.43, -0.14, 0.89)).normalized(), 0.42),
    (Vector((-0.25, -0.62, 0.75)).normalized(), 0.29),
    (Vector((0.60, 0.37, -0.71)).normalized(), 0.37),
    (Vector((-0.36, -0.24, -0.90)).normalized(), 0.43),
    (Vector((-0.78, 0.48, -0.39)).normalized(), 0.28),
    (Vector((0.94, -0.30, 0.12)).normalized(), 0.30),
]
segments, rings, radius = 20, 14, 0.25
verts, faces, indices = [], [], []
for moon_index, (cx, cy) in enumerate(((-0.26, 0.22), (0.26, 0.22), (0.0, -0.22))):
    base = len(verts)
    # Y is the polar axis, matching the upright game coordinate system.
    for j in range(rings + 1):
        theta = math.pi * j / rings
        for i in range(segments):
            phi = 2 * math.pi * i / segments + moon_index * 0.12
            n = Vector((math.sin(theta) * math.cos(phi), math.cos(theta), math.sin(theta) * math.sin(phi)))
            depth = 0.0
            for direction, angular_radius in craters:
                angle = math.acos(max(-1.0, min(1.0, n.dot(direction))))
                if angle < angular_radius:
                    # Rounded bowl with a gently raised outer lip.
                    t = angle / angular_radius
                    depth = min(depth, -0.018 * (1.0 - t * t))
                elif angle < angular_radius * 1.18:
                    depth = max(depth, 0.0025 * (1.0 - (angle / angular_radius - 1.0) / 0.18))
            verts.append((cx + n.x * (radius + depth), cy + n.y * (radius + depth), n.z * (radius + depth)))
    for j in range(rings):
        for i in range(segments):
            a = base + j * segments + i
            b = base + j * segments + (i + 1) % segments
            c = base + (j + 1) * segments + (i + 1) % segments
            d = base + (j + 1) * segments + i
            # Collapse degenerate polar quads to triangles.
            face = (a, d, c) if j == 0 else (a, d, b) if j == rings - 1 else (a, d, c, b)
            faces.append(tuple(reversed(face)))
            center = sum((Vector(verts[k]) for k in face), Vector()) / len(face) - Vector((cx, cy, 0))
            center.normalize()
            crater_t = min(math.acos(max(-1.0, min(1.0, center.dot(direction)))) / ar for direction, ar in craters)
            indices.append(1 if crater_t < 0.72 else 3 if crater_t < 0.94 else 2 if crater_t < 1.15 else 0)

mesh = bpy.data.meshes.new('Three moons mesh')
mesh.from_pydata(verts, [], faces)
mesh.update()
obj = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(obj)
for mat in mats:
    mesh.materials.append(mat)
for poly, mat_index in zip(mesh.polygons, indices):
    poly.material_index = mat_index
    poly.use_smooth = False
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
# Remove coincident pole vertices, yielding a clean closed mesh for each moon.
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=0.00001)
bpy.ops.object.mode_set(mode='OBJECT')

bpy.ops.wm.obj_export(filepath=os.path.join(HERE, 'model.obj'), export_selected_objects=True,
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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target=(0, 0, 0)):
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
    point_at(ob)
    return ob

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4, 1, 4), 100, 5)
camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 4)
point_at(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.42
scene.render.filepath = os.path.join(HERE, 'top.png')
bpy.ops.render.render(write_still=True)
camera.location = (1.05, 0.67, 1.76)
point_at(camera)
camera_data.type = 'PERSP'
camera_data.lens = 50
scene.render.filepath = os.path.join(HERE, 'angle.png')
bpy.ops.render.render(write_still=True)

