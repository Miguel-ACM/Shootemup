import bpy
import math
import os
import sys
from mathutils import Vector

# This model uses game coordinates directly: Y is up and Z is the front.
if globals().get('__file__'):
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.24, 0.78, 1.00, 1),  # glacial cyan
    (0.60, 0.94, 1.00, 1),  # ice highlights
    (0.20, 0.49, 0.74, 1),  # blue steel housing
    (0.42, 0.70, 0.86, 1),  # pale steel
    (0.42, 0.36, 0.81, 1),  # violet field
    (0.72, 0.69, 1.00, 1),  # lavender core
    (0.10, 0.33, 0.53, 1),  # small recessed inset
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.7
    mats.append(mat)

parts = []
def register(obj, material):
    obj.data.materials.append(mats[material])
    for poly in obj.data.polygons:
        poly.use_smooth = False
    parts.append(obj)
    return obj

def drum(radius, depth, z, material, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(0, 0, z))
    return register(bpy.context.object, material)

def crystal(angle, r0, r1, width, depth, material):
    # Six-sided double-pyramid: an icy blade with a raised ridge on each face.
    p = Vector((math.cos(angle), math.sin(angle)))
    q = Vector((-p.y, p.x))
    center = p * (r0 + (r1-r0) * 0.46)
    a, b = p*r0, p*r1
    left, right = center+q*width, center-q*width
    verts = [(a.x,a.y,0), (left.x,left.y,0), (b.x,b.y,0),
             (right.x,right.y,0), (center.x,center.y,depth), (center.x,center.y,-depth)]
    faces = [(0,1,4), (1,2,4), (2,3,4), (3,0,4),
             (1,0,5), (2,1,5), (3,2,5), (0,3,5)]
    mesh = bpy.data.meshes.new('Crystal')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Crystal', mesh)
    bpy.context.collection.objects.link(obj)
    register(obj, material)
    obj.data.materials.append(mats[1])
    for i in (0, 1, 4, 5):
        obj.data.polygons[i].material_index = 1
    return obj

# The broad six-point frost crystal replaces the thin rods of the original.
# Its faceted thickness and matched front and rear faces read during the spin.
for i in range(6):
    a = math.pi/2 + i*math.tau/6
    crystal(a, 0.14, 0.45, 0.085, 0.085, 0)
    # Paired short facets give each arm the forked snowflake outline.
    for side in (-1, 1):
        p = Vector((math.cos(a), math.sin(a)))
        q = Vector((-p.y, p.x))
        start = p*0.255
        end = p*0.345 + q*(side*0.105)
        delta = end-start
        branch_angle = math.atan2(delta.y, delta.x)
        # Build local facet then translate its origin to the branch root.
        obj = crystal(branch_angle, 0, delta.length, 0.035, 0.055, 0)
        obj.location = (start.x, start.y, 0)

# A thick, stepped emitter is legible from the side as well as both faces.
drum(0.205, 0.24, 0, 2, 12)
for side in (-1, 1):
    drum(0.194, 0.045, side*0.135, 3, 12)
    drum(0.150, 0.038, side*0.174, 4, 12)
    drum(0.103, 0.042, side*0.210, 6, 12)
    drum(0.075, 0.042, side*0.247, 5, 12)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Ensure a stable, explicitly triangulated export with flat normals.
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
scene.world = bpy.data.worlds.new('Studio World') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0

def aim(obj, point=(0,0,0)):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(kind, name, location, energy, size=None):
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

sun = light('SUN', 'Upper left sun', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(15)
light('AREA', 'Soft fill', (4, 1, 4), 120, 5)

camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, 0, 4)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.10
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (0.85, 0.58, 1.48)
aim(camera)
camera_data.type = 'PERSP'
camera_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

