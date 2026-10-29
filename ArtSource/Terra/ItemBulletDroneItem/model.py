import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses Y as vertical and Z as the front of the pickup.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv else os.getcwd()
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [
    (0.28, 0.43, 0.49, 1),  # blue-gray hull
    (0.42, 0.55, 0.57, 1),  # hull facets
    (0.39, 0.48, 0.31, 1),  # olive wings
    (0.55, 0.61, 0.39, 1),  # wing edges
    (0.83, 0.65, 0.29, 1),  # brass gun barrels
    (0.96, 0.78, 0.39, 1),  # muzzle collars
    (0.19, 0.26, 0.29, 1),  # gun housings and recesses
    (0.23, 0.75, 0.79, 1),  # sensor glass
    (0.66, 0.76, 0.70, 1),  # light armor
    (0.90, 0.43, 0.19, 1),  # rear thruster accents
]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.74
    mats.append(m)

parts = []
def mesh_part(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for m in mats:
        mesh.materials.append(m)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(ob)
    return ob

def layered_plate(name, outline, inset, back_z, shoulder_z, front_z, side_mat, bevel_mat, face_mat):
    # Both sides have a bevel and a broad central face; inset shares vertex count.
    n = len(outline)
    verts = [(x, y, back_z) for x, y in inset]
    verts += [(x, y, shoulder_z) for x, y in outline]
    verts += [(x, y, front_z) for x, y in inset]
    faces = [tuple(reversed(range(n)))]
    ids = [face_mat]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n+j, n+i)); ids.append(side_mat)
        faces.append((n+i, n+j, 2*n+j, 2*n+i)); ids.append(bevel_mat)
    faces.append(tuple(range(2*n, 3*n))); ids.append(face_mat)
    return mesh_part(name, verts, faces, ids)

def cube(name, location, scale, mat, bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    if bevel:
        mod = ob.modifiers.new('Broad chamfers', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in ob.data.polygons:
        p.use_smooth = False
    parts.append(ob)
    return ob

def cylinder(name, location, radius, depth, mat, axis='Y', vertices=8):
    rot = (math.pi/2, 0, 0) if axis == 'Y' else (0, 0, 0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=location, rotation=rot)
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[mat])
    for p in ob.data.polygons:
        p.use_smooth = False
    parts.append(ob)
    return ob

# A tapered armored fuselage with substantial depth and a raised central crown.
hull = [(-.15,-.32),(.15,-.32),(.21,.10),(.15,.23),(0,.35),(-.15,.23),(-.21,.10)]
hull_in = [(-.105,-.27),(.105,-.27),(.155,.09),(.105,.19),(0,.285),(-.105,.19),(-.155,.09)]
layered_plate('Faceted fuselage', hull, hull_in, -.15, 0, .18, 0, 1, 0)
# Swept olive wings, with a beveled face on each side.
for side in (-1, 1):
    outline = [(side*.12,.10),(side*.43,-.10),(side*.40,-.27),(side*.13,-.20)]
    inner = [(side*.16,.035),(side*.38,-.115),(side*.365,-.215),(side*.16,-.17)]
    if side == -1:
        outline.reverse(); inner.reverse()
    layered_plate('Swept wing', outline, inner, -.095, 0, .105, 2, 3, 2)
    cube('Wing root armor', (side*.245,-.19,.105), (.15,.14,.09), 1, .025)
    # Distinct twin cannons run along Y, matching the original drone's silhouette.
    cube('Gun mount', (side*.12,.13,.08), (.13,.23,.16), 6, .022)
    cylinder('Brass cannon', (side*.12,.285,.08), .049, .30, 4)
    cylinder('Muzzle collar', (side*.12,.425,.08), .060, .052, 5)
    cylinder('Muzzle bore', (side*.12,.454,.08), .029, .008, 6)
    # Chunky rear engine pods are legible from sides and from the back.
    cube('Rear drive pod', (side*.25,-.255,-.06), (.15,.16,.22), 6, .025)
    cylinder('Rear nozzle rim', (side*.25,-.255,-.181), .057, .035, 8, axis='Z')
    cylinder('Rear nozzle center', (side*.25,-.255,-.201), .034, .010, 9, axis='Z')

# A single large, angular sensor gives the drone a readable face.
cylinder('Sensor bezel', (0,-.045,.185), .112, .035, 6, axis='Z')
cylinder('Cyan sensor', (0,-.045,.207), .079, .014, 7, axis='Z')
cube('Sensor glint', (-.025,-.020,.217), (.035,.025,.008), 8, .004)
# Back armor and a central rear vent prevent an unfinished reverse view.
cube('Back spine', (0,-.03,-.16), (.13,.29,.05), 1, .018)
cube('Rear vent', (0,-.09,-.193), (.085,.10,.025), 6, .01)
cube('Rear vent insert', (0,-.09,-.208), (.045,.065,.009), 9, .006)

# Consolidate and bake everything before exporting.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Neutral studio setup; camera up is world +Y for these Y-up assets.
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

def point_at(ob, target, track='-Z', up='Y'):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat(track, up).to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = location
    point_at(ob, target)
    return ob

light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, 1, 4), 110, (0, 0, 0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.65, 1.05, 2.8)
point_at(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 52
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
