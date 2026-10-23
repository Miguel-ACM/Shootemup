"""Rebuild only the player FBX; render matched before/after views for review.
Run: blender -b -P ArtSource/PSXv2/update_player.py
"""
from pathlib import Path
import bpy
from mathutils import Vector
import json, math
ROOT=Path('C:/Users/Miguel/Shootemup')

def load(path):
    ns={'PSXV2_LIBRARY':True}
    exec(compile(path.read_text(encoding='utf-8-sig'),str(path),'exec'),ns)
    return ns

def shot(ns, label):
    ship=ns['player']()[0]
    for suffix, tilt in [('top',0),('angle',math.radians(35))]:
        cam.location=Vector((0,-4*math.sin(tilt),4*math.cos(tilt)))
        cam.rotation_euler=(tilt,0,0)
        bpy.context.scene.render.filepath=str(ROOT/'ArtSource/PSXv2'/f'Player_{label}_{suffix}.png')
        bpy.ops.render.render(write_still=True)
    return ship

sc=bpy.context.scene
for o in list(sc.objects):bpy.data.objects.remove(o,do_unlink=True)
sc.render.engine='CYCLES';sc.cycles.samples=12
sc.render.resolution_x=sc.render.resolution_y=480;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
sc.view_settings.view_transform='Standard'
sc.world.color=(.13,.13,.13)
sc.world.use_nodes=True
bg=next(n for n in sc.world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs[0].default_value=(.075,.085,.1,1);bg.inputs[1].default_value=.8
cam=bpy.data.objects.new('Player review camera',bpy.data.cameras.new('Player review camera'))
sc.collection.objects.link(cam);cam.data.type='ORTHO';cam.data.ortho_scale=1.35;sc.camera=cam
sun=bpy.data.objects.new('Player review sun',bpy.data.lights.new('Player review sun','SUN'))
sc.collection.objects.link(sun);sun.data.energy=2.8;sun.rotation_euler=(.35,-.45,.2)
old=ROOT/'Temp/player_before.py'
if old.exists():
    ns=load(old);before=shot(ns,'before')
    for o in ns['hierarchy'](before):bpy.data.objects.remove(o,do_unlink=True)
    for mesh in list(bpy.data.meshes):
        if mesh.users==0:bpy.data.meshes.remove(mesh)
ns=load(ROOT/'ArtSource/PSXv2/build_ships.py');ship=shot(ns,'detailed')
triangles=ns['export'](ship,'PSXv2_Player')
# Geometry and UV checks on the actual exported model.
# FBX triangulates during export; verify the reimported file below.
assert all(o.data.uv_layers.active is not None for o in ns['hierarchy'](ship) if o.type=='MESH')
assert bpy.data.objects['PSXv2_Player_HitboxCore'].parent==ship
coords=[ship.matrix_world@v.co for v in ship.data.vertices]
report={'triangles':triangles,'width':max(v.x for v in coords)-min(v.x for v in coords),'height':max(v.y for v in coords)-min(v.y for v in coords),'hitboxCenter':list(ns['HITBOX_CENTER']),'hitboxRadius':ns['HITBOX_RADIUS'],'uvs':True}
check=bpy.data.scenes.new('Export verification')
bpy.context.window.scene=check
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/Art/PSXv2/PSXv2_Player.fbx'))
meshes=[o.data for o in check.objects if o.type=='MESH']
assert len(meshes)==3, 'Hull and two hitbox meshes required'
assert all(len(p.vertices)==3 for m in meshes for p in m.polygons), 'FBX must be triangulated'
assert all(m.uv_layers.active is not None for m in meshes), 'FBX UVs missing'
(ROOT/'ArtSource/PSXv2/PlayerDetailReport.json').write_text(json.dumps(report,indent=2))
print('PLAYER DETAIL PASS',report)
