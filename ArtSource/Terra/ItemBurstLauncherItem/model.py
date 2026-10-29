import bpy, math, os, sys
from mathutils import Vector

# Burst launcher: a flared mortar cup over a transverse ammunition drum.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = [
    (0.95, 0.32, 0.075, 1),  # orange enamel
    (1.00, 0.52, 0.14, 1),   # warm orange highlights
    (0.67, 0.20, 0.07, 1),   # burnt orange
    (0.94, 0.68, 0.29, 1),   # brass
    (0.48, 0.54, 0.57, 1),   # blue steel
    (0.27, 0.34, 0.38, 1),   # recessed steel
    (0.15, 0.19, 0.21, 1),   # muzzle bore
    (1.00, 0.78, 0.39, 1),   # pale brass
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value = 0.7
    mats.append(mat)

parts = []
def finish(obj, mat):
    obj.data.materials.append(mats[mat])
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj

def box(name, loc, size, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Broad chamfers', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat)

def cylinder(name, loc, radius, depth, mat, axis='Z', vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.name = name
    if axis == 'Y':
        obj.rotation_euler[0] = math.pi / 2
    return finish(obj, mat)

def cup():
    # Polygonal barrel with a real inset bore and a thick muzzle lip.
    n = 12
    rings = [(-0.04, 0.155), (0.30, 0.265), (0.34, 0.295),
             (0.43, 0.295), (0.43, 0.205), (0.32, 0.205), (0.20, 0.17)]
    verts = []
    for y, r in rings:
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            verts.append((r * math.cos(a), y, r * math.sin(a)))
    faces, indices = [], []
    for j in range(len(rings)-1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
            indices.append([0, 2, 4, 3, 5, 6][j])
    faces.append(tuple(reversed(tuple(range((len(rings)-1)*n, len(rings)*n)))))
    indices.append(6)
    faces.append(tuple(reversed(tuple(range(n)))))
    indices.append(2)
    mesh = bpy.data.meshes.new('Mortar cup mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new('Flared mortar and recessed bore', mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        obj.data.materials.append(m)
    for p, idx in zip(mesh.polygons, indices):
        p.material_index = idx
        p.use_smooth = False
    parts.append(obj)

# Body and axial drum: round faces are visible front and back.
box('Drum cradle', (0, -0.22, 0), (0.51, 0.34, 0.32), 2, 0.045)
cylinder('Orange ammunition drum', (0, -0.20, 0), 0.245, 0.32, 0, vertices=12)
for s in (-1, 1):
    cylinder('Steel drum rim', (0, -0.20, s*0.17), 0.251, 0.055, 4, vertices=12)
    cylinder('Inset orange drum face', (0, -0.20, s*0.204), 0.199, 0.025, 1, vertices=12)
    cylinder('Brass hub', (0, -0.20, s*0.224), 0.087, 0.027, 3, vertices=10)
    cylinder('Hub center', (0, -0.20, s*0.241), 0.038, 0.012, 5, vertices=8)
    # Three broad cartridge marks on both faces, legible at pickup scale.
    for i in range(3):
        a = 2*math.pi*i/3 + math.pi/2
        cylinder('Cartridge mark', (0.145*math.cos(a), -0.20+0.145*math.sin(a), s*0.222), 0.029, 0.012, 7, vertices=8)
cup()
# Side pivot blocks tie the barrel and drum together, adding a readable profile.
for s in (-1, 1):
    box('Side trunnion', (s*0.245, -0.055, 0), (0.115, 0.15, 0.23), 4, 0.025)
    cylinder('Pivot pin', (s*0.245, -0.055, 0.126), 0.034, 0.018, 7, vertices=8)
    cylinder('Rear pivot pin', (s*0.245, -0.055, -0.126), 0.034, 0.018, 7, vertices=8)

# Consolidate geometry, bake all transforms, and triangulate for predictable export.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)

try:
    script_dir = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    script_dir = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(script_dir) or script_dir.lower().endswith('.py'):
        script_dir = os.path.dirname(script_dir)

bpy.ops.wm.obj_export(filepath=os.path.join(script_dir, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0, 0, 0)):
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
    aim(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4, 1, 5), 180, 5.0)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 3)
aim(cam)
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.06
scene.render.filepath = os.path.join(script_dir, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.35, 1.0, 2.7)
aim(cam)
camdata.type = 'PERSP'
camdata.lens = 50
scene.render.filepath = os.path.join(script_dir, 'angle.png')
bpy.ops.render.render(write_still=True)
