import bpy
import math
import os
import sys
from mathutils import Vector

# This asset uses Y as its vertical axis and presents its front towards +Z.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT_DIR = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)


def material(index, color):
    mat = bpy.data.materials.new('tile_%02d' % index)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1.0)
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Metallic'].default_value = 0.0
    bsdf.inputs['Roughness'].default_value = 0.7
    return mat

brass = material(0, (0.83, 0.49, 0.10))
gold = material(1, (1.00, 0.72, 0.22))
light_gold = material(2, (1.00, 0.84, 0.38))
amber = material(3, (0.64, 0.32, 0.07))
slot = material(4, (0.20, 0.13, 0.07))
coral = material(5, (0.95, 0.24, 0.16))
teal = material(6, (0.08, 0.69, 0.68))
cream = material(7, (1.00, 0.89, 0.57))
blue = material(8, (0.10, 0.34, 0.45))
parts = []


def mesh_object(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    parts.append(obj)
    return obj


def box(name, center, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Broad machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj


def chamber():
    # Four rings form a faceted, chamfered round pea chamber, front and back.
    n = 16
    rings = [(-0.145, 0.188), (-0.115, 0.232), (0.115, 0.232), (0.145, 0.188)]
    verts = []
    for z, radius in rings:
        for i in range(n):
            a = 2 * math.pi * i / n
            verts.append((-0.16 + radius * math.cos(a), -0.18 + radius * math.sin(a), z))
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings) - 1):
        for i in range(n):
            k = (i + 1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    obj = mesh_object('Rounded pea chamber', verts, faces, brass)
    obj.data.materials.append(gold)
    obj.data.materials.append(light_gold)
    for p in obj.data.polygons:
        if p.index == 0 or p.index == len(obj.data.polygons)-1:
            p.material_index = 1
        elif 1 <= p.index <= n or 2*n < p.index <= 3*n:
            p.material_index = 2
    return obj


def disc(name, cx, cy, z, radius, depth, mat, n=16):
    verts = []
    for zz in (z-depth/2, z+depth/2):
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((cx+radius*math.cos(a), cy+radius*math.sin(a), zz))
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh_object(name, verts, faces, mat)


def cord(name, phase, mat):
    # Two stout, interwoven strands make the loop legible even as a small icon.
    steps, sides = 32, 6
    verts = []
    for i in range(steps):
        t = 2*math.pi*i/steps
        twist = 6*t + phase
        radial = 0.012*math.cos(twist)
        x = -0.16 + (0.205+radial)*math.cos(t)
        y = 0.258 + (0.157+radial)*math.sin(t)
        z = 0.012*math.sin(twist)
        outward = Vector((math.cos(t), math.sin(t), 0)).normalized()
        for j in range(sides):
            a = 2*math.pi*j/sides
            p = Vector((x,y,z)) + 0.021*(math.cos(a)*outward + Vector((0,0,math.sin(a))))
            verts.append(tuple(p))
    faces = []
    for i in range(steps):
        for j in range(sides):
            faces.append((i*sides+j, ((i+1)%steps)*sides+j, ((i+1)%steps)*sides+(j+1)%sides, i*sides+(j+1)%sides))
    return mesh_object(name, verts, faces, mat)


def plaque(name, points, z0, z1, mat):
    n = len(points)
    verts = [(x,y,z) for z in (z0,z1) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_object(name, verts, faces, mat)

chamber()
# Front and reverse stamped circular medallions retain the round whistle identity as it spins.
for sign in (-1, 1):
    disc('Chamber inset', -0.16, -0.18, sign*0.149, 0.145, 0.012, gold)
    disc('Chamber center', -0.16, -0.18, sign*0.157, 0.096, 0.008, brass)

# A substantial rectangular windway and flattened mouthpiece, all chamfered.
box('Windway', (0.105,-0.13,0), (0.43,0.225,0.225), gold, 0.035)
box('Mouthpiece', (0.337,-0.13,0), (0.20,0.155,0.175), brass, 0.025)
box('Mouthpiece lip', (0.418,-0.13,0), (0.045,0.17,0.19), light_gold, 0.012)
# A dark broad fipple opening and its raised edge read as a whistle at a glance.
for sign in (-1, 1):
    box('Air window', (0.105,-0.075,sign*0.119), (0.16,0.060,0.012), slot, 0.008)
    box('Window lower rim', (0.105,-0.113,sign*0.126), (0.18,0.021,0.018), light_gold, 0.006)

# Short loop and collar connect the rope physically to the chamber.
cord('Coral braid', 0, coral)
cord('Turquoise braid', math.pi, teal)
box('Lanyard socket', (-0.16,0.065,0), (0.115,0.10,0.14), amber, 0.018)
box('Socket collar', (-0.16,0.104,0), (0.13,0.035,0.15), light_gold, 0.008)

# A thick shield-shaped tag hangs beside the loop. The winged drone is repeated on both sides.
box('Badge connector', (0.135,0.19,0), (0.075,0.10,0.065), gold, 0.014)
tag = [(0.075,0.245),(0.195,0.245),(0.215,0.22),(0.215,0.135),(0.135,0.095),(0.055,0.135),(0.055,0.22)]
plaque('Teal shield tag', tag, -0.045, 0.045, blue)
for s in (-1,1):
    z0, z1 = sorted((s*0.046, s*0.054))
    # Simple solid emblem: central drone body and swept wings, no letters.
    plaque('Drone emblem', [(0.135,0.224),(0.154,0.194),(0.135,0.158),(0.116,0.194)], z0,z1,cream)
    plaque('Left wing', [(0.112,0.198),(0.068,0.207),(0.078,0.179),(0.115,0.177)], z0,z1,cream)
    plaque('Right wing', [(0.158,0.198),(0.202,0.207),(0.192,0.179),(0.155,0.177)], z0,z1,cream)

# Join the solid components and bake transforms; preserve flat faces and material slots.
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

bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Studio previews. The world and lights are deliberately simple and neutral.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue-gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'


def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()


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

light('Upper left sun', 'SUN', (-3,5,6), 2.0)
light('Soft fill', 'AREA', (4,1,4), 180, 5.0)
camera_data = bpy.data.cameras.new('Preview camera')
camera = bpy.data.objects.new('Preview camera', camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location = (0,0,3)
aim(camera)
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 1.22
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)

camera.location = (1.55, 0.85, 2.65)
aim(camera, (0,0,0))
camera_data.type = 'PERSP'
camera_data.lens = 90
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

