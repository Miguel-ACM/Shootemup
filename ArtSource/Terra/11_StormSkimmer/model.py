import bpy
import math
import os
import sys
from mathutils import Vector

# Storm Skimmer: pointed spine, swept storm wings and twin capacitor pods.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if OUT.lower().endswith('.py'):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(0.24,0.35,0.40),(0.78,0.83,0.79),(0.35,0.43,0.30),
          (0.95,0.38,0.12),(0.32,0.48,0.55),(0.54,0.18,0.16),
          (0.53,0.61,0.62),(0.34,0.31,0.48),(0.20,0.36,0.40),
          (0.16,0.83,0.91),(1.0,0.77,0.27),(0.055,0.09,0.12),
          (0.62,0.25,0.19),(0.82,0.64,0.26),(0.53,0.61,0.55),(0.80,0.84,0.83)]
mats = []
for i, color in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(mat)

parts = []
def mesh(name, verts, faces, material, face_materials=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        data.materials.append(m)
    for i, poly in enumerate(data.polygons):
        poly.material_index = face_materials[i] if face_materials else material
        poly.use_smooth = False
    parts.append(obj)
    return obj

def plate(name, outline, bottom, shoulder, top, inset, color, topcolor=None):
    # Three contours give the armor a broad sloped bevel and a raised flat crown.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,shoulder) for x,y in outline]
    verts += [(cx + (x-cx)*inset, cy + (y-cy)*inset, top) for x,y in outline]
    faces = [tuple(reversed(range(n)))]
    indices = [11]
    for layer in range(2):
        for i in range(n):
            j = (i+1) % n
            faces.append((layer*n+i, layer*n+j, (layer+1)*n+j, (layer+1)*n+i))
            indices.append(color if layer == 0 else (4 if color == 7 else color))
    faces.append(tuple(range(2*n, 3*n)))
    indices.append(color if topcolor is None else topcolor)
    return mesh(name, verts, faces, color, indices)

def ring_y(name, x, y, z, outer, inner, depth, mat, sides=8):
    # Annular end face with a recessed, dark bore; the visible rim is exactly at y.
    verts = []
    for yy, radius in [(y+depth, outer*.88), (y, outer), (y, inner), (y+depth*.7, inner)]:
        for i in range(sides):
            a = 2*math.pi*i/sides + math.pi/8
            verts.append((x+radius*math.cos(a), yy, z+radius*math.sin(a)))
    faces=[]; ids=[]
    for layer in range(3):
        for i in range(sides):
            j=(i+1)%sides
            faces.append((layer*sides+i,layer*sides+j,(layer+1)*sides+j,(layer+1)*sides+i))
            ids.append(mat if layer < 2 else 11)
    faces.append(tuple(range(3*sides,4*sides))); ids.append(11)
    return mesh(name, verts, faces, mat, ids)

def disk(name, x, y, z, radius, depth, material, vertices=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=(x,y,z))
    obj=bpy.context.object
    obj.name=name
    obj.data.materials.append(mats[material])
    for p in obj.data.polygons: p.use_smooth=False
    parts.append(obj)
    return obj

# Swept angular wing plan is kept close to the original 2.2 by 1.9 footprint.
plate('keel', [(0,-1.15),(.23,-.30),(.25,.42),(.18,.60),(-.18,.60),(-.25,.42),(-.23,-.30)], -.10,.10,.23,.78,7)
for s in (-1,1):
    def mirror(points):
        q=[(s*x,y) for x,y in points]
        return q if s == 1 else list(reversed(q))
    plate('swept wing', mirror([(.17,-.35),(.92,-.07),(1.10,.33),(.67,.22),(.39,.66),(.26,.32)]), -.09,.075,.16,.84,7)
    plate('wing armor', mirror([(.30,-.27),(.89,-.035),(1.00,.22),(.69,.13),(.43,.42)]), .13,.16,.205,.88,4,6)
    plate('wing inset', mirror([(.47,-.08),(.82,.045),(.70,.095)]), .205,.209,.225,.90,11,11)
    plate('capacitor fairing', mirror([(.32,-.16),(.51,-.16),(.55,.48),(.50,.68),(.34,.68),(.29,.48)]), -.08,.14,.265,.80,7)
    # Each short forward-facing gun ends at the gameplay muzzle coordinate.
    plate('gun root', mirror([(.34,-.38),(.50,-.38),(.51,-.61),(.33,-.61)]), .08,.16,.23,.83,4)
    ring_y('beam emitter lip',s*.42,-.70,.20,.095,.054,.16,6)
    # Rear-facing opening at the fixed thruster socket, with a dark inset bore.
    ring_y('engine socket',s*.42,.73,0,.105,.065,-.12,6)
    # Concentric exposed capacitor hardware is prominent but compact from above.
    disk('capacitor collar',s*.42,.30,.282,.125,.05,11,10)
    disk('capacitor casing',s*.42,.30,.315,.101,.045,6,10)
    disk('capacitor core',s*.42,.30,.343,.046,.012,9,8)
    plate('wing flash', mirror([(.65,.025),(.79,.065),(.77,.10),(.63,.06)]), .229,.231,.238,.95,9,9)

# Armored canopy: a dark faceted housing with a steel-blue crown and tiny cyan sensor.
plate('canopy surround',[(-.13,-.47),(.13,-.47),(.17,.04),(.10,.20),(0,.26),(-.10,.20),(-.17,.04)], .22,.26,.36,.78,11)
plate('canopy', [(-.095,-.36),(.095,-.36),(.12,.02),(0,.15),(-.12,.02)], .355,.36,.41,.78,4,8)
plate('nose ridge',[(-.065,-.96),(.065,-.96),(.11,-.52),(-.11,-.52)], .17,.22,.26,.75,6)
plate('sensor',[(-.042,-.78),(.042,-.78),(.035,-.69),(-.035,-.69)], .263,.265,.27,.95,9,9)
plate('rear dorsal inlet',[(-.12,.28),(.12,.28),(.12,.49),(-.12,.49)], .22,.25,.29,.83,6,11)

# Join before creating any presentation objects, and bake all object transforms.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object
model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons: p.use_smooth=False
# Export only the selected model. Blender's Y-forward/Z-up preserves the requested axes.
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
scene.world=bpy.data.worlds.new('Studio world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes=True
bg=scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value=(.05,.07,.09,1)
bg.inputs['Strength'].default_value=1.0
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
light('Upper left sun','SUN',(-3,-4,7),2.0,(0,0,0))
light('Soft fill','AREA',(3,1,5),180,(0,0,0),5)
cam_data=bpy.data.cameras.new('Camera')
cam=bpy.data.objects.new('Camera',cam_data)
bpy.context.collection.objects.link(cam)
scene.camera=cam
cam.location=(0,0,6)
cam.rotation_euler=(0,0,0)
cam_data.type='ORTHO'
cam_data.ortho_scale=2.60
scene.render.filepath=os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type='PERSP'
cam_data.lens=50
cam.location=(3.3,-4.3,4.0)
aim(cam,(0,-.05,.1))
scene.render.filepath=os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
