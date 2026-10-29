import bpy
import math
import os
import sys
from mathutils import Vector

# Resolve output beside this script, including Blender's -- script-path convention.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
           (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
           (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
           (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
           (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),
           (0.80,0.84,0.83)]
materials = []
for i, rgb in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Roughness'].default_value = 0.64
    bsdf.inputs['Metallic'].default_value = 0.18 if i not in (9, 10) else 0.0
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.6
    materials.append(mat)

parts = []
def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(materials[mat])
    parts.append(obj)
    return obj

def layered(name, outline, inset, zbottom, zedge, ztop, mat):
    # Two polygon rings form a sloped, faceted bevel around a raised top.
    n = len(outline)
    verts = [(x,y,zbottom) for x,y in outline] + [(x,y,zedge) for x,y in outline] + [(x,y,ztop) for x,y in inset]
    faces = [tuple(reversed(range(n)))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    faces += [(i+n,(i+1)%n+n,(i+1)%n+2*n,i+2*n) for i in range(n)]
    faces += [tuple(range(2*n,3*n))]
    return mesh(name, verts, faces, mat)

def ring_y(name, x, yfront, z, radius, length, inner, mat, n=8, direction=-1):
    # Open annular mouth with a dark recessed bore; opening plane is exactly yfront.
    verts=[]
    for y, r in [(yfront, radius), (yfront-direction*length, radius*0.9),
                 (yfront, inner), (yfront-direction*length*0.62, inner*0.8)]:
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i), (i,2*n+i,2*n+j,j),
                      (2*n+i,3*n+i,3*n+j,2*n+j)])
    obj=mesh(name,verts,faces,mat)
    obj.data.materials.append(materials[11])
    for p in obj.data.polygons:
        if p.index % 3 == 2: p.material_index=1
    # A dark back surface makes the recess legible from a shallow angle.
    vs=[(x+inner*.8*math.cos(2*math.pi*i/n), yfront-direction*length*.62,
         z+inner*.8*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh(name+' bore',vs,[tuple(range(n))],11)

def mirrored(points, s):
    result=[(s*x,y) for x,y in points]
    return result if s == 1 else list(reversed(result))

# Long central spear and raised armored crown.
layered('keel', [(-.17,-1.12),(.17,-1.12),(.24,-.35),(.20,.55),(0,.76),(-.20,.55),(-.24,-.35)],
        [(-.10,-.97),(.10,-.97),(.16,-.32),(.13,.49),(0,.63),(-.13,.49),(-.16,-.32)],
        -.16,.10,.27,7)
layered('spine', [(-.13,-.74),(.13,-.74),(.16,-.25),(.12,.43),(0,.58),(-.12,.43),(-.16,-.25)],
        [(-.075,-.66),(.075,-.66),(.095,-.23),(.07,.37),(0,.46),(-.07,.37),(-.095,-.23)],
        .245,.27,.34,4)

for s in (-1,1):
    # The swept shoulders leave a deliberate dark gap beside the narrow waist.
    outline=[(.17,.27),(.86,.69),(1.02,.30),(.68,-.37),(.45,-.54),(.50,.10)]
    inset=[(.27,.29),(.82,.57),(.91,.28),(.63,-.29),(.51,-.40),(.57,.12)]
    layered('swept shoulder',mirrored(outline,s),mirrored(inset,s),-.16,.06,.20,7)
    outline=[(.51,.35),(.80,.44),(.70,-.15),(.51,-.24)]
    inset=[(.56,.30),(.74,.36),(.66,-.11),(.56,-.16)]
    layered('capacitor armor',mirrored(outline,s),mirrored(inset,s),.19,.22,.30,6)
    # Recessed, short cyan capacitor window, framed by the armor.
    outline=[(.595,.24),(.665,.26),(.625,-.10),(.565,-.12)]
    inset=[(.605,.21),(.65,.22),(.615,-.07),(.58,-.08)]
    layered('capacitor window',mirrored(outline,s),mirrored(inset,s),.295,.302,.31,9)
    # Rear motor nacelle, with the socket mouth at the fixed gameplay coordinate.
    outline=[(.29,.24),(.52,.23),(.57,.69),(.50,.81),(.34,.81),(.27,.67)]
    inset=[(.33,.30),(.48,.29),(.52,.66),(.48,.75),(.36,.75),(.32,.65)]
    layered('motor nacelle',mirrored(outline,s),mirrored(inset,s),-.13,.10,.21,4)
    ring_y('engine socket',s*.42,.84,0,.115,.17,.068,6,direction=1)
    # Side gun tips terminate exactly at the gameplay muzzle points.
    ring_y('side emitter',s*.46,-.995,.20,.075,.23,.045,6)
    outline=[(.39,-.42),(.53,-.42),(.52,-.85),(.40,-.85)]
    inset=[(.41,-.46),(.51,-.46),(.50,-.81),(.42,-.81)]
    layered('gun fairing',mirrored(outline,s),mirrored(inset,s),.045,.15,.22,7)

ring_y('center emitter',0,-1.24,.20,.085,.25,.05,6)
# Faceted relay sensor: a dark bezel and small cyan inset, not a projectile.
layered('sensor bezel',[(-.14,-.20),(.14,-.20),(.16,.20),(0,.34),(-.16,.20)],
        [(-.09,-.13),(.09,-.13),(.10,.16),(0,.25),(-.10,.16)],.32,.34,.39,11)
layered('sensor lens',[(-.075,-.10),(.075,-.10),(.08,.14),(0,.21),(-.08,.14)],
        [(-.05,-.07),(.05,-.07),(.055,.12),(0,.17),(-.055,.12)],.39,.395,.41,9)

# Join and bake transforms; preserve hard face normals.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for polygon in model.data.polygons:
    polygon.use_smooth = False
# Export only the model, before adding presentation objects.
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
scene.render.film_transparent = False
world = bpy.data.worlds.new('Neutral blue gray') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.05,0.07,0.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0

def point(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data=bpy.data.lights.new(name, kind)
    data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    point(obj,target)

light('Upper left sun','SUN',(-4, -4, 7),2.1,(0,0,0))
light('Soft fill','AREA',(4,1,5),180,(0,0,0),5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam_data.type='ORTHO'
cam_data.ortho_scale=2.48
cam.location=(0,-.20,6)
cam.rotation_euler=(0,0,0)
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type='PERSP'
cam_data.lens=48
cam.location=(3.6,-5.0,4.2)
point(cam,(0,-.17,.05))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
