import bpy
import math
import json
import os
import sys
from mathutils import Vector

# Capacitor Kite: the model uses the same unscaled coordinates as the Unity hull.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    path = os.path.abspath(args[args.index('--') + 1])
    OUT = path if os.path.isdir(path) else os.path.dirname(path)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for datablock in list(datablocks):
        if datablock.users == 0:
            datablocks.remove(datablock)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30), (0.95,0.38,0.12),
    (0.32,0.48,0.55), (0.54,0.18,0.16), (0.53,0.61,0.62), (0.34,0.31,0.48),
    (0.20,0.36,0.40), (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55), (0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = 0.65
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    mats.append(mat)

body = []
parts = []
def mesh_object(name, verts, faces, material, group=body, face_mats=None):
    mesh = bpy.data.meshes.new(name + '_mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for i, p in enumerate(mesh.polygons):
        p.material_index = face_mats[i] if face_mats else material
        p.use_smooth = False
    group.append(obj)
    return obj

def plate(name, outline, zbottom, zedge, ztop, inset, mat, topmat=None, group=body):
    # Three contours produce a tapered underside and a broad, sloping upper bevel.
    n = len(outline)
    cx = sum(p[0] for p in outline)/n
    cy = sum(p[1] for p in outline)/n
    inner = [(cx+(x-cx)*inset, cy+(y-cy)*inset) for x,y in outline]
    bottom = [(cx+(x-cx)*0.90, cy+(y-cy)*0.90) for x,y in outline]
    verts = [(x,y,zbottom) for x,y in bottom] + [(x,y,zedge) for x,y in outline] + [(x,y,ztop) for x,y in inner]
    faces = [tuple(reversed(range(n)))]
    inds = [11]
    for k in range(n):
        j = (k+1)%n
        faces.append((k,j,n+j,n+k)); inds.append(mat)
        faces.append((n+k,n+j,2*n+j,2*n+k)); inds.append(mat)
    faces.append(tuple(range(2*n,3*n))); inds.append(mat if topmat is None else topmat)
    return mesh_object(name, verts, faces, mat, group, inds)

def ring_y(name, x, y_front, y_back, z, outer, inner, mat, recess=11, segments=10, group=body):
    # Front is the visible mouth, not a glowing solid disk.
    verts=[]
    for y, r in ((y_front,outer), (y_front,inner), (y_back,outer), (y_back,inner)):
        for i in range(segments):
            a=2*math.pi*i/segments
            verts.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    faces=[]; indices=[]
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                      (segments+i,segments+j,3*segments+j,3*segments+i)])
        indices.extend([mat,mat,recess])
    faces.append(tuple(3*segments+i for i in range(segments))); indices.append(recess)
    return mesh_object(name,verts,faces,mat,group,indices)

def cylinder(name, center, radius, depth, mat, vertices=10, axis='Z', group=body):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=center)
    obj=bpy.context.object
    obj.name=name
    if axis == 'Y': obj.rotation_euler[0]=math.pi/2
    obj.data.materials.append(mats[mat])
    group.append(obj)
    return obj

def bar(name, a, b, width, depth, mat, group=body):
    # A short structural rail, aligned in the XY plane.
    dx=b[0]-a[0]; dy=b[1]-a[1]
    length=math.hypot(dx,dy)
    bpy.ops.mesh.primitive_cube_add(size=1, location=((a[0]+b[0])/2,(a[1]+b[1])/2,(a[2]+b[2])/2))
    obj=bpy.context.object; obj.name=name
    obj.dimensions=(width,length,depth)
    obj.rotation_euler[2]=-math.atan2(dx,dy)
    obj.data.materials.append(mats[mat]); group.append(obj)
    return obj

# Two distinct swept diamond panels preserve the original kite silhouette.
for s, label in ((-1,'L'),(1,'R')):
    def mirror(poly):
        out=[(s*x,y) for x,y in poly]
        return out if s > 0 else list(reversed(out))
    plate('Wing_'+label, mirror([(0.08,-0.75),(0.95,0.02),(0.50,0.90),(0.20,0.29)]),
          -0.015,0.12,0.23,0.72,7,7)
    plate('Inset_'+label, mirror([(0.22,-0.53),(0.81,0.05),(0.49,0.72),(0.30,0.28)]),
          0.225,0.235,0.265,0.83,4,4)
    # A pale angular edge identifies the swept wings at gameplay scale.
    plate('Edge_'+label, mirror([(0.78,0.04),(0.95,0.02),(0.50,0.90),(0.44,0.75)]),
          0.125,0.16,0.20,0.88,6,6)
    # Paired upright capacitor drums have octagonal dark bases and steel crowns.
    cylinder('Cap_base_'+label,(s*0.52,-0.18,0.265),0.205,0.12,11,10)
    cylinder('Cap_shell_'+label,(s*0.52,-0.18,0.335),0.18,0.12,8,10)
    cylinder('Cap_band_'+label,(s*0.52,-0.18,0.405),0.155,0.035,6,10)
    cylinder('Cap_core_'+label,(s*0.52,-0.18,0.426),0.065,0.012,9,8)
    # Barrel mouth lies exactly at the gameplay muzzle center; it points downscreen.
    ring_y('Muzzle_'+label,s*0.52,-0.48,-0.31,0.26,0.095,0.057,6,11,10)
    # Dark rear-facing nozzle opening is centered on the declared engine socket.
    ring_y('Socket_'+label,s*0.28,0.61,0.49,0.0,0.09,0.055,6,11,10)
    bar('Conductor_'+label,(s*0.30,0.26,0.31),(s*0.46,-0.02,0.36),0.045,0.035,13)
    # Split trailing control surfaces, rooted in the rear wing, can swing gently.
    fin_group=[]
    plate('Fin_'+label, mirror([(0.39,0.61),(0.54,0.71),(0.50,0.90),(0.34,0.76)]),
          0.20,0.23,0.29,0.72,7,6,fin_group)
    parts.append((fin_group[0], {'object':'Fin_'+label, 'motion':'swing',
                  'axis':[0,s,0], 'pivot':[s*0.40,0.65,0.23], 'speed':1.2, 'amplitude':12}))

# Raised center spine and a compact lightning-shaped identification stripe.
plate('Spine', [(-0.13,-0.63),(0,-0.73),(0.13,-0.63),(0.15,0.40),(0,0.68),(-0.15,0.40)],
      0.07,0.22,0.34,0.68,11,8)
plate('Spine_cap', [(-0.075,-0.43),(0,-0.53),(0.075,-0.43),(0.075,0.36),(0,0.47),(-0.075,0.36)],
      0.34,0.35,0.38,0.80,6,6)
plate('Lightning', [(-0.04,0.19),(0.025,0.19),(-0.005,0.05),(0.055,0.05),(-0.035,-0.17),(-0.012,-0.02),(-0.065,-0.02)],
      0.381,0.383,0.386,0.98,9,9)

def join_objects(objects, name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=name
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # Explicit triangulation makes the exported budget and flat facets predictable.
    mod=obj.modifiers.new('Triangulate','TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons: poly.use_smooth=False
    return obj

model=join_objects(body,'Model')
part_objects=[]
for obj, metadata in parts:
    part_objects.append(join_objects([obj],metadata['object']))
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f:
    json.dump([metadata for _,metadata in parts], f, indent=2)
    f.write('\n')

bpy.ops.object.select_all(action='DESELECT')
for obj in [model]+part_objects: obj.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Render setup. Both views include the same body and movable parts.
scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.device='CPU'
scene.cycles.samples=48
scene.render.resolution_x=640
scene.render.resolution_y=640
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Dark blue-gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
background=scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value=(0.05,0.07,0.09,1)
background.inputs['Strength'].default_value=1.0
scene.view_settings.view_transform='Standard'

def point_at(obj, point):
    obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    obj.location=loc; point_at(obj,target)
    return obj
sun=light('Sun','SUN',(-3,-4,7),2.0,(0,0,0))
sun.data.angle=math.radians(18)
light('Soft fill','AREA',(4,1,5),180,(0,0,0),5.0)
camera_data=bpy.data.cameras.new('Camera')
camera=bpy.data.objects.new('Camera',camera_data)
bpy.context.collection.objects.link(camera); scene.camera=camera
camera.location=(0,0.06,5); camera.rotation_euler=(0,0,0)
camera_data.type='ORTHO'; camera_data.ortho_scale=2.25
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camera_data.type='PERSP'; camera_data.lens=50
camera.location=(3.0,-4.0,3.7); point_at(camera,(0,0.04,0.14))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
