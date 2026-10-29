import bpy
import math
import os
import sys
from mathutils import Vector

# The mesh uses game coordinates: X right, Y up and Z toward the viewer.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

palette = [
    (0.36, 0.72, 0.84, 1), # blue glass edge
    (0.62, 0.92, 1.00, 1), # ice blue hull
    (0.43, 0.79, 0.91, 1), # blue facet
    (0.76, 0.97, 1.00, 1), # pale facet
    (0.25, 0.60, 0.74, 1), # engine casing
    (0.48, 0.85, 0.93, 1), # wing face
    (0.92, 1.00, 1.00, 1), # fracture
    (0.25, 0.65, 0.78, 1), # canopy edge
    (0.55, 0.90, 0.98, 1), # canopy
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = color
    mat.use_nodes = True
    mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value = color
    mats.append(mat)

parts = []
def mesh_part(name, vertices, faces, colors):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.clear()
    for mat in mats:
        mesh.materials.append(mat)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for poly, idx in zip(mesh.polygons, colors):
        poly.material_index = idx
        poly.use_smooth = False
    parts.append(obj)
    return obj

def prism(name, outline, front, back, face_mat, side_mat, bevel=0):
    n = len(outline)
    vertices = [(x,y,front) for x,y in outline] + [(x,y,back) for x,y in outline]
    faces = [tuple(range(n)), tuple(range(2*n-1,n-1,-1))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    obj = mesh_part(name, vertices, faces, [face_mat, face_mat] + [side_mat]*n)
    if bevel:
        bpy.context.view_layer.objects.active = obj
        mod = obj.modifiers.new('Cut glass edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        mod.affect = 'EDGES'
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

# Broad swept diamond wings: substantial depth and cut-glass edges.
for s in (-1, 1):
    wing = [(s*x,y) for x,y in [(0.10,0.10),(0.20,0.06),(0.43,-0.21),
                                  (0.36,-0.27),(0.16,-0.23),(0.10,-0.31)]]
    if s < 0:
        wing.reverse()
    prism('Swept crystal wing', wing, 0.065, -0.065, 5, 0, 0.012)
    inset = [(s*x,y) for x,y in [(0.17,0.035),(0.36,-0.202),(0.29,-0.206),(0.16,-0.15)]]
    if s < 0:
        inset.reverse()
    prism('Wing facet', inset, 0.078, 0.066, 3, 1)
    prism('Reverse wing facet', inset, -0.066, -0.078, 2, 1)

# Tapered cross sections produce a real, faceted crystalline fuselage.
# Each ring runs from the front ridge around to the back ridge.
rings = [
    (0.45, 0.012, 0.025),
    (0.29, 0.075, 0.095),
    (0.08, 0.135, 0.140),
    (-0.19, 0.145, 0.145),
    (-0.32, 0.095, 0.105),
    (-0.43, 0.018, 0.030),
]
verts = []
for y,w,d in rings:
    verts += [(0,y,d), (w*0.70,y,d*0.70), (w,y,0), (w*0.70,y,-d*0.70),
              (0,y,-d), (-w*0.70,y,-d*0.70), (-w,y,0), (-w*0.70,y,d*0.70)]
faces=[]
colors=[]
for j in range(len(rings)-1):
    for k in range(8):
        a=j*8+k; b=j*8+(k+1)%8; c=(j+1)*8+(k+1)%8; d=(j+1)*8+k
        faces.extend([(a,b,c),(a,c,d)])
        colors.extend(([3,1,2,0,2,1,3,1][k], [1,2,1,2,1,3,1,3][k]))
faces.extend([tuple(reversed(range(8))), tuple((len(rings)-1)*8+k for k in range(8))])
colors.extend([1,2])
mesh_part('Faceted hull', verts, faces, colors)

# Twin short, chunky nacelles; twelve-sided cylinders read as polished crystal.
def cylinder(name, radius, depth, location, material, bevel=0.012):
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=depth,
                                       location=location, rotation=(math.pi/2,0,0))
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    if bevel:
        bpy.context.view_layer.objects.active = obj
        mod = obj.modifiers.new('Chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons:
        p.use_smooth = False
    parts.append(obj)
    return obj
for s in (-1,1):
    cylinder('Crystal engine', 0.052, 0.285, (s*0.19,-0.095,0), 4)
    cylinder('Engine collar', 0.058, 0.045, (s*0.19,-0.205,0), 1, 0.007)
    cylinder('Engine end', 0.040, 0.014, (s*0.19,-0.246,0), 3, 0.003)

# Raised canopy on both faces, shaped as a short faceted jewel.
def jewel(sign):
    z=sign
    v=[(0,0.235,z*0.101),(-0.058,0.105,z*0.133),(0,-0.035,z*0.145),
       (0.058,0.105,z*0.133),(0,0.100,z*0.182)]
    f=[(0,1,4),(1,2,4),(2,3,4),(3,0,4)]
    if sign < 0: f=[tuple(reversed(x)) for x in f]
    mesh_part('Canopy',v,f,[3,8,7,8])
for side in (-1,1): jewel(side)

# One bold zigzag fracture on each face. The segments sit above the hull,
# changing height with the hull's taper to avoid clipping.
def fracture(sign):
    points=[(-0.014,0.37,0.070),(0.016,0.235,0.112),(-0.012,0.065,0.154),
            (0.021,-0.105,0.156),(-0.010,-0.245,0.135),(0.006,-0.345,0.095)]
    width=0.012
    vs=[]
    for i,(x,y,z) in enumerate(points):
        prev=points[max(0,i-1)]; nex=points[min(len(points)-1,i+1)]
        dx=nex[0]-prev[0]; dy=nex[1]-prev[1]
        length=math.hypot(dx,dy)
        nx=-dy/length*width; ny=dx/length*width
        vs.extend([(x+nx,y+ny,sign*z),(x-nx,y-ny,sign*z)])
    fs=[]
    for i in range(len(points)-1):
        face=(2*i,2*i+1,2*i+3,2*i+2)
        fs.append(face if sign > 0 else tuple(reversed(face)))
    mesh_part('Pale fracture',vs,fs,[6]*len(fs))
for side in (-1,1): fracture(side)

# Join and triangulate explicitly, with all object transforms baked.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Triangulate', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for p in model.data.polygons:
    p.use_smooth = False

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
scene.world = bpy.data.worlds.new('Blue gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target=(0,0,0)):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, size=1):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if kind == 'AREA': data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj)
    return obj
light('Upper left sun', 'SUN', (-3,4,5), 2.0)
light('Soft fill', 'AREA', (4,1,3), 120, 5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,3)
aim(cam)
camdata.type='ORTHO'
camdata.ortho_scale=1.26
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(0.90,0.53,1.71)
aim(cam)
camdata.type='PERSP'
camdata.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

