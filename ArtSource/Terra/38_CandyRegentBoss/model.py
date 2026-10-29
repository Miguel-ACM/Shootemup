import bpy, math, os, sys, json
from mathutils import Vector

# Output beside this script, including when launched with blender --python ... -- <path>.
try:
    OUT = os.path.dirname(os.path.abspath(__file__))
except NameError:
    args = sys.argv
    tail = args[args.index('--') + 1:] if '--' in args else []
    if not tail:
        raise RuntimeError('Pass the script path after -- when __file__ is unavailable')
    p = os.path.abspath(tail[0])
    OUT = p if os.path.isdir(p) else os.path.dirname(p)
os.makedirs(OUT, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors = [(0.78,0.19,0.40),(0.97,0.85,0.71),(0.30,0.78,0.66),(1,0.59,0.25),
          (0.39,0.17,0.12),(0.62,0.28,0.18),(0.45,0.25,0.34),(0.57,0.38,0.79),
          (0.90,0.44,0.65),(0.27,0.91,0.83),(1,0.82,0.39),(0.13,0.055,0.13),
          (0.92,0.31,0.49),(0.83,0.66,0.39),(0.51,0.26,0.58),(0.98,0.96,0.88)]
mats=[]
for i, c in enumerate(colors):
    m=bpy.data.materials.new('tile_%02d' % i); m.diffuse_color=(*c,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*c,1)
    bs.inputs['Roughness'].default_value=0.31
    if i in (9,10):
        bs.inputs['Emission Color'].default_value=(*c,1); bs.inputs['Emission Strength'].default_value=1.5
    mats.append(m)
body=[]
def mesh(name, verts, faces, mat, group=body):
    me=bpy.data.meshes.new(name); me.from_pydata(verts, [], faces); me.update()
    ob=bpy.data.objects.new(name, me); bpy.context.collection.objects.link(ob); ob.data.materials.append(mats[mat])
    group.append(ob); return ob

def plate(name, outline, bottom, top, inset, mat):
    # Beveled polygon with an inset top rim, designed as broad, readable plates.
    n=len(outline); cx=sum(p[0] for p in outline)/n; cy=sum(p[1] for p in outline)/n
    verts=[(x,y,bottom) for x,y in outline]+[(x,y,top-0.06) for x,y in outline]+[(cx+(x-cx)*inset,cy+(y-cy)*inset,top) for x,y in outline]
    faces=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    faces += [(n+i,n+(i+1)%n,2*n+(i+1)%n,2*n+i) for i in range(n)]
    return mesh(name,verts,faces,mat)

def ellipsoid(name, loc, scale, mat, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(mats[mat]); body.append(o); return o

def tube(name, x, y, z, outer, inner, depth, material, direction=-1):
    # Hollow polygonal barrel, with its mouth exactly at y; inset dark back makes the opening legible.
    n=12; vs=[]
    for yy,r in [(y,outer),(y,inner),(y-direction*depth,outer),(y-direction*depth*0.72,inner)]:
        for i in range(n):
            a=2*math.pi*i/n; vs.append((x+r*math.cos(a),yy,z+r*math.sin(a)))
    fs=[]
    for i in range(n):
        j=(i+1)%n; fs.extend([(i,j,n+j,n+i),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    mesh(name,vs,fs,material)
    disc=[(x,y-direction*depth*.70,z)]+[(x+inner*math.cos(2*math.pi*i/n),y-direction*depth*.70,z+inner*math.sin(2*math.pi*i/n)) for i in range(n)]
    mesh(name+'_bore',disc,[(0,i+1,(i+1)%n+1) for i in range(n)],11)

def mirrored(poly,s): return [(s*x,y) for x,y in poly]

# Chocolate keel with a gently raised, faceted icing deck.
plate('Ganache keel',[(0,-1.2),(-.46,-.53),(-.50,.55),(0,.86),(.50,.55),(.46,-.53)],-.20,.25,.83,4)
plate('Cream nose inlay',[(0,-1.13),(-.30,-.58),(-.24,-.39),(0,-.54),(.24,-.39),(.30,-.58)],.24,.32,.84,1)
for s in (-1,1):
    # Split tapered wafer with a thick caramel edge and raspberry icing panel.
    plate('Wafer wing',mirrored([(.34,-.45),(1.34,-.17),(1.51,.65),(.90,.37),(.48,.84)],s),-.12,.13,.89,13)
    plate('Raspberry wing glaze',mirrored([(.55,-.24),(1.29,-.04),(1.39,.54),(.94,.29)],s),.125,.21,.91,0)
    plate('Cream wing slash',mirrored([(.98,.09),(1.32,.18),(1.35,.30),(1.02,.20)],s),.205,.235,.93,1)
    # A raised circular launch housing, echoing the original wing drums.
    ellipsoid('Launch pod', (s*.82,-.08,.22),(.30,.21,.14),14)
    ellipsoid('Launch pod cap', (s*.82,-.08,.32),(.23,.17,.085),8)
    plate('Pod chevron',mirrored([(.68,-.12),(.82,-.24),(.96,-.12),(.96,-.04),(.82,-.14),(.68,-.04)],s),.39,.41,.95,15)
    # Four lips face the nose. The mouth plane is on the gameplay point.
    tube('Wing muzzle',s*.82,-.35,.20,.105,.060,.19,13)
    tube('Nose muzzle',s*.27,-1.07,.20,.085,.048,.25,13)
    # Paired rear nozzles terminate exactly at the engine sockets.
    tube('Engine nozzle',s*.65,.80,0,.115,.075,.23,5,direction=1)
    plate('Rear nacelle',mirrored([(.48,.39),(.82,.42),(.82,.72),(.65,.78),(.48,.69)],s),-.10,.18,.85,7)

# A substantial faceted rose candy canopy and a dark royal visor.
ellipsoid('Pink fondant canopy',(0,-.08,.28),(.39,.50,.24),8,20,10)
plate('Plum visor',[(-.26,-.20),(-.20,.05),(.20,.05),(.26,-.20),(0,-.34)],.44,.52,.87,11)
plate('Aqua visor glint',[(-.15,-.13),(.15,-.13),(.13,-.08),(-.13,-.08)],.52,.535,.95,9)
# Three cut sugar crystals form the original unmistakable crown silhouette.
for x, length in [(-.31,.44),(0,.62),(.31,.44)]:
    y=.49; w=.12
    plate('Sugar crown',[(x-w,y),(x,y+length),(x+w,y)],.27,.43,.72,1)
    plate('Crown facet',[(x-.055,y+.04),(x,y+length-.07),(x+.055,y+.04)],.43,.455,.85,3)

# Join static geometry and bake transforms. There are no naturally moving parts on this design.
bpy.ops.object.select_all(action='DESELECT')
for ob in body: ob.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.object.join(); model=bpy.context.object; model.name='Model'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
for p in model.data.polygons: p.use_smooth=False
with open(os.path.join(OUT,'parts.json'),'w',encoding='utf-8') as f: json.dump([],f,indent=2)
# Only model geometry is selected for export; axis mapping preserves Blender coordinates.
bpy.ops.object.select_all(action='DESELECT'); model.select_set(True)
bpy.context.view_layer.objects.active=model
bpy.ops.wm.obj_export(filepath=os.path.join(OUT,'model.obj'), export_selected_objects=True,
                      export_materials=True, forward_axis='Y', up_axis='Z')

scene=bpy.context.scene
scene.render.engine='CYCLES'; scene.cycles.device='CPU'; scene.cycles.samples=48
scene.render.resolution_x=640; scene.render.resolution_y=640; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
world=bpy.data.worlds.new('Blue gray studio') if not bpy.data.worlds else bpy.data.worlds[0]
scene.world=world; world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.05,.07,.09,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=1
scene.view_settings.view_transform='Standard'
def point_at(ob, target): ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
def light(name, kind, location, energy, size=None):
    data=bpy.data.lights.new(name,kind); data.energy=energy
    if size is not None: data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(ob); ob.location=location; point_at(ob,(0,0,0)); return ob
light('Sun','SUN',(-4,-5,8),2.0)
light('Soft fill','AREA',(4,1,5),110,5)
camdata=bpy.data.cameras.new('Camera'); cam=bpy.data.objects.new('Camera',camdata); bpy.context.collection.objects.link(cam); scene.camera=cam
cam.location=(0,0,8); cam.rotation_euler=(0,0,0); camdata.type='ORTHO'; camdata.ortho_scale=3.55
scene.render.filepath=os.path.join(OUT,'top.png'); bpy.ops.render.render(write_still=True)
camdata.type='PERSP'; camdata.lens=50; cam.location=(3.5,-5.2,4.4); point_at(cam,(0,0,.05))
scene.render.filepath=os.path.join(OUT,'angle.png'); bpy.ops.render.render(write_still=True)

