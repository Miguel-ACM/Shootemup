import bpy
import math
import os
import sys
from mathutils import Vector

# A self-contained, low-poly strawberry macaron, centered on the original pivot.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT_DIR = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT_DIR)[1]:
        OUT_DIR = os.path.dirname(OUT_DIR)
os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

palette = {
    'tile_00': (0.78, 0.19, 0.40),
    'tile_01': (0.97, 0.85, 0.71),
    'tile_08': (0.90, 0.44, 0.65),
    'tile_12': (0.92, 0.31, 0.49),
    'tile_15': (0.98, 0.96, 0.88),
}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.83
    materials[name] = mat

parts = []

def lathe(name, profile, bands, segments=24, scallop=None):
    # Each profile ring has its own polygonal radius and height.
    verts = []
    for ring, (radius, z) in enumerate(profile):
        for i in range(segments):
            a = 2.0 * math.pi * i / segments
            r = radius
            if scallop and ring in scallop:
                r += scallop[ring] * (1 if i % 2 == 0 else -1)
            verts.append((r * math.cos(a), r * math.sin(a), z))
    verts.extend([(0, 0, profile[0][1]), (0, 0, profile[-1][1])])
    faces, indices = [], []
    for j in range(len(profile) - 1):
        for i in range(segments):
            k = (i + 1) % segments
            faces.append((j*segments+i, j*segments+k, (j+1)*segments+k, (j+1)*segments+i))
            indices.append(bands[j])
    bottom, top = len(verts)-2, len(verts)-1
    for i in range(segments):
        k = (i + 1) % segments
        faces.append((bottom, k, i))
        indices.append(bands[0])
        faces.append((top, (len(profile)-1)*segments+i, (len(profile)-1)*segments+k))
        indices.append(bands[-1])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    names = list(materials)
    for n in names:
        mesh.materials.append(materials[n])
    for poly, material_name in zip(mesh.polygons, indices):
        poly.material_index = names.index(material_name)
        poly.use_smooth = False
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
    return obj

# A gently irregular cream layer sits between two distinct baked shells.
lathe('Cream filling', [(0.48,-0.095),(0.575,-0.082),(0.605,-0.045),
                       (0.605,0.045),(0.575,0.082),(0.48,0.095)],
      ['tile_01']*5, scallop={2:0.008, 3:0.008})
# Frilled feet and rounded tops are modeled in the same continuous shell.
lathe('Lower raspberry shell', [(0.12,-0.39),(0.37,-0.365),(0.55,-0.30),
     (0.625,-0.22),(0.64,-0.15),(0.62,-0.115),(0.60,-0.09)],
     ['tile_00']*6, scallop={4:0.006, 5:0.015, 6:0.009})
lathe('Upper strawberry shell', [(0.60,0.09),(0.62,0.115),(0.64,0.15),
     (0.625,0.22),(0.55,0.30),(0.37,0.365),(0.12,0.39)],
     ['tile_12','tile_08','tile_08','tile_08','tile_08','tile_08'],
     scallop={0:0.009, 1:0.015, 2:0.006})

# A handful of tiny baked-in sugar flecks, held close to the domed surface.
def dome_height(r):
    points = [(0.0,0.39),(0.12,0.39),(0.37,0.365),(0.55,0.30),(0.625,0.22)]
    for (r0,z0),(r1,z1) in zip(points, points[1:]):
        if r <= r1:
            return z0 + (z1-z0)*(r-r0)/(r1-r0)
    return 0.22
for i, (x,y,angle) in enumerate([(-.30,.13,.5),(-.14,-.24,-.4),(.08,.20,.8),
                                  (.28,-.10,.3),(.02,-.06,1.0),(.32,.20,-.5),
                                  (-.28,-.12,.7)]):
    z = dome_height(math.hypot(x,y)) + 0.003
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1, location=(x,y,z))
    fleck = bpy.context.object
    fleck.name = 'Sugar fleck %02d' % i
    fleck.scale = (0.034, 0.011, 0.007)
    fleck.rotation_euler[2] = angle
    fleck.data.materials.append(materials['tile_15'])
    parts.append(fleck)

bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.convert(target='MESH')
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# Triangulate explicitly so the export and visible flat facets agree.
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)

bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR, 'model.obj'),
    export_selected_objects=True, export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue-gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 4, 6), 2.0)
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, -2, 4), 100, 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.52
scene.render.filepath = os.path.join(OUT_DIR, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (1.3, -1.65, 1.65)
point_at(cam)
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT_DIR, 'angle.png')
bpy.ops.render.render(write_still=True)

