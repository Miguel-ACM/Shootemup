"""Build the hangar's instruments with Blender --background --factory-startup --python model.py."""
import json
import math
import sys
import tempfile
from pathlib import Path

import bpy
import bmesh

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from obj_to_hull import bake

if not bpy.app.background:
    raise RuntimeError("Use a separate background Blender process")

# Black oxide, gunmetal, enamel, edge steel, porcelain, leather, brass, copper,
# phosphor, faded lime, ivory, dark glass, signal red, violet, tape and cold glass.
colors = ["172027", "374750", "657873", "a2aaa0", "c2bca4", "744933", "b18a52", "83553c",
          "69b7a4", "aaca90", "e1cca0", "183e42", "c46e50", "756b8b", "472e26", "486b77"]
rubber, metal, enamel, edge, ivory, leather, brass, copper, mint, lime, cream, glass, ember, violet, tape, blue = range(16)
materials = []
for i, rgb in enumerate(colors):
    mat = bpy.data.materials.new(f"tile_{i:02d}")
    mat.diffuse_color = (*[int(rgb[j:j+2], 16) / 255 for j in (0, 2, 4)], 1)
    mat.use_nodes = True
    mat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = mat.diffuse_color
    materials.append(mat)

def mesh(name, vertices, faces, paint, bevel=0):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(materials[paint])
    if bevel:
        mod = obj.modifiers.new("Broken edges", 'BEVEL')
        mod.width, mod.segments = bevel, 1
    return obj

def casting(name, outline, z, height, paint, bevel=.03):
    n = len(outline)
    vertices = [(x, y, level) for level in (z, z+height) for x, y in outline]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return mesh(name, vertices, faces, paint, bevel)

def plate(name, x, y, w, h, z, depth, paint, corner=.065):
    a, b, c = w/2, h/2, min(corner,w/4,h/4)
    pts = [(-a+c,-b),(a-c,-b),(a,-b+c),(a,b-c),(a-c,b),(-a+c,b),(-a,b-c),(-a,-b+c)]
    return casting(name, [(x+u,y+v) for u,v in pts], z, depth, paint, min(c*.46,depth*.36))

def roundel(name, x, y, r, z, depth, paint, aspect=1, sides=20):
    return casting(name, [(x+r*math.cos(i*math.tau/sides), y+aspect*r*math.sin(i*math.tau/sides)) for i in range(sides)],z,depth,paint,min(depth*.25,.035))

def arc(name, x, y, r, width, start, end, z, depth, paint, aspect=1):
    steps = max(2,round(abs(end-start)/14))
    verts = []
    for i in range(steps+1):
        a = math.radians(start+(end-start)*i/steps)
        for rad, lvl in [(r-width,z),(r,z),(r,z+depth),(r-width,z+depth)]:
            verts.append((x+rad*math.cos(a),y+rad*math.sin(a)*aspect,lvl))
    faces=[(3,2,1,0),tuple(range(steps*4,steps*4+4))]
    for k in range(steps):
        for j in range(4):
            a=k*4+j; b=k*4+(j+1)%4
            faces.append((a,b,b+4,a+4))
    return mesh(name,verts,faces,paint)

def bar(name, a, b, thickness, z, depth, paint):
    dx,dy=b[0]-a[0],b[1]-a[1]
    k=.5*thickness/math.hypot(dx,dy)
    return casting(name,[(a[0]-dy*k,a[1]+dx*k),(b[0]-dy*k,b[1]+dx*k),
                         (b[0]+dy*k,b[1]-dx*k),(a[0]+dy*k,a[1]-dx*k)],z,depth,paint,min(.02,thickness*.18))

def dial(x,y,r,angle,z=.34):
    roundel("Bezel",x,y,r,z,.065,brass)
    roundel("Scale face",x,y,r*.79,z+.065,.025,ivory)
    a=math.radians(angle)
    bar("Needle",(x,y),(x+math.cos(a)*r*.65,y+math.sin(a)*r*.65),.036,z+.097,.016,rubber)
    roundel("Pivot",x,y,.052,z+.112,.018,metal,sides=8)

def hull(name, rings, paint, cx=0, cy=0):
    """Elliptical loft with independently shaped stations, used for the simulator canopy."""
    n=12
    v=[]
    for y,width,base,peak in rings:
        for j in range(n):
            a=math.tau*j/n
            v.append((cx+width*math.cos(a),cy+y,base+(peak-base)*math.sin(a)))
    faces=[tuple(reversed(range(n))),tuple(range(len(v)-n,len(v)))]
    for i in range(len(rings)-1):
        for j in range(n):
            k=i*n+j; l=i*n+(j+1)%n
            faces.append((k,l,l+n,k+n))
    return mesh(name,v,faces,paint)

def fleet():
    # Split-throttle quadrant, with two staggered handles over recessed travel slots.
    casting("Sage pedestal",[(-1.34,-.52),(-1.03,-.84),(.96,-.84),(1.28,-.5),(1.16,.65),(.68,.84),(-.89,.76)],0,.34,enamel,.11)
    plate("Bakelite bed",-.44,-.07,1.03,1.15,.34,.12,metal)
    for x, y in [(-.68,.18),(-.23,-.16)]:
        plate("Lever slot",x,0,.19,.82,.46,.025,rubber)
        bar("Steel stem",(x,-.32),(x,y+.13),.09,.50,.14,edge)
        plate("Grain grip",x,y+.18,.36,.23,.65,.14,leather)
    dial(.54,.22,.35,58)
    plate("Porcelain legend",.55,-.33,.55,.18,.35,.04,ivory)
    for j in range(3):
        plate("Test light",.34+.19*j,-.52,.11,.09,.35,.04,mint if j==0 else rubber)

def catalogue():
    # A punch-card reader. Fanned cream cards feed a metal throat beside the index wheel.
    casting("Reader chassis",[(-1.42,-.38),(-1.16,-.7),(.98,-.7),(1.29,-.42),(1.25,.61),(.94,.78),(-1.22,.7)],0,.31,enamel,.10)
    plate("Feed throat",-.53,.17,1.36,.77,.31,.10,rubber)
    for j in range(3):
        x,y=-.54+j*.09,.14+j*.20
        casting("Index card",[(x-.53,y-.14),(x+.51,y-.14),(x+.51,y+.30),(x+.37,y+.43),(x-.53,y+.43)],.42+j*.055,.04,ivory if j<2 else cream,.008)
    for j in range(5):
        for k in range(2):
            if (k+j)%3:
                plate("Punched hole",-.69+j*.14,.57+k*.16,.061,.09,.576,.013,tape,.012)
    bar("Scanner lip",(-1.14,-.09),(.11,-.09),.11,.59,.10,edge)
    roundel("Selector",.63,.10,.38,.31,.16,metal)
    arc("Calibrated rim",.63,.10,.35,.05,15,300,.48,.025,brass)
    bar("Thumb latch",(.48,-.02),(.81,.22),.13,.50,.09,copper)
    for j in range(3): plate("Index key",.26+.26*j,-.44,.19,.14,.31,.085,ember if j==0 else ivory)

def simulator():
    # A suspended trainer bucket in a crescent gimbal, with exposed pivot bearings.
    arc("Rear yoke",0,.08,1.36,.16,13,167,.18,.24,enamel,1.02)
    for side in (-1,1):
        casting("Trestle",[(side*.86,-.76),(side*1.33,-.82),(side*1.52,-.46),(side*1.32,.45),(side*.99,.57)],0,.24,metal)
        roundel("Yaw bearing",side*1.12,.02,.28,.24,.19,brass)
        roundel("Pin",side*1.12,.02,.12,.43,.08,rubber)
    hull("Pod keel",[(-1.42,.27,.34,.48),(-.99,.67,.35,.60),(-.22,.90,.46,.77),(.63,.76,.46,.81),(1.16,.39,.41,.63)],enamel)
    hull("Smoked windscreen",[(-.08,.71,.65,.79),(.22,.73,.67,1.08),(.68,.57,.63,1.07),(.98,.32,.58,.85)],blue)
    hull("Canopy glint",[(.22,.035,.97,1.01),(.50,.064,1.04,1.07),(.78,.024,.95,.97)],mint,-.23)
    casting("Open cockpit",[(-.59,-.89),(-.42,-1.12),(.42,-1.12),(.59,-.89),(.63,-.12),(-.63,-.12)],.69,.09,rubber)
    plate("Bucket cushion",0,-.65,.78,.58,.78,.12,leather,.18)
    plate("Headrest",0,-.93,.55,.18,.90,.10,tape)
    for side in (-1,1):
        bar("Harness",(side*.33,-.48),(side*.12,-.70),.085,.91,.033,ivory)
        plate("Handgrip",side*.54,-.41,.13,.27,.81,.12,metal)
    plate("Instrument brow",0,-.17,.89,.15,.82,.10,metal)
    for x in (-.22,.03,.27): roundel("Gauge",x,-.18,.065,.92,.028,mint,sides=8)

motions=[]
def spin(parts,name,x,y,z,speed):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts: obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    bpy.context.object.name=name
    motions.append(dict(object=name,motion="spin",axis=[0,0,1],pivot=[x,y,z],speed=speed,amplitude=0))

def recorder():
    # An open transport with full and take-up reels and a visible brown tape path.
    plate("Walnut cheeks",0,0,3.1,1.94,0,.27,leather,.19)
    plate("Brushed deck",0,.01,2.89,1.79,.27,.09,ivory,.13)
    for x,fill,speed in [(-.73,.43,26),(.73,.31,36)]:
        y=.31
        roundel("Tape pack",x,y,fill,.36,.13,tape,sides=24)
        parts=[arc("Reel rim",x,y,.54,.066,0,359.7,.50,.045,edge),
               roundel("Spindle",x,y,.13,.50,.10,metal)]
        for j in range(3):
            a=math.radians(j*120+16)
            parts.append(bar("Spoke",(x+.095*math.cos(a),y+.095*math.sin(a)),(x+.51*math.cos(a),y+.51*math.sin(a)),.094,.51,.046,edge))
        spin(parts, "Supply" if x<0 else "Takeup",x,y,.50,speed)
    path=[(-1.13,.16),(-1.16,-.29),(-.68,-.47),(.60,-.47),(1.06,-.26),(1.02,.22)]
    for a,b in zip(path,path[1:]): bar("Tensioned tape",a,b,.053,.39,.057,tape)
    for x in (-1.12,1.05): roundel("Capstan",x,-.27,.12,.37,.12,brass)
    plate("Playback head",0,-.37,.78,.32,.36,.17,metal)
    for j in range(4): plate("Piano key",-.41+j*.26,-.70,.20,.21,.36,.09,ember if j==0 else rubber,.035)

def flight():
    # A maritime plan-position indicator with an offset switch bank and a moving sweep.
    plate("Scope plinth",0,-.08,2.94,1.87,0,.32,enamel,.3)
    x,y=-.31,.12
    roundel("Scope cowling",x,y,.99,.27,.26,metal,sides=28)
    roundel("Bronze gasket",x,y,.87,.53,.06,brass,sides=28)
    roundel("Phosphor glass",x,y,.79,.59,.045,glass,sides=28)
    for radius in (.29,.54,.735): arc("Range ring",x,y,radius,.018,0,359.5,.638,.012,enamel)
    for a in range(0,360,30):
        t=math.radians(a)
        bar("Compass index",(x+.67*math.cos(t),y+.67*math.sin(t)),(x+.74*math.cos(t),y+.74*math.sin(t)),.028,.653,.011,ivory)
    sweep=[casting("Phosphor wake",[(x,y),(x+.65,y+.30),(x+.57,y+.43)],.668,.012,enamel),
           bar("Trace",(x,y),(x+.57,y+.43),.035,.682,.015,lime)]
    spin(sweep,"Sweep",x,y,.67,46)
    for dx,dy in [(-.27,.24),(.20,.37),(.39,-.21)]: roundel("Return",x+dx,y+dy,.043,.703,.013,mint,sides=8)
    dial(.99,.36,.25,108)
    for j in range(3): plate("Rocker",.98,-.02-j*.22,.37,.14,.32,.09,cream if j==0 else metal)

def cradle():
    # Two low service rails leave the centre hollow, including when the pilot's berth is empty.
    for side in (-1,1):
        casting("Drydock shoe",[(side*.62,-1.03),(side*.94,-1.16),(side*1.14,-.73),(side*1.05,.71),
                                  (side*.77,1.04),(side*.57,.93),(side*.79,.46),(side*.8,-.52)],0,.22,metal)
        bar("Saddle rail",(side*.92,-.56),(side*.90,.53),.085,.22,.10,enamel)
        for y in (-.55,.40):
            plate("Rubber chuck",side*.73,y,.30,.21,.23,.15,rubber,.04)
        plate("Docking marker",side*.88,-.86,.17,.14,.23,.025,brass)

def receiver():
    # Cast monitor back with a stepped shoulder and a pair of exposed ventilation cheeks.
    plate("Back casting",0,0,7.99,7.36,0,.38,metal,.42)
    plate("Raised lip",0,0,7.73,7.12,.38,.19,enamel,.37)
    for side in (-1,1):
        for y in (-2.7,2.7):
            plate("Clamp",side*3.82,y,.34,.53,.45,.16,rubber,.07)
            bar("Fastener",(side*3.82-.065,y-.06),(side*3.82+.065,y+.06),.04,.625,.014,brass)
        for j in range(5):
            plate("Heat fin",side*3.91,-.74+j*.36,.20,.17,.35,.15,metal,.04)

def hover_sign():
    # Enamel direction board carried between two lift engines. The game attaches
    # the exhaust to the open bells and projects live pixel lettering onto the face.
    plate("Backing", 0, 0, 2.84, 1.04, 0, .18, metal, .17)
    plate("Rolled edge", 0, .025, 2.77, .97, .18, .08, brass, .13)
    plate("Enamel face", 0, .025, 2.65, .85, .26, .06, rubber, .09)
    for side in (-1, 1):
        x = side * 1.55
        plate("Mounting ear", side*1.42, .02, .42, .35, .025, .13, metal)
        casting("Lift engine", [(x-.20, .24),(x-.11, .48),(x+.10, .48),(x+.21, .24),
                                (x+.19,-.17),(x+.11,-.31),(x-.11,-.31),(x-.19,-.17)],
                .08, .31, enamel, .065)
        plate("Inlet", x, .22, .27, .16, .39, .04, rubber)
        plate("Collar", x, -.18, .40, .12, .36, .07, brass, .035)
        casting("Nozzle bell", [(x-.11,-.28),(x+.11,-.28),(x+.19,-.55),(x-.19,-.55)],
                .12, .18, metal)
        plate("Hot throat", x, -.55, .28, .055, .28, .04, cream, .012)
        for y in (-.32, .36):
            roundel("Face rivet", side*1.24, y, .042, .32, .025, edge, sides=8)

def export(name, build, directory):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    motions.clear()
    build()
    path=directory/"model.obj"
    (directory/"parts.json").write_text(json.dumps(motions))
    bpy.ops.wm.obj_export(filepath=str(path),forward_axis='Y',up_axis='Z',export_normals=False,export_uv=False)
    bake(path,name,palette=True)

selected = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
with tempfile.TemporaryDirectory() as tmp:
    for name, build in [("DockQuadrant",fleet),("DockReader",catalogue),("DockTrainer",simulator),
                        ("DockTransport",recorder),("DockScope",flight),("DockSaddle",cradle),("DockReceiver",receiver),
                        ("DockHoverSign",hover_sign)]:
        if not selected or name in selected:
            export(name,build,Path(tmp))
