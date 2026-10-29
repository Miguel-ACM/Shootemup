import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- output_directory]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    if '--' not in sys.argv or not sys.argv[sys.argv.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(sys.argv[sys.argv.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.105, 0.15, 0.20), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.48, 0.36, 0.18), (0.53, 0.61, 0.55),
    (0.66, 0.70, 0.71),
]
# The space atlas uses tile_07 for blue photovoltaic cells.
colors[7] = (0.18, 0.27, 0.48)
mats = []
for i, c in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*c, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*c, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*c, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(m)

body = []
dish = []
def register(obj, tile, group=body):
    obj.data.materials.append(mats[tile])
    for p in obj.data.polygons:
        p.use_smooth = False
    group.append(obj)
    return obj

def mesh(name, verts, faces, tile, group=body):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return register(obj, tile, group)

def box(name, loc, size, tile, bevel=0.0, group=body):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Edge facets', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return register(o, tile, group)

def polyplate(name, outline, bottom, top, tile, inset=0.04, group=body):
    # Tapered bevel between the vertical side and inset upper surface.
    cx = sum(x for x, y in outline) / len(outline)
    cy = sum(y for x, y in outline) / len(outline)
    n = len(outline)
    upper = [(x + (cx-x)*inset, y + (cy-y)*inset) for x,y in outline]
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,top-0.045) for x,y in outline] + [(x,y,top) for x,y in upper]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    for i in range(n):
        j = (i+1)%n
        faces += [(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i)]
    return mesh(name, verts, faces, tile, group)

def cylinder(name, loc, radius, depth, tile, vertices=12, axis='Z', group=body):
    rotation = (math.pi/2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    o = bpy.context.object
    o.name = name
    return register(o, tile, group)

def beam(name, a, b, width, tile, group=body):
    a, b = Vector(a), Vector(b)
    d = b-a
    o = box(name, (a+b)*0.5, (width, width, d.length), tile, 0.006, group)
    o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
    return o

def annulus(name, center, outer, inner, depth, tile, axis='Z', group=body, n=12):
    # Closed hollow ring; for Y axis its front face is exactly center.y-depth/2.
    verts = []
    for axial, rad in [(-depth/2,outer), (depth/2,outer), (-depth/2,inner), (depth/2,inner)]:
        for i in range(n):
            a = 2*math.pi*i/n
            if axis == 'Y':
                verts.append((center[0]+rad*math.cos(a), center[1]+axial, center[2]+rad*math.sin(a)))
            else:
                verts.append((center[0]+rad*math.cos(a), center[1]+rad*math.sin(a), center[2]+axial))
    faces = []
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i), (2*n+j,2*n+i,3*n+i,3*n+j),
                      (j,i,2*n+i,2*n+j), (n+i,n+j,3*n+j,3*n+i)])
    return mesh(name, verts, faces, tile, group)

# Two long ivory side rails and a navy central spine form the satellite chassis.
polyplate('Core', [(-.27,-.88),(.27,-.88),(.32,.51),(.19,.94),(-.19,.94),(-.32,.51)], -.12, .17, 6)
for s in (-1, 1):
    outline = [(s*.13,-.78),(s*.35,-.78),(s*.40,.50),(s*.20,.88)]
    if s < 0: outline.reverse()
    polyplate('Ivory outrigger', outline, -.08, .20, 15, .14)
    polyplate('Gold rear brace', [(s*.25,.79),(s*.57,1.08),(s*.44,.27)][::s], -.035, .08, 13)
    box('Solar boom', (s*.60,.38,.04), (.70,.095,.13), 13, .018)
    # Layered, framed blue solar wing, with a restrained two-by-three cell pattern.
    box('Array backing', (s*.89,.40,.035), (.51,.88,.09), 6, .025)
    box('Solar cells', (s*.89,.40,.091), (.43,.78,.027), 7, .01)
    for xx in (-.238,.238):
        box('Array edge', (s*.89+xx,.40,.105), (.035,.85,.045), 13, .008)
    for yy in (-.405,.405):
        box('Array end', (s*.89,.40+yy,.105), (.51,.035,.045), 15, .008)
    box('Array divider', (s*.89,.40,.11), (.017,.78,.016), 13)
    for yy in (-.13,.13):
        box('Cell division', (s*.89,.40+yy,.11), (.43,.014,.016), 13)
    polyplate('Forward shoulder', [(s*.14,-.74),(s*.33,-.71),(s*.36,-.37),(s*.14,-.40)][::s], .20, .24, 15)

# Long forward laser housing, with a hollow ring facing -Y.  The lip plane is
# exactly at the gameplay muzzle (0, -1.05, +0.24).
box('Laser breech', (0,-.59,.19), (.30,.75,.25), 6, .055)
box('Raised barrel spine', (0,-.57,.33), (.19,.49,.06), 15, .02)
for s in (-1,1):
    box('Laser shoulder', (s*.17,-.62,.24), (.065,.42,.12), 13, .015)
cylinder('Barrel sleeve', (0,-.90,.24), .145, .24, 13, axis='Y')
annulus('Muzzle lip', (0,-1.025,.24), .137, .079, .05, 15, axis='Y')
# Dark recessed bore and a tiny charged core are behind the lip, not a projectile.
cylinder('Bore shadow', (0,-1.000,.24), .078, .008, 11, axis='Y')
cylinder('Laser core', (0,-1.006,.24), .033, .009, 9, axis='Y')

# The bowl is a genuine faceted paraboloid, with a thick rim and raised feed.
# It is a separate gently tilting part, hinged at the centre of its mount.
cylinder('Dish pedestal', (0,.28,.20), .22, .16, 13)
cylinder('Turntable', (0,.28,.29), .17, .06, 6)
N = 16
cx, cy = 0, .28
rings = [(0.10,.30), (.28,.355), (.48,.50), (.53,.55)]
verts=[]
for radius,z in rings:
    for i in range(N):
        a=2*math.pi*i/N
        verts.append((cx+radius*math.cos(a),cy+radius*math.sin(a),z))
faces=[]
for r in range(len(rings)-1):
    for i in range(N):
        j=(i+1)%N
        faces.append((r*N+i,r*N+j,(r+1)*N+j,(r+1)*N+i))
mesh('Reflector bowl', verts, faces, 15, dish)
cylinder('Reflector centre', (0,.28,.30), .102, .035, 13, group=dish)
annulus('Reflector rim', (0,.28,.55), .55, .50, .045, 13, group=dish, n=N)
# Three short feed struts make the concavity and focus clear from above.
for a in (math.pi/2, math.pi/2+2*math.pi/3, math.pi/2+4*math.pi/3):
    beam('Feed support', (.43*math.cos(a), .28+.43*math.sin(a), .50), (0,.28,.70), .025, 6, dish)
cylinder('Feed collar', (0,.28,.68), .065, .08, 13, vertices=10, group=dish)
cylinder('Feed lens', (0,.28,.73), .045, .03, 9, vertices=10, group=dish)

def join_group(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    o = bpy.context.object
    o.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for p in o.data.polygons:
        p.use_smooth = False
    return o

model = join_group(body, 'Model')
reflector = join_group(dish, 'Dish')
# Position the part origin at its hinge while keeping world-space geometry fixed.
pivot = Vector((0, .28, .32))
for v in reflector.data.vertices:
    v.co -= pivot
reflector.location = pivot
parts = [{'object':'Dish', 'motion':'swing', 'axis':[1,0,0],
          'pivot':[0,0.28,0.32], 'speed':0.25, 'amplitude':12}]
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump(parts, f, indent=2)

# Export before adding lights and cameras; the selected meshes retain part names.
bpy.ops.object.select_all(action='DESELECT')
for o in (model, reflector): o.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue-gray world') if not scene.world else scene.world
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, pos, energy, target, size=None):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=pos
    aim(obj,target)
    return obj
light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 180, (0,0,0), 5.0)
camera_data = bpy.data.cameras.new('Camera')
camera = bpy.data.objects.new('Camera', camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location=(0,0,7)
camera.rotation_euler=(0,0,0)  # local -Z looks down; local +Y is screen up
camera_data.type='ORTHO'
camera_data.ortho_scale=2.75
scene.render.filepath=os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camera_data.type='PERSP'
camera_data.lens=50
camera.location=(3.6,-5.2,5.0)
aim(camera,(0,0,.15))
scene.render.filepath=os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
