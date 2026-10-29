import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

# The asset is centered on its original pivot: X runs along the wrapper,
# Y across it, and positive Z faces the camera.
if globals().get('__file__'):
    OUT = Path(__file__).resolve().parent
elif '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv):
    arg = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
    OUT = arg if arg.is_dir() else arg.parent
else:
    OUT = Path.cwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = {
    2: (0.30, 0.78, 0.66), 1: (0.97, 0.85, 0.71),
    15: (0.98, 0.96, 0.88), 0: (0.78, 0.19, 0.40),
    12: (0.92, 0.31, 0.49),
}
mats = {}
for n, rgb in palette.items():
    mat = bpy.data.materials.new('tile_%02d' % n)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.82
    mats[n] = mat

parts = []
def mesh_part(name, vertices, faces, material_ids):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for n in palette:
        mesh.materials.append(mats[n])
    indices = {n: i for i, n in enumerate(palette)}
    for poly, n in zip(mesh.polygons, material_ids):
        poly.material_index = indices[n]
        poly.use_smooth = False
    parts.append(obj)
    return obj

# A rounded, faceted pillow with narrow cream bands embedded in its surface.
# The rings include the wrapper's pinched shoulders at either end.
rings = [(-.53,.12,.105),(-.48,.23,.16),(-.40,.32,.205),
         (-.30,.38,.24),(-.23,.405,.255),(-.19,.413,.26),
         (-.10,.42,.27),(.10,.42,.27),(.19,.413,.26),
         (.23,.405,.255),(.30,.38,.24),(.40,.32,.205),
         (.48,.23,.16),(.53,.12,.105)]
N = 16
verts = []
for x, ry, rz in rings:
    for j in range(N):
        a = 2 * math.pi * j / N
        verts.append((x, ry * math.cos(a), rz * math.sin(a)))
faces, ids = [], []
faces.append(tuple(reversed(range(N)))); ids.append(2)
for i in range(len(rings)-1):
    for j in range(N):
        faces.append((i*N+j, i*N+(j+1)%N, (i+1)*N+(j+1)%N, (i+1)*N+j))
        ids.append(1 if i in (4, 8) else 2)
faces.append(tuple((len(rings)-1)*N+j for j in range(N))); ids.append(2)
mesh_part('Candy pillow', verts, faces, ids)

# Each end is a folded, opaque wrapper fan. The alternating high and low
# ridges make the twist readable from directly overhead and from the side.
for sign in (-1, 1):
    xs = [.48, .57, .71, .83, .91]
    widths = [.12, .115, .205, .285, .32]
    verts = []
    for k, (x, width) in enumerate(zip(xs, widths)):
        for j in range(7):
            t = -1 + j / 3
            # The outer edge has the characteristic concave, notched end.
            xx = x - (0.09 * (1 - abs(t)) if k == 4 else 0)
            ridge = (0.018 if j % 2 == 0 else -0.012) * (k / 4)
            verts.append((sign * xx, width * t, .045 + ridge))
    for k, (x, width) in enumerate(zip(xs, widths)):
        for j in range(7):
            t = -1 + j / 3
            xx = x - (0.09 * (1 - abs(t)) if k == 4 else 0)
            ridge = (0.012 if j % 2 == 0 else -0.008) * (k / 4)
            verts.append((sign * xx, width * t, -.045 - ridge))
    faces, ids = [], []
    for k in range(4):
        for j in range(6):
            a = k*7+j; b = a+7
            faces.append((a, b, b+1, a+1)); ids.append(15 if j in (1,4) else (1 if j in (0,5) else 2))
            faces.append((35+a+1,35+b+1,35+b,35+a)); ids.append(1 if j % 2 else 2)
    for k in range(4):
        for j in (0, 6):
            a=k*7+j; b=(k+1)*7+j
            faces.append((a,35+a,35+b,b)); ids.append(2)
    for j in range(6):
        a=28+j
        faces.append((a,a+1,35+a+1,35+a)); ids.append(1)
    mesh_part('Pleated wrapper', verts, faces, ids)

# Narrow faceted collars visibly gather each end of the wrapper.
for sign in (-1, 1):
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=1, depth=.075,
        end_fill_type='NGON', location=(sign*.515, 0, 0), rotation=(0, math.pi/2, 0))
    obj = bpy.context.object
    obj.name = 'Gathered collar'
    obj.scale = (.125, .145, 1)
    obj.data.materials.append(mats[1])
    parts.append(obj)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
# Preserve the original center as the object's origin.
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
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
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(obj, point=(0,0,0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 4, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, -4, 5), 90, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 6)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.15
scene.render.filepath = str(OUT / 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.45, -1.9, 2.25)
aim(cam)
scene.render.filepath = str(OUT / 'angle.png')
bpy.ops.render.render(write_still=True)

