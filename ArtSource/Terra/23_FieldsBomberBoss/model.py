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

# Original planform and a raised center keel, with a stepped, faceted crown.
armor('Flying wing', [(-1.70,.50),(-.58,1.10),(0,.80),(.58,1.10),(1.70,.50),
                       (1.36,-.40),(.70,-.72),(0,-.32),(-.70,-.72),(-1.36,-.40)],
      -.14,.15,.34,.91,2,14)
armor('Armored center fuselage', [(-.40,.80),(.40,.80),(.57,-.30),(.28,-.95),
                                  (0,-1.13),(-.28,-.95),(-.57,-.30)],
      .10,.35,.55,.86,0,6)
armor('Nose shield', [(-.32,-.47),(.32,-.47),(.22,-.87),(0,-1.02),(-.22,-.87)],
      .38,.49,.55,.86,2,14)
armor('Bridge surround', [(-.31,.12),(.31,.12),(.34,-.35),(.22,-.48),(-.22,-.48),(-.34,-.35)],
      .49,.57,.62,.87,6,14)
armor('Bridge glazing', [(-.24,-.04),(.24,-.04),(.23,-.27),(-.23,-.27)],
      .615,.625,.64,.9,11,4)
box('Bridge slit', (0,-.17,.644), (.35,.035,.012), 9)
# Distinctive central payload-bay doors.
for s in (-1,1):
    armor('Payload bay', [(s*.045,.19),(s*.31,.19),(s*.32,.68),(s*.045,.70)],
          .51,.55,.58,.88,2,6)
    box('Bay seam', (s*.18,.43,.588), (.15,.025,.014), 11)
    # Swept upper panels connect the central body to the side wing.
    armor('Swept shoulder armor', [(s*.59,-.12),(s*1.47,.08),(s*1.30,-.55),(s*.75,-.88)],
          .22,.32,.40,.88,6,14)
    armor('Outer olive panel', [(s*1.23,.12),(s*1.60,.37),(s*1.49,-.14),(s*1.29,-.38)],
          .24,.29,.33,.89,2,0)
    # Spool: stacked, faceted collars around a broad recessed impeller.
    disk('Spool housing', (s*1.01,.43,.34), .24,.22,0,segments=12)
    ring('Spool rim', (s*1.01,.43,.47), .24,.18,.07,6,segments=12)
    disk('Recessed impeller', (s*1.01,.43,.438), .175,.02,11,segments=12)
    for k in range(4):
        a = k*math.pi/2 + .35
        blade = box('Impeller blade', (s*1.01 + .09*math.cos(a), .43 + .09*math.sin(a), .452),
                    (.14,.055,.025), 4, .012)
        blade.rotation_euler[2] = a + .55
    disk('Spool hub', (s*1.01,.43,.47), .06,.05,13,segments=10)
    # Ammunition deck below the six fixed upward-facing launcher mouths.
    armor('Launcher deck', [(s*.65,-.12),(s*1.28,.07),(s*1.28,-.34),(s*.73,-.60)],
          .36,.42,.455,.94,2,0)
    for x,y in ((.75,-.48),(.97,-.36),(1.19,-.24)):
        x *= s
        disk('Launcher base', (x,y,.465), .091,.075,0,segments=10)
        ring('Launcher mouth', (x,y,.51), .070,.042,.055,6,segments=10)
        disk('Launcher dark bore', (x,y,.489), .041,.008,11,segments=10)
    # Forward-firing outer gun: its front lip is exactly the gameplay point.
    disk('Wing cannon body', (s*1.40,-.54,.16), .105,.28,0,axis='Y',segments=10)
    ring('Wing cannon muzzle', (s*1.40,-.68,.16), .087,.055,.07,6,axis='-Y',segments=10)
    disk('Cannon bore', (s*1.40,-.655,.16), .053,.008,11,axis='Y',segments=10)
    # Shoulder emitter mouth, centered exactly on the fixed position.
    ring('Shoulder emitter lip', (s*1.34,.32,.36), .112,.068,.11,6,segments=12)
    disk('Shoulder emitter bore', (s*1.34,.32,.335), .066,.012,11,segments=12)
    disk('Emitter core', (s*1.34,.32,.343), .034,.006,3,segments=10)
    # Rear socket mouth is at the exact supplied socket, facing aft (+Y).
    ring('Engine socket rim', (s*1.04,.72,0), .13,.085,.10,6,axis='Y',segments=12)
    disk('Engine socket interior', (s*1.04,.695,0), .083,.01,11,axis='Y',segments=12)
    armor('Outer wing marking', [(s*1.43,.29),(s*1.56,.35),(s*1.48,.07),(s*1.38,.03)],
          .35,.365,.375,.93,14,14)

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

