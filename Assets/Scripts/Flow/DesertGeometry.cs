using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Broad asymmetric sand ridges share one continuous, faceted eighty-unit surface.
public static class DesertGeometry
{
    static readonly Vector3 Sun=new Vector3(-.55f,-.70f,1).normalized;
    sealed class MeshBuilder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector3> surfacePoints=new List<Vector3>(),surfaceNormals=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        struct Solid { public Vector3 min,max;public bool round; }
        readonly List<Solid> solids=new List<Solid>();
        public void Face(Color color,params Vector3[] points)
        {
            int start=vertices.Count;color.a=1;
            var normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
            var centre=Vector3.zero;foreach(var p in points)centre+=p;centre/=points.Length;
            foreach(var p in points)
            {
                vertices.Add(new Vector3(p.x+p.z*.20f,p.y+p.z*.30f,-p.z));colors.Add(color);
                surfacePoints.Add(Vector3.Lerp(p,centre,.08f));surfaceNormals.Add(normal);
            }
            for(int i=1;i<points.Length-1;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        }
        public void Box(Vector3 p,Vector3 size,Color color)
        {
            float a=p.x-size.x/2,b=p.x+size.x/2,c=p.y-size.y/2,d=p.y+size.y/2,z=p.z,t=z+size.z;
            solids.Add(new Solid{min=new Vector3(a,c,z),max=new Vector3(b,d,t)});
            Face(color,new Vector3(a,c,z),new Vector3(b,c,z),new Vector3(b,c,t),new Vector3(a,c,t));
            Face(color,new Vector3(b,c,z),new Vector3(b,d,z),new Vector3(b,d,t),new Vector3(b,c,t));
            Face(color,new Vector3(a,d,z),new Vector3(a,c,z),new Vector3(a,c,t),new Vector3(a,d,t));
            Face(color,new Vector3(b,d,z),new Vector3(a,d,z),new Vector3(a,d,t),new Vector3(b,d,t));
            Face(color,new Vector3(a,c,t),new Vector3(b,c,t),new Vector3(b,d,t),new Vector3(a,d,t));
        }
        public void Silo(Vector3 p,float radius,float height,Color color)
        {
            solids.Add(new Solid{min=p-new Vector3(radius,radius,0),max=p+new Vector3(radius,radius,height),round=true});
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Vector3 u=p+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),v=p+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius);
                Face(color,u,v,v+Vector3.forward*height,u+Vector3.forward*height);
                Face(color,u+Vector3.forward*height,v+Vector3.forward*height,p+Vector3.forward*(height+radius*.28f));
            }
        }
        public bool Occluded(Vector3 p,Vector3 direction)
        {
            foreach(var solid in solids)
            {
                float enter=.003f,exit=5;
                bool Slab(float at,float velocity,float min,float max)
                {
                    if(Mathf.Abs(velocity)<.00001f)return at>=min && at<=max;
                    float a=(min-at)/velocity,b=(max-at)/velocity;
                    enter=Mathf.Max(enter,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));return exit>=enter;
                }
                if(!Slab(p.z,direction.z,solid.min.z,solid.max.z))continue;
                if(!solid.round)
                {
                    if(Slab(p.x,direction.x,solid.min.x,solid.max.x) && Slab(p.y,direction.y,solid.min.y,solid.max.y))return true;
                    continue;
                }
                Vector2 offset=(Vector2)(p-(solid.min+solid.max)*.5f),d=direction;
                float radius=(solid.max.x-solid.min.x)*.5f,a2=Vector2.Dot(d,d),b2=Vector2.Dot(offset,d);
                float discriminant=b2*b2-a2*(offset.sqrMagnitude-radius*radius);
                if(discriminant<0)continue;
                float root=Mathf.Sqrt(discriminant);
                if(Mathf.Max(enter,(-b2-root)/a2)<=Mathf.Min(exit,(-b2+root)/a2))return true;
            }
            return false;
        }
        public float Shadow(Vector3 p)
        {
            // A small sun disc softens the silhouette edge without a detached rectangular shadow card.
            float shade=Occluded(p,Sun) ? .5f : 0;
            if(Occluded(p,(Sun+new Vector3(.035f,-.025f,0)).normalized))shade+=.25f;
            if(Occluded(p,(Sun+new Vector3(-.035f,.025f,0)).normalized))shade+=.25f;
            return shade;
        }
        public Mesh Finish(string name)
        {
            for(int i=0;i<colors.Count;i++)
            {
                var normal=surfaceNormals[i];float direct=Mathf.Max(0,Vector3.Dot(normal,Sun));
                float shade=direct>0 ? Shadow(surfacePoints[i]+normal*.012f) : 0;
                var c=colors[i]*(.62f+.38f*direct*(1-shade*.8f));c.a=1;colors[i]=c;
            }
            var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetColors(colors);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
    struct Dune { public float x,y,width,windward,lee,rise,bow,c,s; }
    static Dune[] dunes;
    static int layoutSeed;
    static void Layout(int seed)
    {
        if(dunes!=null && layoutSeed==seed)return;
        layoutSeed=seed;var random=new System.Random(seed);dunes=new Dune[32];
        float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        for(int row=0;row<8;row++)for(int col=0;col<4;col++)
        {
            float angle=R(-.8f,-.35f);
            dunes[row*4+col]=new Dune{x=-13+col*8+(row%2)*3.7f+R(-2,2),y=row*10+R(-2.2f,2.2f),
                width=R(3.8f,6.5f),windward=R(3.5f,6),lee=R(1.6f,2.8f),rise=R(.9f,1.5f),bow=R(-.55f,.55f),c=Mathf.Cos(angle),s=Mathf.Sin(angle)};
        }
    }
    public static float Height(float x,float y,int seed)
    {
        Layout(seed);float height=0;
        foreach(var dune in dunes)
        {
            float dx=x-dune.x,dy=Mathf.Repeat(y-dune.y+40,80)-40;
            if(Mathf.Abs(dx)>11 || Mathf.Abs(dy)>11)continue;
            float u=(dx*dune.c+dy*dune.s)/dune.width;
            if(Mathf.Abs(u)>=1)continue;
            // Deliberate broad planes and a bent crest, rather than a serrated row of tiny triangles.
            float bend=u<-.35f ? Mathf.Lerp(0,-1,(u+1)/.65f) : u<.4f ? Mathf.Lerp(-1,1,(u+.35f)/.75f) : Mathf.Lerp(1,0,(u-.4f)/.6f);
            float v=-dx*dune.s+dy*dune.c+dune.bow*bend;
            float span=v<0 ? dune.windward : dune.lee;
            if(Mathf.Abs(v)>=span)continue;
            float profile=1-Mathf.Abs(v)/span;
            float end=Mathf.Abs(u);
            float taper=end<.32f ? Mathf.Lerp(1,.9f,end/.32f) : end<.72f ? Mathf.Lerp(.9f,.46f,(end-.32f)/.4f) : Mathf.Lerp(.46f,0,(end-.72f)/.28f);
            float mound=dune.rise*taper*profile;
            height=Mathf.Max(height,mound);
        }
        return .08f+height;
    }
    public static Mesh Ground(int section,int seed)
    {
        const int columns=80,rows=80;
        float origin=(section-1)*20;
        var points=new Vector3[(columns+1)*(rows+1)];
        var lighting=new float[points.Length];var shadows=new float[points.Length];
        var vertices=new Vector3[columns*rows*6];var colors=new Color[vertices.Length];var triangles=new int[vertices.Length];
        var buildings=BuildBuildings(section,seed);
        float Jitter(int x,int y,int salt)
        {
            unchecked{uint h=(uint)(seed+x*374761393+y*668265263+salt);h=(h^(h>>13))*1274126177;return ((h^(h>>16))&65535)/65535f-.5f;}
        }
        for(int row=0;row<=rows;row++)for(int col=0;col<=columns;col++)
        {
            int worldRow=((section-1)*rows+row+rows*4)%(rows*4);
            float x=-14+col*(28f/columns)+(col>0 && col<columns ? Jitter(col,worldRow,0)*.12f : 0);
            float y=-10+row*(20f/rows)+(row>0 && row<rows ? Jitter(col,worldRow,71)*.10f : 0);
            int k=row*(columns+1)+col;var p=new Vector3(x,y,Height(x,origin+y,seed));points[k]=p;
            const float e=.09f;
            var normal=new Vector3(-(Height(x+e,origin+y,seed)-Height(x-e,origin+y,seed))/(2*e),
                -(Height(x,origin+y+e,seed)-Height(x,origin+y-e,seed))/(2*e),1).normalized;
            lighting[k]=Mathf.InverseLerp(.28f,.98f,Vector3.Dot(normal,Sun));
            shadows[k]=buildings.Shadow(p+Vector3.forward*.025f);
        }
        int vertex=0;
        void Triangle(int ia,int ib,int ic)
        {
            var a=points[ia];var b=points[ib];var c=points[ic];
            var normal=Vector3.Cross(b-a,c-a).normalized;if(normal.z<0)normal=-normal;
            float light=Mathf.InverseLerp(.28f,.98f,Vector3.Dot(normal,Sun));
            void Add(int index)
            {
                var p=points[index];float shadow=shadows[index];
                var sand=Color.Lerp(new Color(.58f,.43f,.27f),new Color(.80f,.66f,.44f),Mathf.Lerp(lighting[index],light,.18f));
                colors[vertex]=Color.Lerp(sand,new Color(sand.r*.69f,sand.g*.72f,sand.b*.76f),shadow);
                vertices[vertex]=new Vector3(p.x+p.z*.2f,p.y+p.z*.3f,-p.z);triangles[vertex]=vertex;vertex++;
            }
            Add(ia);Add(ib);Add(ic);
        }
        for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
        {
            int k=row*(columns+1)+col;int a=k,b=k+1,c=k+columns+1,d=k+columns+2;
            if((row+col)%2==0){Triangle(a,c,d);Triangle(a,d,b);}else{Triangle(a,c,b);Triangle(b,c,d);}
        }
        var mesh=new Mesh{name="Faceted wind-sculpted dunes "+section};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static Mesh Buildings(int section,int seed)=>BuildBuildings(section,seed).Finish("Desert silos and compact outposts "+section);
    static MeshBuilder BuildBuildings(int section,int seed)
    {
        var b=new MeshBuilder();var r=new System.Random(seed+section*3817);float origin=(section-1)*20;
        float R(float a,float z)=>Mathf.Lerp(a,z,(float)r.NextDouble());
        var gray=new Color(.39f,.42f,.42f);var dark=new Color(.16f,.19f,.19f);
        for(int site=0;site<4;site++)
        {
            float x=(site%2==0 ? -1 : 1)*R(2.1f,2.9f),y=-7+site*4.4f+R(-.5f,.5f),z=Height(x,origin+y,seed)+.05f;
            bool silos=(site+section)%2==0;
            b.Box(new Vector3(x,y,z),new Vector3(silos ? 2.05f : 1.82f,silos ? 1.45f : 1.62f,.08f),new Color(.55f,.52f,.44f));
            if((site+section)%2==0)
            {
                // Small grain/fuel silos: concrete plinths, collars, conical caps and pipe manifold.
                foreach(float dx in new[]{-.62f,.62f})
                {
                    var p=new Vector3(x+dx,y,z+.09f);b.Silo(p,.43f,.14f,dark);b.Silo(p+Vector3.forward*.14f,.36f,.85f,gray);
                    b.Silo(p+Vector3.forward*.58f,.375f,.075f,dark);
                    b.Box(p+new Vector3(.33f,-.12f,.2f),new Vector3(.06f,.10f,.73f),new Color(.65f,.56f,.37f));
                }
                b.Box(new Vector3(x,y-.65f,z+.15f),new Vector3(1.7f,.13f,.15f),gray*.8f);
            }
            else
            {
                float h=R(.45f,.8f);b.Box(new Vector3(x,y,z+.08f),new Vector3(1.6f,1.4f,h),gray);
                b.Box(new Vector3(x,y,z+h+.08f),new Vector3(1.72f,1.52f,.10f),gray*1.18f);
                b.Box(new Vector3(x+.36f,y+.2f,z+h+.18f),new Vector3(.6f,.44f,.25f),gray*.83f);
                for(int vent=0;vent<4;vent++)b.Box(new Vector3(x+.36f,y+.05f+vent*.09f,z+h+.44f),new Vector3(.44f,.035f,.01f),dark);
                b.Box(new Vector3(x-.2f,y-.711f,z+.1f),new Vector3(.42f,.018f,.39f),dark);
                b.Box(new Vector3(x+.55f,y-.715f,z+.38f),new Vector3(.3f,.02f,.13f),new Color(.52f,.64f,.64f));
                b.Box(new Vector3(x-.65f,y+.48f,z+h+.16f),new Vector3(.045f,.045f,.65f),gray*1.3f);
            }
            // A partly swallowed service block makes the installations feel rooted in the dunes.
            b.Box(new Vector3(x+1.1f,y+.65f,z),new Vector3(.45f,.6f,.28f),gray*.75f);
        }
        return b;
    }
    public static Mesh Near(int section,int seed)
    {
        var b=new MeshBuilder();
        foreach(int side in new[]{-1,1})for(int i=0;i<4;i++)
        {
            float x=side*(11.8f+.35f*Mathf.Sin(seed+i)),y=-8+i*5;
            b.Face(new Color(.53f,.36f,.19f),new Vector3(x-side*1.8f,y-2,0),new Vector3(x+side*2,y-2,0),new Vector3(x+side*2,y+3,0),new Vector3(x,y+1,1.3f));
        }
        return b.Finish("Near dune crests "+section);
    }
}
