"""Render exact Unity-exported meshes offline; never opens the live editor.
Blender -b --python Tools/render_boss_pass.py
These are geometry/material review images, not gameplay screenshots.
"""
from pathlib import Path
import bpy, math, sys
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]/'ArtSource/BossPass'
palette=[(.24,.35,.40),(.78,.83,.79),(.35,.43,.30),(.95,.38,.12),(.32,.48,.55),(.54,.18,.16),(.53,.61,.62),(.34,.31,.48),(.20,.36,.40),(.16,.83,.91),(1,.77,.27),(.055,.09,.12),(.62,.25,.19),(.82,.64,.26),(.53,.61,.55),(.80,.84,.83)]
heaven=[(.96,.91,.73)]*16
for i,c in {2:(.92,.59,.13),3:(.92,.59,.13),7:(.37,.22,.065),8:(1,.82,.34),9:(.12,.48,.66),11:(.014,.022,.035),15:(.99,.97,.88)}.items():heaven[i]=c
void=[(.055,.025,.095)]*16
for i,c in {3:(.43,.16,.68),8:(.66,.25,.92),9:(.66,.25,.92),11:(.022,.013,.040),14:(.17,.08,.27)}.items():void[i]=c

def render(path):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    vertices=[];faces=[];slots=[];tile='hull_6'
    for line in path.read_text().splitlines():
        fields=line.split()
        if not fields:continue
        if fields[0]=='v':vertices.append(tuple(map(float,fields[1:4])))
        elif fields[0]=='usemtl':tile=fields[1]
        elif fields[0]=='f':faces.append(tuple(int(i)-1 for i in fields[1:]));slots.append(tile)
    if not faces:return
    mesh=bpy.data.meshes.new(path.stem);mesh.from_pydata(vertices,[],faces);mesh.update()
    model=bpy.data.objects.new(path.stem,mesh);bpy.context.collection.objects.link(model)
    mats={}
    for label in sorted(set(slots)):
        family,index=label.split('_');index=int(index);color={'hull':palette,'heaven':heaven,'void':void}[family][index]
        mat=bpy.data.materials.new(label);mat.diffuse_color=(*color,1);mat.use_nodes=True
        bs=mat.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.63 if family!='void' else .48
        if family=='void' and index in (3,8,9):bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=.6
        mats[label]=len(mesh.materials);mesh.materials.append(mat)
    for face,label in zip(mesh.polygons,slots):face.material_index=mats[label];face.use_smooth=False
    low=Vector(tuple(min(v[i] for v in vertices) for i in range(3)));high=Vector(tuple(max(v[i] for v in vertices) for i in range(3)));center=(low+high)/2
    extent=max((high-low).x,(high-low).y)*1.25
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=12
    scene.render.resolution_x=560;scene.render.resolution_y=560;scene.render.resolution_percentage=100
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.045,.055,.08,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.55
    def aim(obj):obj.rotation_euler=(center-obj.location).to_track_quat('-Z','Y').to_euler()
    for delta,energy in [((-4,-3,7),800),((4,4,5),500)]:
        data=bpy.data.lights.new('Softbox','AREA');data.energy=energy;data.size=5
        obj=bpy.data.objects.new('Softbox',data);bpy.context.collection.objects.link(obj);obj.location=center+Vector(delta);aim(obj)
    data=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',data);bpy.context.collection.objects.link(cam);scene.camera=cam;data.type='ORTHO';data.ortho_scale=extent
    cam.location=center+Vector((0,0,12));aim(cam)
    out=ROOT/'Renders';out.mkdir(exist_ok=True);scene.render.filepath=str(out/(path.stem+'.png'));bpy.ops.render.render(write_still=True)

for path in sorted((ROOT/'Meshes').glob('*.obj')):
    if '--void-only' in sys.argv and not path.stem.startswith('Void-'):continue
    if '--final-only' in sys.argv and 'Final' not in path.stem:continue
    if path.stem.startswith('SpaceCitadel'):continue # Shader-only event horizon has no hull.
    render(path)
