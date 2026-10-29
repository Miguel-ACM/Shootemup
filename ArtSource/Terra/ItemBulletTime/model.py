import bpy
import math
import os
import sys
from mathutils import Vector

# All modeled coordinates use Y as the item's vertical axis and +Z as its front.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1:
    OUT = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))
else:
    OUT = os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.38, 0.29, 0.72, 1),  # violet case
    (0.54, 0.43, 0.95, 1),  # signature lavender
    (0.72, 0.65, 1.00, 1),  # bevel highlight
    (0.19, 0.29, 0.54, 1),  # deep blue face border
    (0.34, 0.77, 0.88, 1),  # icy clock face
    (0.78, 0.94, 0.98, 1),  # markings
    (0.98, 0.71, 0.28, 1),  # brass buttons
    (0.99, 0.87, 0.49, 1),  # pause symbol
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
def material(obj, index):
    obj.data.materials.append(mats[index])
    parts.append(obj)
    return obj

def ringed_disc(name, rings, n, indices):
    # Each ring is (radius, z); cap and bevel faces use specified palette entries.
    if rings[0][1] > rings[-1][1]:
        rings = list(reversed(rings))
        indices = list(reversed(indices))
    verts = [(r * math.sin(2*math.pi*i/n), r * math.cos(2*math.pi*i/n), z)
             for r, z in rings for i in range(n)]
    faces = [tuple(range(n))]
    face_mats = [indices[0]]
    for j in range(len(rings)-1):
        for i in range(n):
            a = j*n+i
            b = j*n+(i+1)%n
            faces.append((a+n, b+n, b, a))
            face_mats.append(indices[j+1])
    faces.append(tuple(reversed([(len(rings)-1)*n+i for i in range(n)])))
    face_mats.append(indices[-1])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, idx in zip(mesh.polygons, face_mats):
        poly.material_index = idx
    parts.append(obj)
    return obj

def box(name, center, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return material(obj, mat)

# A chunky, faceted pocket stopwatch. The front and back have matching faces.
ringed_disc('Case', [(.285,-.135), (.326,-.108), (.340,-.075),
                     (.340,.075), (.326,.108), (.285,.135)], 16,
            [0, 2, 1, 0, 1, 2, 0])
for side in (-1, 1):
    # Deliberately thick, opaque layers survive small HUD sizes.
    z = side * .137
    ringed_disc('Face rim', [(.284, z-side*.005), (.284, z+side*.013),
                             (.262, z+side*.027)], 16, [3, 3, 2, 3])
    ringed_disc('Clock face', [(.257, z+side*.026), (.257, z+side*.032)],
                16, [4, 4, 4])
    # Twelve large hour pips, embedded on both faces.
    for i in range(12):
        a = 2*math.pi*i/12
        pip = box('Hour marker', (.213*math.sin(a), .213*math.cos(a), side*.179),
                  (.026, .043 if i % 3 == 0 else .030, .014), 5, .004)
        pip.rotation_euler[2] = -a
    for x in (-.055, .055):
        box('Pause bar', (x, 0, side*.185), (.064, .220, .024), 7, .012)

# The top crown and the offset secondary button give a stopwatch silhouette.
box('Crown stem', (0, .350, 0), (.105, .080, .13), 3, .012)
box('Crown', (0, .408, 0), (.175, .090, .18), 6, .015)
box('Crown top', (0, .455, 0), (.125, .014, .13), 7, .005)
button = box('Side button stem', (.264, .274, 0), (.083, .072, .11), 3, .01)
button.rotation_euler[2] = -.65
button = box('Side button', (.294, .308, 0), (.125, .065, .14), 6, .012)
button.rotation_euler[2] = -.65

# Join, bake transforms, and retain flat polygon normals.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
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
scene.world = bpy.data.worlds.new('Blue gray studio') if not scene.world else scene.world
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05, .07, .09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-3, 5, 4), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (3, 1, 4), 140, (0, 0, 0), 4)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
point_at(cam, (0, .06, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (.83, .55, 1.57)
point_at(cam, (0, .02, 0))
cam_data.type = 'PERSP'
cam_data.lens = 52
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

