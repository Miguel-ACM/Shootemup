import bpy
import math
import os
import sys
from mathutils import Vector

# All dimensions are in game units: front is -Y, top is +Z.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):
    bpy.data.materials.remove(m)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
           (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
           (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
           (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
           (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
           (0.80,0.84,0.83)]
mats = []
for i, rgb in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = 0.75
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(m)

parts = []
def mesh_obj(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

def plate(name, outline, zbottom, zshoulder, ztop, inset, mat):
    # The top ring is inset toward the polygon centroid to create a broad bevel.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    top = [(cx + (x-cx)*(1-inset), cy + (y-cy)*(1-inset)) for x,y in outline]
    verts = [(x,y,zbottom) for x,y in outline] + [(x,y,zshoulder) for x,y in outline] + [(x,y,ztop) for x,y in top]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    for i in range(n):
        j = (i+1)%n
        faces.extend([(i,j,n+j,n+i), (n+i,n+j,2*n+j,2*n+i)])
    return mesh_obj(name, verts, faces, mat)

def tube_y(name, x, front, back, z, outer, inner, mat, inside=11, sides=10):
    # Annular front and back rims, with an actual recessed dark bore.
    verts=[]
    for y,r in ((front,outer),(front,inner),(back,outer),(back,inner)):
        for i in range(sides):
            a=2*math.pi*i/sides
            verts.append((x+r*math.cos(a),y,z+r*math.sin(a)))
    faces=[]
    for i in range(sides):
        j=(i+1)%sides
        faces.extend([(i,j,sides+j,sides+i), (i,2*sides+i,2*sides+j,j),
                      (sides+i,sides+j,3*sides+j,3*sides+i),
                      (2*sides+i,3*sides+i,3*sides+j,2*sides+j)])
    obj=mesh_obj(name,verts,faces,mat)
    obj.data.materials.append(mats[inside])
    for i,p in enumerate(obj.data.polygons):
        if i%4 == 2: p.material_index=1
    return obj

def disk_y(name, x, y, z, radius, mat, sides=10):
    verts=[(x,y,z)] + [(x+radius*math.cos(2*math.pi*i/sides),y,z+radius*math.sin(2*math.pi*i/sides)) for i in range(sides)]
    return mesh_obj(name,verts,[(0,1+i,1+(i+1)%sides) for i in range(sides)],mat)

# Heavy octagonal sled: dark lower belt and a pale, broad sloped armor deck.
outline=[(-1.25,.25),(-.92,.72),(.92,.72),(1.25,.25),(1.14,-.42),(.65,-.65),(-.65,-.65),(-1.14,-.42)]
plate('Armored keel',outline,-.06,.17,.28,.09,0)
plate('Snow armor deck',[(-1.19,.25),(-.89,.66),(.89,.66),(1.19,.25),(1.09,-.37),(.62,-.59),(-.62,-.59),(-1.09,-.37)],.20,.31,.43,.11,1)
# Paired shoulders and long recessed blue panels establish width at gameplay scale.
for s in (-1,1):
    def mirror(points):
        p=[(s*x,y) for x,y in points]
        return p if s==1 else list(reversed(p))
    plate('Shoulder armor',mirror([(.65,.40),(1.12,.19),(1.09,-.29),(.73,-.25)]),.34,.43,.49,.12,4)
    plate('Shoulder highlight',mirror([(.78,.30),(1.05,.16),(1.02,-.17),(.79,-.13)]),.48,.49,.51,.08,6)
    # Rear motor cowling, cut short at the fixed socket's y coordinate.
    plate('Rear motor cowling',mirror([(.52,.46),(.98,.46),(1.02,.76),(.91,.92),(.61,.92),(.50,.76)]),-.04,.25,.37,.16,6)
    tube_y('Engine socket rim',s*.76,.84,.94,0,.155,.100,4,11,10)
    disk_y('Engine socket recess',s*.76,.855,0,.098,11,10)

# Single raised bridge with a broad, dark slit and a restrained cyan ice-glass strip.
plate('Bridge plinth',[(-.49,.69),(.49,.69),(.54,.30),(.37,.12),(-.37,.12),(-.54,.30)],.40,.52,.63,.13,6)
plate('Bridge roof',[(-.38,.66),(.38,.66),(.43,.34),(.31,.22),(-.31,.22),(-.43,.34)],.59,.64,.70,.14,15)
plate('Bridge visor surround',[(-.35,.28),(.35,.28),(.31,.16),(-.31,.16)],.62,.69,.705,.04,11)
plate('Bridge visor',[(-.26,.255),(.26,.255),(.24,.195),(-.24,.195)],.704,.706,.711,.02,9)

# Three massive recoil cradles, with actual hollow mouths ending at gameplay points.
for x, muzzle in ((-.78,-.98),(0,-1.06),(.78,-.98)):
    rear = -.15 if x else -.21
    plate('Gun cradle',[(x-.25,rear),(x+.25,rear),(x+.23,muzzle+.25),(x+.16,muzzle+.13),(x-.16,muzzle+.13),(x-.23,muzzle+.25)],.27,.43,.53,.12,6)
    plate('Recoil deck',[(x-.16,rear-.04),(x+.16,rear-.04),(x+.15,muzzle+.30),(x-.15,muzzle+.30)],.52,.55,.58,.10,1)
    # A short, wide gun with dark bore; the front annulus lies exactly at the muzzle.
    tube_y('Gun barrel',x,muzzle,muzzle+.30,.34,.135,.078,4,11,12)
    tube_y('Muzzle lip',x,muzzle,muzzle+.065,.34,.160,.078,15,11,12)
    disk_y('Muzzle bore',x,muzzle+.095,.34,.076,11,12)

# Join all authored parts, apply transforms, and export only the mesh.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
model.data.calc_loop_triangles()
assert 300 <= len(model.data.loop_triangles) <= 2500, 'Triangle budget exceeded: %d' % len(model.data.loop_triangles)
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Rendering setup. The cameras and lights are created after exporting.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name,kind)
    data.energy = energy
    if size is not None: data.shape='DISK'; data.size=size
    obj = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    point_at(obj,target)

light('Upper left sun','SUN',(-4,-5,8),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,5),180,(0,0,0),5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,7)
cam.rotation_euler=(0,0,0)
camdata.type='ORTHO'
camdata.ortho_scale=2.95
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam.location=(3.7,-5.2,4.8)
point_at(cam,(0,-.04,.22))
camdata.type='PERSP'
camdata.lens=50
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
