import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Blender's -- argument supplies the output directory when __file__ is unavailable.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

def material(index, color):
    m = bpy.data.materials.new('tile_%02d' % index)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.82
    return m

purple = material(0, (0.38, 0.32, 0.57))
purple_light = material(1, (0.48, 0.41, 0.68))
trim = material(2, (0.78, 0.82, 0.79))
trim_shadow = material(3, (0.58, 0.66, 0.67))
gold = material(4, (1.0, 0.76, 0.23))
gold_edge = material(5, (0.84, 0.52, 0.12))

parts = []
def mesh_object(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    parts.append(ob)
    return ob

def rings(name, sections, mat, cap=True):
    # Sections are matching loops. Quads give deliberately faceted rounded forms.
    n = len(sections[0])
    verts = [v for section in sections for v in section]
    faces = []
    if cap:
        faces.append(tuple(reversed(range(n))))
    for j in range(len(sections) - 1):
        for i in range(n):
            k = (i + 1) % n
            faces.append((j*n+i, j*n+k, (j+1)*n+k, (j+1)*n+i))
    if cap:
        faces.append(tuple((len(sections)-1)*n+i for i in range(n)))
    return mesh_object(name, verts, faces, mat)

def torso_loop(y, width, depth):
    # Chamfered rectangular section, broad front and softly clipped corners.
    return [(x*width, y, z*depth) for x,z in [(-.78,-1),(.78,-1),(1,-.65),(1,.65),(.78,1),(-.78,1),(-1,.65),(-1,-.65)]]

rings('Knitted body', [torso_loop(-.33,.205,.115), torso_loop(-.27,.225,.137),
                       torso_loop(.16,.225,.145), torso_loop(.29,.18,.115)], purple)
rings('Bottom welt', [torso_loop(-.37,.205,.116), torso_loop(-.35,.225,.137),
                      torso_loop(-.285,.225,.137), torso_loop(-.27,.21,.122)], trim)
# Cuff and sleeve cross sections lie perpendicular to the downward-outward arm axis.
def arm_loop(side, x, y, radius, depth):
    # tangent is outward and downward; perpendicular points upward and outward
    perp = Vector((side*.42, .907, 0)).normalized()
    result = []
    for i in range(8):
        a = 2*math.pi*i/8
        result.append((side*x + perp.x*radius*math.cos(a),
                       y + perp.y*radius*math.cos(a), depth*math.sin(a)))
    return result

for side in (-1, 1):
    rings('Sleeve', [arm_loop(side,.17,.205,.135,.115),
                     arm_loop(side,.275,.16,.13,.125),
                     arm_loop(side,.355,-.015,.105,.105),
                     arm_loop(side,.385,-.075,.09,.09)], purple_light)
    rings('Cuff', [arm_loop(side,.365,-.03,.108,.108),
                   arm_loop(side,.40,-.105,.102,.105),
                   arm_loop(side,.408,-.12,.086,.09)], trim)

# A raised ribbed crew neck, with a dark inset that reads as an opening.
def oval_loop(y, rx, rz, n=12):
    return [(rx*math.cos(2*math.pi*i/n), y, rz*math.sin(2*math.pi*i/n)) for i in range(n)]
rings('Collar', [oval_loop(.285,.122,.092), oval_loop(.325,.119,.090),
                 oval_loop(.338,.105,.080), oval_loop(.338,.075,.055),
                 oval_loop(.304,.075,.055)], trim, False)
rings('Collar opening', [oval_loop(.304,.075,.055), oval_loop(.305,.075,.055)], trim_shadow)

# Raised, chunky six-point lightning applique on the front and back.
bolt = [(.035,.19),(-.115,-.015),(-.018,-.015),(-.060,-.205),(.135,.045),(.035,.045)]
def applique(name, points, z0, z1):
    n = len(points)
    verts = [(x,y,z0) for x,y in points] + [(x,y,z1) for x,y in points]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    ob = mesh_object(name, verts, faces, gold)
    ob.data.materials.append(gold_edge)
    for p in ob.data.polygons:
        p.material_index = 0 if p.index < 2 else 1
    return ob
applique('Front lightning', bolt, .143, .158)
applique('Back lightning', [(-x,y) for x,y in bolt], -.143, -.158)

# Join and triangulate before export, keeping the flat facets and all material slots.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bm = bmesh.new()
bm.from_mesh(model.data)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(model.data)
bm.free()
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    point_at(ob, (0,0,0))
    return ob

light('Sun upper left', 'SUN', (-3,4,5), 2.0)
light('Soft fill', 'AREA', (4,1,3), 90, 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,3)
point_at(cam, (0,0,0))
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.18
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (.78, .50, 1.50)
point_at(cam, (0,0,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

