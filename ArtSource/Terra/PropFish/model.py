import bpy
import bmesh
import math
import os
import sys
from mathutils import Vector

# The asset is authored in the game's XY plane, with the camera on +Z.
if '__file__' in globals():
    OUT = os.path.dirname(os.path.abspath(__file__))
else:
    args = sys.argv
    if '--' not in args or not args[args.index('--') + 1:]:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    OUT = os.path.dirname(os.path.abspath(args[args.index('--') + 1]))

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)

palette = {
    0:(0.24,0.35,0.40), 1:(0.78,0.83,0.79), 2:(0.35,0.43,0.30),
    3:(0.95,0.38,0.12), 4:(0.32,0.48,0.55), 5:(0.54,0.18,0.16),
    6:(0.53,0.61,0.62), 7:(0.34,0.31,0.48), 8:(0.20,0.36,0.40),
    11:(0.055,0.09,0.12), 12:(0.62,0.25,0.19), 13:(0.82,0.64,0.26),
    14:(0.53,0.61,0.55), 15:(0.80,0.84,0.83),
}
mats = {}
for n, rgb in palette.items():
    m = bpy.data.materials.new('tile_%02d' % n)
    m.diffuse_color = (*rgb, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1.0)
    bsdf.inputs['Roughness'].default_value = 0.82
    mats[n] = m

parts = []
def mesh_part(name, verts, faces, tiles):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    ob = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(ob)
    for n in sorted(set(tiles)):
        mesh.materials.append(mats[n])
    slots = {m.name: i for i, m in enumerate(mesh.materials)}
    for p, tile in zip(mesh.polygons, tiles):
        p.material_index = slots[mats[tile].name]
        p.use_smooth = False
    # Keep faceted surface normals coherent even for the custom fin pieces.
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(mesh)
    bm.free()
    parts.append(ob)
    return ob

# Elliptical cross sections define a blunt snout, deep reef-fish body and
# narrow caudal peduncle. Close-spaced sections make integrated colour bands.
rings = [
    (-0.73, .025, .025), (-0.68, .095, .050), (-0.58, .18, .075),
    (-0.47, .245, .095), (-0.37, .29, .108), (-0.32, .305, .115),
    (-0.24, .33, .122), (-0.13, .35, .128), (-0.08, .355, .13),
    ( 0.01, .35, .128), ( 0.12, .32, .115), ( 0.17, .30, .105),
    ( 0.25, .25, .09), ( 0.33, .16, .065), ( 0.41, .075, .045),
    ( 0.51, .035, .027), ( 0.59, .018, .018),
]
N = 12
verts = []
for y, width, height in rings:
    for j in range(N):
        a = 2 * math.pi * j / N
        verts.append((width * math.cos(a), y, height * math.sin(a)))
faces, tiles = [], []
faces.append(tuple(reversed(range(N))))
tiles.append(8)
for i in range(len(rings)-1):
    mid = (rings[i][0] + rings[i+1][0]) * .5
    for j in range(N):
        faces.append((i*N+j, i*N+(j+1)%N, (i+1)*N+(j+1)%N, (i+1)*N+j))
        # Contrasting transverse bands are part of the body, not floating decals.
        band = (-.32 <= mid < -.24) or (-.08 <= mid < .01) or (.17 <= mid < .25)
        top = math.sin(2*math.pi*(j+.5)/N)
        tiles.append((8 if band else (14 if top > 0.25 else 4)) if top > -.5 else (0 if band else 6))
faces.append(tuple((len(rings)-1)*N+j for j in range(N)))
tiles.append(8)
mesh_part('Faceted body', verts, faces, tiles)

# Pectoral fins: shallow, pointed solids, distinct from the animated tail.
def fin(name, outline, z, thickness, upper, edge):
    v = [(x,y,z+thickness/2) for x,y in outline] + [(x,y,z-thickness/2) for x,y in outline]
    k = len(outline)
    f = [tuple(range(k)), tuple(reversed(range(k, 2*k)))]
    t = [upper, edge]
    for j in range(k):
        f.append((j, (j+1)%k, (j+1)%k+k, j+k))
        t.append(edge)
    return mesh_part(name, v, f, t)
fin('Left pectoral fin', [(-.22,-.22),(-.52,.20),(-.45,.29),(-.25,.13)], .005,.025,13,8)
fin('Right pectoral fin',[(.23,-.22),(.38,.12),(.37,.23),(.25,.12)], .005,.025,13,8)

# Low, serrated dorsal ridge, deliberately restrained in height.
mesh_part('Dorsal ridge', [(-.035,-.30,.112),(.035,-.30,.112),
    (-.045,.26,.075),(.045,.26,.075), (0,-.22,.13), (0,-.08,.13),
    (0,.06,.125),(0,.20,.105)],
    [(0,1,5,4),(1,3,7,6,5),(3,2,7),(2,0,4,5,6,7)], [8,8,4,4])

# Eyes sit on the upper shoulder and remain visible from directly overhead.
def eye(name, loc, scale, tile, segments=10, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    ob = bpy.context.object
    ob.name = name
    ob.scale = scale
    ob.data.materials.append(mats[tile])
    for p in ob.data.polygons:
        p.use_smooth = False
    parts.append(ob)
for s in (-1,1):
    eye('Eye surround', (s*.135,-.525,.067), (.052,.056,.018), 15)
    eye('Eye pupil', (s*.137,-.533,.083), (.027,.032,.011), 11)
    eye('Eye glint', (s*.142,-.543,.093), (.008,.009,.004), 1, 8, 4)

# One mesh, with geometry and normals fixed before exporting.
bpy.ops.object.select_all(action='DESELECT')
for ob in parts:
    ob.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
tri = model.modifiers.new('Export triangles', 'TRIANGULATE')
bpy.ops.object.modifier_apply(modifier=tri.name)
for poly in model.data.polygons:
    poly.use_smooth = False
bpy.ops.wm.obj_export(filepath=os.path.join(OUT, 'model.obj'),
    export_selected_objects=True, export_materials=True,
    forward_axis='Y', up_axis='Z')

# Render-only objects are created after export.
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Deep water studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def light(name, kind, loc, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None:
        data.shape = 'DISK'
        data.size = size
    ob = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(ob)
    ob.location = loc
    point_at(ob, target)
    return ob
light('Upper left sun', 'SUN', (-3,-4,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,1,4), 120, (0,0,0), 5)
cam_data = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera', cam_data)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,0,4)
point_at(cam, (0,0,0))
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 1.56
scene.render.filepath = os.path.join(OUT, 'top.png')
bpy.ops.render.render(write_still=True)
cam_data.type = 'PERSP'
cam_data.lens = 50
cam.location = (1.05,-1.27,1.68)
point_at(cam, (0,-.04,0))
scene.render.filepath = os.path.join(OUT, 'angle.png')
bpy.ops.render.render(write_still=True)

