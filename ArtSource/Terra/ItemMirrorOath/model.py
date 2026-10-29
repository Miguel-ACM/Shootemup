import bpy
import math
import os
import sys
from mathutils import Vector

# Mirror Oath: a substantial oval hand mirror with a cool silver-blue face.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[0]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [
    ('tile_00', (0.82, 0.50, 0.12, 1)), # warm gold body
    ('tile_01', (1.00, 0.73, 0.25, 1)), # raised gold bevel
    ('tile_02', (0.66, 0.35, 0.10, 1)), # recessed gold
    ('tile_03', (0.49, 0.74, 0.86, 1)), # silver blue glass
    ('tile_04', (0.70, 0.88, 0.94, 1)), # pale reflection
    ('tile_05', (0.38, 0.62, 0.76, 1)), # glass facet
    ('tile_06', (0.91, 0.96, 0.96, 1)), # glint
    ('tile_07', (0.64, 0.39, 0.16, 1)), # grip
    ('tile_08', (0.88, 0.65, 0.29, 1)), # back medallion
    ('tile_09', (0.35, 0.69, 0.78, 1)), # turquoise jewel
]
mats = []
for name, color in palette:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.65
    mats.append(mat)

parts = []
def mesh_object(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for m in mats:
        mesh.materials.append(m)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(ob)
    return ob

# The rim is a closed, multi-step oval profile with substantial edge depth.
N = 20
CY = 0.115
# Each tuple is (horizontal radius, vertical radius, depth); front is +Z.
profile = [(.247,.282,.078), (.275,.311,.078), (.307,.339,.049),
           (.315,.347,.018), (.315,.347,-.025), (.301,.333,-.060),
           (.272,.303,-.079), (.245,.278,-.079)]
verts = []
for rx, ry, z in profile:
    for i in range(N):
        a = 2 * math.pi * i / N
        verts.append((rx * math.sin(a), CY + ry * math.cos(a), z))
faces, ids = [], []
for j in range(len(profile)-1):
    for i in range(N):
        k = (i+1) % N
        faces.append((j*N+i, j*N+k, (j+1)*N+k, (j+1)*N+i))
        ids.append([1, 0, 1, 0, 2, 0, 1][j])
mesh_object('Sculpted oval frame', verts, faces, ids)

# Recessed, faceted mirror surface. Large color facets survive at HUD size.
verts = [(0, CY, .071)]
for i in range(N):
    a = 2 * math.pi * i / N
    verts.append((.248 * math.sin(a), CY + .283 * math.cos(a), .071))
faces, ids = [], []
for i in range(N):
    faces.append((0, 1+i, 1+(i+1)%N))
    ids.append(4 if i in (16,17,18,19,0,1) else (5 if i in (6,7,8,9,10) else 3))
mesh_object('Mirror glass', verts, faces, ids)

# A broad diagonal reflection stripe is integrated as a geometric inlay.
mesh_object('Reflection', [(-.177,.155,.074),(-.127,.244,.074),(.036,.384,.074),(.089,.366,.074),(-.074,.226,.074),(-.126,.137,.074)],
            [(0,1,2,3,4,5)], [6])

# Solid ornamental back, visible while the item rotates.
verts = [(0,CY,-.081)]
for i in range(N):
    a = 2*math.pi*i/N
    verts.append((.246*math.sin(a), CY+.279*math.cos(a), -.081))
mesh_object('Back plate', verts, [(0,1+(i+1)%N,1+i) for i in range(N)], [8]*N)

def bevel_box(name, location, scale, mat, bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    mod = ob.modifiers.new('Broad edge bevel', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    mod.affect = 'EDGES'
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

def ico(name, loc, scale, mat, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mats[mat])
    parts.append(ob)
    return ob

# Tapered octagonal grip, with collars and a jewel at the pommel.
def grip():
    verts=[]
    for y, rx, rz in [(-.18,.057,.052),(-.25,.069,.058),(-.40,.052,.050),(-.435,.062,.055)]:
        for i in range(8):
            a=2*math.pi*i/8 + math.pi/8
            verts.append((rx*math.cos(a),y,rz*math.sin(a)))
    faces=[tuple(reversed(range(8))),tuple(24+i for i in range(8))]
    ids=[0,0]
    for j in range(3):
        for i in range(8):
            faces.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
            ids.append(7 if j==1 else 0)
    mesh_object('Tapered grip',verts,faces,ids)
grip()
bevel_box('Upper ferrule',(0,-.218,0),(.151,.067,.14),1,.018)
bevel_box('Lower ferrule',(0,-.397,0),(.132,.044,.12),1,.012)
ico('Pommel',(0,-.431,0),(.073,.053,.070),0,2)
ico('Pommel jewel front',(0,-.431,.061),(.034,.034,.018),9,1)
ico('Pommel jewel back',(0,-.431,-.061),(.034,.034,.018),9,1)
# Broad crown and side bosses lend the frame a distinctive outline.
ico('Crown',(0,.444,0),(.070,.038,.075),1,1)
for x in (-.299,.299):
    ico('Side boss',(x,.115,0),(.034,.054,.075),1,1)
# Back engraving: a simple raised diamond.
mesh_object('Back diamond', [(0,.275,-.087),(.095,.115,-.087),(0,-.045,-.087),(-.095,.115,-.087),
                             (0,.255,-.105),(.076,.115,-.105),(0,-.025,-.105),(-.076,.115,-.105)],
            [(4,5,6,7),(0,4,7,3),(0,1,5,4),(1,2,6,5),(2,3,7,6)], [1,0,0,0,0])

# Consolidate the model and bake all transforms before export.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Render setup; lighting and cameras are created only after exporting the asset.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    ob.rotation_euler = (Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    aim(ob, target)
    return ob
light('Key sun','SUN',(-3,4,5),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,4),180,(0,0,0),5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,3)
aim(cam,(0,0,0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (.85,.52,1.65)
aim(cam,(0,0,0))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

