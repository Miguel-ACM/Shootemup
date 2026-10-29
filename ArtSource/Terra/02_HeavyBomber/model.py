import bpy
import math
import os
import sys
from mathutils import Vector

# Run with: blender --background --python model.py [-- /path/to/model.py]
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.materials):
    bpy.data.materials.remove(block)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.0, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
mats = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.82
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.5
    mats.append(mat)

parts = []
def mesh_part(name, verts, faces, material, face_materials=None):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for m in mats:
        mesh.materials.append(m)
    for poly in mesh.polygons:
        poly.material_index = material if face_materials is None else face_materials[poly.index]
        poly.use_smooth = False
    parts.append(obj)
    return obj

def loft(name, rings, material, top_material=None):
    # Corresponding contours form sloped armor shoulders and a faceted crown.
    n = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = [tuple(reversed(range(n)))]
    colors = [11]
    for j in range(len(rings) - 1):
        for i in range(n):
            faces.append((j*n+i, j*n+(i+1)%n, (j+1)*n+(i+1)%n, (j+1)*n+i))
            colors.append(material)
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    colors.append(material if top_material is None else top_material)
    return mesh_part(name, verts, faces, material, colors)

def plate(name, outline, bottom, top, inset, mat, cap=None):
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    base = [(x, y, bottom) for x, y in outline]
    mid = [(x, y, top - 0.045) for x, y in outline]
    crown = [(cx + (x-cx)*inset, cy + (y-cy)*inset, top) for x, y in outline]
    return loft(name, [base, mid, crown], mat, cap)

def tube_y(name, x, front_y, back_y, z, outer, inner, mat, count=10):
    # An open annular lip and recessed dark bore; front_y is the exact mouth plane.
    verts = []
    for y, radius in ((front_y, outer), (back_y, outer), (front_y, inner),
                      (front_y + (back_y-front_y)*0.35, inner)):
        verts += [(x + radius*math.cos(2*math.pi*i/count), y,
                   z + radius*math.sin(2*math.pi*i/count)) for i in range(count)]
    faces, colors = [], []
    for i in range(count):
        j = (i+1) % count
        faces.extend([(i, j, count+j, count+i), (i, 2*count+i, 2*count+j, j),
                      (2*count+i, 3*count+i, 3*count+j, 2*count+j)])
        colors.extend([mat, 6, 11])
    faces.append(tuple(3*count+i for i in range(count)))
    colors.append(11)
    return mesh_part(name, verts, faces, mat, colors)

# Continuous, broad olive flying wing: the former silhouette remains 3.30 x 1.72.
outline = [(-1.65, .25), (-1.12, -.37), (-.43, -.69), (0, -.90),
           (.43, -.69), (1.12, -.37), (1.65, .25), (1.29, .29),
           (.52, .43), (0, .65), (-.52, .43), (-1.29, .29)]
plate('swept wing', outline, -.13, .14, .94, 2)

# The center fuselage is an angular, elevated armored mass, not a flat decal.
body = [(-.39, .51), (-.42, -.30), (-.30, -.62), (0, -.86),
        (.30, -.62), (.42, -.30), (.39, .51), (0, .62)]
plate('raised fuselage', body, .08, .33, .82, 0, 0)
# Cockpit surround and faceted armored glass.
plate('cockpit coaming', [(-.31,-.49), (0,-.70), (.31,-.49), (.29,-.16),
                           (.20,.02), (-.20,.02), (-.29,-.16)], .28, .40, .90, 11)
plate('cockpit', [(-.24,-.45), (0,-.60), (.24,-.45), (.22,-.18),
                  (.14,-.07), (-.14,-.07), (-.22,-.18)], .39, .47, .70, 4, 8)
# Dorsal bomb bay spine and a dark recessed intake.
plate('bay spine', [(-.25,.08), (.25,.08), (.26,.47), (0,.56), (-.26,.47)],
      .31, .39, .86, 14, 0)
plate('intake recess', [(-.15,.20), (.15,.20), (.15,.40), (-.15,.40)], .39, .405, .90, 11)
plate('intake divider', [(-.022,.21), (.022,.21), (.022,.39), (-.022,.39)], .405, .42, .90, 6)

for s in (-1, 1):
    def mirror(points):
        pts = [(s*x, y) for x, y in points]
        return pts if s == 1 else list(reversed(pts))
    # Thick nacelles carry the two ports and terminate exactly at the socket plane.
    nacelle = mirror([(.75,-.58), (.79,-.20), (.77,.43), (.81,.73),
                      (.89,.82), (1.01,.82), (1.09,.73), (1.13,.37),
                      (1.11,-.15), (1.08,-.49), (1.02,-.58)])
    plate('engine nacelle', nacelle, -.13, .27, .84, 2, 0)
    # Mouth center is the gameplay point, oriented forward toward -Y.
    tube_y('weapon muzzle', s*.95, -.68, -.47, .20, .085, .050, 11)
    # Actual annular nozzle opening at the engine socket; no flame or thruster.
    tube_y('engine socket', s*.95, .82, .66, 0.0, .125, .082, 0, 12)
    # Contrasting inner wing armor and a single bold orange identification stripe.
    plate('wing root armor', mirror([(.44,-.49), (.75,-.36), (.72,.18), (.44,.22)]),
          .15, .205, .88, 6, 14)
    plate('orange bomb marking', mirror([(.51,-.35), (.67,-.29), (.67,-.19), (.51,-.23)]),
          .202, .218, .98, 3)
    plate('outer wing panel', mirror([(1.12,-.23), (1.49,.19), (1.31,.18), (1.09,-.05)]),
          .14, .175, .90, 0, 0)
    plate('nacelle dorsal plate', mirror([(.84,-.20), (1.06,-.20), (1.06,.44), (.84,.44)]),
          .265, .31, .84, 14, 2)

# Join, triangulate and bake every transform before export.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangulation', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for face in model.data.polygons:
    face.use_smooth = False
assert 300 <= len(model.data.polygons) <= 2500, 'Triangle budget exceeded: %d' % len(model.data.polygons)

bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

# Export is complete before cameras and lights are created.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Blue gray studio') if scene.world is None else scene.world
scene.world.use_nodes = True
background = scene.world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Upper left sun', 'SUN', (-4, 3, 6), 2.0, (0, 0, 0))
light('Soft fill', 'AREA', (4, -3, 5), 300, (0, 0, 0), 5.0)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 8)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 3.85
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 52
cam.location = (4.0, -5.4, 4.2)
point_at(cam, (0, 0, .05))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)
