import bpy
import math
import os
import sys
from mathutils import Vector

# Blender can execute scripts without defining __file__ in some launch modes.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    OUT = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(OUT)[1]:
        OUT = os.path.dirname(OUT)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

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
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Roughness'].default_value = 0.75
    if i in (9, 10):
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = 2.0
    mats.append(m)

parts = []
def mesh(name, verts, faces, material, face_mats=None):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    for m in mats:
        data.materials.append(m)
    for p in data.polygons:
        p.material_index = material if face_mats is None else face_mats[p.index]
        p.use_smooth = False
    parts.append(ob)
    return ob

def layered(name, outline, inset, bottom, shoulder, top, material, topmat=None):
    # Two matching polygon rings give the armor a broad sloped bevel.
    n = len(outline)
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,shoulder) for x,y in outline] + [(x,y,top) for x,y in inset]
    faces = [tuple(reversed(range(n)))]
    indices = [11]
    for j in range(n):
        k = (j+1) % n
        faces.append((j,k,n+k,n+j)); indices.append(material)
        faces.append((n+j,n+k,2*n+k,2*n+j)); indices.append(material)
    faces.append(tuple(range(2*n,3*n))); indices.append(material if topmat is None else topmat)
    return mesh(name, verts, faces, material, indices)

def box(name, loc, scale, mat, bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ob.data.materials.append(mats[mat])
    if bevel:
        mod = ob.modifiers.new('Edge bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(ob)
    return ob

def tube_y(name, x, face_y, z, radius, inner, depth, direction, material):
    # An annulus and recessed dark back make a genuine open mouth. face_y is
    # exactly the gameplay muzzle/socket plane; direction points into the craft.
    n = 10
    verts = []
    for y, r in [(face_y, radius), (face_y, inner),
                 (face_y + direction * depth, radius * 0.90),
                 (face_y + direction * depth * 0.65, inner * 0.88)]:
        for j in range(n):
            a = 2*math.pi*j/n
            verts.append((x + r*math.cos(a), y, z + r*math.sin(a)))
    faces, idx = [], []
    for j in range(n):
        k = (j+1)%n
        faces.extend([(j,k,n+k,n+j), (j,2*n+j,2*n+k,k),
                      (n+j,n+k,3*n+k,3*n+j)])
        idx.extend([material, material, 11])
    faces.append(tuple(3*n+j for j in range(n))); idx.append(11)
    return mesh(name, verts, faces, material, idx)

# Broad shoulder spar and the distinctive, short central fuselage.
box('Cross spar', (0, 0.12, -0.01), (1.50, 0.29, 0.19), 11, 0.035)
layered('Main fuselage', [(-.34,.56),(-.43,-.37),(-.23,-.79),(.23,-.79),(.43,-.37),(.34,.56)],
        [(-.26,.48),(-.34,-.34),(-.18,-.69),(.18,-.69),(.34,-.34),(.26,.48)],
        -.24, .11, .25, 0, 2)
# A small nose shield gives the center a pointed, readable front.
layered('Nose shield', [(-.23,-.48),(-.18,-.79),(0,-.88),(.18,-.79),(.23,-.48)],
        [(-.16,-.49),(-.13,-.74),(0,-.80),(.13,-.74),(.16,-.49)],
        .16,.22,.29, 0, 6)

for side in (-1, 1):
    def mirror(points):
        pts = [(side*x,y) for x,y in points]
        return list(reversed(pts)) if side < 0 else pts
    # Outboard pods carry the guns and engines, with a stepped armor crown.
    layered('Armored side pod', mirror([(.47,.70),(.81,.65),(.85,-.50),(.74,-.78),(.64,-.86),(.47,-.55)]),
            mirror([(.52,.62),(.76,.57),(.79,-.46),(.70,-.69),(.64,-.76),(.53,-.51)]),
            -.20,.10,.20, 0, 2)
    layered('Pod shoulder plate', mirror([(.52,.49),(.77,.45),(.78,-.27),(.53,-.35)]),
            mirror([(.56,.43),(.72,.40),(.73,-.23),(.57,-.28)]),
            .18,.22,.27, 0, 4)
    # Deep intake, framed by two angled cheeks and a contrasting inset.
    layered('Intake rim', mirror([(.53,.26),(.76,.27),(.76,-.10),(.53,-.16)]),
            mirror([(.56,.23),(.73,.23),(.73,-.08),(.56,-.12)]),
            .25,.28,.30, 6, 11)
    box('Intake blade', (side*.645, .065, .305), (.15,.035,.018), 4)
    # Short gun sleeves end exactly at the gameplay positions.
    box('Gun housing', (side*.64,-.76,.16), (.22,.32,.18), 0, .035)
    tube_y('Gun muzzle', side*.64, -1.0, .20, .083, .052, .19, 1, 6)
    # Nozzle annulus lies at the engine socket plane, with a dark recess.
    tube_y('Engine nozzle', side*.64, .93, 0, .135, .094, .23, -1, 6)
    # One broad, restrained wing flash per side.
    layered('Wing marking', mirror([(.70,.39),(.76,.38),(.77,.18),(.71,.18)]),
            mirror([(.70,.39),(.76,.38),(.77,.18),(.71,.18)]),
            .276,.279,.282, 14)

# Raised faceted cockpit; opaque slate-blue glass and strong armored framing.
layered('Cockpit base', [(-.27,-.59),(-.25,-.18),(-.17,-.05),(.17,-.05),(.25,-.18),(.27,-.59)],
        [(-.20,-.53),(-.19,-.21),(-.12,-.12),(.12,-.12),(.19,-.21),(.20,-.53)],
        .21,.29,.37, 11, 4)
layered('Canopy glazing', [(-.18,-.50),(-.17,-.23),(-.10,-.15),(.10,-.15),(.17,-.23),(.18,-.50)],
        [(-.13,-.46),(-.12,-.25),(-.07,-.20),(.07,-.20),(.12,-.25),(.13,-.46)],
        .365,.39,.44, 8, 4)
box('Canopy transverse frame', (0,-.28,.442), (.29,.035,.018), 6)
layered('Dorsal armor', [(-.23,.43),(-.23,.02),(.23,.02),(.23,.43)],
        [(-.18,.38),(-.18,.08),(.18,.08),(.18,.38)], .23,.29,.34, 0, 6)
box('Dorsal identification stripe', (0,.25,.347), (.22,.052,.012), 14)

# Join and bake geometry; only this object is included in the OBJ.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
model.data.calc_loop_triangles()
triangles = len(model.data.loop_triangles)
assert 300 <= triangles <= 2500, 'Unexpected triangle count: %s' % triangles
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
scene.world = bpy.data.worlds.new('Studio world') if scene.world is None else scene.world
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(ob, target):
    ob.rotation_euler = (Vector(target) - ob.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    aim(ob, target)
    return ob

light('Sun upper left', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 240, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,6)
cam.rotation_euler = (0,0,0)
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.30
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (3.2,-4.2,3.6)
aim(cam, (0,0,0.02))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
