import bpy
import math
import os
import sys
from mathutils import Vector

# Run with Blender's Python interpreter. The argument after -- may be either
# this script's path or its containing directory when __file__ is unavailable.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[0])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.7
    bsdf.inputs['Metallic'].default_value = 0.15
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

parts = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def loft(name, rings, material):
    # Each ring is an equal-length outline in the XY plane at a given height.
    n = len(rings[0][1])
    verts = [(x,y,z) for z, ring in rings for x,y in ring]
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n):
            k = (i+1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, material)

def plate(name, outline, z0, z1, inset, material):
    cx = sum(x for x,y in outline)/len(outline)
    cy = sum(y for x,y in outline)/len(outline)
    top = [(cx+(x-cx)*inset, cy+(y-cy)*inset) for x,y in outline]
    return loft(name, [(z0, outline), (z1, top)], material)

def box(name, location, scale, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[material])
    if bevel:
        mod = obj.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def ring_y(name, x, y, z, outer, inner, depth, material, segments=12, direction=-1):
    # Annular mouth lies precisely at y; it has a short inner wall and a dark bore.
    verts = []
    for yy, radius in [(y-direction*depth, outer), (y, outer), (y, inner), (y-direction*depth*.65, inner)]:
        verts += [(x+radius*math.cos(2*math.pi*i/segments), yy,
                   z+radius*math.sin(2*math.pi*i/segments)) for i in range(segments)]
    faces = []
    for r in range(3):
        for i in range(segments):
            j = (i+1) % segments
            faces.append((r*segments+i,r*segments+j,(r+1)*segments+j,(r+1)*segments+i))
    obj = mesh(name, verts, faces, material)
    bore = [(x+inner*math.cos(2*math.pi*i/segments), y-direction*depth*.65,
             z+inner*math.sin(2*math.pi*i/segments)) for i in range(segments)]
    mesh(name+' dark interior', bore, [tuple(range(segments))], 11)
    return obj

def disk_z(name, x, y, z, radius, depth, mat, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(x,y,z))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

# Twin broad armored pontoons preserve the original forked carrier silhouette.
for s in (-1, 1):
    outline = [(s*x,y) for x,y in [(0.65,1.12),(1.34,.83),(1.33,-.74),
                                    (.99,-1.27),(.65,-.85)]]
    if s < 0:
        outline.reverse()
    plate('Pontoon lower keel', outline, -.22, .13, .94, 0)
    plate('Pontoon sloped armor', outline, .10, .30, .87, 12)
    top = [(s*x,y) for x,y in [(.72,.79),(1.24,.66),(1.23,-.65),
                                (1.00,-1.04),(.72,-.73)]]
    if s < 0:
        top.reverse()
    plate('Steel flight deck', top, .295, .345, .96, 4)
    stripe = [(s*x,y) for x,y in [(.77,.69),(.85,.67),(.85,-.76),(.78,-.68)]]
    if s < 0:
        stripe.reverse()
    plate('Pale deck stripe', stripe, .346, .355, 1.0, 1)
    panel = [(s*x,y) for x,y in [(.94,.46),(1.18,.39),(1.18,-.41),(.94,-.48)]]
    if s < 0:
        panel.reverse()
    plate('Recessed hangar deck', panel, .35, .365, .98, 11)
    inset = [(s*x,y) for x,y in [(.975,.39),(1.15,.34),(1.15,-.34),(.975,-.39)]]
    if s < 0:
        inset.reverse()
    plate('Hangar hatch', inset, .366, .38, .96, 8)
    box('Hangar cross brace', (s*1.065,-.02,.393), (.20,.07,.035), 6, .01)
    # Barrel front face at the gameplay muzzle, with its visible hollow mouth.
    ring_y('Forward gun lip', s*1.04, -1.33, .18, .115, .067, .30, 6)
    # Socket face is exactly on the specified rear attachment point.
    ring_y('Engine socket rim', s*1.05, 1.04, 0, .155, .105, .16, 6, direction=1)
    box('Raised aft shoulder', (s*1.05,.70,.37), (.40,.35,.15), 0, .045)
    box('Shoulder flash', (s*1.05,.68,.454), (.30,.075,.018), 13, .008)

# Crossbeams and a wedge-shaped command island bridge the twin hulls.
box('Aft bridge beam', (0,.65,.035), (1.62,.25,.29), 6, .04)
box('Forward bridge beam', (0,-.32,.045), (1.56,.22,.25), 6, .035)
base = [(-.42,.87),(.42,.87),(.50,.12),(0,-.40),(-.50,.12)]
plate('Command island foundation', base, .12, .40, .90, 0)
bridge = [(-.30,.73),(.30,.73),(.35,.13),(0,-.23),(-.35,.13)]
plate('Bridge upper armor', bridge, .39, .54, .83, 12)
# A broad dark observation band with a thin cyan command display.
box('Bridge visor surround', (0,.055,.539), (.53,.17,.055), 11, .025)
box('Bridge cyan visor', (0,.015,.574), (.42,.045,.012), 9, .005)
# Recessed radar has a heavy rim and one directional amber index.
disk_z('Radar steel rim', 0,.46,.54,.205,.06,6)
disk_z('Radar dark well', 0,.46,.574,.157,.012,11)
box('Radar index', (0,.46,.585), (.17,.027,.012), 10, .004)

# Join before export; all objects have flat normals and baked transforms.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
# Triangulate explicitly so the export and triangle budget are deterministic.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert len(model.data.polygons) <= 4000, 'Triangle budget exceeded'
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

light('Upper left sun', 'SUN', (-4,-5,8), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 250, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,8)
cam.rotation_euler = (0,0,0)  # local -Z down, local Y toward screen up
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.20
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (4.2,-5.3,4.3)
aim(cam, (0,0,.12))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
