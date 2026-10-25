using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Four seamless twenty-unit sections. Height and wind ridges repeat over the full eighty-unit loop.
public static class DesertGeometry
{
    sealed class MeshBuilder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        public void Face(Color color,params Vector3[] points)
        {
            int start=vertices.Count;color.a=1;
            foreach(var p in points){vertices.Add(new Vector3(p.x+p.z*.20f,p.y+p.z*.30f,-p.z));colors.Add(color);}
            for(int i=1;i<points.Length-1;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        }
        public void Box(Vector3 p,Vector3 size,Color color)
        {
            float a=p.x-size.x/2,b=p.x+size.x/2,c=p.y-size.y/2,d=p.y+size.y/2,z=p.z,t=z+size.z;
            Face(color*.65f,new Vector3(a,c,z),new Vector3(b,c,z),new Vector3(b,c,t),new Vector3(a,c,t));
            Face(color*.78f,new Vector3(b,c,z),new Vector3(b,d,z),new Vector3(b,d,t),new Vector3(b,c,t));
            Face(color*.86f,new Vector3(a,d,z),new Vector3(a,c,z),new Vector3(a,c,t),new Vector3(a,d,t));
            Face(color,new Vector3(a,c,t),new Vector3(b,c,t),new Vector3(b,d,t),new Vector3(a,d,t));
        }
        public void Silo(Vector3 p,float radius,float height,Color color)
        {
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Vector3 u=p+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius),v=p+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius);
                Face(color*(.60f+.27f*Mathf.Sin(a)*Mathf.Sin(a)),u,v,v+Vector3.forward*height,u+Vector3.forward*height);
                Face(color*.94f,u+Vector3.forward*height,v+Vector3.forward*height,p+Vector3.forward*(height+radius*.28f));
            }
        }
        public Mesh Finish(string name)
        {var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetColors(colors);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
    }
    public static float Height(float x,float y,int seed)
    {
        float phase=seed%97*.065f;
        float wind=y*Mathf.PI/10+x*.50f+phase;
        float crest=.5f+.5f*Mathf.Sin(wind+.75f*Mathf.Sin(x*.72f));
        return .10f+Mathf.Pow(crest,3)*.85f+(.5f+.5f*Mathf.Sin(y*Mathf.PI/20-x*.45f+phase))*.22f;
    }
    public static Mesh Ground(int section,int seed)
    {
        var b=new MeshBuilder();float origin=(section-1)*20;
        for(int row=0;row<80;row++)for(int col=0;col<72;col++)
        {
            float x=-14+col*28f/72,y=-10+row*.25f;
            Vector3 P(float dx,float dy)=>new Vector3(x+dx,y+dy,Height(x+dx,origin+y+dy,seed));
            float h=Height(x,origin+y,seed),slope=Height(x,origin+y+.1f,seed)-h;
            var sand=Color.Lerp(new Color(.39f,.25f,.12f),new Color(.76f,.58f,.33f),Mathf.Clamp01(.55f+slope*24));
            // Narrow wind-cut contour bands sit in the terrain mesh, beneath the combat plane.
            float ripple=Mathf.Sin((origin+y)*30+x*11)*.004f;
            sand+=new Color(ripple,ripple,ripple,0);
            b.Face(sand,P(0,0),P(28f/72,0),P(28f/72,.25f),P(0,.25f));
        }
        // A thin windward lip and a broad lee face make each long dune legible through the PSX filter.
        float phase=seed%97*.065f;
        for(int ridge=-5;ridge<=5;ridge++)for(int col=0;col<140;col++)
        {
            float x=-14+col*.2f,next=x+.2f;
            float Y(float q)=>(Mathf.PI*.5f-q*.50f-phase-.75f*Mathf.Sin(q*.72f)+ridge*Mathf.PI*2)*10/Mathf.PI-origin;
            float a=Y(x),c=Y(next);if(Mathf.Min(a,c)<-9.8f || Mathf.Max(a,c)>9.8f)continue;
            Vector3 P(float q,float y)=>new Vector3(q,y,Height(q,origin+y,seed)+.025f);
            b.Face(new Color(.79f,.62f,.37f),P(x,a-.06f),P(next,c-.06f),P(next,c+.06f),P(x,a+.06f));
        }
        return b.Finish("Wind-carved dunes "+section);
    }
    public static Mesh Buildings(int section,int seed)
    {
        var b=new MeshBuilder();var r=new System.Random(seed+section*3817);float origin=(section-1)*20;
        float R(float a,float z)=>Mathf.Lerp(a,z,(float)r.NextDouble());
        var gray=new Color(.39f,.42f,.42f);var dark=new Color(.16f,.19f,.19f);
        for(int site=0;site<4;site++)
        {
            float x=(site%2==0 ? -1 : 1)*R(2.1f,2.9f),y=-7+site*4.4f+R(-.5f,.5f),z=Height(x,origin+y,seed)+.05f;
            b.Box(new Vector3(x,y,z),new Vector3(2.6f,2.3f,.08f),new Color(.42f,.37f,.27f));
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
        return b.Finish("Desert silos and compact outposts "+section);
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
