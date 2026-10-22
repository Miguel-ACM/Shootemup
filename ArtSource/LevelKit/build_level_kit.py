import bpy, math, random, json, os
from mathutils import Vector
ROOT = r'C:/Users/Miguel/Shootemup'
rng = random.Random(927)
collection=bpy.data.collections.get('Five Level Kit')
if collection:
    for obj in list(collection.objects): bpy.data.objects.remove(obj,do_unlink=True)
else:
    collection=bpy.data.collections.new('Five Level Kit');bpy.context.scene.collection.children.link(collection)
meshes=[]
class Builder:
    def __init__(self,name): self.name=name;self.v=[];self.f=[];self.c=[]
    def box(self,x,y,z,w,d,h,c):
        k=len(self.v)
        self.v += [(x+sx*w/2,y+sy*d/2,z+sz*h/2) for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        for face,light in [((0,3,2,1),.65),((4,5,6,7),1),((0,1,5,4),.73),((1,2,6,5),.86),((2,3,7,6),.8),((3,0,4,7),.7)]:
            self.f.append(tuple(k+i for i in face));self.c.append(tuple(v*light for v in c))
    def cone(self,x,y,z,r,h,c,n=7):
        k=len(self.v); self.v += [(x+math.cos(i*math.tau/n)*r,y+math.sin(i*math.tau/n)*r,z) for i in range(n)]+[(x,y,z+h)]
        for i in range(n):self.f.append((k+i,k+(i+1)%n,k+n));self.c.append(tuple(v*(.76+.24*(i%3)/2) for v in c))
    def ellipsoid(self,x,y,z,rx,ry,rz,c):
        k=len(self.v);n=12;m=6
        for j in range(m+1):
            p=math.pi*j/m
            for i in range(n):
                a=i*math.tau/n; self.v.append((x+rx*math.sin(p)*math.cos(a),y+ry*math.sin(p)*math.sin(a),z+rz*math.cos(p)))
        for j in range(m):
            for i in range(n): self.f.append((k+j*n+i,k+j*n+(i+1)%n,k+(j+1)*n+(i+1)%n,k+(j+1)*n+i));self.c.append(tuple(v*(.7+.3*j/m) for v in c))
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
            bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');mat.node_tree.links.new(node.outputs['Color'],bsdf.inputs['Base Color']);bsdf.inputs['Roughness'].default_value=.85
        mesh.materials.append(mat)
        # Export exact game-plane coordinates. Height points toward the camera at negative Z.
        vertices=[];triangles=[];outcolors=[]
        for face,c in zip(self.f,self.c):
            base=len(vertices)//3
            for index in face:
                x,y,z=self.v[index];vertices += [x,y,-z];outcolors += [*c,1]
            for j in range(1,len(face)-1):triangles += [base,base+j+1,base+j]
        meshes.append(dict(name=self.name,vertices=vertices,triangles=triangles,colors=outcolors))
        return obj
for variant in range(3):
    b=Builder('CityBlock'+str(variant));b.box(0,0,-.25,16,20,.5,(.085,.105,.13))
    for x in [-7.6,0,7.6]:
        b.box(x,0,.015,.06,20,.02,(.65,.5,.25))
        for y in range(-9,10,2):b.box(x+.12,y,.02,.05,.65,.02,(.7,.58,.36))
    for x in [-5.45,-2.15,2.15,5.45]:
        for y in [-7.6,-2.6,2.6,7.6]:
            w=rng.uniform(1.7,2.65);d=rng.uniform(2.7,3.8);h=rng.uniform(1,4.4)
            c=rng.choice([(.24,.29,.34),(.30,.25,.23),(.35,.36,.33),(.2,.26,.30)])
            b.box(x,y,h/2,w,d,h,c);b.box(x,y,h+.09,w+.08,d+.08,.18,(.42,.45,.45))
            b.box(x,y,h+.3,w*.5,d*.4,.42,(.20,.24,.26))
            if h>3.5:b.cone(x,y,h+.5,.06,1.2,(.52,.59,.61),4)
            for sy in [-1,1]:
                for wx in [-.55,0,.55]:
                    for floor in range(1,int(h/.48)):
                        b.box(x+wx,y+sy*(d/2+.012),floor*.48,.22,.025,.21,(.76,.65,.37) if rng.random()<.45 else (.12,.19,.24))
            for yy in [-.9,0,.9]:
                b.box(x+w/2+.012,y+yy,h*.6,.025,.25,.25,(.66,.63,.43))
    for y in [-10,0,10]:
        b.box(0,y,.02,16,.65,.03,(.1,.12,.14))
        for x in [-6,-3,3,6]: b.box(x,y,.045,1.1,.07,.03,(.75,.73,.6))
    b.finish((variant*19,0,0))
b=Builder('TunnelSection');b.box(0,0,-.4,16,20,.8,(.15,.18,.20))
for side in [-1,1]:
    b.box(side*6.2,0,.5,3.5,20,1.8,(.22,.26,.27))
    for x in [4.4,5,7.4]: b.box(side*x,0,1,.13,20,.24,(.43,.48,.45))
    for y in [-8,-4,0,4,8]:
        b.box(side*6.1,y,1.7,3.8,.25,.35,(.4,.44,.43))
        b.box(side*4.6,y,1.4,.22,1.2,.12,(.48,.78,.8))
        b.box(side*6,y+.9,1.48,1.6,.8,.08,(.10,.13,.14))
        for j in range(4):b.box(side*6,y+.6+j*.18,1.55,1.5,.065,.1,(.35,.4,.4))
for y in range(-10,11,2):b.box(0,y,.04,.09,.8,.03,(.55,.48,.3))
b.finish((0,25,0))
b=Builder('SnowSection');b.box(0,0,-.4,16,20,.8,(.66,.77,.81))
for i in range(45):
    x=rng.uniform(-8,8);y=rng.uniform(-10,10)
    b.ellipsoid(x,y,0,rng.uniform(.25,1.1),rng.uniform(.3,1.3),rng.uniform(.1,.35),(.76,.86,.9))
for side in [-1,1]:
    for j in range(12):
        x=side*rng.uniform(3.6,7.8);y=rng.uniform(-10,10)
        b.box(x,y,.3,.15,.15,.7,(.3,.33,.32))
        for k in range(3):
            b.cone(x,y,.35+k*.4,.65-k*.12,.95,(.16,.28,.29));b.cone(x,y,.58+k*.4,.48-k*.1,.73,(.82,.89,.9))
b.finish((19,25,0))
b=Builder('StormCloud')
for i in range(17):b.ellipsoid(rng.uniform(-2.7,2.7),rng.uniform(-1,1),rng.uniform(0,.5),rng.uniform(.9,1.6),rng.uniform(.65,1.2),rng.uniform(.4,.8),(.12,.16,.21))
b.finish((38,25,0))
out=ROOT+'/Assets/Resources/LevelKit.json'
with open(out,'w') as f:json.dump(dict(meshes=meshes),f,separators=(',',':'))
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/ArtSource/LevelKit/LevelKit.blend')
print('Exported',[(m['name'],len(m['vertices'])//3,len(m['triangles'])//3) for m in meshes])
