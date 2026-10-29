import bpy
import bmesh
import math
import os
import sys
import json
from mathutils import Vector

# Output is relative to this script even when Blender is launched elsewhere.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1])) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
           (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
           (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
           (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
           (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
           (0.80,0.84,0.83)]
materials = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.8
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 2.0
    materials.append(m)

body = []
parts = []
def mesh_obj(name, verts, faces, material, target):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(materials[material])
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    target.append(obj)
    return obj

def plate(name, outline, bottom, shoulder, top, inset, material, target=body):
    # Three concentric rings produce a broad flat crown and a substantial bevel.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    def ring(scale, z):
        return [(cx + (x-cx)*scale, cy + (y-cy)*scale, z) for x,y in outline]
    verts = ring(0.94, bottom) + ring(1.0, shoulder) + ring(inset, top)
    faces = [tuple(reversed(range(n)))]
    for k in range(2):
        for i in range(n):
            j = (i+1) % n
            faces.append((k*n+i, k*n+j, (k+1)*n+j, (k+1)*n+i))
    faces.append(tuple(range(2*n, 3*n)))
    return mesh_obj(name, verts, faces, material, target)

def cylinder(name, loc, radius, depth, mat, target=body, vertices=10, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rotation)
    o = bpy.context.object
    o.name = name
    o.data.materials.append(materials[mat])
    target.append(o)
    return o

def join_objects(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    ob.name = name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for poly in ob.data.polygons:
        poly.use_smooth = False
    return ob

# A low faceted shell with a dark mouth channel and a pale brow.
plate('Undershell', [(-.46,.51),(.46,.51),(.60,-.18),(.29,-.78),(-.29,-.78),(-.60,-.18)], -.17, .02, .22, .78, 11)
plate('Brass carapace', [(-.46,.50),(.46,.50),(.59,-.14),(.39,-.48),(.23,-.57),(-.23,-.57),(-.39,-.48),(-.59,-.14)], .05, .20, .33, .76, 13)
plate('Rear collar', [(-.43,.49),(.43,.49),(.46,.30),(.28,.22),(-.28,.22),(-.46,.30)], .24, .30, .36, .85, 12)
plate('Ceramic brow', [(-.32,.39),(.32,.39),(.25,.08),(.15,.02),(-.15,.02),(-.25,.08)], .31, .36, .42, .78, 1)
plate('Mouth recess', [(-.19,.02),(.19,.02),(.19,-.51),(.10,-.72),(-.10,-.72),(-.19,-.51)], .23, .30, .32, .90, 11)
plate('Mouth keel', [(-.09,-.13),(.09,-.13),(.11,-.53),(0,-.64),(-.11,-.53)], .31, .33, .36, .80, 6)

# Mirrored angular mandibles, each built as its own hingeable assembly.
for side, label in [(-1, 'L'), (1, 'R')]:
    jaw = []
    def mirrored(points):
        return [(side*x,y) for x,y in points]
    cylinder('Hinge', (side*.38,-.12,.25), .115, .10, 11, jaw, 10)
    cylinder('Hinge cap', (side*.38,-.12,.31), .070, .025, 13, jaw, 10)
    plate('Mandible', mirrored([(.19,-.12),(.53,-.27),(.45,-.96),(.28,-.83),(.12,-.64)]), .04, .21, .31, .74, 6, jaw)
    plate('Jaw ceramic edge', mirrored([(.20,-.26),(.31,-.29),(.34,-.71),(.43,-.90),(.29,-.80),(.15,-.62)]), .27, .31, .36, .78, 1, jaw)
    # Short triangular teeth point into the central mouth gap.
    for y, x in [(-.42,.18),(-.60,.17)]:
        plate('Tooth', mirrored([(x+.05,y+.075),(x+.05,y-.075),(x-.045,y-.045)]), .25, .30, .34, .82, 15, jaw)
    join_objects(jaw, 'Jaw_' + label)
    parts.append({'object':'Jaw_' + label, 'motion':'swing', 'axis':[0,0,side], 'pivot':[side*.38,-.12,.25], 'speed':1.4, 'amplitude':12})
    # Recessed amber eye under a dark protective brow, clearly visible from above.
    plate('Eye socket', mirrored([(.22,.13),(.39,.10),(.41,-.08),(.26,-.10)]), .30, .34, .37, .86, 11)
    plate('Eye', mirrored([(.265,.095),(.345,.08),(.36,-.055),(.28,-.06)]), .365, .375, .385, .90, 3)
    plate('Cheek', mirrored([(.40,.20),(.51,.12),(.54,-.11),(.46,-.20),(.39,-.09)]), .17, .25, .31, .80, 12)

model = join_objects(body, 'Model')
with open(os.path.join(OUT, 'parts.json'), 'w', encoding='utf-8') as f:
    json.dump(parts, f, indent=2)

# Export only the body and articulated jaws; scene lighting is not exported.
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
for entry in parts:
    bpy.data.objects[entry['object']].select_set(True)
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
scene.world = bpy.data.worlds.new('Dark blue-gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

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

light('Sun', 'SUN', (-3,-4,6), 2.2, (0,0,0))
light('Soft fill', 'AREA', (3,1,4), 150, (0,0,0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,-.225,5)
cam.rotation_euler = (0,0,0)
aim(cam, (0,-.225,0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.72
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 48
cam.location = (2.4,-3.3,3.0)
aim(cam, (0,-.22,.12))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
