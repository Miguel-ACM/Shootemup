import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Shield transport: dimensions are in the source hull's local units.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.isfile(OUT):
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
    for block in list(datablocks):
        if block.users == 0:
            datablocks.remove(block)

palette = [
    (0.24,0.35,0.40), (0.78,0.83,0.79), (0.35,0.43,0.30),
    (0.95,0.38,0.12), (0.32,0.48,0.55), (0.54,0.18,0.16),
    (0.53,0.61,0.62), (0.34,0.31,0.48), (0.20,0.36,0.40),
    (0.16,0.83,0.91), (1.0,0.77,0.27), (0.055,0.09,0.12),
    (0.62,0.25,0.19), (0.82,0.64,0.26), (0.53,0.61,0.55),
    (0.80,0.84,0.83),
]
mats = []
for i, color in enumerate(palette):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.7
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.7
    mats.append(m)

parts = []
def mesh(name, verts, faces, tile):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    # Recalculate each closed component's face orientation consistently.
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mats[tile])
    parts.append(obj)
    return obj

def armor(name, outline, bottom, shoulder, top, inset, tile):
    # Three rings make a sloping, faceted crown rather than a flat extrusion.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,shoulder) for x,y in outline]
    verts += [(cx+(x-cx)*inset, cy+(y-cy)*inset, top) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    for ring in range(2):
        for i in range(n):
            j = (i+1)%n
            faces.append((ring*n+i, ring*n+j, (ring+1)*n+j, (ring+1)*n+i))
    return mesh(name, verts, faces, tile)

def arc(name, r0, r1, a0, a1, z0, z1, tile, steps):
    verts = []
    for z in (z0, z1):
        for radius in (r0, r1):
            for i in range(steps+1):
                a = math.radians(a0+(a1-a0)*i/steps)
                verts.append((radius*math.cos(a), radius*math.sin(a), z))
    n = steps+1
    faces = []
    for i in range(steps):
        j = i+1
        faces.extend([(i,j,n+j,n+i), (2*n+i,3*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j,j), (n+i,n+j,3*n+j,3*n+i)])
    faces += [(0,n,3*n,2*n), (steps,2*n+steps,3*n+steps,n+steps)]
    return mesh(name, verts, faces, tile)

def tube(name, x, y_front, y_back, z, outer, inner, tile, segments=10, socket=False):
    # A real annular mouth and recessed dark bore, opening in the Y direction.
    verts = []
    for y,r in ((y_front,outer),(y_front,inner),(y_back,outer),(y_back,inner)):
        for i in range(segments):
            a = 2*math.pi*i/segments
            verts.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    faces = []
    for i in range(segments):
        j = (i+1)%segments
        faces.extend([(i,j,segments+j,segments+i), (i,2*segments+i,2*segments+j,j),
                      (segments+i,segments+j,3*segments+j,3*segments+i),
                      (2*segments+i,3*segments+i,3*segments+j,2*segments+j)])
    mesh(name, verts, faces, tile)
    # Dark inset is behind the lip, preserving a visibly open mouth.
    y = y_front + (y_back-y_front)*0.72
    disc = [(x,y,z)] + [(x+inner*math.cos(2*math.pi*i/segments),y,z+inner*math.sin(2*math.pi*i/segments)) for i in range(segments)]
    mesh(name+' bore', disc, [(0,i+1,(i+1)%segments+1) for i in range(segments)], 11)

# Heavy crescent shield, retaining the original 2.64 by ~2.32 footprint.
arc('Shield dark structural rim', .72, 1.32, 183, 357, -.17, .17, 11, 16)
# Three separated pale ceramic shield segments, with dark joints between them.
for a,b in ((188,242),(247,293),(298,352)):
    arc('Shield ceramic armor', .88, 1.30, a, b, .17, .255, 1, max(3,round((b-a)/10)))
# A thin, subdued cyan shield conductor, as in the original.
arc('Shield field strip', .985, 1.025, 194, 346, .257, .268, 9, 16)
arc('Shield outer steel lip', 1.265, 1.32, 188, 352, .255, .29, 6, 16)

# Central chassis and upper bridge.
armor('Central chassis', [(-.36,.80),(.36,.80),(.48,.04),(.24,-.46),(-.24,-.46),(-.48,.04)], -.20,.12,.30,.78,4)
armor('Bridge casing', [(-.24,.12),(-.17,-.35),(.17,-.35),(.24,.12),(.16,.27),(-.16,.27)], .28,.34,.44,.76,11)
armor('Bridge glazing', [(-.145,.09),(-.105,-.24),(.105,-.24),(.145,.09),(0,.18)], .435,.445,.47,.89,8)
# Small cyan core rather than a large luminous window.
armor('Shield core', [(-.065,.04),(0,-.06),(.065,.04),(0,.12)], .47,.475,.485,.88,9)
armor('Rear intake rim', [(-.19,.37),(-.15,.62),(.15,.62),(.19,.37),(.13,.30),(-.13,.30)], .28,.32,.36,.82,6)
armor('Rear intake recess', [(-.12,.39),(-.10,.56),(.10,.56),(.12,.39)], .36,.365,.37,.95,11)

for s in (-1,1):
    def mirrored(points):
        return [(s*x,y) for x,y in points]
    armor('Wing brace', mirrored([(.28,.22),(.96,-.04),(1.04,-.40),(.70,-.52),(.42,-.19)]), -.13,.10,.22,.8,1)
    armor('Brace inset', mirrored([(.47,.02),(.91,-.12),(.94,-.32),(.73,-.39),(.55,-.17)]), .22,.23,.26,.86,6)
    armor('Motor pod', mirrored([(.39,.35),(.71,.35),(.75,.76),(.69,.93),(.41,.93),(.35,.76)]), -.18,.13,.26,.78,1)
    armor('Motor top cap', mirrored([(.42,.45),(.68,.45),(.69,.72),(.64,.82),(.46,.82),(.41,.72)]), .26,.275,.31,.84,4)
    # A short black and brass hazard marking on the shoulder.
    armor('Hazard ground', mirrored([(.76,-.13),(.91,-.18),(.92,-.29),(.77,-.24)]), .26,.265,.27,.98,13)
    armor('Hazard slash', mirrored([(.79,-.15),(.83,-.16),(.88,-.27),(.84,-.26)]), .27,.273,.277,.98,11)
    # Muzzle front face is exactly at the gameplay point, with center z = +0.2.
    tube('Weapon muzzle', s*.55, -.65, -.48, .20, .105, .060, 6)
    # Nozzle front face is exactly at the gameplay socket; cavity recesses forward into the pod.
    tube('Engine socket', s*.55, .99, .82, 0.0, .145, .095, 6, 12, True)

# Export exactly one flat-shaded mesh with applied transforms and no modifiers.
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
triangles = len(model.data.loop_triangles)
assert 300 <= triangles <= 2500, 'Unexpected triangle count: %d' % triangles
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Rendering setup. Cameras and lights are created only after the mesh export.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
world = bpy.data.worlds.new('Dark blue-gray world') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name,kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj,target)
    return obj

light('Upper left sun','SUN',(-4,-3,6),2.0,(0,0,0))
light('Soft fill','AREA',(4,1,5),150,(0,0,0),5.0)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
camdata.type = 'ORTHO'
camdata.ortho_scale = 3.15
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
scene.render.filepath = os.path.join(OUT,'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 52
cam.location = (3.7,-5.3,4.3)
aim(cam,(0,-.10,.05))
scene.render.filepath = os.path.join(OUT,'angle.png')
bpy.ops.render.render(write_still=True)
print('Shield transport exported; triangles:', triangles)
