import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# Fields fortress: broad armored flying wing, payload decks and paired spools.
def output_directory():
    if globals().get('__file__'):
        return os.path.dirname(os.path.abspath(__file__))
    if '--' in sys.argv:
        args = sys.argv[sys.argv.index('--') + 1:]
        if args:
            p = os.path.abspath(args[0])
            return p if os.path.isdir(p) else os.path.dirname(p)
    return os.getcwd()

OUT = output_directory()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

colors = [(.24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),
          (.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),
          (.20,.36,.40),(.16,.83,.91),(1.,.77,.27),(.055,.09,.12),
          (.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
mats = []
for i, color in enumerate(colors):
    m = bpy.data.materials.new('tile_%02d' % i)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = .76
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

parts = []
def mesh_object(name, verts, faces, material, face_materials=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for poly in mesh.polygons:
        poly.material_index = material if face_materials is None else face_materials[poly.index]
        poly.use_smooth = False
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    parts.append(obj)
    return obj

def armor(name, outline, bottom, shoulder, top, inset, mat, bevel_mat=None):
    # A tapered lower edge and inset upper crown give each plate a real bevel.
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    def ring(scale, z):
        return [(cx+(x-cx)*scale, cy+(y-cy)*scale, z) for x,y in outline]
    verts = ring(.96, bottom) + ring(1, shoulder) + ring(inset, top)
    faces = [tuple(reversed(range(n)))]
    indices = [mat]
    for layer in range(2):
        for i in range(n):
            j = (i+1) % n
            faces.append((layer*n+i, layer*n+j, (layer+1)*n+j, (layer+1)*n+i))
            indices.append(mat if layer == 0 or bevel_mat is None else bevel_mat)
    faces.append(tuple(range(2*n, 3*n)))
    indices.append(mat)
    return mesh_object(name, verts, faces, mat, indices)

def box(name, center, size, mat, bevel=.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mats[mat])
    if bevel:
        mod = obj.modifiers.new('Armor edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def ring(name, center, outer, inner, depth, mat, axis='Z', segments=12):
    # Open annulus with an actual inner wall; center is the mouth plane.
    x,y,z = center
    verts = []
    for axial, radius in ((0,outer), (0,inner), (-depth,outer), (-depth,inner)):
        for i in range(segments):
            a = 2*math.pi*i/segments
            u,v = radius*math.cos(a), radius*math.sin(a)
            if axis == 'Z': verts.append((x+u,y+v,z+axial))
            elif axis == '-Y': verts.append((x+u,y-axial,z+v))
            else: verts.append((x+u,y+axial,z+v))
    faces = []
    for i in range(segments):
        j = (i+1)%segments
        faces.extend(((i,j,segments+j,segments+i),
                      (i,2*segments+i,2*segments+j,j),
                      (segments+i,segments+j,3*segments+j,3*segments+i),
                      (2*segments+i,3*segments+i,3*segments+j,2*segments+j)))
    return mesh_object(name, verts, faces, mat)

def disk(name, center, radius, depth, mat, axis='Z', segments=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments, radius=radius, depth=depth, location=center)
    obj = bpy.context.object
    obj.name = name
    if axis in ('Y', '-Y'):
        obj.rotation_euler[0] = math.pi/2
    obj.data.materials.append(mats[mat])
    parts.append(obj)
    return obj

# Blunt railcar body with a deliberately offset cab and signal bridge.
armor('Armored railcar',[(-.88,1.20),(.84,1.20),(1.0,.88),(1.0,-.91),(.72,-1.19),(-.73,-1.19),(-1.0,-.91),(-1.0,.86)],-.16,.08,.30,.94,0,6)
armor('Forward buffer beam',[(-1.13,-.96),(1.13,-.96),(1.05,-1.20),(.66,-1.30),(-.66,-1.30),(-1.05,-1.20)],-.09,.12,.25,.94,11,6)
armor('Locomotive hood',[(-.65,-.78),(.65,-.78),(.65,.59),(-.65,.59)],.27,.37,.51,.90,0,6)
armor('Offset cab',[(-.65,.32),(.42,.32),(.42,1.07),(-.65,1.07)],.34,.51,.73,.92,1,6)
box('Cab window frame',(-.11,.37,.73),(.80,.12,.035),11,.02)
for x in (-.41,-.11,.19): box('Cab window', (x,.36,.754),(.23,.085,.014),9,.01)
for s in (-1,1):
    box('Side underframe',(s*.98,.02,.02),(.23,1.87,.31),11,.04)
    for y in (-.63,.15,.79): disk('Bogie wheel',(s*1.10,y,-.015),.19,.12,6,axis='Y')
    box('Copper busbar',(s*.54,-.25,.54),(.075,1.12,.07),13,.015)
    for y in (-.65,-.21,.23): box('Busbar isolator',(s*.54,y,.52),(.15,.08,.08),11,.01)
    box('Deck edge stripe',(s*.82,-.20,.31),(.10,1.35,.035),1,.01)
box('Gantry crossbeam',(.12,.53,.83),(2.82,.19,.18),11,.025)
for x in (-1.14,1.38):
    box('Gantry upright',(x,.53,.55),(.14,.22,.59),6,.025)
    box('Signal housing',(x,.50,1.01),(.34,.48,.36),11,.035)
    for y,col in ((.64,5),(.50,10),(.36,9)):
        disk('Signal lens',(x,y,1.205),.055,.035,col,segments=12)
        ring('Lens bezel',(x,y,1.23),.065,.052,.025,6,segments=12)
box('Roof control cabinet',(-.27,.80,.79),(.54,.36,.14),0,.03)
for x in (-.46,-.29,-.12,.05): box('Roof grille',(x,.81,.87),(.055,.25,.025),11,.008)
for x in (-.32,.32):
    for y in (-.64,-.38,-.12,.14): box('Hood vent',(x,y,.525),(.35,.055,.025),11,.008)
for s in (-1,1):
    for j in range(2):
        stripe=box('Buffer chevron',(s*(.35+j*.34),-1.12,.275),(.24,.055,.025),10,.005)
        stripe.rotation_euler[2]=s*.65
# Forward launcher and two projector lips sit at their gameplay anchors.
disk('Launcher barrel',(0,-1.17,.22),.19,.30,0,axis='Y')
ring('Launcher lip',(0,-1.32,.22),.18,.12,.09,6,axis='-Y')
disk('Launcher bore',(0,-1.305,.22),.115,.01,11,axis='Y')
for s in (-1,1):
    disk('Projector barrel',(s*1.20,-.32,.25),.16,.28,0,axis='Y')
    ring('Projector lip',(s*1.20,-.46,.25),.155,.10,.08,6,axis='-Y')
    disk('Projector bore',(s*1.20,-.445,.25),.095,.01,11,axis='Y')
    ring('Exhaust socket rim',(s*.67,1.04,.11),.17,.11,.10,6,axis='Y')
    disk('Exhaust recess',(s*.67,1.025,.11),.105,.01,11,axis='Y')
# Join into a single mesh and bake every transform before exporting.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons:
    p.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Render setup is deliberately separate from the selected exported mesh.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue gray') if scene.world is None else scene.world
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape = 'DISK'; data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-4,-5,8), 2.0, (0,0,0))
light('Soft fill', 'AREA', (4,1,5), 160, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,8)
cam.rotation_euler = (0,0,0)  # camera local -Z points down; local +Y is screen up
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 4.0
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam.location = (4,-6,5)
point_at(cam, (0,0,.15))
cam_data.type = 'PERSP'
cam_data.lens = 48
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)



