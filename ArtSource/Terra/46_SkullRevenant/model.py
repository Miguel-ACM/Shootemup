import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

# Blender can be launched either with --python model.py or with a directory
# or script path after -- (for launchers which do not define __file__).
try:
    OUT = Path(__file__).resolve().parent
except NameError:
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    if not args:
        raise RuntimeError('Pass the script path or output directory after --')
    p = Path(args[0]).resolve()
    OUT = p.parent if p.suffix.lower() == '.py' else p
OUT.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for datablock in bpy.data.materials:
    bpy.data.materials.remove(datablock)

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
    bsdf.inputs['Roughness'].default_value = 0.82
    if i in (9, 10):
        bsdf.inputs['Emission Color'].default_value = (*color, 1.0)
        bsdf.inputs['Emission Strength'].default_value = 1.8
    mats.append(m)

body = []
def finish(obj, tile):
    obj.data.materials.append(mats[tile])
    for face in obj.data.polygons:
        face.use_smooth = False
    body.append(obj)
    return obj

def mesh(name, verts, faces, tile):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return finish(obj, tile)

def ellipsoid(name, center, scale, tile, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    return finish(obj, tile)

def plate(name, outline, z_bottom, z_top, tile):
    n = len(outline)
    verts = [(x,y,z_bottom) for x,y in outline] + [(x,y,z_top) for x,y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, verts, faces, tile)

def horn(name, side):
    # Swept, faceted horn that curls out and back to the original silhouette.
    centers = [(side*.49,.42,.08), (side*.64,.57,.14),
               (side*.77,.78,.16), (side*.88,1.02,.12)]
    radii = [.145,.12,.073,.006]
    verts = []
    for (x,y,z), r in zip(centers, radii):
        for j in range(8):
            a = 2*math.pi*j/8
            verts.append((x+r*math.cos(a), y, z+r*math.sin(a)))
    faces = [tuple(reversed(range(8)))]
    for k in range(len(centers)-1):
        for j in range(8):
            faces.append((k*8+j,k*8+(j+1)%8,(k+1)*8+(j+1)%8,(k+1)*8+j))
    faces.append(tuple(range(24,32)))
    return mesh(name, verts, faces, 1)

def ring_y(name, x, y_front, z, radius, inner, depth, tile, dark=11):
    # Open, thick-walled polygonal tube. The front rim is exactly at y_front.
    n=10
    verts=[]
    for y,r in [(y_front,radius),(y_front,inner),(y_front+depth,radius),(y_front+depth,inner)]:
        verts += [(x+r*math.cos(2*math.pi*i/n), y, z+r*math.sin(2*math.pi*i/n)) for i in range(n)]
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces += [(i,j,n+j,n+i), (i,2*n+i,2*n+j,j), (n+i,n+j,3*n+j,3*n+i)]
    obj=mesh(name,verts,faces,tile)
    obj.data.materials.append(mats[dark])
    for i, poly in enumerate(obj.data.polygons):
        if i%3==2: poly.material_index=1
    # Recessed dark end makes the mouth visibly hollow from oblique views.
    disk=[(x+inner*math.cos(2*math.pi*i/n), y_front+depth*.75, z+inner*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh(name+'_bore',disk,[tuple(range(n))],dark)

# Broad low cranium and a smaller lower jaw. The face looks toward -Y.
ellipsoid('Cranium', (0,.15,0), (.70,.72,.33), 1, 20, 10)
ellipsoid('Jaw', (0,-.42,.015), (.39,.24,.225), 6, 16, 8)
# Recess-like dark eye sockets stand proud of the faceted bone and are partly
# covered by heavy brow plates, ensuring a clear skull face at game scale.
for s in (-1,1):
    horn('Horn', s)
    ellipsoid('Socket', (s*.275,.10,.300), (.225,.205,.055), 11, 12, 6)
    ellipsoid('Ember_eye', (s*.275,.095,.352), (.062,.064,.018), 10, 10, 6)
    # Angular, raised brow ridges give the skull a scowling expression.
    outline=[(s*.075,.27),(s*.16,.38),(s*.42,.37),(s*.50,.24),(s*.40,.205),(s*.20,.23)]
    if s < 0: outline.reverse()
    plate('Brow',outline,.305,.375,15)
    outline=[(s*.40,-.13),(s*.57,-.04),(s*.55,-.26),(s*.39,-.36),(s*.32,-.27)]
    if s < 0: outline.reverse()
    plate('Cheek',outline,.18,.27,14)
    # The game provides two engine sockets even for this apparition.
    # Their openings face rearward at the exact socket coordinates.
    ring_y('Rear_socket',s*.874,.93,.02,.070,.043,-.10,12)

# Nasal cavity and central forehead ridge.
plate('Nasal_cavity',[(-.105,-.225),(0,-.055),(.105,-.225),(.055,-.27),(-.055,-.27)],.305,.327,11)
plate('Forehead_ridge',[(-.06,.38),(0,.61),(.06,.38),(0,.29)],.305,.345,14)
# Dark mouth band and distinct bone teeth. The middle space is the gun.
plate('Mouth_shadow',[(-.32,-.48),(-.28,-.33),(.28,-.33),(.32,-.48),(.24,-.56),(-.24,-.56)],.20,.235,11)
for x in (-.255,-.145,.145,.255):
    plate('Tooth',[(x-.042,-.49),(x-.039,-.36),(x+.039,-.36),(x+.042,-.49),(x,-.52)],.235,.275,15)
# Forward-firing central mouth barrel: muzzle center is (0, -0.48, +0.4).
ring_y('Muzzle',0,-.48,.40,.083,.048,.15,12)

# Join static geometry and bake transforms, with no active modifiers.
bpy.ops.object.select_all(action='DESELECT')
for obj in body:
    obj.select_set(True)
bpy.context.view_layer.objects.active = body[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for poly in model.data.polygons:
    poly.use_smooth = False
# A skull apparition has no separate naturally articulated parts.
(OUT / 'parts.json').write_text('[]\n', encoding='utf-8')
bpy.ops.wm.obj_export(filepath=str(OUT / 'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Dark blue-gray world')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.05,.07,.09,1)
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 1.0
scene.view_settings.view_transform = 'Standard'

def point_at(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()

def light(name, kind, location, energy, target, size=None):
    data = bpy.data.lights.new(name, kind)
    data.energy = energy
    if size is not None and kind == 'AREA': data.shape='DISK'; data.size=size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    point_at(obj, target)
    return obj

light('Sun', 'SUN', (-4,-3,6), 2.0, (0,0,0))
light('Soft fill', 'AREA', (3,1,4), 180, (0,0,0), 5)
camdata = bpy.data.cameras.new('Camera')
cam = bpy.data.objects.new('Camera',camdata)
bpy.context.collection.objects.link(cam)
scene.camera = cam
cam.location = (0,.20,5)
cam.rotation_euler = (0,0,0)
# Camera local -Z looks down and local Y is screen up (+Y).
camdata.type = 'ORTHO'
camdata.ortho_scale = 2.24
scene.render.filepath = str(OUT / 'top.png')
bpy.ops.render.render(write_still=True)
camdata.type = 'PERSP'
camdata.lens = 50
cam.location = (2.7,-3.6,3.5)
point_at(cam, (0,.18,.04))
scene.render.filepath = str(OUT / 'angle.png')
bpy.ops.render.render(write_still=True)
