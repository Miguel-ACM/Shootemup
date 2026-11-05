"""Blender background authoring pass. Preserves the original sources and weapon lips.
Run: blender -b --python Tools/refit_boss_models.py
"""
from pathlib import Path
import bpy, math, json, sys
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
NAMES={81:'PlainsRedKiteBoss',82:'PlainsThreshingDrumBoss',83:'CitySwitchyardBoss',84:'SnowIcebreakerBoss'}

def refit(number,name):
    source=ROOT/'ArtSource/Terra'/f'{number}_{name}'/'model.py'
    code=source.read_text(encoding='utf-8-sig')
    marker='import json' if number in (81,82) else '# Join into a single mesh' if number==83 else "bpy.ops.object.select_all(action='DESELECT')"
    context={'__file__':str(source),'__name__':'boss_source'}
    exec(compile(code[:code.index(marker)],str(source),'exec'),context)
    parts=context['parts'];mats=context['mats'];animated=context.get('animated',[]);specs=context.get('specs',[])
    output=ROOT/'ArtSource/BossPass'/f'{number}_{name}';output.mkdir(parents=True,exist_ok=True)

    def poly(label,points,z,depth,tile):
        n=len(points);cx=sum(x for x,y in points)/n;cy=sum(y for x,y in points)/n
        verts=[(x,y,z) for x,y in points]+[(cx+(x-cx)*.84,cy+(y-cy)*.84,z+depth) for x,y in points]
        faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        data=bpy.data.meshes.new(label);data.from_pydata(verts,[],faces);data.materials.append(mats[tile]);data.update()
        obj=bpy.data.objects.new(label,data);bpy.context.collection.objects.link(obj);parts.append(obj);return obj
    def box(label,center,size,tile):
        x,y,z=center;w,h,d=size
        return poly(label,[(x-w/2,y-h/2),(x+w/2,y-h/2),(x+w/2,y+h/2),(x-w/2,y+h/2)],z-d/2,d,tile)
    def mirror(points,s):
        points=[(s*x,y) for x,y in points]
        return points if s==1 else points[::-1]
    def joint(objects,label,pivot,axis,speed,amplitude=0):
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects:
            obj.select_set(True)
            if obj in parts:parts.remove(obj)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=label
        animated.append(obj);specs.append(dict(object=label,motion='swing' if amplitude else 'spin',axis=axis,pivot=pivot,speed=speed,amplitude=amplitude))

    if number==81:
        # A hooked falcon: deep shoulder armor and split swept elevators, not a flat delta.
        for s in (-1,1):
            poly('Kite shoulder spar',mirror([(.36,-.40),(.65,-.29),(1.42,.26),(1.30,.48),(.53,.16)],s),.25,.16,5)
            poly('Cut ivory leading edge',mirror([(.38,-.41),(.48,-.38),(1.43,.24),(1.40,.31)],s),.42,.045,15)
            for n in range(3):
                x=.78+n*.19;y=.12+n*.13
                poly('Wing layered feather',mirror([(x,y-.13),(x+.31,y+.11),(x+.23,y+.26),(x-.04,y+.02)],s),.32+n*.012,.055,12 if n%2 else 5)
            tail=poly('Hinged split elevator',mirror([(.54,.61),(.94,.66),(1.23,1.07),(.66,.90)],s),.22,.13,5)
            edge=poly('Elevator ivory tip',mirror([(.91,.75),(1.02,.82),(1.23,1.07),(1.08,1.02)],s),.355,.028,15)
            joint([tail,edge],f'Elevator_{s}',[s*.57,.64,.26],[0,1,0],.55,14*s)
        poly('Dorsal knife',[(0,.18),(.12,.52),(0,1.02),(-.12,.52)],.35,.27,5)
    elif number==82:
        # Feed belts, deep drive housings and lifting shields around the existing drums.
        for s in (-1,1):
            poly('Harvest gearbox',mirror([(1.20,-.57),(1.55,-.46),(1.62,.47),(1.35,.70),(1.18,.47)],s),.37,.24,2)
            for n in range(4):box('Drive cooling rib',(s*1.39,-.32+n*.19,.64),(.26,.075,.07),6)
            shield=poly('Hinged chaff guard',mirror([(.67,-.80),(1.27,-.79),(1.23,-.50),(.70,-.48)],s),.39,.11,13)
            joint([shield],f'ChaffGuard_{s}',[s*.95,-.50,.43],[1,0,0],.48,12)
            poly('Rear hopper cheek',mirror([(.50,.50),(.82,.68),(.64,1.06),(.31,.92)],s),.40,.25,2)
        for n in range(5):box('Inclined feed belt',(0,-.71+n*.20,.69),(.35,.085,.055),13 if n%2 else 6)
    elif number==83:
        # Tiered freight containers and paired mechanical semaphore paddles.
        for s in (-1,1):
            poly('Railcar shoulder armor',mirror([(.67,-.89),(1.08,-.76),(1.15,.46),(.89,.72),(.65,.40)],s),.30,.18,0)
            for n in range(4):
                y=-.55+n*.29
                box('Cargo panel recessed bed',(s*.88,y,.50),(.31,.22,.035),11)
                box('Cargo raised rib',(s*.88,y,.53),(.27,.055,.05),6)
            base=box('Semaphore socket',(s*.83,.70,.46),(.24,.19,.15),11)
            arm=poly('Articulated signal arm',mirror([(.83,.70),(1.23,.84),(1.21,.99),(.80,.82)],s),.55,.075,15)
            tip=box('Signal tip',(s*1.12,.87,.65),(.10,.13,.025),3)
            joint([arm,tip],f'Semaphore_{s}',[s*.83,.76,.55],[0,0,1],.25,24*s)
        poly('Forward armored buffer',[(-.72,-.93),(.72,-.93),(.58,-1.10),(-.58,-1.10)],.23,.16,6)
    else:
        # Deep folded ice cheeks, articulated side fins and stepped pressure housings.
        for s in (-1,1):
            poly('Layered icebreaker cheek',mirror([(.17,-1.26),(.73,-.99),(1.05,-.58),(.79,-.61),(.45,-.93)],s),.36,.19,15)
            poly('Prow dark bevel',mirror([(.17,-1.26),(.73,-.99),(.69,-.90),(.20,-1.16)],s),.39,.075,4)
            fin=poly('Runner stabilizer',mirror([(1.03,-.20),(1.32,-.06),(1.25,.40),(.96,.50)],s),.04,.10,4)
            joint([fin],f'IceFin_{s}',[s*1.04,.11,.05],[0,1,0],.31,9*s)
            for n in range(3):poly('Raised pressure hood',mirror([(.37,.31+n*.21),(.70,.31+n*.21),(.68,.48+n*.21),(.40,.48+n*.21)],s),.57,.085,4)

    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();model=bpy.context.object;model.name='Model'
    for obj in [model]+animated:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for face in obj.data.polygons:face.use_smooth=False
    (output/'parts.json').write_text(json.dumps(specs,indent=2))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [model]+animated:obj.select_set(True)
    bpy.ops.wm.obj_export(filepath=str(output/'model.obj'),export_selected_objects=True,export_materials=True,forward_axis='Y',up_axis='Z')
    sys.path.insert(0,str(ROOT/'ArtSource/Terra'))
    import obj_to_hull
    obj_to_hull.bake(str(output/'model.obj'),f'Hull{number}')
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16
    scene.render.resolution_x=640;scene.render.resolution_y=640;scene.render.resolution_percentage=100
    scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.045,.06,.085,1);scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.8
    def aim(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    for loc,energy,size in [((-3,-4,7),600,5),((4,1,4),350,4)]:
        data=bpy.data.lights.new('Softbox','AREA');data.energy=energy;data.shape='DISK';data.size=size
        obj=bpy.data.objects.new('Softbox',data);bpy.context.collection.objects.link(obj);obj.location=loc;aim(obj,(0,0,0))
    data=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',data);bpy.context.collection.objects.link(cam);scene.camera=cam;data.type='ORTHO';data.ortho_scale=4.3
    for label,loc in [('top',(0,0,8)),('angle',(3,-4,7))]:
        cam.location=loc;aim(cam,(0,0,.1));scene.render.filepath=str(output/(label+'.png'));bpy.ops.render.render(write_still=True)

for number,name in NAMES.items():refit(number,name)
