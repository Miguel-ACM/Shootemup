import bpy
import math
import os
import sys
from mathutils import Vector

# Blender 5.x. Run with: blender --background --python model.py -- <script directory>
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or args.index('--') + 1 >= len(args):
        raise RuntimeError('Pass the script directory after -- when __file__ is unavailable')
    OUT = os.path.abspath(args[args.index('--') + 1])

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

# Icy blue mirror glass, silver-blue frame, and mint reflected light.
palette = [
    (0.42, 0.57, 0.78, 1),  # frame side
    (0.68, 0.80, 0.96, 1),  # frame bevel
    (0.38, 0.65, 0.86, 1),  # mirror blue
    (0.62, 0.83, 0.99, 1),  # mirror light blue
    (0.79, 0.91, 1.00, 1),  # mirror highlight
    (0.32, 0.72, 0.76, 1),  # reflected mint
    (0.54, 0.73, 0.91, 1),  # wing blue
    (0.78, 0.89, 0.99, 1),  # wing edge
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.58
    mats.append(mat)

# Meshes are made from rings so the borders are actual bevels, visible in profile.
def ring_mesh(name, rings, side_colors, cap_colors=None, centers=None):
    n = len(rings[0])
    verts = [v for ring in rings for v in ring]
    faces, colors = [], []
    for j in range(len(rings) - 1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
            colors.append(side_colors[j])
    if centers:
        for end in (0, len(rings)-1):
            idx = len(verts)
            verts.append(centers[0 if end == 0 else 1])
            for i in range(n):
                # Opposite winding on opposite sides.
                face = (idx, end*n+(i+1)%n, end*n+i) if end == 0 else (idx, end*n+i, end*n+(i+1)%n)
                faces.append(face)
                colors.append(cap_colors[i % len(cap_colors)])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for p, color in zip(mesh.polygons, colors):
        p.material_index = color
    return obj

def ring(points, scale, z, cx=0, cy=0):
    return [(cx + (x-cx)*scale, cy + (y-cy)*scale, z) for x, y in points]

# Five-sided shield mirror, framed by a substantial chamfered housing.
outline = [(0, .44), (-.265, .135), (-.19, -.38), (.19, -.38), (.265, .135)]
ring_mesh('Mirror housing', [ring(outline, .88, -.13), ring(outline, 1, -.085),
                            ring(outline, 1, .085), ring(outline, .88, .13)],
          [1, 0, 1], [1], [(0, 0, -.13), (0, 0, .13)])
# A low, faceted crown on each face creates a readable mirror reflection.
inner = [(0, .355), (-.195, .105), (-.14, -.305), (.14, -.305), (.195, .105)]
ring_mesh('Mirror facets', [ring(inner, 1, -.136), ring(inner, 1, .136)], [2],
          [4, 3, 2, 5, 3], [(-.035, .01, -.175), (-.035, .01, .175)])

# Two robust, beveled shards splay out from the central mirror. Their inset
# facets and rear faces use the same visual language as the center.
for s in (-1, 1):
    # Maintain consistent counterclockwise ordering on both wings.
    wing = [(s*.19, -.13), (s*.45, -.20), (s*.46, .31), (s*.31, .16)]
    if s < 0:
        wing.reverse()
    cx, cy = s*.34, .035
    ring_mesh('Wing frame', [ring(wing, .86, -.115, cx, cy), ring(wing, 1, -.075, cx, cy),
                             ring(wing, 1, .075, cx, cy), ring(wing, .86, .115, cx, cy)],
              [7, 0, 7], [6], [(cx, cy, -.115), (cx, cy, .115)])
    inset = ring(wing, .73, 0, cx, cy)
    inset_xy = [(x, y) for x, y, z in inset]
    ring_mesh('Wing facets', [ring(inset_xy, 1, -.12), ring(inset_xy, 1, .12)], [6],
              [3, 4, 6, 5], [(cx, cy, -.145), (cx, cy, .145)])

# Low-poly hinge pins and broad rivets make the layered construction legible
# from the side while keeping the front graphic simple.
for s in (-1, 1):
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=.055, depth=.27,
                                       location=(s*.235, -.055, 0), rotation=(math.pi/2, 0, 0))
    pin = bpy.context.object
    pin.name = 'Wing hinge'
    pin.data.materials.append(mats[1])
    for z in (-.132, .132):
        for y in (-.16, .10):
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.035,
                                                  location=(s*.235, y, z))
            rivet = bpy.context.object
            rivet.name = 'Frame rivet'
            rivet.scale = (1, 1, .42)
            rivet.data.materials.append(mats[7])

# Join, apply transforms, and triangulate before export.
bpy.ops.object.select_all(action='DESELECT')
models = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
for obj in models:
    obj.select_set(True)
bpy.context.view_layer.objects.active = models[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (.05, .07, .09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 5, 6), 2.0, (0, 0, 0))
sun.data.angle = math.radians(12)
light('Soft fill', 'AREA', (4, 1, 4), 100, (0, 0, 0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 4)
point_at(cam, (0, 0, 0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.08
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (.85, .55, 1.42)
point_at(cam, (0, 0, 0))
cam_data.type = 'PERSP'
cam_data.lens = 50
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

