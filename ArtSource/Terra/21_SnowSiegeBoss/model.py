import bpy
import math
import os
import sys
from mathutils import Vector

# Blender 5.x: run with blender --background --python model.py [-- output_directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in list(bpy.data.materials):
    bpy.data.materials.remove(datablock)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

parts = []
def finish(obj, material):
    obj.data.materials.append(mats[material])
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

def uvball(loc, scale, material, segments=32, rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, material)

def mesh_obj(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material)

def ring(name, center, outer, inner, depth, material, axis='Z', n=16):
    # Actual annular tube, with a dark recessed disk separately behind its mouth.
    verts = []
    for axial, radius in ((-depth, outer), (0, outer), (0, inner), (-depth, inner)):
        for i in range(n):
            a = 2 * math.pi * i / n
            if axis == 'Z':
                verts.append((center[0] + radius*math.cos(a), center[1] + radius*math.sin(a), center[2] + axial))
            else:
                verts.append((center[0] + radius*math.cos(a), center[1] + axial, center[2] + radius*math.sin(a)))
    faces = []
    for j in range(4):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, ((j+1)%4)*n+k, ((j+1)%4)*n+i))
    return mesh_obj(name, verts, faces, material)

def cylinder(loc, radius, depth, material, axis='Z', vertices=16):
    rot = (math.pi/2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    return finish(bpy.context.object, material)

def plate(name, points, zbottom, material):
    # Flat polygon with sloping bevel: inset top preserves a broad, readable facet.
    cx = sum(p[0] for p in points)/len(points)
    cy = sum(p[1] for p in points)/len(points)
    n = len(points)
    verts = [(x,y,zbottom) for x,y in points]
    verts += [(cx+(x-cx)*0.87, cy+(y-cy)*0.87, zbottom+0.075) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh_obj(name, verts, faces, material)

def shutter(angle, material):
    # A thick, gently crowned annular sector: six curved, distinct armor petals.
    n = 8
    radii = (0.69, 0.80, 1.08, 1.25)
    heights = (0.32, 0.38, 0.31, 0.17)
    verts = []
    for r, z in zip(radii, heights):
        for i in range(n+1):
            a = angle + math.radians(-26 + 52*i/n)
            verts.append((r*math.cos(a), r*math.sin(a), z))
    top_count = len(verts)
    verts += [(x,y,z-0.075) for x,y,z in verts]
    faces = []
    for j in range(3):
        for i in range(n):
            a=j*(n+1)+i; b=a+1; c=b+n+1; d=a+n+1
            faces.extend(((a,b,c,d),(d+top_count,c+top_count,b+top_count,a+top_count)))
    for j in range(3):
        for i in (0,n):
            a=j*(n+1)+i; b=(j+1)*(n+1)+i
            faces.append((a,b,b+top_count,a+top_count))
    for j in (0,3):
        for i in range(n):
            a=j*(n+1)+i; b=a+1
            faces.append((a,a+top_count,b+top_count,b))
    mesh_obj('Shutter petal', verts, faces, material)

# The dark equatorial belt and shallow, faceted pressure hull establish a round craft.
uvball((0,0,-0.04), (1.32,1.29,0.24), 11, 32, 12)
uvball((0,0,-0.025), (1.29,1.26,0.23), 4, 32, 12)
for i in range(6):
    shutter(math.radians(30 + i*60), 1 if i % 2 == 0 else 15)

# Raised pressure dome, inset inside the petals, with a forward bridge visor.
uvball((0,0.15,0.25), (0.66,0.67,0.35), 15, 24, 10)
plate('Bridge armor', [(-.30,.48),(.30,.48),(.42,.12),(.31,-.22),(-.31,-.22),(-.42,.12)], .53, 4)
plate('Forward bridge visor', [(-.29,-.24),(.29,-.24),(.24,-.43),(-.24,-.43)], .425, 11)
plate('Visor ice', [(-.23,-.265),(.23,-.265),(.19,-.385),(-.19,-.385)], .45, 8)

# Forecastle cheeks frame the recessed, fixed gameplay launcher at (0,-.65,.25).
for side in (-1,1):
    pts = [(side*.29,-.51),(side*.53,-.49),(side*.69,-.76),
           (side*.54,-1.05),(side*.31,-1.10),(side*.22,-.83)]
    if side == -1: pts.reverse()
    plate('Forward cheek', pts, .235, 6)
# A top-facing open mortar mouth: rim is centered exactly at the gameplay point.
ring('Mortar lip', (0,-.65,.25), .145, .092, .105, 6, n=16)
cylinder((0,-.65,.17), .091, .012, 11, vertices=16)

# Paired aft engine socket openings, centered at the exact gameplay sockets.
for x in (-.74,.74):
    # Housing ends at the socket; the opening is a recessed, dark annulus facing aft.
    cylinder((x,.99,0), .19, .12, 4, axis='Y')
    ring('Engine nozzle rim', (x,1.05,0), .16, .105, .10, 6, axis='Y')
    cylinder((x,1.049,0), .104, .008, 11, axis='Y')

# Join the complete asset and bake every object transform before OBJ export.
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
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Render only after exporting, so cameras and lights never enter the OBJ.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(kind, name, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj, target)
    return obj

light('SUN', 'Upper left sun', (-4,-5,8), 2.1, (0,0,0))
light('AREA', 'Soft fill', (4,3,5), 220, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
# With camera local Y aligned to world +Y, screen up is +Y.
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.20
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (4.2,-5.7,5.2)
point_at(cam, (0,0,0.10))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
