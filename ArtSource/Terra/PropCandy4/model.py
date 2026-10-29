import bpy
import math
import os
import sys
from mathutils import Vector

# The Unity prop lies in XY, centered around the original pivot; +Z faces the camera.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and sys.argv.index('--') + 1 < len(sys.argv):
    OUT = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))
else:
    raise RuntimeError('Pass the script path after -- when __file__ is unavailable')

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

def material(name, color):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    return mat

cream = material('tile_15', (0.98, 0.96, 0.88))
red = material('tile_00', (0.78, 0.19, 0.40))

# Parallel cross sections follow the original centerline. Rotating each ring
# creates a continuous diagonal candy stripe without layered or floating faces.
side_count = 12
radius = 0.12
straight_length = 1.44
bend_radius = 0.36
bend_length = math.pi * bend_radius
length = straight_length + bend_length
steps = 47

def center_tangent(s):
    if s <= straight_length:
        return Vector((0.36, -1.05 + s, 0)), Vector((0, 1, 0))
    a = (s - straight_length) / bend_radius
    return Vector((bend_radius * math.cos(a), 0.39 + bend_radius * math.sin(a), 0)), Vector((-math.sin(a), math.cos(a), 0))

vertices = []
faces = []
face_materials = []
rings = []

def ring(center, tangent, r, phase):
    normal = Vector((-tangent.y, tangent.x, 0))
    indices = []
    for j in range(side_count):
        a = 2 * math.pi * j / side_count + phase
        p = center + r * (normal * math.cos(a) + Vector((0, 0, math.sin(a))))
        indices.append(len(vertices))
        vertices.append(tuple(p))
    rings.append(indices)

# Rounded tips extend to the same overall bounds as the original end balls.
start, start_tan = center_tangent(0)
end, end_tan = center_tangent(length)
ring(start - start_tan * radius * 0.866, start_tan, radius * 0.5, 0)
ring(start - start_tan * radius * 0.5, start_tan, radius * 0.866, 0)
for i in range(steps + 1):
    s = length * i / steps
    c, t = center_tangent(s)
    ring(c, t, radius, 2 * math.pi * s / 0.55)
end_phase = 2 * math.pi * length / 0.55
ring(end + end_tan * radius * 0.5, end_tan, radius * 0.866, end_phase)
ring(end + end_tan * radius * 0.866, end_tan, radius * 0.5, end_phase)

for i in range(len(rings) - 1):
    for j in range(side_count):
        k = (j + 1) % side_count
        faces.append((rings[i][j], rings[i][k], rings[i+1][k], rings[i+1][j]))
        face_materials.append(0 if j < 6 else 1)
# Closed round tips, tapering to a single point.
start_tip = len(vertices)
vertices.append(tuple(start - start_tan * radius))
end_tip = len(vertices)
vertices.append(tuple(end + end_tan * radius))
for j in range(side_count):
    k = (j + 1) % side_count
    faces.append((start_tip, rings[0][k], rings[0][j]))
    face_materials.append(0 if j < 6 else 1)
    faces.append((end_tip, rings[-1][j], rings[-1][k]))
    face_materials.append(0 if j < 6 else 1)

mesh = bpy.data.meshes.new('Candy cane mesh')
mesh.from_pydata(vertices, [], faces)
mesh.materials.append(cream)
mesh.materials.append(red)
mesh.update()
obj = bpy.data.objects.new('Model', mesh)
bpy.context.collection.objects.link(obj)
for poly, index in zip(mesh.polygons, face_materials):
    poly.material_index = index
    poly.use_smooth = False
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Export only the finished model, before creating the render rig.
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
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
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
    bpy.context.collection.objects.link(ob)
    ob.location = location
    point_at(ob, target)
    return ob

center = (0, -0.15, 0)
light('Upper left sun', 'SUN', (-3, 4, 6), 2.0, center).data.angle = math.radians(18)
light('Soft fill', 'AREA', (4, -2, 5), 180, center, 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, -0.15, 6)
point_at(cam, center)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.40
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (2.6, -3.3, 4.2)
point_at(cam, center)
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
