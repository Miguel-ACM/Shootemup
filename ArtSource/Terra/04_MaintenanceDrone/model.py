import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

# Run with: blender --background --python model.py [-- OUTPUT_DIRECTORY]
try:
    OUT = Path(__file__).resolve().parent
except NameError:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script directory after -- when __file__ is unavailable')
    candidate = Path(args[args.index('--') + 1]).resolve()
    OUT = candidate.parent if candidate.suffix.lower() == '.py' else candidate
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in list(bpy.data.materials):
    bpy.data.materials.remove(block)

palette = [
    (0.24, 0.35, 0.40), (0.78, 0.83, 0.79), (0.35, 0.43, 0.30),
    (0.95, 0.38, 0.12), (0.32, 0.48, 0.55), (0.54, 0.18, 0.16),
    (0.53, 0.61, 0.62), (0.34, 0.31, 0.48), (0.20, 0.36, 0.40),
    (0.16, 0.83, 0.91), (1.00, 0.77, 0.27), (0.055, 0.09, 0.12),
    (0.62, 0.25, 0.19), (0.82, 0.64, 0.26), (0.53, 0.61, 0.55),
    (0.80, 0.84, 0.83),
]
materials = []
for i, color in enumerate(palette):
    mat = bpy.data.materials.new('tile_%02d' % i)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.72
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.4
    materials.append(mat)

parts = []
def mesh(name, verts, faces, tile):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(materials[tile])
    parts.append(obj)
    return obj

def loft(name, rings, tile):
    # Corresponding polygon rings give the hull a real faceted shoulder and crown.
    n = len(rings[0])
    assert all(len(r) == n for r in rings)
    verts = [v for ring in rings for v in ring]
    faces = [tuple(reversed(range(n)))]
    for k in range(len(rings) - 1):
        for i in range(n):
            j = (i + 1) % n
            faces.append((k*n+i, k*n+j, (k+1)*n+j, (k+1)*n+i))
    faces.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return mesh(name, verts, faces, tile)

def plate(name, outline, bottom, top, inset, tile):
    if sum(outline[i][0]*outline[(i+1)%len(outline)][1] - outline[(i+1)%len(outline)][0]*outline[i][1] for i in range(len(outline))) < 0:
        outline = list(reversed(outline))
    cx = sum(p[0] for p in outline) / len(outline)
    cy = sum(p[1] for p in outline) / len(outline)
    rings = [[(x, y, bottom) for x, y in outline],
             [(x, y, top-inset*0.45) for x, y in outline],
             [(cx+(x-cx)*(1-inset), cy+(y-cy)*(1-inset), top) for x, y in outline]]
    return loft(name, rings, tile)

def box(name, center, scale, tile, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(materials[tile])
    if bevel:
        mod = obj.modifiers.new('Edge chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    parts.append(obj)
    return obj

def drum(name, center, radius, depth, tile, sides=10):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(materials[tile])
    parts.append(obj)
    return obj

def mouth(name, x, y, z, radius, direction, tile):
    # Open annulus with a recessed dark back, axis along Y. y is the opening plane.
    n = 10
    verts = []
    for yy, rr in [(y-direction*0.10, radius*0.82), (y, radius),
                   (y, radius*0.62), (y-direction*0.075, radius*0.62)]:
        for i in range(n):
            a = 2*math.pi*i/n
            verts.append((x+rr*math.cos(a), yy, z+rr*math.sin(a)))
    faces = []
    for k in range(3):
        for i in range(n):
            j = (i+1)%n
            faces.append((k*n+i, k*n+j, (k+1)*n+j, (k+1)*n+i))
    obj = mesh(name, verts, faces, tile)
    obj.data.materials.append(materials[11])
    for p in obj.data.polygons:
        if p.index >= 2*n:
            p.material_index = 1
    # The dark disk is behind the opening, never a protruding projectile.
    verts = [(x, y-direction*0.077, z)] + [
        (x+radius*0.62*math.cos(2*math.pi*i/n), y-direction*0.077,
         z+radius*0.62*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh(name+' dark bore', verts, [(0, i+1, (i+1)%n+1) for i in range(n)], 11)

# Original footprint: roughly 1.80 wide, -0.93 to +0.94 long.
hull = [(-.36,.74),(-.60,.36),(-.52,-.27),(-.26,-.47),
        (.26,-.47),(.52,-.27),(.60,.36),(.36,.74)]
plate('Main faceted tug hull', hull, -.12, .27, .16, 0)
plate('Raised shoulder armor', [(-.43,.38),(-.42,-.14),(-.25,-.30),(.25,-.30),
                                  (.42,-.14),(.43,.38),(.29,.55),(-.29,.55)],
      .25, .34, .18, 6)
# A broad recessed-looking tool bay, framed by a raised steel rim.
plate('Tool bay rim', [(-.25,-.12),(.25,-.12),(.25,.36),(-.25,.36)], .333, .376, .08, 11)
plate('Tool bay well', [(-.19,-.08),(.19,-.08),(.19,.32),(-.19,.32)], .373, .382, .05, 8)
drum('Service core bezel', (0,.13,.391), .112, .035, 11)
drum('Service core', (0,.13,.413), .073, .012, 9)
# Small offset repair markings are broad enough to survive the gameplay view.
box('Bay crossbar', (0,-.015,.395), (.23,.035,.018), 6, .006)

for s in (-1, 1):
    def mirror(poly):
        return [(s*x, y) for x,y in poly]
    # Two hooked manipulators, with an obvious open throat between jaw and hull.
    jaw = mirror([(.42,.10),(.70,-.08),(.89,-.28),(.77,-.82),
                  (.49,-.93),(.53,-.73),(.65,-.62),(.66,-.35),(.36,-.12)])
    plate('Articulated grabber', jaw, -.06, .17, .09, 6)
    shoulder = mirror([(.46,.04),(.68,-.13),(.76,-.27),(.65,-.32),(.47,-.18)])
    plate('Jaw shoulder plate', shoulder, .165, .218, .14, 0)
    tip = mirror([(.67,-.57),(.78,-.58),(.75,-.79),(.52,-.88),(.55,-.75)])
    plate('Jaw gripping tip', tip, .165, .205, .08, 2)
    drum('Hinge housing', (s*.52,-.12,.22), .125, .09, 11)
    drum('Hinge cap', (s*.52,-.12,.273), .085, .025, 14)
    drum('Hinge axle', (s*.52,-.12,.290), .036, .012, 13, 8)
    # Muzzle lip opening lies exactly on the gameplay point.
    mouth('Tool gun muzzle', s*.40, -.55, .12, .072, -1, 6)
    box('Gun mount', (s*.40,-.45,.12), (.15,.14,.15), 0, .025)
    # Inset warning pad on the outer jaw, not a luminous projectile.
    plate('Jaw amber marking', mirror([(.685,-.48),(.76,-.49),(.75,-.57),(.68,-.56)]),
          .204, .211, .02, 13)

# Rear motor casing and a real, recessed opening centered on the engine socket.
plate('Rear motor cowling', [(-.15,.48),(.15,.48),(.19,.72),(.14,.87),(-.14,.87),(-.19,.72)],
      -.10, .23, .16, 0)
box('Motor dorsal panel', (0,.65,.238), (.20,.21,.022), 11, .02)
box('Motor panel bridge', (0,.65,.253), (.16,.034,.012), 6, .004)
mouth('Rear engine socket', 0, .88, 0, .135, 1, 6)

# Join and bake all transforms; geometry remains flat shaded.
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
if not 300 <= triangles <= 2500:
    raise RuntimeError('Triangle budget exceeded: %d' % triangles)

# Export only the baked model, before creating cameras or lights.
bpy.ops.wm.obj_export(filepath=str(OUT / 'model.obj'), export_selected_objects=True,
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
world = bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world = world
world.use_nodes = True
background = world.node_tree.nodes.get('Background')
background.inputs['Color'].default_value = (0.05, 0.07, 0.09, 1)
background.inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if kind == 'AREA':
        data.shape = 'DISK'
        data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    aim(obj, target)
    return obj

sun = light('Upper left sun', 'SUN', (-3, 4, 6), 2.0, (0,0,0))
sun.data.angle = math.radians(18)
light('Soft fill', 'AREA', (3, -2, 4), 180, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0, 0, 6)
cam.rotation_euler = (0, 0, 0)
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.22
scene.render.filepath = str(OUT / 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (3.2, -4.5, 4.0)
aim(cam, (0, 0, .08))
scene.render.filepath = str(OUT / 'angle.png')
bpy.ops.render.render(write_still=True)
print('Exported maintenance drone: %d triangles' % triangles)

