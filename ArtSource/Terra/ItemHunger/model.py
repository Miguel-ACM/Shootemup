import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as vertical and looks at the front from +Z.
try:
    SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    SCRIPT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)


def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.78
    return mat


gum = material(0, (0.55, 0.12, 0.22))
gum_light = material(1, (0.72, 0.23, 0.31))
gum_side = material(2, (0.43, 0.105, 0.17))
tooth = material(3, (0.96, 0.88, 0.68))
tooth_light = material(4, (1.0, 0.96, 0.82))


def mesh_object(name, vertices, faces, materials, face_materials=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.clear()
    for mat in materials:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    if face_materials:
        for polygon, index in zip(mesh.polygons, face_materials):
            polygon.material_index = index
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    return obj


# An eight-sided rounded cross-section swept along a broad arch. The two
# complete arches have the same finish on front and back as the pickup spins.
arch = [(-0.382, 0.105, 0.065), (-0.337, 0.175, 0.081),
        (-0.255, 0.235, 0.088), (-0.13, 0.269, 0.090),
        (0.0, 0.280, 0.092), (0.13, 0.269, 0.090),
        (0.255, 0.235, 0.088), (0.337, 0.175, 0.081),
        (0.382, 0.105, 0.065)]


def make_jaw(sign):
    vertices, faces, indices = [], [], []
    sides = 8
    for x, y, radius in arch:
        for j in range(sides):
            a = 2.0 * math.pi * j / sides
            vertices.append((x, sign * (y + radius * math.cos(a)),
                             0.135 * math.sin(a)))
    for i in range(len(arch) - 1):
        for j in range(sides):
            faces.append((i*sides+j, (i+1)*sides+j,
                          (i+1)*sides+(j+1)%sides, i*sides+(j+1)%sides))
            # A little colour variation reinforces the rounded gum volume.
            indices.append(1 if j in (0, 7) else (2 if j in (3, 4) else 0))
    faces.append(tuple(reversed(range(sides))))
    indices.append(0)
    faces.append(tuple((len(arch)-1)*sides+j for j in range(sides)))
    indices.append(0)
    obj = mesh_object('Upper gum' if sign == 1 else 'Lower gum', vertices,
                      faces, [gum, gum_light, gum_side], indices)
    # Recalculate winding for both mirrored halves and their caps.
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)


def make_fang(x, base_y, tip_y, width, lean):
    # Two octagonal rings and an offset point give each tooth a robust root,
    # faceted taper and a true three-dimensional, slightly hooked tip.
    n = 8
    vertices, faces, indices = [], [], []
    for y, radius, zradius, xoffset in ((base_y, width, 0.085, 0.0),
                                        (base_y + (tip_y-base_y)*0.38,
                                         width*0.78, 0.069, lean*0.25)):
        for j in range(n):
            a = 2.0 * math.pi * j / n
            vertices.append((x + xoffset + radius*math.cos(a), y,
                             zradius*math.sin(a)))
    vertices.append((x + lean, tip_y, 0.0))
    for j in range(n):
        k = (j+1) % n
        faces.append((j, k, n+k, n+j))
        indices.append(1 if j in (1, 2, 5, 6) else 0)
        faces.append((n+j, n+k, 2*n))
        indices.append(1 if j in (1, 2, 5, 6) else 0)
    faces.append(tuple(reversed(range(n))))
    indices.append(0)
    obj = mesh_object('Fang', vertices, faces, [tooth, tooth_light], indices)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)


for sign in (1, -1):
    make_jaw(sign)
    # Long outer canines frame three shorter teeth, leaving a visible gap
    # between the upper and lower rows.
    for x, root, end, width, lean in [
        (-0.270, 0.195, 0.006, 0.044, 0.022),
        (-0.140, 0.205, 0.075, 0.042, 0.010),
        (0.000, 0.210, 0.085, 0.044, 0.000),
        (0.140, 0.205, 0.075, 0.042, -0.010),
        (0.270, 0.195, 0.006, 0.044, -0.022),
    ]:
        make_fang(x, sign*root, sign*end, width, lean)

bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Explicitly triangulate the export mesh, retaining the intended flat facets.
triangulate = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=triangulate.name)
for poly in model.data.polygons:
    poly.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(SCRIPT_DIR, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Render setup is created only after exporting the single mesh.
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
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1.0)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(obj, point=(0, 0, 0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()


sun_data = bpy.data.lights.new('Upper left sun', type='SUN')
sun = bpy.data.objects.new('Upper left sun', sun_data)
bpy.context.collection.objects.link(sun)
sun.location = (-3, 5, 6)
aim(sun)
sun_data.energy = 2.0
sun_data.angle = math.radians(15)
fill_data = bpy.data.lights.new('Soft fill', type='AREA')
fill = bpy.data.objects.new('Soft fill', fill_data)
bpy.context.collection.objects.link(fill)
fill.location = (4, 1, 5)
aim(fill)
fill_data.energy = 160
fill_data.shape = 'DISK'
fill_data.size = 5

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 4)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(SCRIPT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (0.78, 0.39, 1.38)
aim(camera)
camera_data.type = 'PERSP'
camera_data.lens = 50
scene.render.filepath = os.path.join(SCRIPT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

