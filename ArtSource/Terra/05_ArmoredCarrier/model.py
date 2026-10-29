import bpy
import math
import os
import sys
from mathutils import Vector

# Blender 5.x: run with blender --background --python model.py [-- output-directory]
def output_dir():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except NameError:
        args = sys.argv
        if '--' in args and len(args) > args.index('--') + 1:
            path = os.path.abspath(args[args.index('--') + 1])
            return path if os.path.isdir(path) else os.path.dirname(path)
        return os.getcwd()

OUT = output_dir()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [(0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
           (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
           (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
           (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
           (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
           (0.80,0.84,0.83)]
materials = []
for i, rgb in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.75
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 2.0
    materials.append(m)

parts = []
def mesh_part(name, vertices, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(materials[mat])
    parts.append(obj)
    return obj

def loft(name, outline, inset, z0, z1, z2, mat):
    # Three corresponding rings produce a broad bevel and real sloping armor.
    area = sum(outline[i][0]*outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0]*outline[i][1] for i in range(len(outline)))
    if area < 0:
        outline = list(reversed(outline))
        inset = list(reversed(inset))
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline] + [(x,y,z2) for x,y in inset]
    faces = [tuple(reversed(range(n)))]
    for level in range(2):
        for i in range(n):
            j = (i+1) % n
            faces.append((level*n+i, level*n+j, (level+1)*n+j, (level+1)*n+i))
    faces.append(tuple(range(2*n,3*n)))
    return mesh_part(name, verts, faces, mat)

def plate(name, outline, z, thickness, mat):
    return loft(name, outline, outline, z, z+thickness, z+thickness, mat)

def ring_y(name, x, y, z, outer, inner, depth, mat, cavity_mat=11, segments=10, direction=-1):
    # Open metal annulus with a dark recessed inner bore, oriented along Y.
    v = []
    for yy, rad in ((y,outer), (y,inner), (y-direction*depth,outer), (y-direction*depth,inner)):
        for i in range(segments):
            a = 2*math.pi*i/segments
            v.append((x+rad*math.cos(a), yy, z+rad*math.sin(a)))
    f = []
    for i in range(segments):
        j = (i+1)%segments
        f.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                  (segments+i,segments+j,3*segments+j,3*segments+i)])
    obj = mesh_part(name, v, f, mat)
    # Recessed dark back, not an emissive disc.
    yy = y-direction*depth*0.88
    vv = [(x+inner*math.cos(2*math.pi*i/segments), yy, z+inner*math.sin(2*math.pi*i/segments)) for i in range(segments)]
    mesh_part(name+' bore', vv, [tuple(range(segments))], cavity_mat)
    return obj

# Central armored keel and the raised, faceted carrier deck.
loft('center keel', [(-.33,1.02),(-.41,-.82),(0,-1.2),(.41,-.82),(.33,1.02)],
     [(-.25,.91),(-.31,-.76),(0,-1.08),(.31,-.76),(.25,.91)], -.25,.12,.28, 0)
loft('raised bridge', [(-.29,.68),(-.30,-.09),(-.22,-.31),(.22,-.31),(.30,-.09),(.29,.68)],
     [(-.22,.58),(-.23,-.08),(-.16,-.22),(.16,-.22),(.23,-.08),(.22,.58)], .27,.31,.43, 4)
loft('bridge inset', [(-.18,.49),(-.18,.08),(-.10,-.02),(.10,-.02),(.18,.08),(.18,.49)],
     [(-.14,.43),(-.14,.10),(-.08,.04),(.08,.04),(.14,.10),(.14,.43)], .43,.44,.47, 11)
plate('bridge steel spine', [(-.055,.39),(-.055,.10),(.055,.10),(.055,.39)], .471,.008,6)
loft('nose armor', [(-.31,-.39),(-.31,-.77),(0,-1.1),(.31,-.77),(.31,-.39)],
     [(-.22,-.45),(-.22,-.75),(0,-.98),(.22,-.75),(.22,-.45)], .28,.30,.35, 8)
plate('nose identification', [(-.17,-.61),(-.14,-.70),(.14,-.70),(.17,-.61)], .351,.012,3)

for s in (-1, 1):
    def mirror(points):
        return [(s*x,y) for x,y in points]
    loft('armored side pod', mirror([(.49,1.00),(.98,.92),(1.06,-.64),(.83,-1.08),(.50,-.86)]),
         mirror([(.56,.88),(.90,.83),(.97,-.59),(.81,-.96),(.57,-.80)]), -.20,.10,.25, 0)
    # Broad recessed loading bay with two offset, layered armored doors.
    loft('loading bay', mirror([(.58,.63),(.92,.58),(.94,-.55),(.84,-.76),(.58,-.71)]),
         mirror([(.62,.57),(.87,.54),(.89,-.50),(.82,-.67),(.62,-.65)]), .25,.26,.285, 11)
    loft('rear loading door', mirror([(.62,.53),(.87,.49),(.88,.07),(.62,.07)]),
         mirror([(.65,.49),(.84,.46),(.85,.11),(.65,.11)]), .287,.30,.34, 2)
    loft('forward loading door', mirror([(.62,.02),(.88,.02),(.89,-.44),(.81,-.60),(.62,-.58)]),
         mirror([(.65,-.02),(.85,-.02),(.86,-.42),(.80,-.55),(.65,-.54)]), .287,.30,.34, 4)
    plate('orange carrier marking', mirror([(.66,.39),(.83,.37),(.83,.28),(.66,.30)]), .342,.012, 3)
    # Angular shoulder cap gives the outer pod a distinct readable edge.
    loft('shoulder rim', mirror([(.90,.68),(.99,.62),(1.02,-.43),(.94,-.53)]),
         mirror([(.92,.63),(.96,.59),(.98,-.41),(.94,-.46)]), .25,.27,.31, 6)
    # Gun housing terminates exactly at the gameplay muzzle plane y=-.80.
    loft('gun mount', mirror([(.61,-.55),(.87,-.55),(.90,-.75),(.84,-.88),(.64,-.88),(.58,-.75)]),
         mirror([(.64,-.59),(.84,-.59),(.86,-.74),(.82,-.83),(.66,-.83),(.62,-.74)]), .27,.31,.38, 2)
    ring_y('weapon muzzle', s*.74, -.80, .39, .105,.061,.15, 6)
    # Engine outlets at the exact supplied sockets, opening toward the rear.
    ring_y('engine socket', s*.87, 1.18, 0.0, .13,.075,.22, 6, direction=1)
    loft('engine fairing', mirror([(.72,.68),(.99,.68),(.99,1.02),(.94,1.10),(.78,1.10),(.72,1.02)]),
         mirror([(.76,.73),(.95,.73),(.95,1.00),(.92,1.06),(.80,1.06),(.76,1.00)]), -.12,.12,.23, 8)
    plate('engine fairing stripe', mirror([(.79,.78),(.93,.78),(.93,.86),(.79,.86)]), .231,.01, 3)

# Join before adding presentation objects; apply transforms and triangulate for a predictable budget.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2500, 'Triangle budget exceeded: %d' % len(model.data.polygons)
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
scene.world = bpy.data.worlds.new('Blue-gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    aim(obj, target)
    return obj

light('Sun upper left', 'SUN', (-4,-5,8), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,8)
cam.rotation_euler = (0,0,0)  # camera local -Z down, local +Y screen up
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.80
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam.location = (3.8,-5.5,5.0)
aim(cam, (0,0,.10))
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
