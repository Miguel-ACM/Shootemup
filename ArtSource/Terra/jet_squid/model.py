import bpy
import math
import json
import os
import sys
from mathutils import Matrix, Vector

# Jet Squid: faceted pressure mantle, swept hydrofoils and four inkward vanes.
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    HERE = os.path.abspath(args[args.index('--') + 1]) if '--' in args and len(args) > args.index('--') + 1 else os.getcwd()
    if os.path.splitext(HERE)[1]:
        HERE = os.path.dirname(HERE)

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
for i, rgb in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (*rgb, 1.0)
    shader.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        shader.inputs['Emission Color'].default_value = (*rgb, 1.0)
        shader.inputs['Emission Strength'].default_value = 1.6
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, material, face_materials=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for mat in mats:
        mesh.materials.append(mat)
    for idx, poly in enumerate(mesh.polygons):
        poly.material_index = face_materials[idx] if face_materials else material
        poly.use_smooth = False
    parts.append(obj)
    return obj

def plate(name, outline, bottom, top, material, edge=11, inset=0.12):
    # Chamfered perimeter: a broad top face, sloping shoulders and real thickness.
    area = sum(outline[i][0]*outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0]*outline[i][1] for i in range(len(outline)))
    if area < 0:
        outline = list(reversed(outline))
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    n = len(outline)
    inner = [(x + (cx-x)*inset, y + (cy-y)*inset) for x,y in outline]
    verts = [(x,y,bottom) for x,y in outline] + [(x,y,top-0.035) for x,y in outline] + [(x,y,top) for x,y in inner]
    faces = [tuple(range(n-1,-1,-1))]
    indices = [edge]
    for i in range(n):
        j = (i+1)%n
        faces.append((i,j,n+j,n+i)); indices.append(edge)
        faces.append((n+i,n+j,2*n+j,2*n+i)); indices.append(material)
    faces.append(tuple(range(2*n,3*n))); indices.append(material)
    return mesh_part(name, verts, faces, material, indices)

def ring_body():
    # Each cross-section gives y, half-width, bottom, shoulder height, crown.
    sections = [(-0.66,0.13,0.075,0.19,0.24), (-0.39,0.31,0.055,0.25,0.36),
                (0.18,0.37,0.055,0.28,0.43), (0.43,0.33,0.075,0.25,0.38),
                (0.78,0.15,0.095,0.19,0.27), (0.95,0.015,0.13,0.15,0.16)]
    verts = []
    for y,w,base,shoulder,crown in sections:
        verts.extend([(-w,y,base), (w,y,base), (w,y,shoulder),
                      (w*0.57,y,crown), (0,y,crown+0.025),
                      (-w*0.57,y,crown), (-w,y,shoulder)])
    faces = [tuple(reversed(range(7)))]
    mid = [11]
    for k in range(len(sections)-1):
        for j in range(7):
            faces.append((k*7+j,k*7+(j+1)%7,(k+1)*7+(j+1)%7,(k+1)*7+j))
            mid.append([11,8,0,4,4,0,8][j])
    faces.append(tuple((len(sections)-1)*7+j for j in range(7))); mid.append(8)
    mesh_part('Faceted mantle', verts, faces, 8, mid)

def cylinder(name, radius, depth, location, material, vertices=12, rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, end_fill_type='NGON', location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(mats[material])
    for p in obj.data.polygons: p.use_smooth = False
    parts.append(obj)
    return obj

ring_body()
moving_groups = []
# Pale, angular side fins make the squid silhouette legible from directly above.
for s in (-1, 1):
    side = 'L' if s == -1 else 'R'
    fin_start = len(parts)
    plate('Swept fin', [(s*.24,.43),(s*.49,.36),(s*.80,.08),(s*.47,-.19),(s*.30,-.10)], .055,.145,6,0,.13)
    plate('Fin inset', [(s*.39,.29),(s*.69,.08),(s*.47,-.08),(s*.36,-.05)], .148,.166,8,8,.08)
    fin_pieces = parts[fin_start:]
    del parts[fin_start:]
    # Two tapering, individually swept vanes on each side; overlapping roots sit under mantle.
    plate('Outer tentacle vane', [(s*.28,-.25),(s*.48,-.48),(s*.53,-.87),(s*.40,-1.10),(s*.38,-.70),(s*.22,-.42)], .04,.13,13,11,.12)
    moving_groups.append(('VaneOuter_' + side, parts.pop(), (s*.28, -.25, .085), (0, 0, s), 1.0, 12))
    plate('Inner tentacle vane', [(s*.10,-.34),(s*.27,-.57),(s*.26,-.93),(s*.16,-1.09),(s*.13,-.72),(s*.06,-.48)], .045,.155,4,11,.12)
    moving_groups.append(('VaneInner_' + side, parts.pop(), (s*.10, -.34, .10), (0, 0, s), 1.0, 10))
    # Brass leading edges give the fins a clear mechanical rhythm.
    plate('Fin edge', [(s*.32,.41),(s*.49,.34),(s*.76,.09),(s*.69,.10),(s*.45,.29)], .17,.185,13,13,.06)
    fin_pieces.append(parts.pop())
    moving_groups.append(('Fin_' + side, fin_pieces, (s*.27, .16, .10), (0, s, 0), 1.2, 15))

# A raised, armored lance on the mantle, with a dark seam and a small cyan sensor.
plate('Dorsal armor', [(-.16,-.27),(-.22,.19),(-.15,.50),(0,.75),(.15,.50),(.22,.19),(.16,-.27),(0,-.39)], .36,.46,0,11,.17)
plate('Dorsal spine', [(-.065,-.21),(-.09,.24),(0,.62),(.09,.24),(.065,-.21),(0,-.30)], .458,.485,13,0,.18)
plate('Sensor socket', [(-.105,-.22),(-.13,-.06),(0,.015),(.13,-.06),(.105,-.22),(0,-.28)], .46,.49,11,11,.1)
plate('Sensor glass', [(-.067,-.19),(-.075,-.085),(0,-.045),(.075,-.085),(.067,-.19),(0,-.23)], .491,.505,9,8,.12)

# Forward-facing annular launcher: the open face lies precisely at (0, -0.92).
# Axis is Y; a dark recessed bore is visibly surrounded by a thick brass lip.
def launcher():
    n=12
    verts=[]
    for y,r in [(-.66,.125),(-.92,.105),(-.92,.066),(-.855,.066)]:
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((r*math.cos(a),y,.20+r*math.sin(a)))
    faces=[]; ids=[]
    for k,mat in [(0,8),(1,13),(2,11)]:
        for i in range(n):
            j=(i+1)%n
            faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i)); ids.append(mat)
    faces.append(tuple(3*n+i for i in range(n))); ids.append(11)
    mesh_part('Ink launcher with recessed bore',verts,faces,8,ids)
launcher()

# Join the static hull and each layered moving part separately.
def join_meshes(objects, name, pivot=None):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    if pivot is not None:
        # Move the object origin to its hinge without moving its visible geometry.
        obj.data.transform(Matrix.Translation(-Vector(pivot)))
        obj.location = pivot
    mod = obj.modifiers.new('Export triangulation', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    return obj

model = join_meshes(parts, 'Model')
export_objects = [model]
manifest = []
for name, geometry, pivot, axis, speed, amplitude in moving_groups:
    objects = geometry if isinstance(geometry, list) else [geometry]
    export_objects.append(join_meshes(objects, name, pivot))
    manifest.append({'object': name, 'motion': 'swing', 'axis': list(axis),
                     'pivot': list(pivot), 'speed': speed, 'amplitude': amplitude})
with open(os.path.join(HERE, 'parts.json'), 'w', encoding='utf-8') as file:
    json.dump(manifest, file, indent=2)
    file.write('\n')

bpy.ops.object.select_all(action='DESELECT')
for obj in export_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = model
bpy.ops.wm.obj_export(filepath=os.path.join(HERE, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio')
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05,0.07,0.09,1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape = 'DISK'; data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    point_at(obj,target)
    return obj
light('Upper left sun', 'SUN', (-3,4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,-2,4), 120, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, -.075, 5)
cam.rotation_euler = (0,0,0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.36
scene.render.filepath = os.path.join(HERE, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (2.65,-3.65,3.0)
point_at(cam,(0,-.06,.14))
scene.render.filepath = os.path.join(HERE, 'angle.png')
bpy.ops.render.render(write_still=True)

