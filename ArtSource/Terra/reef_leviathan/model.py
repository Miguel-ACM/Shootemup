import bpy
import math
import os
import sys
from mathutils import Vector

# The game uses X right, Y up-screen, Z up; the head faces -Y.
def script_directory():
    path = globals().get('__file__')
    if path:
        return os.path.dirname(os.path.abspath(path))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            path = os.path.abspath(args[0])
            return path if os.path.isdir(path) else os.path.dirname(path)
    return os.getcwd()

OUT = script_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablocks in (bpy.data.materials, bpy.data.meshes, bpy.data.cameras, bpy.data.lights):
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
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.63
    bsdf.inputs['Metallic'].default_value = 0.25 if i in (4, 6, 8, 13) else 0.08
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(mat)

parts = []
def mesh(name, verts, faces, material, face_materials=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.clear()
    for m in mats:
        data.materials.append(m)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    for idx, poly in enumerate(data.polygons):
        poly.material_index = face_materials[idx] if face_materials else material
        poly.use_smooth = False
    parts.append(obj)
    return obj

# Hull cross sections, ordered from snout to tail. Eight facets give a
# substantial curved body without making the top view into a flat outline.
def hull():
    rings = [(-1.57, .16, .07, .16), (-1.37, .49, .12, .29),
             (-1.05, .77, .13, .40), (-.66, .96, .12, .48),
             (-.18, 1.06, .10, .53), (.30, 1.04, .09, .52),
             (.69, .88, .08, .43), (1.02, .57, .06, .31),
             (1.23, .25, .05, .19)]
    verts = []
    for y, w, mid, h in rings:
        for k in range(10):
            a = 2 * math.pi * k / 10
            verts.append((w * math.cos(a), y, mid + h * math.sin(a)))
    faces = [tuple(range(9, -1, -1))]
    ids = [11]
    for j in range(len(rings)-1):
        for k in range(10):
            faces.append((j*10+k, j*10+(k+1)%10, (j+1)*10+(k+1)%10, (j+1)*10+k))
            ids.append(8 if k in (0, 1, 2, 3, 4) else 0)
    faces.append(tuple((len(rings)-1)*10+k for k in range(10)))
    ids.append(0)
    mesh('Faceted pressure hull', verts, faces, 8, ids)

# A convex beveled armor plate: narrow dark side wall, sloping rim, raised face.
def plate(name, outline, z, rise, material, rim=0.075, side=11):
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    verts = [(x,y,z) for x,y in outline]
    verts += [(cx+(x-cx)*(1-rim), cy+(y-cy)*(1-rim), z+rise) for x,y in outline]
    faces = []
    ids = []
    for i in range(n):
        faces.append((i,(i+1)%n,(i+1)%n+n,i+n))
        ids.append(side)
    faces.append(tuple(range(n, 2*n)))
    ids.append(material)
    return mesh(name, verts, faces, material, ids)

hull()
# Heavy swept pectoral fins, held within the old side armor's width.
for s in (-1, 1):
    plate('Swept pectoral fin', [(s*.76,-.23),(s*1.16,-.43),(s*1.18,.36),
          (s*.98,.67),(s*.70,.49)], .17, .10, 4, .13, 0)
    plate('Fin inset', [(s*.91,-.17),(s*1.10,-.28),(s*1.10,.29),
          (s*.96,.46),(s*.89,.32)], .282, .025, 13, .12, 8)
    # Side cheek shields and the broad jaw make the head unmistakably forward-facing.
    plate('Cheek armor', [(s*.25,-1.36),(s*.55,-1.27),(s*.83,-.97),
          (s*.85,-.72),(s*.56,-.70),(s*.32,-.95)], .33, .085, 6, .09, 0)
    plate('Eye socket', [(s*.38,-1.12),(s*.58,-1.08),(s*.68,-.94),
          (s*.47,-.96)], .425, .025, 11, .10, 11)
    plate('Amber eye', [(s*.43,-1.088),(s*.56,-1.063),(s*.60,-1.005),
          (s*.46,-1.020)], .456, .008, 10, .04, 10)

plate('Armored snout', [(-.25,-1.54),(.25,-1.54),(.48,-1.30),(.40,-1.11),
      (0,-1.02),(-.40,-1.11),(-.48,-1.30)], .25, .10, 4, .10, 0)
plate('Central brow', [(-.23,-1.26),(0,-1.43),(.23,-1.26),(.19,-.93),
      (0,-.80),(-.19,-.93)], .385, .08, 13, .12, 8)

# Three large overlapping dorsal scutes, with a strong central ridge.
for y, w, length, z in [(-.57,.64,.53,.51), (-.06,.75,.57,.59), (.48,.65,.55,.56), (.87,.43,.38,.43)]:
    outline = [(-w*.72,y-length*.48),(0,y-length*.58),(w*.72,y-length*.48),
               (w,y+length*.18),(w*.67,y+length*.48),(0,y+length*.56),
               (-w*.67,y+length*.48),(-w,y+length*.18)]
    plate('Dorsal scute', outline, z, .045, 4 if y < .4 else 6, .10, 11)
    plate('Scute crest', [(-.075,y-length*.40),(0,y-length*.49),(.075,y-length*.40),
          (.085,y+length*.35),(0,y+length*.45),(-.085,y+length*.35)],
          z+.05, .025, 13, .10, 8)

# Forked armored tail is a silhouette cue, not an engine.
for s in (-1, 1):
    plate('Tail fluke', [(0,.94),(s*.25,1.02),(s*.67,1.27),(s*.62,1.05),
          (s*.38,.82),(s*.17,.80)], .16, .055, 8, .10, 0)
    plate('Tail edge', [(s*.23,.99),(s*.62,1.24),(s*.55,1.08),(s*.32,.91)],
          .22, .018, 13, .10, 8)

# Each mouth is an actual annular opening in a horizontal barrel. The front
# annulus is exactly at its gameplay muzzle coordinate; its bore is recessed.
def launcher(x):
    z = .39
    n = 10
    verts = []
    # Front lip outer, front lip inner, recessed bore and rear barrel ring.
    for y, radius in [(-.70,.145),(-.70,.088),(-.61,.088),(-.38,.115)]:
        for k in range(n):
            a = 2*math.pi*k/n
            verts.append((x+radius*math.cos(a), y, z+radius*math.sin(a)))
    faces, ids = [], []
    for k in range(n):
        q=(k+1)%n
        faces.extend([(k,q,n+q,n+k), (n+k,n+q,2*n+q,2*n+k),
                      (k,3*n+k,3*n+q,q)])
        ids.extend([13,11,4])
    faces.append(tuple(2*n+k for k in range(n)))
    ids.append(11)
    mesh('Launcher mouth', verts, faces, 13, ids)
    # Small amber core, recessed behind the dark bore.
    r=.035
    verts2=[(x+r*math.cos(2*math.pi*k/n),-.612,z+r*math.sin(2*math.pi*k/n)) for k in range(n)]
    mesh('Launcher ember', verts2, [tuple(range(n))], 10)
for x in (-.82,.82):
    launcher(x)

# Combine the geometry; authored vertices are already in world coordinates.
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
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Lighting and cameras are created after export so the OBJ contains only Model.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Deep blue-gray world') if scene.world is None else scene.world
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get('Background')
bg.inputs['Color'].default_value = (.05,.07,.09,1)
bg.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'AgX'

def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, typ, location, energy, target, size=None):
    data = bpy.data.lights.new(name, typ)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

sun = light('Upper left sun', 'SUN', (-4, 5, 8), 2.0, (0,0,0))
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (4,-3,5), 350, (0,0,0), 5.0)

def camera(name, location, target, ortho=False):
    data = bpy.data.cameras.new(name)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    if ortho:
        data.type = 'ORTHO'
        data.ortho_scale = 3.45
        # Set camera up to +Y so the nose points down in the render.
        obj.rotation_euler = (0,0,0)
    else:
        data.type = 'PERSP'
        data.lens = 50
    return obj

scene.camera = camera('Top camera', (0,0,8), (0,0,0), True)
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
scene.camera = camera('Front right camera', (4.4,-6.2,5.4), (0,-.05,.12))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
