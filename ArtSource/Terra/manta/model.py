import bpy
import math
import os
import sys
from mathutils import Vector

# Blender 5.x: run with blender --background --python model.py [-- output_directory]
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path or output directory after --')
    fallback = os.path.abspath(args[args.index('--') + 1])
    OUT = fallback if os.path.isdir(fallback) else os.path.dirname(fallback)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.materials):
    bpy.data.materials.remove(block)

colors = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, c in enumerate(colors):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*c, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*c, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*c, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

def mesh(name, verts, faces, material, face_materials=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    if face_materials is None:
        data.materials.append(mats[material])
    else:
        for mat in mats:
            data.materials.append(mat)
        for poly, index in zip(data.polygons, face_materials):
            poly.material_index = index
    return obj

def plate(name, outline, z0, z1, material, bevel=0.0):
    n = len(outline)
    verts = [(x,y,z0) for x,y in outline] + [(x,y,z1) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    obj = mesh(name, verts, faces, material)
    if bevel:
        mod = obj.modifiers.new('Small machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

# Broad swept hydrofoils: the ridge and multiple height stations give each wing
# a real curved, faceted section instead of a flat silhouette extrusion.
for side in (-1, 1):
    def v(x, y, z): return (side*x, y, z)
    verts = [v(.12,-.55,.12), v(.40,-.34,.15), v(1.28,.34,.035),
             v(.87,.75,.065), v(.28,.44,.13), v(.12,.67,.12),
             v(.42,.09,.235), v(.80,.33,.145),
             v(.12,-.55,-.095), v(.40,-.34,-.095), v(1.28,.34,-.035),
             v(.87,.75,-.04), v(.28,.44,-.095), v(.12,.67,-.095)]
    faces = [(0,1,6), (1,2,7,6), (2,3,7), (3,4,6,7),
             (4,5,6), (5,0,6), (13,12,11,10,9,8),
             (0,8,9,1), (1,9,10,2), (2,10,11,3),
             (3,11,12,4), (4,12,13,5), (5,13,8,0)]
    obj = mesh('Faceted hydrofoil', verts, faces, 4,
               [4,4,6,8,4,8,11,0,0,0,0,0,0])
    # Winding reverses on the mirrored wing.
    if side == -1:
        for p in obj.data.polygons: p.flip()
    # One broad contrasting inset and a swept brass leading-edge accent.
    plate('Wing inset', [(side*.48,-.15),(side*1.10,.34),
                          (side*.86,.56),(side*.50,.36)], .145, .157, 8)
    plate('Leading edge', [(side*.40,-.34),(side*1.28,.34),
                           (side*1.18,.34),(side*.39,-.25)], .09, .125, 13)
    plate('Wingtip cap', [(side*1.10,.32),(side*1.28,.34),
                         (side*.87,.75),(side*.83,.64)], .07, .11, 6)

# Central body is a raised, faceted carapace with a pointed bow.
outline = [(0,-.94),(-.27,-.57),(-.35,-.18),(-.30,.38),
           (0,.73),(.30,.38),(.35,-.18),(.27,-.57)]
plate('Keel', outline, -.12, .13, 0, .035)
verts = [(0,-.88,.13),(-.27,-.54,.13),(-.30,-.12,.13),(-.25,.37,.13),
         (0,.66,.13),(.25,.37,.13),(.30,-.12,.13),(.27,-.54,.13),
         (0,-.57,.27),(-.17,-.26,.34),(-.16,.25,.32),(0,.48,.27),
         (.16,.25,.32),(.17,-.26,.34)]
faces = [(0,1,9,8),(1,2,9),(2,3,10,9),(3,4,11,10),
         (4,5,12,11),(5,6,13,12),(6,7,13),(7,0,8,13),
         (8,9,10,11,12,13)]
mesh('Armored carapace', verts, faces, 13, [13,13,6,6,6,6,13,13,4])
# Narrow center spine and a strong, simple forward chevron.
plate('Dorsal spine', [(-.055,-.48),(-.075,.29),(0,.47),(.075,.29),(.055,-.48),(0,-.68)], .335,.355, 13)
for side in (-1,1):
    plate('Bow marking', [(side*.06,-.68),(side*.22,-.49),
                          (side*.22,-.40),(side*.06,-.57)], .15,.17, 6)

# The three mouths are centered exactly on the gameplay muzzle coordinates.
# Their open rims lie in the x-z plane at y=-0.2 and face toward -Y.
def launcher(x):
    y = -.2
    z = .245
    count = 10
    outer = .075
    inner = .043
    back_y = y + .17
    verts = []
    for yy, radius in ((y,outer),(y,inner),(back_y,outer),(y+.025,inner)):
        for i in range(count):
            a = 2*math.pi*i/count
            verts.append((x+radius*math.cos(a), yy, z+radius*math.sin(a)))
    faces = []
    indices = []
    for i in range(count):
        j = (i+1)%count
        faces.extend([(i,j,count+j,count+i), (i,2*count+i,2*count+j,j),
                      (count+i,count+j,3*count+j,3*count+i)])
        indices.extend([13,0,11])
    faces.append(tuple(range(3*count,4*count)))
    indices.append(11)
    mesh('Launcher mouth', verts, faces, 13, indices)
    # Tiny recessed amber core leaves the dark bore and brass lip legible.
    verts2 = [(x,y+.027,z)] + [(x+.018*math.cos(2*math.pi*i/count),y+.027,
                                 z+.018*math.sin(2*math.pi*i/count)) for i in range(count)]
    mesh('Recessed amber core', verts2,
         [(0,i+1,(i+1)%count+1) for i in range(count)], 10)
for x in (-.65, 0, .65):
    launcher(x)

# A tapered tail retains the original 1.475-unit aft reach; the forked
# terminal fin makes it read as a hydrofoil rather than a stretched rod.
plate('Tail root', [(-.14,.53),(-.09,1.02),(-.055,1.25),
                    (.055,1.25),(.09,1.02),(.14,.53)], .02,.14, 6, .015)
plate('Tail fin', [(-.055,1.10),(-.24,1.32),(-.12,1.31),(0,1.475),
                   (.12,1.31),(.24,1.32),(.055,1.10)], .035,.095, 8)

# Consolidate the model and bake all transforms and triangulation before export.
bpy.ops.object.select_all(action='SELECT')
bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for polygon in model.data.polygons:
    polygon.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
                      export_selected_objects=True, export_materials=True,
                      forward_axis='Y', up_axis='Z')

# Render setup comes after export, so only the mesh is included in the OBJ.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None: data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj
light('Upper left sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,2,5), 180, (0,0,0), 5.0)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
camdata.type = 'ORTHO'
camdata.ortho_scale = 3.05
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (3.4,-4.6,3.7)
aim(cam, (0,.15,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
