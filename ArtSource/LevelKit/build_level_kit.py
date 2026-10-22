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

def snow_height(x,y):
    return .22+.12*math.sin(y*math.tau*2/PERIOD+x*.18)+.055*math.cos(y*math.tau*4/PERIOD-x*.38)

# SNOW: an unbroken snowfield with wind-carved drifts and partly buried forest margins.
for section in range(4):
    rng=random.Random(1300+section);origin=section*20
    ground=Builder('SnowGround'+str(section));detail=Builder('SnowDetail'+str(section));near=Builder('SnowNear'+str(section))
    for row in range(40):
        y0=-10+row*.5;y1=y0+.5;g0=origin+y0;g1=origin+y1
        for col in range(16):
            x0=-11+col*22/16;x1=x0+22/16
            h00=snow_height(x0,g0);h01=snow_height(x1,g0)
            h10=snow_height(x0,g1);h11=snow_height(x1,g1)
            shade=.96+.025*math.sin(g0*math.tau*2/PERIOD+x0*.32)
            c=tuple(v*shade for v in (.80,.85,.86))
            ground.strip((x0,x1),(x0,x1),y0,y1,(h00,h01),(h10,h11),c)
    # Broad, shallow snow crests: quiet central terrain with no river, ice or bridge.
    for j in range(5):
        y=-8+j*3.6+rng.uniform(-.35,.35);x=rng.uniform(-5,5)
        z=snow_height(x,origin+y)+.015
        ground.face([(x-2.8,y-.35,z),(x-.4,y+.12,z+.045),(x+2.8,y+.4,z),(x+.3,y-.12,z)],(.76,.82,.84))
        ground.line([(x-2.6,y-.36),(x-.4,y+.1),(x+2.7,y+.39)],.065,(.85,.89,.89),z+.05)
    for side in [-1,1]:
        for cluster in range(5):
            y=-8+cluster*3.6+rng.uniform(-.3,.3)
            for j in range(2):
                x=side*rng.uniform(3.8,7.8);yy=y+rng.uniform(-.65,.65)
                fir(detail,x,yy,rng.uniform(.55,.95),rng)
                # Smooth snow caps cover roots and soften each stand of trees.
                detail.cone(x-.1,yy-.1,.25,.7,.25,(.80,.86,.87),10)
            x=side*rng.uniform(7.8,9.5)
            near.cone(x,y,.25,rng.uniform(1.2,1.8),rng.uniform(.35,.7),(.76,.83,.85),9)
            if cluster%2==section%2:fir(near,x,y+.2,rng.uniform(.9,1.3),rng)
    if section==1:
        # One low, snow-buried shelter gives scale without breaking the open snowfield.
        detail.box(-6.2,2,.38,1.3,1.6,.6,(.29,.34,.35))
        detail.box(-6.2,2,.78,1.55,1.85,.28,(.83,.88,.89))
        detail.cylinder(-5.8,2.4,.92,.09,.4,(.25,.30,.31))
    ground.finish((section*24,0,0));detail.finish((section*24,0,0));near.finish((section*24,0,0))
# TUNNEL: dim service galleries, alternating drainage, plant bays and restrained structural ribs.
for section in range(4):
    rng=random.Random(2300+section)
    ground=Builder('TunnelGround'+str(section));detail=Builder('TunnelDetail'+str(section));near=Builder('TunnelNear'+str(section))
    ground.box(0,0,-.24,22,20,.48,(.043,.060,.069))
    for row in range(8):
        y=-8.75+row*2.5
        for col,x in enumerate([-2.5,0,2.5]):
            c=(.075,.100,.109) if (row+section+col)%3 else (.087,.112,.120)
            ground.box(x,y,.025,2.43,2.42,.05,c)
            if section in [1,3] and col==1 and row%3==1:
                ground.box(x,y,.065,1.35,1.7,.06,(.035,.053,.063))
                for k in range(6):ground.box(x,y-.65+k*.25,.105,1.23,.055,.025,(.115,.145,.150))
            elif (row+col)%3==0:
                ground.box(x,y,.065,.8,.85,.06,(.065,.088,.099))
                ground.box(x,y-.28,.105,.3,.045,.025,(.16,.18,.17))
        for side in [-1,1]:
            ground.box(side*3.9,y,.045,.47,2.42,.08,(.025,.040,.049))
            for j in range(6):ground.box(side*3.9,y-1+j*.4,.1,.43,.055,.025,(.10,.14,.15))
    for side in [-1,1]:
        detail.box(side*6.9,0,.5,5.2,20,1.5,(.07,.10,.12))
        pipes=[(4.65,.12,(.17,.21,.20)),(5.15,.22,(.11,.16,.17))]
        if section in [0,2]:pipes += [(6.4,.38,(.14,.18,.18)),(8.25,.20,(.19,.16,.11))]
        else:pipes += [(7.2,.16,(.12,.17,.18)),(7.65,.16,(.12,.17,.18))]
        for x,r,c in pipes:detail.cylinder(side*x,-10,1.4,r,20,c,8,'y')
        for j in range(4):
            y=-7.5+j*5
            detail.box(side*6.45,y,1.4,4.6,.18,.25,(.13,.18,.19))
            # Pools of subdued light alternate sides; no luminous strip fills the flight lane.
            if (j+section+(side>0))%3==0:
                detail.box(side*4.5,y+.4,1.35,.12,.6,.09,(.25,.39,.39))
                detail.box(side*4.5,y+.4,1.31,.24,.8,.08,(.08,.13,.14))
            if section==0 or (section==2 and j%2==0):
                detail.box(side*6.1,y+1.15,1.42,1.9,1.3,.1,(.028,.047,.059))
                for k in range(5):detail.box(side*6.1,y+.7+k*.22,1.52,1.7,.05,.06,(.12,.17,.18))
            elif section==1:
                # Raised equipment lockers with different silhouettes and recessed access panels.
                detail.box(side*6.1,y+1.2,1.75,1.5,1.55,.65,(.105,.145,.16))
                detail.box(side*6.1,y+1.2,2.1,1.22,1.3,.05,(.055,.085,.10))
                detail.box(side*6.1,y+.75,2.14,.4,.08,.04,(.23,.20,.12))
            else:
                # Large fan housings break up the cable galleries in the fourth district.
                detail.box(side*6.2,y+1.1,1.65,2.1,1.8,.5,(.09,.13,.15))
                detail.cylinder(side*6.2,y+1.1,1.92,.67,.1,(.035,.060,.075),12)
                for angle in [0,math.pi/2,math.pi,math.pi*1.5]:
                    xx=side*6.2+math.cos(angle)*.25;yy=y+1.1+math.sin(angle)*.25
                    detail.box(xx,yy,2.05,.18,.5,.035,(.14,.18,.18))
            if j==section%4:
                for k in range(3):detail.box(side*(4.25+k*.22),y-.35,1.12,.13,.12,.025,(.28,.23,.12))
        if section%2==0:
            y=-3+section*1.1
            detail.cylinder(side*5.15,y,1.65,.32,.11,(.28,.13,.10))
            detail.box(side*5.15,y,1.8,.46,.065,.06,(.15,.19,.19))
            detail.box(side*5.15,y,1.8,.065,.46,.06,(.15,.19,.19))
        near.box(side*7.8,0,2.4,1.2,20,.65,(.045,.075,.095))
    for y in ([-5,5] if section%2==0 else [0]):
        near.box(0,y,2.65,15.5,.19,.25,(.115,.16,.18))
        for side in [-1,1]:
            near.box(side*4.6,y,2.8,.22,.8,.3,(.17,.21,.21))
            near.box(side*3.7,y,2.82,.32,.09,.04,(.27,.23,.13))
    ground.finish((section*24,28,0));detail.finish((section*24,28,0));near.finish((section*24,28,0))
# CITY: a legible avenue grid with restrained, varied New York blocks below the flight lane.
for section in range(4):
    rng=random.Random(3300+section)
    ground=Builder('CityGround'+str(section),(.14,.24));detail=Builder('CityDetail'+str(section),(.14,.24));near=Builder('CityNear'+str(section),(.14,.24))
    ground.box(0,0,-.25,22,20,.5,(.075,.095,.108))
    # Three continuous avenues meet one cross street per section. Markings stop at junctions.
    for x in [-8.5,0,8.5]:
        ground.box(x,0,.01,2.6,20,.02,(.085,.105,.116))
        for y0,y1 in [(-10,-1.7),(1.7,10)]:
            for lane in [-.065,.065]:ground.box(x+lane,(y0+y1)/2,.028,.025,y1-y0,.015,(.30,.28,.19))
            for y in [-8,-5,-2.5,2.5,5,8]:
                if y0<y<y1:
                    for sx in [-1,1]:ground.box(x+sx*.75,y,.03,.025,.5,.012,(.29,.32,.31))
    ground.box(0,0,.018,22,2.6,.024,(.085,.105,.116))
    for avenue in [-8.5,0,8.5]:
        for side in [-1,1]:
            for k in range(6):
                ground.box(avenue-.95+k*.38,side*1.45,.04,.19,.42,.018,(.35,.38,.36))
    for side in [-1,1]:
        for row,y in enumerate([-5.35,5.35]):
            # Connected sidewalk islands follow each block, leaving cross streets completely clear.
            ground.box(side*4.25,y,.055,5.9,7.5,.11,(.20,.235,.24))
            for col in range(2):
                x=side*(2.8+col*3.0)
                style=(section+row*2+col+(side>0))%4
                w=2.35;d=6.0;h=[1.25,2.1,3.0,2.6][style]+rng.uniform(-.12,.12)
                c=[(.22,.205,.195),(.195,.225,.235),(.20,.235,.25),(.235,.23,.21)][style]
                if style==0:
                    # Low brick warehouses: broad quiet roofs and a single clerestory band.
                    detail.box(x,y,h/2,w,d,h,c)
                    detail.box(x,y,h+.06,w+.08,d+.08,.12,(.25,.27,.27))
                    detail.box(x,y,h+.14,w-.18,d-.18,.035,(.16,.19,.20))
                    detail.box(x,y+1.3,h+.22,1.5,.6,.17,(.215,.25,.26))
                elif style==1:
                    # Courtyard apartments: a long wing with a short return, open to the street.
                    detail.box(x-side*.4,y,h/2,w*.64,d,h,c)
                    detail.box(x+side*.55,y+2,h/2,w*.5,2,h,c)
                    detail.box(x-side*.4,y,h+.05,w*.69,d+.08,.1,(.255,.28,.28))
                    detail.box(x+side*.55,y+2,h+.05,w*.54,2.08,.1,(.255,.28,.28))
                elif style==2:
                    # Art Deco setbacks change the skyline without filling the avenue with shadows.
                    detail.box(x,y,h*.36,w,d,h*.72,c)
                    detail.box(x,y,h*.82,w*.76,d*.78,h*.28,(.235,.265,.275))
                    detail.box(x,y,h+.16,w*.48,d*.53,.32,(.255,.28,.29))
                    if row==section%2:
                        detail.box(x,y,h+.55,.065,.065,.65,(.30,.33,.33))
                else:
                    # Brownstone rows: staggered three-part roofs with subtle brick variation.
                    for unit in range(3):
                        yy=y+(unit-1)*1.96;hh=h+(unit%2)*.3
                        cc=tuple(v*(.94+unit*.035) for v in c)
                        detail.box(x,yy,hh/2,w,1.9,hh,cc)
                        detail.box(x,yy,hh+.06,w+.06,1.94,.12,(.27,.275,.26))
                # A few dark window bands replace the former sparkling high-contrast window grid.
                front=y-d/2-.006
                for floor in range(1,max(2,int(h/.65))):
                    z=floor*.6
                    for xx in [-.65,0,.65]:
                        detail.face([(x+xx-.13,front,z-.10),(x+xx+.13,front,z-.10),(x+xx+.13,front,z+.10),(x+xx-.13,front,z+.10)],(.125,.17,.19))
                if style==0 and col==section%2:
                    # Sparse water towers keep the city's identity without rooftop clutter everywhere.
                    tx=x+.45;ty=y+.7
                    for sx in [-1,1]:
                        for sy in [-1,1]:detail.box(tx+sx*.18,ty+sy*.18,h+.3,.035,.035,.55,(.17,.20,.20))
                    detail.cylinder(tx,ty,h+.5,.3,.55,(.255,.235,.20))
                    detail.cone(tx,ty,h+1.05,.33,.20,(.18,.22,.23),8)
    # Distant edge masses give extra parallax; the central avenue stays open.
    for side in [-1,1]:
        near.box(side*9.8,4.8,1.7,1.6,5.8,3.4,(.15,.19,.21))
        near.box(side*9.8,4.8,3.45,1.68,5.88,.1,(.21,.24,.25))
    ground.finish((section*24,56,0));detail.finish((section*24,56,0));near.finish((section*24,56,0))
# Instanced moving road traffic.
for name,c in [('CityTaxi',(.36,.30,.16)),('CitySedan',(.16,.23,.27))]:
    b=Builder(name,(.22,.42));b.box(0,0,.12,.28,.65,.20,c);b.box(0,.02,.25,.24,.31,.17,c)
    b.box(0,-.10,.34,.21,.07,.015,(.14,.25,.30));b.box(0,.14,.34,.21,.07,.015,(.14,.25,.30))
    for side in [-1,1]:b.box(side*.1,-.3,.2,.04,.025,.035,(.76,.78,.58))
    b.finish((0,84,0))

out=ROOT+'/Assets/Resources/LevelKit.json'
with open(out,'w') as f:json.dump(dict(meshes=meshes),f,separators=(',',':'))
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/LevelKit/LevelKit.blend')
print('Exported',len(meshes),'layer meshes;',sum(len(m['triangles'])//3 for m in meshes),'triangles;',sum(len(m['vertices'])//3 for m in meshes),'vertices')
