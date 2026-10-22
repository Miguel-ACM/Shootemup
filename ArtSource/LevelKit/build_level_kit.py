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
    # Tall, broken branch whorls: jagged conifer silhouettes rather than stacked round cones.
    yaw=rng.uniform(0,math.tau);lean=rng.uniform(-.10,.10)*scale
    b.cylinder(x,y,.22*scale,.085*scale,1.1*scale,(.24,.23,.20),5)
    b.face([(x-.7*scale,y-.15*scale,.24),(x+.35*scale,y-.4*scale,.24),(x+1.0*scale,y+1.2*scale,.24),(x+.1*scale,y+1.5*scale,.24)],(.69,.76,.78))
    for tier in range(4):
        radius=(.93-tier*.19)*scale*rng.uniform(.92,1.08)
        z=(.62+tier*.71)*scale
        cx=x+lean*tier/3;cy=y
        peak=(cx,cy,z+1.03*scale)
        ring=[]
        for k in range(10):
            a=yaw+k*math.tau/10+tier*.27
            r=radius*(rng.uniform(.91,1.08) if k%2==0 else rng.uniform(.69,.79))
            ring.append((cx+math.cos(a)*r,cy+math.sin(a)*r,z+(0 if k%2==0 else .12*scale)))
        for k in range(10):
            a=ring[k];d=ring[(k+1)%10]
            light=.82+.18*(math.cos(yaw+k*math.tau/10-1)*.5+.5)
            b.face([a,d,peak],tuple(v*light for v in (.18,.30,.26)))
            # Uneven snow blankets sit directly on the branch planes; exposed tips remain green.
            cover=rng.uniform(.50,.73)
            aa=tuple(peak[j]+(a[j]-peak[j])*cover for j in range(3))
            dd=tuple(peak[j]+(d[j]-peak[j])*cover for j in range(3))
            snow_peak=(peak[0],peak[1],peak[2]+.018*scale)
            aa=(aa[0],aa[1],aa[2]+.018*scale);dd=(dd[0],dd[1],dd[2]+.018*scale)
            snow=(.84,.88,.88) if k%3 else (.67,.77,.80)
            b.face([aa,dd,snow_peak],tuple(v*light for v in snow))
    # A single pointed leader makes each crown read as a fir at the game's small render size.
    b.cone(x+lean,y,3.35*scale,.19*scale,.58*scale,(.68,.79,.80),5)
def snow_height(x,y):
    return .22+.12*math.sin(y*math.tau*2/PERIOD+x*.18)+.055*math.cos(y*math.tau*4/PERIOD-x*.38)

# SNOW: an unbroken snowfield with wind-carved drifts and partly buried forest margins.
for section in range(4):
    rng=random.Random(1300+section);origin=section*20
    ground=Builder('SnowGround'+str(section));detail=Builder('SnowDetail'+str(section),(.12,.55));near=Builder('SnowNear'+str(section),(.12,.55))
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
# CITY: a city grid seen from high above, with many small blocks visible at the same time.
for section in range(4):
    rng=random.Random(3300+section)
    ground=Builder('CityGround'+str(section),(.14,.24));detail=Builder('CityDetail'+str(section),(.14,.24));near=Builder('CityNear'+str(section),(.14,.24))
    ground.box(0,0,-.1,22,20,.2,(.145,.18,.205))
    avenues=[k*2.6 for k in range(-4,5)]
    crossings=[-10+k*2.5 for k in range(9)]
    # The section length is exactly eight street pitches, including half a road at each seam.
    for col,x in enumerate(avenues):
        width=.52 if col%2==0 else .4
        ground.box(x,0,.012,width,20,.024,(.135,.17,.195))
        for row in range(8):
            yy=-8.75+row*2.5
            for lane in [-.035,.035]:ground.box(x+lane,yy,.028,.012,1.75,.01,(.30,.29,.235))
    for row,y in enumerate(crossings):
        half_width=.3 if row%4==0 else .2
        y0=max(-10,y-half_width);y1=min(10,y+half_width)
        ground.box(0,(y0+y1)/2,.016,22,y1-y0,.025,(.14,.175,.20))
        # Small crossing marks occur only on the broader junctions, well below the flight layer.
        if row%4==0:
            for x in avenues:
                for side in [-1,1]:
                    for k in range(3):ground.box(x-.14+k*.14,y+side*.35,.035,.06,.12,.012,(.31,.34,.335))
    palette=[(.255,.27,.275),(.275,.265,.245),(.225,.255,.275),(.29,.285,.265),(.24,.265,.27)]
    for col in range(8):
        x=-9.1+col*2.6
        for row in range(8):
            y=-8.75+row*2.5
            ground.box(x,y,.035,2.04,1.93,.07,(.20,.235,.25))
            park=section==1 and col==5 and row==4
            if park:
                ground.box(x,y,.085,1.82,1.7,.04,(.20,.285,.245))
                ground.box(x,y,.115,.11,1.7,.015,(.30,.315,.29))
                for j in range(5):
                    xx=x+(-.5 if j%2 else .5);yy=y+(j//2-1)*.48
                    detail.cone(xx,yy,.12,.16,.15,(.19,.265,.23),6)
                continue
            # Four separate small lots per block, with narrow alleys between the buildings.
            for lot in range(4):
                xx=x+(-.49 if lot%2==0 else .49)+rng.uniform(-.035,.035)
                yy=y+(-.46 if lot<2 else .46)+rng.uniform(-.035,.035)
                w=rng.uniform(.63,.85);d=rng.uniform(.66,.84);h=rng.uniform(.19,.48)
                style=(col*3+row+lot+section)%5;c=palette[(style+section)%len(palette)]
                if style==0:
                    # Broad warehouse roofs, with a single low clerestory.
                    detail.box(xx,yy,h/2,w,d,h,c)
                    detail.box(xx,yy,h+.018,w+.025,d+.025,.036,tuple(v+.025 for v in c))
                    detail.box(xx,yy,h+.055,w*.62,d*.23,.055,(.25,.28,.29))
                elif style==1:
                    # L-shaped apartment blocks leave visible interior courtyards.
                    detail.box(xx-w*.18,yy,h/2,w*.64,d,h,c)
                    detail.box(xx+w*.28,yy+d*.28,h/2,w*.44,d*.44,h,c)
                    detail.box(xx-w*.18,yy,h+.02,w*.66,d+.02,.04,tuple(v+.025 for v in c))
                elif style==2:
                    # A taller stepped tower is still a tiny object from this altitude.
                    h*=1.55
                    detail.box(xx,yy,h*.33,w,d,h*.66,c)
                    detail.box(xx,yy,h*.80,w*.7,d*.72,h*.28,tuple(v+.025 for v in c))
                    detail.box(xx,yy,h+.035,w*.46,d*.48,.07,tuple(v+.045 for v in c))
                    if (col+row+section)%7==0:detail.box(xx,yy,h+.16,.025,.025,.25,(.31,.335,.345))
                elif style==3:
                    # Paired brick row houses with staggered rooflines.
                    for unit in [-1,1]:
                        hh=h+(unit+1)*.035
                        detail.box(xx+unit*w*.25,yy,hh/2,w*.47,d,hh,c)
                        detail.box(xx+unit*w*.25,yy,hh+.016,w*.48,d+.015,.032,tuple(v+.025 for v in c))
                else:
                    detail.box(xx,yy,h/2,w,d,h,c)
                    detail.box(xx,yy,h+.018,w*.86,d*.86,.035,(.205,.24,.265))
                    if (row+lot)%4==0:
                        detail.cylinder(xx+w*.2,yy-d*.18,h+.07,.075,.11,(.27,.255,.225),6)
                        detail.cone(xx+w*.2,yy-d*.18,h+.18,.085,.045,(.225,.26,.27),6)
                # One subdued facade band suggests floors without glittering window grids.
                if h>.32:
                    z=h*.48;front=yy-d/2-.002
                    detail.face([(xx-w*.3,front,z-.025),(xx+w*.3,front,z-.025),(xx+w*.3,front,z+.025),(xx-w*.3,front,z+.025)],(.18,.22,.245))
    # Small edge rooftops provide a second depth response, never a close-up canyon wall.
    for side in [-1,1]:
        for y in [-6.2,3.8]:
            near.box(side*9.7,y,.3,.7,.8,.6,(.23,.26,.28))
            near.box(side*9.7,y,.62,.55,.63,.04,(.275,.295,.305))
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
