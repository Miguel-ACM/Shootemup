import bpy
import math
import os
import sys
from mathutils import Vector

# The source model uses Y as its vertical axis and +Z as its front.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    (0.92, 0.29, 0.36, 1),  # coral armor
    (0.68, 0.20, 0.27, 1),  # edge of the armor
    (1.00, 0.45, 0.49, 1),  # bevel highlight
    (0.78, 0.25, 0.32, 1),  # rear armor
    (1.00, 0.83, 0.65, 1),  # pale double chevron
    (0.95, 0.62, 0.40, 1),  # fasteners
    (0.48, 0.19, 0.23, 1),  # seam
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.68
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], [tuple(reversed(face)) for face in faces])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

# Chamfered octagonal armour; four rings create broad, real faceted bevels.
outline = [(-.235,.265), (.235,.265), (.315,.185), (.315,-.185),
           (.235,-.265), (-.235,-.265), (-.315,-.185), (-.315,.185)]
def transform(x, y, center, angle):
    c, s = math.cos(angle), math.sin(angle)
    return (center[0] + c*x - s*y, center[1] + s*x + c*y)

def plate(center, angle, face_mat):
    # Front and rear are equally bevelled; front cap slightly crowned.
    rings = [(0.90, -.072), (1.0, -.046), (1.0, .046), (0.90, .072)]
    verts = []
    for scale, z in rings:
        for x, y in outline:
            xx, yy = transform(x*scale, y*scale, center, angle)
            verts.append((xx, yy, center[2]+z))
    verts += [(center[0], center[1], center[2]-.079),
              (center[0], center[1], center[2]+.079)]
    faces, ids = [], []
    for j in range(3):
        for i in range(8):
            k = (i+1) % 8
            faces.append((j*8+i, j*8+k, (j+1)*8+k, (j+1)*8+i))
            ids.append([2, 1, 2][j])
    for i in range(8):
        k = (i+1) % 8
        faces.extend([(32, k, i), (33, 24+i, 24+k)])
        ids.extend([face_mat, face_mat])
    mesh_part('Beveled armor', verts, faces, ids)

# Slight offset and opposing rotations make the two layers apparent even head-on.
rear = (.075, .075, -.090)
front = (-.065, -.060, .090)
ra, fa = math.radians(-10), math.radians(8)
plate(rear, ra, 3)
plate(front, fa, 0)

def bar(a, b, width, z0, z1, center, angle, material):
    dx, dy = b[0]-a[0], b[1]-a[1]
    length = math.hypot(dx, dy)
    nx, ny = -dy/length*width/2, dx/length*width/2
    corners = [(a[0]+nx,a[1]+ny), (b[0]+nx,b[1]+ny),
               (b[0]-nx,b[1]-ny), (a[0]-nx,a[1]-ny)]
    verts = []
    for z in (z0, z1):
        for x,y in corners:
            xx, yy = transform(x, y, center, angle)
            verts.append((xx, yy, center[2]+z))
    faces = [(3,2,1,0), (4,5,6,7), (0,1,5,4), (1,2,6,5), (2,3,7,6), (3,0,4,7)]
    mesh_part('Chevron', verts, faces, [material]*6)

def markings(center, angle, side):
    # Thick ivory chevrons preserve the characteristic original symbol at HUD size.
    z0, z1 = ((.076, .101) if side > 0 else (-.101, -.076))
    for y in (-.10, .055):
        bar((-.155,y), (0,y+.085), .055, z0,z1, center,angle,4)
        bar((0,y+.085), (.155,y), .055, z0,z1, center,angle,4)

markings(front, fa, 1)
markings(rear, ra, -1)

def rivet(center, angle, x, y, z):
    xx, yy = transform(x, y, center, angle)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=(xx,yy,center[2]+z))
    obj = bpy.context.object
    obj.name = 'Armor fastener'
    obj.scale = (.029,.029,.014)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[5])
    for face in obj.data.polygons:
        face.use_smooth = False
    parts.append(obj)

for center, angle, sign in ((front,fa,1), (rear,ra,-1)):
    for x in (-.235,.235):
        for y in (-.175,.175):
            rivet(center, angle, x, y, sign*.075)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for face in model.data.polygons:
    face.use_smooth = False

# Export only the pickup before adding the render rig.
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
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, 1, 4), 120, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
aim(cam)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.85, 0.60, 1.55)
aim(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

