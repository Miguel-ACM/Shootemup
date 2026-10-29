import bpy
import math
import os
import sys
from mathutils import Vector

# The launcher may execute a script without setting __file__.
try:
    OUT_DIR = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT_DIR = path if os.path.isdir(path) else os.path.dirname(path)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)

palette = [
    (0.27, 0.62, 0.34, 1),  # armor green
    (0.40, 0.88, 0.46, 1),  # raised face green
    (0.17, 0.42, 0.25, 1),  # edge green
    (0.72, 0.90, 0.71, 1),  # pale mint cross
    (0.61, 0.70, 0.61, 1),  # steel fasteners
    (0.35, 0.51, 0.39, 1),  # fastener collar
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = color
    bsdf.inputs['Roughness'].default_value = 0.76
    mats.append(m)

parts = []
def mesh_obj(name, verts, faces, material_indices):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for poly, idx in zip(mesh.polygons, material_indices):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(ob)
    return ob

# Successive clipped-corner rings make the armor thick and chamfered on both faces.
outline = [(-.30, .36), (.30, .36), (.40, .26), (.40, -.26),
           (.30, -.36), (-.30, -.36), (-.40, -.26), (-.40, .26)]
rings = [(0.91, -.125), (1.0, -.075), (1.0, .075), (0.91, .125)]
verts = [(x*s, y*s, z) for s,z in rings for x,y in outline]
faces = [tuple(range(8)), tuple(reversed(range(24, 32)))]
indices = [0, 0]
for r in range(3):
    for j in range(8):
        k = (j + 1) % 8
        faces.append((r*8+j, (r+1)*8+j, (r+1)*8+k, r*8+k))
        indices.append(2 if r == 1 else 0)
mesh_obj('Chamfered plate', verts, faces, indices)

def oct_panel(z, side):
    # A smaller raised octagonal face preserves a broad visible outer rim.
    scales = [0.79, 0.73]
    zs = [z, z + side*.025]
    vs = [(x*s,y*s,zz) for s,zz in zip(scales,zs) for x,y in outline]
    fs = [tuple(range(8,16))]
    mi = [1]
    for j in range(8):
        k=(j+1)%8
        fs.append((j,k,8+k,8+j))
        mi.append(0)
    ob = mesh_obj('Raised armor face',vs,fs,mi)
    # Orient normals consistently for the reverse face.
    if side > 0:
        for p in ob.data.polygons:
            p.flip()

for side in (-1,1):
    oct_panel(side*.123, side)

def cross(side):
    # A single solid plus-sign rather than overlapping bars.
    w, l = .048, .155
    p = [(-w,-l),(w,-l),(w,-w),(l,-w),(l,w),(w,w),
         (w,l),(-w,l),(-w,w),(-l,w),(-l,-w),(-w,-w)]
    z0, z1 = side*.146, side*.174
    vs = [(x,y,z) for z in (z0,z1) for x,y in p]
    fs = [tuple(reversed(range(12))),tuple(range(12,24))]
    for j in range(12):
        k=(j+1)%12
        fs.append((j,k,12+k,12+j))
    ob = mesh_obj('Raised plus', vs, fs, [3]*len(fs))
    if side < 0:
        for poly in ob.data.polygons:
            poly.flip()

for side in (-1,1):
    cross(side)
    for x in (-.255,.255):
        for y in (-.215,.215):
            # Hexagonal collars and twelve-sided bolt heads read as substantial hardware.
            for radius, depth, z, material, n in [(.046,.018,.147,5,8),(.031,.022,.164,4,12)]:
                bpy.ops.mesh.primitive_cylinder_add(vertices=n, radius=radius, depth=depth,
                                                    location=(x,y,side*z))
                ob=bpy.context.object
                ob.name='Fastener'
                ob.data.materials.append(mats[material])
                for poly in ob.data.polygons:
                    poly.use_smooth=False
                parts.append(ob)

# Join before export; applying scale, rotation, and location keeps a clean single mesh.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
# Triangulate explicitly so the exported budget and faceting are deterministic.
tri=model.modifiers.new('Export triangulation','TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth=False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT_DIR,'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=512
scene.render.resolution_y=512
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='AgX'

def point_at(ob, target=(0,0,0)):
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None:
        data.shape='DISK'
        data.size=size
    ob=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(ob)
    ob.location=location
    point_at(ob)
    return ob
light('Upper left sun','SUN',(-3,4,5),2.0)
light('Soft fill','AREA',(3,-1,4),120,5.0)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,4)
point_at(cam)
cam_data.type='ORTHO'
cam_data.ortho_scale=1.12
scene.render.filepath=os.path.join(OUT_DIR,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(.88,.64,1.38)
point_at(cam)
cam_data.type='PERSP'
cam_data.lens=50
scene.render.filepath=os.path.join(OUT_DIR,'angle.png')
bpy.ops.render.render(write_still=True)



