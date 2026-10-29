import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Mirage Obelisk: a pair of sand-worn pillars linked by a rotating seal.
def script_dir():
    path = globals().get('__file__')
    if path:
        return os.path.dirname(os.path.abspath(path))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            return os.path.dirname(os.path.abspath(args[0]))
    return os.getcwd()

OUT = script_dir()
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
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.76
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    materials.append(mat)

body = []
parts = []
def mesh_object(name, verts, faces, mats, indices=None, group=body):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        mesh.materials.append(materials[m])
    for i, poly in enumerate(mesh.polygons):
        poly.use_smooth = False
        if indices is not None:
            poly.material_index = indices[i]
    group.append(obj)
    return obj

def tier(name, rings, mats, group=body):
    # Corresponding polygon rings make sloped facets and a genuine raised crown.
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    indices = [0]
    for j in range(len(rings)-1):
        for k in range(n):
            faces.append((j*n+k, j*n+(k+1)%n, (j+1)*n+(k+1)%n, (j+1)*n+k))
            indices.append(min(j, len(mats)-1))
    faces.append(tuple((len(rings)-1)*n+k for k in range(n)))
    indices.append(len(mats)-1)
    return mesh_object(name, verts, faces, mats, indices, group)

def box(name, location, size, material, bevel=0.0, group=body):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[material])
    if bevel:
        mod = obj.modifiers.new('Cut corners', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for face in obj.data.polygons:
        face.use_smooth = False
    group.append(obj)
    return obj

def cylinder(name, center, radius, depth, material, axis='Z', vertices=12, group=body):
    rot = (math.pi/2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center, rotation=rot)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(materials[material])
    for f in obj.data.polygons:
        f.use_smooth = False
    group.append(obj)
    return obj

def annulus_y(name, x, y_front, z, outer, inner, depth, material, interior=11):
    # Open ring, with its lip exactly at y_front; bore walls lead to a dark inset.
    n = 12
    vs = []
    for y, r in ((y_front, outer), (y_front, inner), (y_front+depth, inner), (y_front+depth, outer)):
        for i in range(n):
            a = 2*math.pi*i/n
            vs.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    fs, ids = [], []
    for ring in range(4):
        nxt = (ring+1)%4
        for i in range(n):
            fs.append((ring*n+i, ring*n+(i+1)%n, nxt*n+(i+1)%n, nxt*n+i))
            ids.append(1 if ring == 1 else 0)
    fs.append(tuple(2*n+i for i in reversed(range(n))))
    ids.append(1)
    return mesh_object(name, vs, fs, [material, interior], ids)

for side in (-1, 1):
    x = side * 0.43
    # Long, independently faceted obelisks, with a tapered rear shoulder.
    def outline(width, z, front, shoulder, back, rear_width):
        return [(x-width, front, z), (x+width, front, z),
                (x+width*1.06, shoulder, z), (x+rear_width, back, z),
                (x-rear_width, back, z), (x-width*1.06, shoulder, z)]
    tier('Pillar', [outline(.19, -.04, -.79, .68, 1.06, .095),
                    outline(.215, .10, -.79, .68, 1.06, .10),
                    outline(.165, .28, -.72, .62, .97, .075),
                    outline(.12, .34, -.61, .55, .85, .055)], [11, 12, 13, 13])
    # Raised, pale ceramic slab is inset enough to leave broad brass shoulders.
    tier('Ceramic crown', [outline(.117, .342, -.58, .49, .79, .045),
                            outline(.085, .385, -.51, .43, .69, .025)], [12, 15])
    # One wide inlaid stripe makes the paired pillars legible from above.
    box('Obelisk inlay', (x, .02, .39), (.035, .77, .014), 13, .007)
    # The muzzle center is exactly the gameplay position: (+/- .43, -.87, .20).
    cylinder('Gun housing', (x, -.765, .20), .115, .20, 12, 'Y')
    annulus_y('Muzzle lip', x, -.87, .20, .091, .056, .065, 13)
    # Engine socket at (+/- .43, .79, 0); its opening faces aft (+Y).
    # The dark disk sits inside the collar rather than depicting a flame.
    annulus_y('Rear socket', x, .79, 0.0, .102, .065, .09, 13)
    # A rear-facing mouth at the exact socket plane; collar sits forward of it.
    cylinder('Socket shadow', (x, .792, 0.0), .061, .006, 11, 'Y')

# Strong crossbar connects the two pillars and supports an independent rotating seal.
box('Crossbar', (0, .43, .12), (.84, .17, .18), 11, .025)
box('Crossbar cap', (0, .43, .225), (.72, .115, .05), 6, .018)
cylinder('Seal socket', (0, .43, .255), .205, .055, 11, vertices=12)
rotor = []
cylinder('Seal rim', (0, .43, .29), .158, .045, 13, vertices=8, group=rotor)
cylinder('Seal inset', (0, .43, .315), .108, .014, 12, vertices=8, group=rotor)
# Four broad facets form a wind-carved compass rather than a row of small details.
for a in (0, math.pi/2, math.pi, 3*math.pi/2):
    ca, sa = math.cos(a), math.sin(a)
    points = [(0.015, -0.025), (.13, 0), (.045, .035)]
    verts = [(px*ca-py*sa, .43+px*sa+py*ca, .324) for px, py in points]
    mesh_object('Seal facet', verts, [(0, 1, 2)], [15], group=rotor)
cylinder('Seal jewel', (0, .43, .33), .027, .015, 10, vertices=8, group=rotor)

def join_group(objects, name, origin=None):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    # Apply rotations and scale, retaining a meaningful part origin if supplied.
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if origin is not None:
        bpy.context.scene.cursor.location = origin
        bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    else:
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
    return obj

model = join_group(body, 'Model')
seal = join_group(rotor, 'Seal', (0, .43, .29))
parts.append({'object': 'Seal', 'motion': 'spin', 'axis': [0, 0, 1],
              'pivot': [0, .43, .29], 'speed': 360})
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump(parts, f, indent=2)

# Only model objects are selected for export. Blender's OBJ exporter retains object names.
bpy.ops.object.select_all(action='DESELECT')
for obj in (model, seal):
    obj.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z', apply_modifiers=True)

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new('Blue gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05, .07, .09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

light('Sun', 'SUN', (-4, -4, 7), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 5), 300, (0, 0, 0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 6)
cam.rotation_euler = (0, 0, 0)  # Local -Z looks down; local +Y is screen up.
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.55
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.25, -4.3, 4.3)
aim(cam, (0, .07, .12))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

