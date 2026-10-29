import bpy
import math
import os
import sys
from mathutils import Vector

# Output beside this script, including when Blender executes it via --python.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    p = os.path.abspath(args[args.index('--') + 1])
    OUT = p if os.path.isdir(p) else os.path.dirname(p)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
          (0.80,0.84,0.83)]
mats = []
for i, col in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*col, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*col, 1)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*col, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)
parts = []

def mesh(name, vertices, faces, material, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        data.materials.append(m)
    for j, poly in enumerate(data.polygons):
        poly.material_index = face_mats[j] if face_mats else material
        poly.use_smooth = False
    parts.append(obj)
    return obj

def plate(name, outline, zbottom, zshoulder, ztop, inset, topmat, sidemat):
    # Inset the top vertices toward the polygon centroid for broad chamfered shoulders.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    inner = [(x + (cx-x)*inset, y + (cy-y)*inset) for x,y in outline]
    n = len(outline)
    verts = [(x,y,zbottom) for x,y in outline] + [(x,y,zshoulder) for x,y in outline] + [(x,y,ztop) for x,y in inner]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    indices = [sidemat, topmat]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i)])
        indices.extend([sidemat, topmat])
    return mesh(name, verts, faces, topmat, indices)

def ring_z(name, cx, cy, outer, inner, bottom, top, material, segments=16):
    verts=[]
    for z, radius in ((bottom,outer),(top,outer),(top,inner),(bottom,inner)):
        verts += [(cx+radius*math.cos(2*math.pi*i/segments), cy+radius*math.sin(2*math.pi*i/segments), z) for i in range(segments)]
    faces=[]
    for i in range(segments):
        j=(i+1)%segments
        for a,b in ((0,1),(1,2),(2,3),(3,0)):
            faces.append((a*segments+i,a*segments+j,b*segments+j,b*segments+i))
    return mesh(name, verts, faces, material)

def cylinder(name, loc, radius, depth, material, vertices=12, axis='Z'):
    rot = (math.pi/2,0,0) if axis == 'Y' else (0,0,0)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=loc, rotation=rot)
    obj=bpy.context.object
    obj.name=name
    obj.data.materials.append(mats[material])
    for p in obj.data.polygons: p.use_smooth=False
    parts.append(obj)
    return obj

def tube_y(name, x, y_end, z, outer, inner, depth, material, direction=-1, n=12):
    # The annular front face lies exactly on the gameplay point y_end.
    verts=[]
    for y,r in ((y_end-direction*depth,outer),(y_end,outer),(y_end,inner),(y_end-direction*depth*0.65,inner)):
        verts += [(x+r*math.cos(2*math.pi*i/n),y,z+r*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]
    for i in range(n):
        j=(i+1)%n
        for a,b in ((0,1),(1,2),(2,3)):
            faces.append((a*n+i,a*n+j,b*n+j,b*n+i))
    return mesh(name,verts,faces,material)

def arc_carriage():
    # Horseshoe opens toward the player (-Y); the ends are buried in the cheeks.
    n=16
    verts=[]
    angles=[math.radians(-28 + 236*i/n) for i in range(n+1)]
    for z,r in ((-0.16,1.31),(0.14,1.31),(0.22,1.20),(0.22,0.83),(-0.16,0.79)):
        verts += [(min(r, 1.08)*math.cos(a), min(r, 1.08)*math.sin(a), z) if (abs(math.degrees(a)-43) < 17 or abs(math.degrees(a)-137) < 17) else (r*math.cos(a),r*math.sin(a),z) for a in angles]
    faces=[]; ids=[]; stride=n+1
    for band in range(4):
        for i in range(n):
            faces.append((band*stride+i,band*stride+i+1,(band+1)*stride+i+1,(band+1)*stride+i))
            ids.append([11,6,0,11][band])
    faces.extend([(0,stride,2*stride,3*stride,4*stride), (n, stride+n,2*stride+n,3*stride+n,4*stride+n)])
    ids.extend([11,11])
    mesh('armored horseshoe',verts,faces,0,ids)

arc_carriage()
for s in (-1,1):
    # Mirroring reverses polygon winding, so restore it for consistent outward faces.
    def mirror(points):
        result=[(s*x,y) for x,y in points]
        return result if s == 1 else list(reversed(result))
    cheek=mirror([(0.65,0.91),(1.27,0.52),(1.37,-0.28),(0.99,-0.84),(0.77,-0.62),(0.96,0.13)])
    plate('side armor',cheek,-0.18,0.19,0.36,0.17,0,11)
    plate('raised cheek',mirror([(0.99,0.43),(1.22,0.28),(1.27,-0.23),(1.07,-0.57),(0.99,-0.48),(1.09,-0.05)]),0.35,0.39,0.44,0.12,6,8)
    plate('rust end cap',mirror([(0.91,-0.63),(1.02,-0.74),(1.15,-0.55),(1.06,-0.48)]),0.35,0.39,0.42,0.10,12,11)
    # Compact faceted motor housings at the rear, with socket openings at exactly +/-0.93, .86, 0.
    plate('motor housing',mirror([(0.70,0.34),(1.13,0.34),(1.17,0.70),(1.06,0.82),(0.77,0.82),(0.69,0.70)]),-0.18,0.24,0.34,0.18,4,11)
    tube_y('rear engine socket',s*0.93,0.86,0,0.16,0.105,0.16,6,direction=1)
    cylinder('socket dark interior',(s*0.93,0.85,0),0.103,0.012,11,12,'Y')
    # Diagonal hazard flashes on the side armor, kept broad for a tiny sprite.
    for y in (-0.20,0.03):
        plate('hazard marking',mirror([(1.115,y-0.105),(1.205,y-0.045),(1.205,y+0.035),(1.115,y-0.025)]),0.443,0.445,0.448,0,13,13)
    plate('breech brace',mirror([(0.47,-0.22),(0.91,-0.22),(0.94,0.03),(0.49,0.07)]),0.02,0.22,0.27,0.14,6,11)

# Central suspended, multi-level rotary breech.
cylinder('breech foundation',(0,-0.16,0.11),0.61,0.32,11,12)
cylinder('breech shoulder',(0,-0.16,0.29),0.54,0.22,6,12)
ring_z('rotary collar',0,-0.16,0.44,0.28,0.40,0.49,0,12)
cylinder('rotor face',(0,-0.16,0.465),0.275,0.055,11,12)
cylinder('rotor hub',(0,-0.16,0.505),0.12,0.035,13,8)
plate('rear hazard bar',[(-0.35,0.51),(0.35,0.51),(0.35,0.60),(-0.35,0.60)],0.27,0.29,0.30,0,3,11)

# Three substantial gun housings and recessed open lips, exactly at the firing points.
for x in (-0.2,0.0,0.2):
    cylinder('gun barrel',(x,-0.70,0.48),0.072,0.68,6,10,'Y')
    tube_y('muzzle lip',x,-1.09,0.48,0.087,0.052,0.13,11,n=10)
    cylinder('muzzle bore',(x,-1.074,0.48),0.050,0.012,11,10,'Y')

# Consolidate before export; only the Model mesh is exported.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons: p.use_smooth=False
# Triangulate the export mesh deterministically and enforce the stated budget.
tri=model.modifiers.new('Triangulate','TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
assert 300 <= len(model.data.polygons) <= 4000, 'Triangle budget exceeded: %d' % len(model.data.polygons)
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
background=scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value=(0.05,0.07,0.09,1)
background.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def aim(obj, target):
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name,kind)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    aim(obj,target)
    return obj
light('Key sun','SUN',(-4,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,5),180,(0,0,0),5)
camdata=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,7)
cam.rotation_euler=(0,0,0)
aim(cam,(0,0,0))
# Explicitly orient screen-up along +Y for the top-down view.
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=3.25
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type='PERSP'
camdata.lens=50
cam.location=(4.5,-6.5,5.8)
aim(cam,(0,0,0.10))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)

