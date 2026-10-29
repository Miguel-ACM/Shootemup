import bpy, math, os, sys, json
from mathutils import Vector

# Run with: blender --background --python model.py [-- /path/to/output]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script directory or script path after --')
    p = os.path.abspath(args[args.index('--') + 1])
    OUT = p if os.path.isdir(p) else os.path.dirname(p)
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.105,0.15,0.20),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.48,0.36,0.18),(0.53,0.61,0.55),
          (0.66,0.70,0.71)]
# The Space atlas supplies the solar-cell texture for tile_07 in game; the
# preview uses a flat solar blue, with structural lines made from palette tiles.
colors[7] = (0.18, 0.27, 0.43)
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.65
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

body = []
def mesh(name, verts, faces, material):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mats[material])
    body.append(ob)
    return ob

def box(name, pos, size, material, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = ob.modifiers.new('Edge bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    ob.data.materials.append(mats[material])
    body.append(ob)
    return ob

def layered_hull(name, outline, z_bottom, z_shoulder, z_top, inset, material):
    n = len(outline)
    verts = [(x,y,z_bottom) for x,y in outline]
    verts += [(x,y,z_shoulder) for x,y in outline]
    verts += [(x*inset,y*inset,z_top) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    faces += [(i+n,(i+1)%n+n,(i+1)%n+2*n,i+2*n) for i in range(n)]
    faces += [tuple(range(2*n,3*n))]
    return mesh(name, verts, faces, material)

def cylinder(name, pos, radius, depth, material, axis='Z', vertices=12):
    rot = (math.pi/2,0,0) if axis == 'Y' else (0,0,0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=pos, rotation=rot)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    body.append(ob)
    return ob

def muzzle(x):
    # The front rim is exactly at the gameplay muzzle point (x, -.68, .15).
    cylinder('Gun housing', (x,-.585,.15), .082, .19, 6, 'Y', 10)
    # Annular face and short bore, with the front plane at y=-.68.
    n=10; verts=[]
    for y,r in [(-.68,.070),(-.68,.040),(-.645,.040),(-.645,.070)]:
        verts.extend([(x+math.cos(2*math.pi*i/n)*r,y,.15+math.sin(2*math.pi*i/n)*r) for i in range(n)])
    faces=[]
    for j in range(4):
        for i in range(n):
            faces.append((j*n+i,j*n+(i+1)%n,((j+1)%4)*n+(i+1)%n,((j+1)%4)*n+i))
    mesh('Muzzle rim',verts,faces,15)
    cylinder('Dark bore', (x,-.647,.15), .039, .003, 11, 'Y', 10)

# Faceted, raised central spacecraft bus, with a pointed front and rear.
outline = [(0,-.88),(.26,-.48),(.32,.31),(0,.72),(-.32,.31),(-.26,-.48)]
layered_hull('Armored bus', outline, -.15, .08, .25, .76, 15)
layered_hull('Dorsal navy spine', [(0,-.43),(.14,-.25),(.15,.25),(.10,.39),(-.10,.39),(-.15,.25),(-.14,-.25)], .245,.27,.32,.82,6)
layered_hull('Sensor inset', [(0,-.32),(.08,-.22),(.08,.08),(-.08,.08),(-.08,-.22)], .318,.325,.34,.86,11)
box('Sensor slit', (0,-.12,.345), (.075,.15,.012), 9, .012)
for s in (-1,1):
    box('Gold shoulder', (s*.20,.26,.249), (.085,.30,.045), 13, .012)
    # Broad hinge and rail link the array to the central bus.
    box('Array arm', (s*.55,.12,.035), (.62,.11,.12), 13, .018)
    box('Hinge block', (s*.38,.12,.075), (.15,.19,.14), 6, .022)
    for cell in range(3):
        x=s*(.82+cell*.14); y=.48-cell*.43
        # Original stepped panel dimensions, with an ivory rim and inset blue cells.
        box('Solar frame', (x,y,-.04), (.56,.44,.10), 15, .018)
        box('Solar glass', (x,y,.016), (.48,.36,.018), 7, .006)
        # Sparse cross rails echo the atlas cell grid, remaining legible at game size.
        box('Cell divider', (x,y,.028), (.012,.35,.009), 13)
        box('Cell divider', (x,y,.029), (.47,.012,.009), 13)
    muzzle(s*.19)
# A compact rear antenna, within the old .81 + .20 footprint.
box('Antenna base', (0,.62,.17), (.11,.15,.09), 6, .012)
box('Antenna mast', (0,.82,.105), (.035,.35,.045), 13, .006)
box('Antenna tip', (0,.99,.105), (.07,.035,.05), 15, .006)

# Join static geometry, leaving no natural animated parts on this rigid scout.
bpy.ops.object.select_all(action='DESELECT')
for ob in body:
    ob.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
with open(os.path.join(OUT,'parts.json'), 'w', encoding='utf-8') as f:
    json.dump([], f, indent=2)

# Export only the asset; preserve the world axes and material names.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (0.05,0.07,0.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, pos, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA': data.shape = 'DISK'; data.size = size
    if kind == 'SUN': data.angle = math.radians(18)
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = pos
    aim(ob, target)
light('Upper left sun', 'SUN', (-3,-4,7), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 180, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,7)
cam.rotation_euler = (0,0,0)  # local -Z points down, local Y is screen up
camdata.type = 'ORTHO'
camdata.ortho_scale = 3.18
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (3.4,-4.8,4.2)
aim(cam,(0,0,.04))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
