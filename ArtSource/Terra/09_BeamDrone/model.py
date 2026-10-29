import bpy
import math
import os
import sys
from mathutils import Vector

# BeamDrone: an open crescent cradling a downward-facing beam aperture.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT) or OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials:
    bpy.data.materials.remove(data)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, rgb in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*rgb, 1)
    p.inputs['Roughness'].default_value = 0.67
    p.inputs['Metallic'].default_value = 0.12 if i not in (9, 10) else 0.0
    if i in (9, 10):
        p.inputs['Emission Color'].default_value = (*rgb, 1)
        p.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

parts = []
def mesh_part(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def plate(name, points, bottom, shoulder, top, inset, material):
    # Polygonal armour with an inset top and sloped shoulders.
    n = len(points)
    cx = sum(p[0] for p in points) / n
    cy = sum(p[1] for p in points) / n
    verts = [(x, y, bottom) for x, y in points]
    verts += [(x, y, shoulder) for x, y in points]
    verts += [(cx + (x-cx)*inset, cy + (y-cy)*inset, top) for x, y in points]
    faces = [tuple(reversed(range(n))), tuple(range(2*n, 3*n))]
    for i in range(n):
        j = (i+1) % n
        faces.extend([(i, j, n+j, n+i), (n+i, n+j, 2*n+j, 2*n+i)])
    return mesh_part(name, verts, faces, material)

def ring(name, cx, cy, z_bottom, z_top, outer, inner, material, n=12):
    # Actual annular lip, with inner wall and an open center.
    verts = []
    for r, z in ((outer, z_bottom), (outer, z_top), (inner, z_top), (inner, z_bottom)):
        verts.extend([(cx+r*math.cos(2*math.pi*i/n), cy+r*math.sin(2*math.pi*i/n), z) for i in range(n)])
    faces = []
    for i in range(n):
        j = (i+1) % n
        faces.extend([(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i),
                      (2*n+i,2*n+j,3*n+j,3*n+i), (3*n+i,3*n+j,j,i)])
    return mesh_part(name, verts, faces, material)

def cylinder(name, radius, depth, loc, material, vertices=12, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc, rotation=rotation or (0, 0, 0))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    parts.append(obj)
    return obj

def rear_nozzle(x):
    # Ring in XZ plane, centered precisely on the rear socket at y=.83, z=0.
    n = 10
    verts = []
    for r, y in ((.103, .78), (.103, .83), (.067, .83), (.067, .775)):
        verts.extend([(x+r*math.cos(2*math.pi*i/n), y, r*math.sin(2*math.pi*i/n)) for i in range(n)])
    faces = []
    for i in range(n):
        j = (i+1) % n
        faces.extend([(i,n+i,n+j,j), (n+i,2*n+i,2*n+j,n+j), (2*n+i,3*n+i,3*n+j,2*n+j)])
    mesh_part('Nozzle rim', verts, faces, 6)
    cylinder('Nozzle dark recess', .066, .012, (x,.778,0), 11, 10, (math.pi/2,0,0))

# Broad rear body and two independently sculpted hooked arms.
plate('Rear bridge', [(-.28,.72),(.28,.72),(.48,.22),(.27,-.16),(-.27,-.16),(-.48,.22)], -.12,.11,.25,.78,7)
plate('Bridge crown', [(-.23,.57),(.23,.57),(.34,.23),(.19,-.06),(-.19,-.06),(-.34,.23)], .245,.26,.31,.78,4)
for s in (-1, 1):
    def mirror(points):
        out = [(s*x,y) for x,y in points]
        return out if s == 1 else list(reversed(out))
    plate('Crescent arm', mirror([(.33,.38),(.94,.02),(.82,-.59),(.44,-.93),(.52,-.43),(.56,-.09)]), -.10,.10,.22,.80,7)
    plate('Raised steel shoulder', mirror([(.54,.18),(.85,-.03),(.73,-.48),(.53,-.68),(.57,-.37)]), .205,.22,.275,.81,6)
    plate('Arm inset', mirror([(.63,.05),(.76,-.02),(.67,-.38),(.59,-.46)]), .274,.28,.29,.86,11)
    plate('Capacitor stripe', mirror([(.657,-.10),(.706,-.12),(.665,-.30),(.62,-.33)]), .29,.295,.30,.9,9)
    plate('Motor pod', mirror([(.20,.49),(.40,.49),(.425,.72),(.39,.82),(.21,.82),(.175,.72)]), -.11,.10,.20,.78,7)
    plate('Motor cap', mirror([(.225,.55),(.375,.55),(.39,.72),(.36,.77),(.24,.77),(.21,.72)]), .195,.21,.24,.8,4)
    rear_nozzle(s*.30)

# The central downward laser mouth is suspended in the open arms. Its lip top
# is exactly at the gameplay muzzle height; dark throat and cyan lens sit below.
ring('Emitter outer collar', 0,-.28,.075,.19,.36,.245,4,12)
ring('Emitter lip', 0,-.28,.17,.24,.245,.164,6,12)
cylinder('Emitter throat', .164,.018,(0,-.28,.175),11,12)
cylinder('Emitter lens', .108,.012,(0,-.28,.188),9,12)
# Small forward-facing prongs emphasize the suspended aperture without closing it.
for s in (-1,1):
    plate('Lens brace', [(s*.24,-.10),(s*.42,-.13),(s*.43,-.23),(s*.25,-.23)] if s == 1 else [(-.25,-.23),(-.43,-.23),(-.42,-.13),(-.24,-.10)], .05,.12,.17,.8,4)
plate('Dorsal spine', [(-.07,.48),(.07,.48),(.09,.08),(0,-.03),(-.09,.08)], .305,.315,.34,.82,11)

# Collapse every component to one flat-shaded mesh and bake all transforms.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for face in model.data.polygons:
    face.use_smooth = False
# Explicitly triangulate the exported mesh for predictable in-game shading.
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
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
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05,.07,.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=1):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)

light('Sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,2,4), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,5)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.25
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.25,-4.4,4.25)
point_at(cam, (0,-.05,.04))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
