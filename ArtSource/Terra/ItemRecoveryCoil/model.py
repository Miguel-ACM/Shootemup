import bpy
import math
import os
import sys
from mathutils import Vector

# Output beside this script, including when Blender is launched with --python.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    OUT = os.path.abspath(sys.argv[sys.argv.index('--') + 1]) if '--' in sys.argv and len(sys.argv) > sys.argv.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

colors = [
    (0.10, 0.34, 0.72, 1), # blue shield edge
    (0.18, 0.56, 1.00, 1), # signature blue
    (0.34, 0.75, 1.00, 1), # light blue bevel
    (0.16, 0.38, 0.56, 1), # mechanical blue steel
    (0.63, 0.79, 0.87, 1), # silver blue
    (1.00, 0.68, 0.20, 1), # copper gold coil
    (1.00, 0.85, 0.39, 1), # gold end caps
    (0.07, 0.17, 0.27, 1), # small muzzle aperture
]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.72
    mats.append(mat)

def mesh_obj(name, verts, faces, indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for poly, idx in zip(mesh.polygons, indices):
        poly.material_index = idx
        poly.use_smooth = False
    return ob

# The thick pentagonal shield is bevelled on both faces, with inset blue faces.
def shield():
    outline = [(-.40,.30), (-.31,-.18), (0,-.45), (.31,-.18), (.40,.30)]
    rings = []
    for z, scale in [(-.16,.88),(-.12,1),(.12,1),(.16,.88)]:
        rings.append([(x*scale, y*scale, z) for x,y in outline])
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(5))), tuple(range(15,20))]
    ids = [1,1]
    for j in range(3):
        for i in range(5):
            faces.append((j*5+i, j*5+(i+1)%5, (j+1)*5+(i+1)%5, (j+1)*5+i))
            ids.append([2,0,2][j])
    mesh_obj('Bevelled shield', verts, faces, ids)

def plate(name, outline, z0, z1, mat):
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh_obj(name, verts, faces, [mat]*len(faces))

def cylinder(name, location, radius, depth, material, axis='Y', vertices=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=location, rotation=(math.pi/2,0,0) if axis=='Y' else (0,0,0))
    ob = bpy.context.object
    ob.name = name
    ob.data.materials.append(mats[material])
    for p in ob.data.polygons: p.use_smooth = False
    return ob

def coil(name, zcenter):
    # One continuous, substantial angular winding around the vertical barrel.
    steps, sides, turns = 48, 6, 3
    verts = []
    for i in range(steps + 1):
        t = i / steps
        a = 2*math.pi*turns*t
        center = Vector((.105*math.cos(a), -.23 + .46*t, zcenter + .105*math.sin(a)))
        radial = Vector((math.cos(a), 0, math.sin(a)))
        tangent = Vector((-.105*math.sin(a)*2*math.pi*turns, .46, .105*math.cos(a)*2*math.pi*turns)).normalized()
        binormal = tangent.cross(radial).normalized()
        for j in range(sides):
            q = 2*math.pi*j/sides
            v = center + .033*(math.cos(q)*radial + math.sin(q)*binormal)
            verts.append(tuple(v))
    faces = [tuple(reversed(range(sides))), tuple(steps*sides+j for j in range(sides))]
    for i in range(steps):
        for j in range(sides):
            faces.append((i*sides+j, i*sides+(j+1)%sides, (i+1)*sides+(j+1)%sides, (i+1)*sides+j))
    mesh_obj(name, verts, faces, [5]*len(faces))

shield()
# Broad raised facets make the shield read from front and back while spinning.
for sign in (-1, 1):
    z0, z1 = sorted((sign*.16, sign*.185))
    for side in (-1, 1):
        pts = [(side*.17,.23),(side*.34,.24),(side*.265,-.14),(side*.12,-.28)]
        if side < 0: pts.reverse()
        plate('Shield facet', pts, z0, z1, 2)
    z = sign*.215
    cylinder('Barrel core', (0,.015,z), .076,.63, 3)
    coil('Copper winding', z)
    cylinder('Lower collar', (0,-.255,z), .12,.07, 4)
    cylinder('Upper collar', (0,.27,z), .12,.075, 4)
    cylinder('Muzzle', (0,.335,z), .105,.085, 6)
    cylinder('Muzzle bore', (0,.38,z), .061,.008, 7)
    # Reinforced foot ties the exposed assembly into the shield.
    cylinder('Foot', (0,-.32,z), .09,.065, 1)

# Join, bake transforms, triangulate explicitly for predictable exported geometry.
bpy.ops.object.select_all(action='DESELECT')
mesh_objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for ob in mesh_objects: ob.select_set(True)
bpy.context.view_layer.objects.active = mesh_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for p in model.data.polygons: p.use_smooth = False

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1
scene.view_settings.view_transform = 'Standard'

def point_at(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, position, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA': data.shape = 'DISK'; data.size = size
    ob = bpy.data.objects.new(name, data)
    scene.collection.objects.link(ob)
    ob.location = position
    point_at(ob, target)
    return ob

light('Upper left sun', 'SUN', (-3,4,5), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,1,4), 110, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,4)
point_at(cam, (0,0,0))
camdata.type = 'ORTHO'
camdata.ortho_scale = 1.12
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (0.85,0.52,1.24)
point_at(cam, (0,0,0))
camdata.type = 'PERSP'
camdata.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

