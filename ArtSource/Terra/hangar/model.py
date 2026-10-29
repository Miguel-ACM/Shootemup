import bpy
import math
import os
import sys
from mathutils import Vector

# All geometry and its palette are self-contained. Dimensions are in gameplay units.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
elif '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1:
    OUT = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))
else:
    OUT = os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.materials):
    bpy.data.materials.remove(block)

palette = {
    'floor': (0.115, 0.155, 0.18, 1), 'floor_alt': (0.14, 0.185, 0.21, 1),
    'seam': (0.065, 0.085, 0.10, 1), 'steel': (0.25, 0.32, 0.35, 1),
    'steel_light': (0.37, 0.44, 0.45, 1), 'edge': (0.12, 0.17, 0.19, 1),
    'concrete': (0.28, 0.30, 0.28, 1), 'cream': (0.69, 0.66, 0.51, 1),
    'yellow': (0.72, 0.52, 0.16, 1), 'black': (0.075, 0.085, 0.08, 1),
    'rust': (0.37, 0.20, 0.13, 1), 'olive': (0.29, 0.33, 0.23, 1),
    'wood': (0.42, 0.29, 0.18, 1), 'sky': (0.045, 0.085, 0.16, 1),
    'cloud': (0.34, 0.43, 0.51, 1), 'red': (0.44, 0.15, 0.10, 1),
    'glow_amber': (0.95, 0.57, 0.19, 1), 'glow_teal': (0.35, 0.78, 0.72, 1),
    'glow_red': (0.95, 0.25, 0.12, 1), 'glow_cream': (0.91, 0.83, 0.59, 1),
}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 1.0
    # The baked face colours are also visible in the inspection renders.
    attr = mat.node_tree.nodes.new('ShaderNodeVertexColor')
    attr.layer_name = 'Color'
    mat.node_tree.links.new(attr.outputs['Color'], bsdf.inputs['Base Color'])
    if name.startswith('glow_'):
        mat.node_tree.links.new(attr.outputs['Color'], bsdf.inputs['Emission Color'])
        bsdf.inputs['Emission Strength'].default_value = 0.65
    materials[name] = mat

parts = []
def finish(obj, mat):
    obj.data.materials.append(materials[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

def box(x, y, z, sx, sy, sz, mat, angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(x, y, z))
    obj = bpy.context.object
    obj.dimensions = (sx, sy, sz)
    obj.rotation_euler[2] = angle
    return finish(obj, mat)

def cyl(x, y, z, radius, depth, mat, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(x, y, z))
    return finish(bpy.context.object, mat)

def poly(points, z, mat):
    mesh = bpy.data.meshes.new('paint')
    mesh.from_pydata([(x, y, z) for x, y in points], [], [list(range(len(points)))])
    mesh.update()
    obj = bpy.data.objects.new('paint', mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, mat)

def beam(a, b, width, mat):
    a, b = Vector(a), Vector(b)
    delta = b - a
    obj = box(*(0.5 * (a + b)), width, width, delta.length, mat)
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return obj

def stripe_bar(x, y, z, length, horizontal=True):
    # Separate short diagonal bars sit on a yellow steel plate.
    box(x, y, z, length if horizontal else .22, .22 if horizontal else length, .06, 'yellow')
    count = max(1, int(length / .42))
    for i in range(count):
        t = (i + .5) * length / count - length / 2
        box(x + (t if horizontal else 0), y + (0 if horizontal else t), z + .035,
            .11, .22, .012, 'black', math.radians(-35 if horizontal else 35))

def crate(x, y, z, s=.55, mat='olive'):
    box(x, y, z+s/2, s, s, s, mat)
    box(x, y, z+s+.018, s*.88, s*.12, .035, 'steel_light', math.pi/4)
    box(x, y, z+s+.02, s*.88, s*.12, .035, 'steel_light', -math.pi/4)

def barrel(x, y, z, mat='rust'):
    cyl(x,y,z+.25,.22,.50,mat,12)
    cyl(x,y,z+.44,.235,.055,'edge',12)
    cyl(x,y,z+.08,.235,.055,'edge',12)
    cyl(x,y,z+.51,.11,.025,'steel',12)

# Deck, restrained panel seams, and boundary structure.
box(0, 0, -.18, 10.5, 18.3, .36, 'floor')
for y in [-7.2,-4.8,-2.4,0,2.4,4.8,7.2]:
    box(0,y,.005,10.2,.025,.012,'seam')
for x in [-4.8,-2.15,2.15,4.8]:
    box(x,0,.008,.035,18.1,.015,'seam')
# Long guide lines and generously spaced arrows maintain a clear flying lane.
for x in [-1.70,1.70]:
    box(x,-.1,.018,.045,13.8,.016,'steel_light')
for y in [-4.4,-1.5,1.4,4.3]:
    poly([(-.13,y-.48),(.13,y-.48),(.13,y+.04),(.46,y+.04),(0,y+.62),(-.46,y+.04),(-.13,y+.04)], .025, 'cream')
for x in [-5.05,5.05]:
    box(x,0,.55,.42,18.5,1.10,'edge')
    box(x,0,1.12,.48,18.5,.14,'steel')
    for y in [-7,-4.5,-2, .5,3,5.5,8]:
        box(x,y,1.23,.5,.22,.12,'steel_light')

# Night visible beyond the open mouth, with deliberately broad low-poly cloud shapes.
box(0,8.44,.025,4.9,1.55,.05,'sky')
for x,y,s in [(-1.65,8.30,.65),(-.95,8.45,.55),(1.05,8.65,.72),(1.8,8.40,.45)]:
    poly([(x-s,y),(x-s*.65,y+.13),(x-s*.25,y+.18),(x,y+.32),(x+s*.45,y+.25),(x+s,y+.06),(x+s*.75,y-.03)], .06, 'cloud')
# Threshold, open retracted armored leaves and overhead framing.
stripe_bar(0,7.54,.13,4.8)
for side in [-1,1]:
    x = side * 3.85
    box(x,8.20,.49,2.35,1.8,.98,'steel')
    box(x,7.43,.78,2.35,.20,1.45,'edge')
    for yy in [7.75,8.18,8.61]:
        box(x,yy,1.01,2.05,.08,.08,'steel_light')
    box(side*2.50,8.15,.72,.28,2.0,1.44,'steel_light')
    stripe_bar(side*2.45,7.90,1.48,1.0,False)
    box(side*2.62,7.20,.43,.55,.40,.86,'edge')
    cyl(side*2.62,7.20,.91,.15,.12,'glow_red',12)
    box(side*2.62,7.2,1.01,.34,.12,.06,'steel_light')
# Lane edge lights frame the exit.
for x in [-1.95,1.95]:
    for y in [5.9,6.6,7.15]:
        box(x,y,.05,.10,.25,.06,'glow_amber')

# Four empty ship pads: concentric solid layers, markings, and small inset lights.
for y in [4.2,1.4,-1.4,-4.2]:
    cyl(-3.4,y,.10,.90,.20,'edge',32)
    cyl(-3.4,y,.205,.82,.10,'steel',32)
    cyl(-3.4,y,.26,.68,.025,'concrete',32)
    # Minimal painted landing cross and registration bars; no physical item on top.
    box(-3.4,y,.28,.56,.055,.008,'cream')
    box(-3.4,y,.28,.055,.56,.008,'cream')
    for a in range(12):
        t = 2*math.pi*a/12
        cyl(-3.4+.755*math.cos(t),y+.755*math.sin(t),.275,.035,.022,'glow_amber',8)
    # Service pipe against outer wall and a compact hinged tool arm.
    box(-4.67,y,.28,.13,1.60,.26,'rust')
    for dy in [-.58,.58]:
        cyl(-4.67,y+dy,.45,.12,.10,'steel_light',12)
    box(-4.55,y+.98,.50,.35,.18,1.0,'edge')
    box(-4.38,y+.98,1.02,.47,.18,.15,'steel_light')
    box(-4.19,y+.98,.83,.10,.14,.34,'yellow')

# Shop bay: bounded counter, empty plinths and illuminated rims.
box(4.78,.6,.40,.32,8.7,.80,'steel')
box(4.72,.6,.86,.44,8.7,.13,'wood')
for y in [3.4,.6,-2.2]:
    cyl(3.4,y,.13,.69,.26,'edge',32)
    cyl(3.4,y,.30,.62,.16,'steel_light',32)
    cyl(3.4,y,.39,.58,.045,'glow_teal',32)
    cyl(3.4,y,.42,.49,.035,'steel',32)
    # Feet and markings are outside the clear plinth top.
    for a in range(8):
        t = 2*math.pi*a/8
        cyl(3.4+.63*math.cos(t), y+.63*math.sin(t), .34, .035, .035, 'glow_amber', 8)
# Rear shelving with a few large distinct packages.
for yy in [-3.5,-.7,2.1,4.9]:
    box(4.62,yy,.72,.45,.18,1.44,'edge')
for z in [.42,.91,1.32]:
    box(4.61,.7,z,.48,7.1,.10,'steel_light')
for y,z,mat in [(-3.2,.48,'rust'),(-1.9,.98,'olive'),(-.5,.48,'yellow'),(1.0,.98,'rust'),(2.6,.48,'olive'),(4.0,.98,'wood')]:
    box(4.55,y,z+.14,.32,.35,.28,mat)
# Vending machine with a single luminous screen and dispensing recess.
box(4.25,5.95,.68,.85,.66,1.36,'olive')
box(4.25,5.60,.93,.56,.035,.45,'glow_teal')
box(4.25,5.59,.38,.48,.04,.20,'edge')
box(4.25,5.57,.30,.52,.09,.07,'steel_light')
# Unlettered shop sign, tilted into the top view.
box(3.38,5.75,.85,1.2,.18,.80,'rust')
box(3.38,5.64,.87,.95,.04,.55,'yellow')
poly([(3.18,5.61),(3.38,5.43),(3.58,5.61),(3.38,5.79)],1.27,'glow_amber')

# Back workshop, below the starting point. An overhead rail and gantry read as a crane.
box(0,-8.97,.43,10.4,.40,.86,'edge')
box(0,-8.70,1.32,9.6,.26,.25,'steel_light')
for x in [-4.65,4.65]:
    box(x,-8.4,.73,.3,.3,1.46,'steel')
box(-.95,-8.66,1.48,1.0,.45,.26,'yellow')
box(-.95,-8.44,1.16,.13,.13,.62,'edge')
box(-.95,-8.44,.85,.36,.16,.10,'rust')
# Workbench, big machine, storage and barrels.
box(-2.55,-7.55,.43,1.7,.65,.12,'wood')
for x in [-3.2,-1.9]:
    box(x,-7.55,.20,.12,.50,.40,'edge')
box(-2.8,-7.55,.55,.48,.29,.13,'steel_light',.12)
box(-2.18,-7.52,.54,.25,.32,.10,'rust',-.25)
box(.45,-7.70,.55,1.3,.85,1.10,'steel')
box(.45,-7.70,1.13,1.05,.68,.12,'steel_light')
cyl(.45,-7.70,1.21,.24,.08,'edge',16)
box(.45,-7.70,1.26,.22,.09,.035,'yellow')
box(1.45,-7.68,.35,.44,.65,.7,'olive')
box(1.45,-7.68,.72,.30,.40,.05,'glow_amber')
crate(2.35,-7.75,0,.62,'olive')
crate(3.05,-7.85,0,.70,'wood')
crate(3.05,-7.85,.70,.48,'rust')
barrel(-4.1,-7.55,0,'olive')
barrel(-3.65,-8.10,0,'rust')
barrel(4.02,-7.52,0,'rust')
barrel(4.43,-8.12,0,'olive')
# Small pools of warm fixtures at the perimeter.
for x,y in [(-4.75,6.3),(-4.75,-5.6),(4.75,-4.8),(4.75,6.75)]:
    box(x,y,1.20,.28,.40,.18,'edge')
    box(x,y,1.31,.18,.27,.05,'glow_amber')

# Plate joints, worn wheel paths, drains and service-side oil patches.
for y in [-6,-3.6,-1.2,1.2,3.6,6]:
    for x in [-3.55,3.55]:
        box(x,y,.015,2.65,.035,.02,'seam')
for x in [-1.22,1.22]:
    box(x,-.1,.013,.18,13.8,.012,'edge')
    for y in [-5,-2,1,4]:
        box(x,y,.022,.11,1.5,.006,'floor_alt')
for x,y,s in [(-2.35,-5.35,.42),(-4.35,2.65,.28),(2.48,-4.65,.45),(4.1,4.6,.22),(-3,-6.5,.3)]:
    poly([(x-s,y-.13*s),(x-.65*s,y-.55*s),(x+.4*s,y-.48*s),(x+s,y+.12*s),(x+.36*s,y+.42*s),(x-.6*s,y+.35*s)],.024,'edge')
for x,y in [(-2.36,-3.1),(-2.36,2.5),(2.36,-4.4),(2.36,1.2)]:
    box(x,y,.03,.25,1.05,.025,'edge')
    for k in range(6):
        box(x,y-.42+k*.17,.046,.20,.035,.012,'steel_light')
for side in [-1,1]:
    for y in [-5.35,-3.15,-.95,1.25,3.45,5.65]:
        stripe_bar(side*2.05,y,.035,1.65,False)
for y in [4.2,1.4,-1.4,-4.2]:
    for i in range(12):
        a=2*math.pi*i/12
        box(-3.4+.88*math.cos(a),y+.88*math.sin(a),.205,.12,.16,.025,'yellow' if i%2==0 else 'black',a)

# Structural piers and service conduits build thickness into the perimeter.
for side in [-1,1]:
    box(side*4.91,0,.67,.35,18.3,1.34,'steel')
    box(side*4.70,0,.22,.18,18.2,.30,'edge')
    for y in [-6.5,-3.7,-.9,1.9,4.7,7.0]:
        box(side*4.72,y,.72,.58,.40,1.44,'edge')
        box(side*4.69,y,1.45,.66,.46,.10,'steel_light')
        box(side*4.39,y,.22,.28,.48,.44,'steel')
    for dx,z,mat in [(-.12,.62,'rust'),(.04,.91,'steel_light')]:
        xx=side*(4.55+dx)
        beam((xx,-6.5,z),(xx,6.8,z),.09,mat)
        for y in [-5,-2,1,4,6]:
            box(xx,y,z,.16,.13,.14,'edge')
# Narrow crane bridges leave the gameplay floor visible underneath.
for y in [-6.25,5.85]:
    for x in [-4.35,4.35]:
        box(x,y,.72,.25,.25,1.44,'steel')
        box(x,y,1.43,.38,.40,.13,'yellow')
    box(0,y,1.45,8.65,.13,.16,'edge')
    box(0,y,1.54,8.65,.07,.04,'steel_light')
    for x in [-3,-1.5,0,1.5,3]:
        box(x,y,1.38,.10,.20,.16,'steel')
box(2.05,-6.25,1.57,.68,.36,.18,'yellow')
beam((2.05,-6.25,1.48),(2.05,-6.25,.68),.055,'edge')
beam((2.05,-6.25,.68),(2.05,-6.08,.52),.085,'rust')
beam((2.05,-6.08,.52),(2.05,-5.94,.64),.085,'rust')

# Fuel hoses end at the perimeter of the pads, leaving their surfaces empty.
for y in [4.2,1.4,-1.4,-4.2]:
    hose=[(-4.61,y-.55,.20),(-4.46,y-.65,.16),(-4.30,y-.75,.14),(-4.08,y-.80,.14),(-3.91,y-.73,.14)]
    for a,b in zip(hose,hose[1:]):
        beam(a,b,.075,'black')
    box(-3.89,y-.72,.17,.20,.10,.10,'yellow',-.35)
for y in [2.8,-2.8]:
    box(-4.23,y,.29,.47,.34,.40,'rust')
    box(-4.23,y,.52,.54,.39,.08,'steel_light')
    for dx in [-.19,.19]:
        for dy in [-.14,.14]:
            cyl(-4.23+dx,y+dy,.08,.065,.14,'black',8)
    box(-4.23,y,.58,.27,.08,.05,'yellow',.3)
for y in [-5.65,0,5.55]:
    poly([(-4.78,y-.36),(-4.24,y-.12),(-4.09,y+.13),(-4.75,y+.27)],.76,'steel_light')
    box(-4.54,y,.80,.22,.08,.08,'rust')
box(-4.08,6.72,.52,1.05,.76,1.04,'edge')
box(-4.08,6.72,1.07,1.16,.86,.10,'steel_light')
box(-4.08,6.30,.69,.72,.035,.36,'glow_teal')
box(-4.08,6.27,.35,.78,.08,.12,'steel')

# The shop canopy and counter enclose stocked wall shelves and empty displays.
box(4.38,.60,1.44,1.00,8.1,.12,'olive')
for y in [-3.45,-1.45,.55,2.55,4.55]:
    box(4.30,y,1.51,.86,.32,.035,'yellow')
for y in [-3.4,4.6]:
    box(4.14,y,.78,.10,.10,1.4,'steel_light')
box(4.15,-4.55,.48,1.25,.58,.96,'steel')
box(4.15,-4.55,.99,1.38,.70,.11,'wood')
box(4.30,-4.55,1.07,.38,.30,.08,'rust',.2)
box(4.13,-5.38,.12,1.05,.65,.18,'edge')
for x in [3.66,4.60]:
    for y in [-5.66,-5.10]:
        box(x,y,.56,.07,.07,.95,'steel_light')
for z in [.35,.95]:
    box(4.13,-5.66,z,1.0,.07,.07,'steel_light')
    box(4.13,-5.10,z,1.0,.07,.07,'steel_light')
box(4.03,-5.38,.34,.40,.32,.35,'olive')
cyl(4.38,-5.36,.35,.16,.40,'rust',12)
for x in [3.12,3.80]:
    beam((x,5.24,1.45),(x,5.24,1.18),.06,'edge')
box(3.46,5.24,1.14,.96,.18,.25,'rust')
box(3.46,5.24,1.28,.70,.12,.035,'yellow')
for y in [-2.6,.6,3.8]:
    box(4.08,y,1.39,.16,.23,.10,'glow_amber')

# Parallel launch rails and heavy interlocking teeth on the open door leaves.
for x in [-.67,.67]:
    box(x,6.67,.035,.10,1.72,.05,'steel_light')
    for y in [6.10,6.65,7.20]:
        box(x,y,.068,.15,.16,.025,'glow_amber')
for side in [-1,1]:
    for y in [7.66,8.05,8.44,8.83]:
        box(side*2.36,y,.72,.25,.20,.34,'steel_light')
    cyl(side*2.72,7.20,1.13,.12,.12,'glow_amber',12)

# An unfinished ship skeleton rests on trestles in the workshop.
for x in [-.65,.65]:
    box(x,-7.20,.25,.16,.55,.50,'rust')
    beam((x,-7.48,.53),(x,-6.82,.53),.09,'steel_light')
beam((0,-8.06,.57),(0,-6.65,.57),.12,'steel_light')
for side in [-1,1]:
    beam((0,-6.70,.57),(side*.92,-7.45,.57),.10,'steel')
    beam((side*.92,-7.45,.57),(side*.36,-7.86,.57),.10,'steel')
    beam((side*.36,-7.86,.57),(0,-8.06,.57),.10,'steel')
    beam((side*.25,-7.18,.57),(side*.70,-7.45,.57),.07,'yellow')
# Join all visible meshes; transform every primitive first to preserve normals and scale.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.convert(target='MESH')
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# A faceted upper-left light baked directly into per-corner vertex colours. Glow
# materials retain their full palette value. No external bake, image, or UVs.
mesh = model.data
mesh.update()
colors = mesh.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='CORNER')
light_dir = Vector((-0.45, 0.55, 0.70)).normalized()
for face in mesh.polygons:
    mat = model.material_slots[face.material_index].material
    base = palette[mat.name]
    diffuse = 1.0 if mat.name.startswith('glow_') else 0.60 + 0.40 * max(0.0, face.normal.dot(light_dir))
    for loop_index in face.loop_indices:
        colors.data[loop_index].color = (base[0]*diffuse, base[1]*diffuse, base[2]*diffuse, 1.0)
# Keep OBJ export restricted to the sole asset object.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, export_colors=True, forward_axis='Y', up_axis='Z')

# Inspection renders: camera and lights are intentionally added after export.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGB'
scene.render.resolution_percentage = 100
scene.world.color = (.12, .12, .12)
scene.view_settings.view_transform = 'Standard'
def aim(obj, target):
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
    aim(obj, target)
    return obj
light('Upper left sun', 'SUN', (-8,10,14), 1.8, (0,0,0))
light('Soft fill', 'AREA', (3,-4,12), 700, (0,0,0), 12)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 17.8
cam.location = (0,0,35)
cam.rotation_euler = (0,0,0)
scene.render.resolution_x = 450
scene.render.resolution_y = 800
# Match the requested 10:17.8 world rectangle despite the 450:800 image raster.
scene.render.pixel_aspect_x = 800 / 801
scene.render.pixel_aspect_y = 1
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 42
cam.location = (14,-22,23)
aim(cam, (0,0,0))
scene.render.resolution_x = 800
scene.render.resolution_y = 800
scene.render.pixel_aspect_x = 1
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

