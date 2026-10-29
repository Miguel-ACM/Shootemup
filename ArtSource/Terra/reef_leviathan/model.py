import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Reef Leviathan: coordinates are kept in gameplay space throughout.
def output_directory():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except NameError:
        if '--' in sys.argv:
            args = sys.argv[sys.argv.index('--') + 1:]
            if args:
                return os.path.dirname(os.path.abspath(args[0]))
        return os.getcwd()

OUT = output_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.48 if i in (9, 10, 13) else 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.7
    mats.append(m)

body = []
parts = []
metadata = []

def mesh(name, vertices, faces, material, group):
    data = bpy.data.meshes.new(name + '_mesh')
    data.from_pydata(vertices, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[material])
    group.append(ob)
    return ob

def ellipsoid(name, loc, scale, material, group, segments=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mats[material])
    group.append(ob)
    return ob

def cylinder(name, loc, radius, depth, material, group, axis='Z', vertices=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    ob = bpy.context.object
    ob.name = name
    if axis == 'Y':
        ob.rotation_euler[0] = math.pi / 2
    ob.data.materials.append(mats[material])
    group.append(ob)
    return ob

def loft(name, sections, material, group, sides=10):
    # Each section is (x, y, z, horizontal radius, vertical radius).
    vertices = []
    for x, y, z, rx, rz in sections:
        for k in range(sides):
            a = 2 * math.pi * k / sides
            vertices.append((x + rx * math.cos(a), y, z + rz * math.sin(a)))
    faces = []
    faces.append(tuple(reversed(range(sides))))
    for j in range(len(sections) - 1):
        for k in range(sides):
            n = (k + 1) % sides
            faces.append((j*sides+k, j*sides+n, (j+1)*sides+n, (j+1)*sides+k))
    faces.append(tuple((len(sections)-1)*sides+k for k in range(sides)))
    return mesh(name, vertices, faces, material, group)

def plate(name, outline, center, material, group, bottom=0.02):
    # Faceted double-sided, convex plate with a raised center ridge.
    verts = [(x, y, z) for x, y, z in outline]
    verts += [center, (center[0], center[1], bottom)]
    n = len(outline)
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.extend(((i, j, n), (j, i, n + 1)))
    return mesh(name, verts, faces, material, group)

def join_group(group, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = group[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for poly in ob.data.polygons:
        poly.use_smooth = False
    return ob

# Deep, pointed mantle. Overlapping lofts form raised, substantial armor.
loft('Mantle', [(0,-0.12,0.19,0.36,0.16), (0,0.12,0.20,0.48,0.20),
                (0,0.43,0.19,0.43,0.19), (0,0.76,0.15,0.29,0.15),
                (0,1.10,0.09,0.025,0.025)], 8, body, 12)
loft('Mantle armor', [(0,0.26,0.24,0.375,0.17), (0,0.52,0.22,0.34,0.16),
                      (0,0.78,0.17,0.235,0.12), (0,1.07,0.10,0.018,0.02)], 0, body, 12)
# Surface bands follow the faceted crown and remain visible above the shell.
def surface_band(name, y, half_width, z_center, z_edge):
    xs = [-half_width, -half_width*0.72, 0, half_width*0.72, half_width]
    zs = [z_edge, z_center-0.025, z_center, z_center-0.025, z_edge]
    verts = [(x, yy, z) for yy in (y-0.035, y+0.035) for x,z in zip(xs,zs)]
    mesh(name, verts, [(i,i+1,i+6,i+5) for i in range(4)], 13, body)

for side in (-1, 1):
    plate('Deep teal panel', [(side*0.35,0.34,0.345), (side*0.29,0.56,0.343),
          (side*0.19,0.76,0.279), (side*0.09,0.78,0.286), (side*0.12,0.34,0.410)],
          (side*0.21,0.53,0.385), 8, body, 0.27)
surface_band('Lower brass band', 0.31, 0.395, 0.426, 0.285)
surface_band('Upper brass band', 0.65, 0.275, 0.353, 0.252)
plate('Steel crown', [(-0.13,0.35,0.420), (-0.10,0.65,0.360),
      (0,0.94,0.218), (0.10,0.65,0.360), (0.13,0.35,0.420)],
      (0,0.53,0.440), 6, body, 0.30)
plate('Dorsal ridge', [(-0.045,0.37,0.438), (-0.035,0.68,0.362),
      (0,0.99,0.175), (0.035,0.68,0.362), (0.045,0.37,0.438)],
      (0,0.57,0.480), 13, body, 0.31)
for y, x, z in [(0.31,0.27,0.390), (0.65,0.18,0.322)]:
    for side in (-1, 1):
        ellipsoid('Band rivet', (side*x,y,z), (0.024,0.024,0.016), 15, body, 8, 4)
for side in (-1, 1):
    ellipsoid('Plate rivet', (side*0.105,0.45,0.425), (0.022,0.022,0.014), 13, body, 8, 4)

# Head and pressure visor. Large inset cyan eyes read strongly from overhead.
ellipsoid('Head', (0,-0.26,0.17), (0.43,0.31,0.20), 8, body, 16, 8)
ellipsoid('Visor surround', (0,-0.30,0.30), (0.38,0.20,0.10), 13, body, 16, 6)
ellipsoid('Pressure visor', (0,-0.31,0.355), (0.34,0.16,0.07), 11, body, 16, 6)
for side in (-1, 1):
    ellipsoid('Eye', (side*0.205,-0.315,0.405), (0.105,0.095,0.04), 9, body, 12, 6)
    ellipsoid('Eye pupil', (side*0.205,-0.325,0.439), (0.028,0.055,0.012), 11, body, 8, 4)
    # Fixed housings and barrels: the forward face of each lip is exactly y=-0.7.
    ellipsoid('Gill housing', (side*0.75,-0.46,0.14), (0.16,0.22,0.12), 0, body, 10, 6)
    cylinder('Gill cannon', (side*0.82,-0.59,0.18), 0.068, 0.18, 13, body, 'Y', 10)
    cylinder('Muzzle lip', (side*0.82,-0.69,0.18), 0.079, 0.02, 6, body, 'Y', 10)
    # Dark inset and small amber bore sit on the forward-facing mouth.
    cylinder('Muzzle bore', (side*0.82,-0.701,0.18), 0.052, 0.004, 11, body, 'Y', 10)
    cylinder('Muzzle core', (side*0.82,-0.704,0.18), 0.025, 0.004, 10, body, 'Y', 8)

# Swinging mantle fins, each hinged at the inner root. Their plate silhouette
# points aft (+Y), making the mantle unmistakably squid-like.
for side, suffix in [(-1, 'L'), (1, 'R')]:
    group = []
    outline = [(side*0.32,0.26,0.13), (side*0.62,0.30,0.10),
               (side*0.82,0.38,0.09), (side*0.92,0.52,0.08),
               (side*0.90,0.66,0.08), (side*0.80,0.79,0.08),
               (side*0.60,0.86,0.09), (side*0.29,0.93,0.10)]
    plate('Fin brass rim', outline, (side*0.55,0.60,0.205), 13, group, 0.025)
    inset = [(side*0.34,0.32,0.145), (side*0.60,0.36,0.125),
             (side*0.77,0.43,0.115), (side*0.85,0.53,0.105),
             (side*0.83,0.64,0.105), (side*0.75,0.73,0.105),
             (side*0.59,0.80,0.115), (side*0.32,0.87,0.125)]
    plate('Fin lobe', inset, (side*0.53,0.60,0.23), 4, group, 0.08)
    plate('Fin inset', [(side*0.39,0.42,0.17), (side*0.70,0.51,0.16),
                        (side*0.69,0.67,0.15), (side*0.38,0.80,0.15)],
          (side*0.52,0.60,0.24), 8, group, 0.10)
    part = join_group(group, 'Fin_' + suffix)
    parts.append(part)
    metadata.append({'object': part.name, 'motion': 'swing', 'axis': [0,side,0],
                     'pivot': [side*0.34,0.55,0.13], 'speed': 0.72, 'amplitude': 12})

# Eight tapering arms, plus a longer hunting tentacle on each outer edge.
# All details on an arm are joined to that arm, so they move together.
for side, suffix in [(-1, 'L'), (1, 'R')]:
    for j in range(5):
        hunting = (j == 4)
        root_x = side * (0.07 + 0.105*j)
        root_y = -0.39 + 0.025*j
        end_x = side * ([0.16, 0.36, 0.57, 0.75, 0.91][j])
        end_y = [-1.08, -1.16, -1.08, -0.98, -1.19][j]
        group = []
        section_data = []
        # The outward sweep increases towards the tip, creating a curled fan.
        def arm_point(t):
            x = root_x + (end_x-root_x)*(0.22*t + 0.78*t*t)
            y = root_y + (end_y-root_y)*t
            return x, y
        for k in range(7):
            t = k / 6
            x, y = arm_point(t)
            if hunting:
                radius = [0.078,0.069,0.057,0.048,0.043,0.065,0.040][k]
            else:
                radius = [0.105,0.096,0.081,0.067,0.052,0.038,0.014][k]
            section_data.append((x,y,0.12 - 0.025*t,radius,radius*0.70))
        loft('Arm', section_data, 8 if j % 2 == 0 else 0, group, 8)
        # Contrasting suckers are embedded on the visible upper/inner surface.
        for t in (0.42, 0.72):
            x, y = arm_point(t)
            cylinder('Sucker', (x,y,0.166 - 0.015*t), 0.034 if not hunting else 0.028,
                     0.014, 15, group, 'Z', 8)
        if hunting:
            club_x, club_y = arm_point(0.93)
            ellipsoid('Tentacle club', (club_x,club_y,0.10), (0.075,0.10,0.055), 4, group, 10, 6)
            cylinder('Club sucker', (club_x,club_y,0.157), 0.033, 0.014, 15, group, 'Z', 8)
        name = ('Tent_' if hunting else 'Arm%d_' % (j+1)) + suffix
        part = join_group(group, name)
        parts.append(part)
        metadata.append({'object': name, 'motion': 'swing', 'axis': [0,0,side],
                         'pivot': [root_x,root_y,0.11], 'speed': round(0.62+0.11*j,2),
                         'amplitude': 8 + 2*j})

model = join_group(body, 'Model')
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump(metadata, f, indent=2)
    f.write('\n')

# Preserve the body and the 12 individually named moving meshes in the OBJ.
bpy.ops.object.select_all(action='DESELECT')
for ob in [model] + parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    aim(ob, target)
    return ob

light('Sun', 'SUN', (-3,4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,-3,4), 160, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)  # camera local -Z looks down, local +Y is screen up
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.8
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.2,-4.3,3.5)
aim(cam, (0,-0.03,0.12))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
