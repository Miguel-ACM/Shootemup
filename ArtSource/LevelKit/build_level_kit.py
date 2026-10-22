"""Author the scrolling level kit in Blender; export vertex-coloured meshes in the game's XY plane.
Each district is 80 units long, split into four matching 20-unit sections with generous horizontal overscan.
"""
import bpy, math, random, json
from mathutils import Vector
ROOT = r'C:/Users/Miguel/Shootemup'
PERIOD = 80
collection = bpy.data.collections.get('Five Level Kit')
if collection:
    for obj in list(collection.objects): bpy.data.objects.remove(obj, do_unlink=True)
else:
    collection = bpy.data.collections.new('Five Level Kit')
    bpy.context.scene.collection.children.link(collection)
meshes = []

class Builder:
    def __init__(self, name, projection=(.16,.30)):
        self.name=name; self.v=[]; self.f=[]; self.c=[]; self.projection=projection
    def face(self, points, color):
        k=len(self.v); self.v.extend(points); self.f.append(tuple(range(k,k+len(points)))); self.c.append(color)
    def box(self,x,y,z,w,d,h,c):
        pts=[(x+sx*w/2,y+sy*d/2,z+sz*h/2) for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        for face,light in [((0,3,2,1),.60),((4,5,6,7),1),((0,1,5,4),.70),((1,2,6,5),.83),((2,3,7,6),.78),((3,0,4,7),.65)]:
            self.face([pts[i] for i in face],tuple(v*light for v in c))
    def cone(self,x,y,z,r,h,c,n=7):
        for i in range(n):
            a=i*math.tau/n;b=(i+1)*math.tau/n
            self.face([(x+r*math.cos(a),y+r*math.sin(a),z),(x+r*math.cos(b),y+r*math.sin(b),z),(x,y,z+h)],tuple(v*(.76+.24*(i%3)/2) for v in c))
    def cylinder(self,x,y,z,r,h,c,n=8,axis='z'):
        def p(a,t):
            return (x+r*math.cos(a),y+r*math.sin(a),z+t) if axis=='z' else (x+r*math.cos(a),y+t,z+r*math.sin(a))
        for i in range(n):
            a=i*math.tau/n;b=(i+1)*math.tau/n
            self.face([p(a,0),p(b,0),p(b,h),p(a,h)],tuple(v*(.72+.28*(i%4)/3) for v in c))
        self.face([p(i*math.tau/n,h) for i in range(n)],c)
    def line(self,points,width,color,height=.03):
        for a,b in zip(points,points[1:]):
            delta=Vector((b[0]-a[0],b[1]-a[1])); normal=Vector((-delta.y,delta.x)).normalized()*width/2
            self.face([(a[0]-normal.x,a[1]-normal.y,height),(b[0]-normal.x,b[1]-normal.y,height),(b[0]+normal.x,b[1]+normal.y,height),(a[0]+normal.x,a[1]+normal.y,height)],color)
    def strip(self,x0,x1,y0,y1,z0,z1,color):
        self.face([(x0[0],y0,z0[0]),(x0[1],y0,z0[1]),(x1[1],y1,z1[1]),(x1[0],y1,z1[0])],color)
    def finish(self,offset):
        mesh=bpy.data.meshes.new(self.name);mesh.from_pydata(self.v,[],self.f);mesh.update()
        colors=mesh.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
        for p,c in zip(mesh.polygons,self.c):
            for i in p.loop_indices:colors.data[i].color=(*c,1)
        obj=bpy.data.objects.new(self.name,mesh);collection.objects.link(obj);obj.location=offset
        mat=bpy.data.materials.get('LevelKit Vertex Colors')
        if not mat:
            mat=bpy.data.materials.new('LevelKit Vertex Colors');mat.use_nodes=True
            node=mat.node_tree.nodes.new('ShaderNodeVertexColor');node.layer_name='Color'
            bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            mat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.87
        mesh.materials.append(mat)
        vertices=[];triangles=[];outcolors=[]
        for face,c in zip(self.f,self.c):
            base=len(vertices)//3
            for index in face:
                x,y,z=self.v[index];vertices += [x+z*self.projection[0],y+z*self.projection[1],-z];outcolors += [*c,1]
            for j in range(1,len(face)-1):triangles += [base,base+j+1,base+j]
        meshes.append(dict(name=self.name,vertices=vertices,triangles=triangles,colors=outcolors))
        return obj

def fir(b,x,y,scale,rng):
    b.box(x,y,.4*scale,.13*scale,.13*scale,.8*scale,(.24,.25,.23))
    for k in range(3):
        z=(.4+k*.43)*scale;r=(.73-k*.15)*scale
        b.cone(x,y,z,r,1.03*scale,(.12,.23,.24),12)
        b.cone(x,y,z+.24*scale,r*.92,1.02*scale,(.77,.86,.87),12)

def river(y): return 1.7*math.sin(y*math.tau*2/PERIOD)+.35*math.sin(y*math.tau*4/PERIOD+.5)
def river_width(y): return 1.3+.17*math.sin(y*math.tau*2/PERIOD+.3)

# SNOW: a continuous frozen river, raised banks, grouped fir forest and one timber bridge.
for section in range(4):
    rng=random.Random(1300+section);origin=section*20
    ground=Builder('SnowGround'+str(section));detail=Builder('SnowDetail'+str(section));near=Builder('SnowNear'+str(section))
    for row in range(40):
        y0=-10+row*.5;y1=y0+.5;g0=origin+y0;g1=origin+y1
        def edges(y):
            c=river(y);r=river_width(y)
            return [-11,-7.7,c-r-1.0,c-r-.28,c-r,c+r,c+r+.28,c+r+1.0,7.7,11]
        a=edges(g0);b=edges(g1)
        heights=[.28,.38,.20,.14,.02,.02,.14,.20,.38,.28]
        for i in range(len(a)-1):
            color=(.35,.57,.65) if i==4 else (.63,.74,.78) if i in [3,5] else (.72,.81,.82)
            if i!=4:color=tuple(v*(.98+.02*math.sin(g0*math.tau*4/PERIOD+i)) for v in color)
            ground.strip((a[i],a[i+1]),(b[i],b[i+1]),y0,y1,(heights[i],heights[i+1]),(heights[i],heights[i+1]),color)
        # Long, soft glacial streaks follow the river instead of random circular bumps.
        for side in [-1,1]:
            x0=river(g0)+side*river_width(g0)*.66;x1=river(g1)+side*river_width(g1)*.66
            ground.strip((x0-.02,x0+.02),(x1-.02,x1+.02),y0,y1,(.027,.027),(.027,.027),(.49,.70,.75))
    for j in range(8):
        y=rng.uniform(-9.5,9.5);x=river(origin+y)+rng.uniform(-.6,.6)
        ground.line([(x-.34,y-.5),(x-.08,y-.1),(x+.1,y+.1),(x+.42,y+.38)],.018,(.23,.44,.53),.035)
    for side in [-1,1]:
        for cluster in range(6):
            y=-8.5+cluster*3.1+rng.uniform(-.25,.25)
            shore=river(origin+y)+side*(river_width(origin+y)+1.55)
            for j in range(3):
                x=shore+side*rng.uniform(.1,2.6);yy=y+rng.uniform(-.55,.55)
                fir(detail,x,yy,rng.uniform(.65,1.15),rng)
            # Faceted snow ridges have a coherent silhouette at the outside edge.
            x=side*rng.uniform(7.1,8.8)
            near.cone(x,y,.2,rng.uniform(1.0,1.6),rng.uniform(.5,1.1),(.66,.77,.79),7)
            fir(near,x-side*.4,y+.25,rng.uniform(1.1,1.6),rng)
    if section==2:
        y=-1.5;c=river(origin+y);width=river_width(origin+y)*2+1.5
        detail.box(c,y,.35,width,1.15,.18,(.29,.30,.27))
        for j in range(15): detail.box(c-width/2+(j+.5)*width/15,y,.47,width/15-.025,1.1,.08,(.51,.50,.40))
        for sy in [-1,1]:
            detail.box(c,y+sy*.57,.78,width,.09,.1,(.35,.37,.34))
            for x in [c-width/2,c,c+width/2]:detail.box(x,y+sy*.57,.63,.08,.08,.65,(.36,.38,.34))
    if section in [0,3]:
        y=1.5;x=river(origin+y)-river_width(origin+y)-2.2
        detail.box(x,y,.5,1.25,1.5,.8,(.29,.33,.34))
        detail.box(x,y,1,1.45,1.7,.2,(.83,.87,.86))
        detail.box(x,y-.76,.65,.25,.025,.26,(.83,.65,.28))
        detail.cylinder(x+.4,y+.4,1.1,.10,.6,(.23,.27,.28))
    ground.finish((section*24,0,0));detail.finish((section*24,0,0));near.finish((section*24,0,0))

# TUNNEL: recessed deck plates, cable trenches, structural ribs, valves, conduits and overhead gantries.
for section in range(4):
    rng=random.Random(2300+section)
    ground=Builder('TunnelGround'+str(section));detail=Builder('TunnelDetail'+str(section));near=Builder('TunnelNear'+str(section))
    ground.box(0,0,-.24,22,20,.48,(.13,.17,.19))
    for row in range(8):
        y=-8.75+row*2.5
        for x in [-2.5,0,2.5]:
            ground.box(x,y,.025,2.43,2.42,.05,(.19,.24,.26) if (row+section)%3 else (.21,.26,.27))
            for sx in [-1,1]:
                for sy in [-1,1]: ground.box(x+sx*1.08,y+sy*1.05,.059,.05,.05,.025,(.37,.42,.40))
        for side in [-1,1]:
            ground.box(side*3.9,y,.045,.47,2.42,.08,(.055,.095,.11))
            for j in range(11):ground.box(side*3.9,y-1.1+j*.22,.1,.43,.06,.035,(.29,.36,.37))
    for side in [-1,1]:
        detail.box(side*6.9,0,.5,5.2,20,1.5,(.19,.25,.27))
        for x,r,c in [(4.65,.12,(.42,.48,.44)),(5.05,.18,(.30,.38,.38)),(6.2,.28,(.36,.41,.39)),(8.25,.20,(.43,.35,.23))]:
            detail.cylinder(side*x,-10,1.4,r,20,c,8,'y')
        for j in range(5):
            y=-8+j*4
            detail.box(side*6.45,y,1.4,4.6,.2,.3,(.34,.40,.40))
            detail.box(side*4.5,y+.4,1.35,.18,.85,.12,(.42,.78,.81))
            detail.box(side*6.1,y+1.1,1.42,1.8,1.0,.1,(.07,.12,.14))
            for k in range(6):detail.box(side*6.1,y+.7+k*.15,1.52,1.65,.05,.06,(.32,.41,.42))
            # Warm hazard stripes frame the work bays, with room left for enemy readability.
            for k in range(4):detail.box(side*(4.25+k*.19),y-.33,1.12,.12,.14,.025,(.64,.46,.17))
        y=-3+section*1.1
        detail.cylinder(side*5.05,y,1.55,.32,.11,(.63,.25,.17))
        detail.box(side*5.05,y,1.7,.46,.065,.06,(.29,.35,.35))
        detail.box(side*5.05,y,1.7,.065,.46,.06,(.29,.35,.35))
        near.box(side*7.8,0,2.4,1.2,20,.65,(.11,.17,.20))
    for y in [-5,5]:
        near.box(0,y,2.65,15.5,.28,.4,(.25,.33,.35))
        near.box(0,y+.01,2.89,4,.06,.045,(.44,.53,.51))
        for side in [-1,1]:
            near.box(side*4.6,y,2.8,.22,.8,.3,(.36,.44,.43))
            near.box(side*2.9,y,2.88,.7,.12,.06,(.65,.49,.20))
    ground.finish((section*24,28,0));detail.finish((section*24,28,0));near.finish((section*24,28,0))

# CITY: Manhattan-like blocks, brick infill, setbacks, fire escapes, water tanks and rooftop plant.
for section in range(4):
    rng=random.Random(3300+section)
    ground=Builder('CityGround'+str(section),(.22,.42));detail=Builder('CityDetail'+str(section),(.22,.42));near=Builder('CityNear'+str(section),(.22,.42))
    ground.box(0,0,-.25,22,20,.5,(.085,.115,.14))
    for x in [-8.5,0,8.5]:
        for lane in [-.11,.11]:ground.box(x+lane,0,.015,.035,20,.02,(.57,.46,.25))
        for y in range(-9,10,2):ground.box(x+.53,y,.025,.04,.8,.02,(.54,.59,.54))
    for y in [0]:
        ground.box(0,y,.02,22,2.1,.03,(.10,.14,.16))
        for x in [-8.5,0,8.5]:
            for k in range(7):ground.box(x-1.0+k*.33,y+.84,.045,.15,.48,.025,(.64,.67,.60))
    for col,x in enumerate([-7.0,-4.45,-1.9,1.9,4.45,7.0]):
        for row,y in enumerate([-7.4,-3.3,3.3,7.4]):
            w=rng.uniform(1.7,2.25);d=rng.uniform(2.3,3.2);h=rng.uniform(1.2,4.0)
            if col in [2,3] and row==(section%4): h=5.3
            c=rng.choice([(.30,.27,.24),(.32,.36,.36),(.21,.29,.34),(.37,.31,.28),(.26,.31,.33)])
            ground.box(x,y,.05,w+.3,d+.3,.12,(.34,.37,.34))
            # Cast silhouette under each tower, in the same projection as the raised architecture.
            ground.face([(x-w/2,y-d/2,.12),(x+w/2,y-d/2,.12),(x+w/2-h*.25,y-d/2-h*.38,.12),(x-w/2-h*.25,y-d/2-h*.38,.12)],(.035,.065,.085))
            detail.box(x,y,h/2,w,d,h,c)
            detail.box(x,y,h+.055,w+.1,d+.1,.11,tuple(v+.10 for v in c))
            detail.box(x,y,h+.12,w-.16,d-.16,.05,(.19,.25,.27))
            roof=h+.15
            if h>4:
                for step in range(3):
                    ww=w*(.80-step*.18);dd=d*(.82-step*.18)
                    detail.box(x,y,roof+.25,ww,dd,.5,tuple(v+.06 for v in c));roof+=.5
                detail.cone(x,y,roof,.11,1.2,(.55,.61,.60),5)
            else:
                for n in range(2):
                    detail.box(x+(n-.5)*w*.36,y+.25,roof+.13,w*.25,.45,.26,(.36,.42,.42))
                    for k in range(3):detail.box(x+(n-.5)*w*.36,y+.1+k*.13,roof+.27,w*.20,.025,.012,(.14,.22,.25))
                if rng.random()<.55:
                    tx=x+w*.22;ty=y-d*.18
                    for sx in [-1,1]:
                        for sy in [-1,1]:detail.box(tx+sx*.18,ty+sy*.18,roof+.2,.035,.035,.4,(.24,.30,.30))
                    detail.cylinder(tx,ty,roof+.38,.28,.55,(.36,.31,.25))
                    detail.cone(tx,ty,roof+.93,.31,.20,(.22,.29,.29),8)
            # Window rows are quads, not thousands of tiny cubes.
            for floor in range(1,int(h/.42)):
                z=floor*.42
                for wx in [-.58,0,.58]:
                    light=rng.choice([(.60,.56,.35),(.14,.24,.30),(.19,.32,.38),(.12,.20,.26)])
                    xx=x+wx*w/2
                    for sy in [-1,1]:
                        yy=y+sy*(d/2+.003)
                        detail.face([(xx-.12,yy,z-.1),(xx+.12,yy,z-.1),(xx+.12,yy,z+.1),(xx-.12,yy,z+.1)],light)
                if floor%2==0:
                    detail.box(x-w/2-.06,y-d*.2,z,.12,d*.34,.04,(.14,.20,.22))
            detail.box(x,y-d/2-.02,.32,.33,.045,.5,(.15,.22,.24))
            if (row+col+section)%5==0:detail.box(x,y-d/2-.1,.73,w*.7,.4,.09,(.24,.37,.38))
    # Near rooftop silhouettes frame the flight lane without filling the centre.
    for side in [-1,1]:
        for y in [-6.5,6.5]:
            near.box(side*9.0,y,3.8,2.0,3.2,1.0,(.16,.23,.27))
            near.cylinder(side*8.7,y,4.4,.45,.9,(.31,.28,.23))
            near.cone(side*8.7,y,5.3,.49,.35,(.19,.26,.29),8)
    ground.finish((section*24,56,0));detail.finish((section*24,56,0));near.finish((section*24,56,0))

# Instanced moving road traffic.
for name,c in [('CityTaxi',(.71,.52,.16)),('CitySedan',(.24,.36,.43))]:
    b=Builder(name,(.22,.42));b.box(0,0,.12,.28,.65,.20,c);b.box(0,.02,.25,.24,.31,.17,c)
    b.box(0,-.10,.34,.21,.07,.015,(.14,.25,.30));b.box(0,.14,.34,.21,.07,.015,(.14,.25,.30))
    for side in [-1,1]:b.box(side*.1,-.3,.2,.04,.025,.035,(.76,.78,.58))
    b.finish((0,84,0))

out=ROOT+'/Assets/Resources/LevelKit.json'
with open(out,'w') as f:json.dump(dict(meshes=meshes),f,separators=(',',':'))
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/LevelKit/LevelKit.blend')
print('Exported',len(meshes),'layer meshes;',sum(len(m['triangles'])//3 for m in meshes),'triangles;',sum(len(m['vertices'])//3 for m in meshes),'vertices')
