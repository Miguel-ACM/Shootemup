import bpy
import math
import os
import sys
from mathutils import Vector

# Blender's script path is available in normal use; -- also supports text-editor launchers.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if not os.path.isdir(OUT):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.76, 0.84, 0.82, 1),  # pale aircraft
    (0.43, 0.57, 0.60, 1),  # blue-gray facets
    (0.16, 0.82, 0.90, 1),  # cyan roll arrow
    (0.08, 0.51, 0.61, 1),  # arrow sides
    (0.42, 0.94, 0.96, 1),  # arrow highlight
    (0.13, 0.43, 0.53, 1),  # cockpit
    (0.96, 0.70, 0.25, 1),  # engine accents
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, material_ids):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for face, idx in zip(mesh.polygons, material_ids):
        face.material_index = idx
        face.use_smooth = False
    parts.append(obj)
    return obj

def prism(name, points, z0, z1, face_mat, side_mat):
    n = len(points)
    verts = [(x,y,z0) for x,y in points] + [(x,y,z1) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    ids = [face_mat, face_mat]
    for i in range(n):
        j = (i+1) % n
        faces.append((i,j,j+n,i+n))
        ids.append(side_mat)
    return mesh_part(name, verts, faces, ids)

# Segmented, solid circular sweep: a broad cyan band with bevelled edges.
# Starts at lower right and sweeps counter-clockwise to the lower left.
segments = 32
start, end = math.radians(-50), math.radians(230)
verts = []
for i in range(segments + 1):
    a = start + (end-start)*i/segments
    # cross-section: front and rear faces, chamfered to the outer wall
    for radius, z in ((.345,-.035),(.357,-.055),(.408,-.055),(.423,-.035),
                      (.423,.035),(.408,.055),(.357,.055),(.345,.035)):
        verts.append((radius*math.cos(a), radius*math.sin(a), z))
faces, ids = [], []
for i in range(segments):
    for j in range(8):
        faces.append((i*8+j, i*8+(j+1)%8, (i+1)*8+(j+1)%8, (i+1)*8+j))
        ids.append((3,2,4,3,3,2,4,3)[j])
faces.extend([tuple(reversed(range(8))), tuple(segments*8+j for j in range(8))])
ids.extend([3,3])
mesh_part('Rolling arc', verts, faces, ids)
# Arrow point continues along the tangent, with a generous icon-sized head.
prism('Arrowhead', [(-.334,-.247),(-.153,-.311),(-.275,-.408)], -.061,.061,2,3)

# Swept wings and tail: the original forward-pointing fighter silhouette.
prism('Wings', [(0,.255),(.292,-.135),(.112,-.104),(.075,-.197),
                (0,-.237),(-.075,-.197),(-.112,-.104),(-.292,-.135)],
      -.075,.075,0,1)
# Raised faceted fuselage, with a ridge on each face for a complete reverse view.
outline = [(0,.315),(.085,.105),(.083,-.136),(0,-.235),(-.083,-.136),(-.085,.105)]
for sign in (-1,1):
    zbase = sign*.074
    ridge = [(0,.24,sign*.135),(0,-.09,sign*.16)]
    verts = [(x,y,zbase) for x,y in outline] + ridge
    faces = [(0,1,6),(1,2,7,6),(2,3,7),(3,4,7),(4,5,6,7),(5,0,6),(6,7,3)]
    if sign < 0:
        faces = [tuple(reversed(f)) for f in faces]
    mesh_part('Fuselage', verts, faces, [0,0,1,1,0,0,1])
    # Squat jewel-like cockpit rather than a thin painted detail.
    v = [(-.043,.125,sign*.131),(.043,.125,sign*.131),(.052,.025,sign*.151),
         (0,-.035,sign*.168),(-.052,.025,sign*.151),(0,.065,sign*.187)]
    f = [(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5),(4,3,2,1,0)]
    if sign < 0:
        f = [tuple(reversed(face)) for face in f]
    mesh_part('Cockpit', v, f, [5,5,5,5,5,5])
    # Wing stripes and compact engine blocks reinforce the aircraft shape.
    for side in (-1,1):
        stripe = [(side*.13,-.095),(side*.255,-.128),(side*.205,-.145),(side*.12,-.126)]
        prism('Wing marking', stripe, sign*.076, sign*.085, 1, 1)
        prism('Engine', [(side*.045,-.205),(side*.085,-.183),(side*.088,-.229),(side*.047,-.244)],
              sign*.075, sign*.099, 6, 1)

# Join all components into the sole exported mesh and bake transforms.
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
# Export before creating cameras/lights, and export only the model.
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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05,.07,.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, (0,0,0))
    return obj

sun = light('Upper left sun', 'SUN', (-3,4,6), 2.0)
sun.data.angle = math.radians(15)
light('Soft fill', 'AREA', (4,1,4), 180, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,4)
point_at(cam, (0,0,0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.20
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.45,1.05,2.9)
point_at(cam, (0,0,0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
