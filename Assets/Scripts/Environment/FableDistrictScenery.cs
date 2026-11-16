using System.Collections.Generic;
using UnityEngine;

// A single vertex-coloured mesh per scrolling section. All decoration stays inside
// its section; the uninterrupted ground reaches both seams at exactly the same colour.
public static class FableDistrictScenery
{
    public static Mesh Section(LevelId level, int seed, int section, float length)
    {
        var b = new Builder();
        var random = new System.Random(unchecked(seed * 7919 + section * 104729 + 1927));
        float edge = length * .5f;
        bool mushroom = level == LevelId.Mushroom;
        b.Quad(-19, -edge, 19, edge, 0, mushroom ? C(.075f,.105f,.085f) : C(.19f,.20f,.25f));
        if (mushroom) Forest(b, random, edge);
        else Dream(b, random, edge);
        return b.Finish(mushroom ? "Mushroom forest" : "Dream painted landscape");
    }

    static Color C(float r, float g, float b) => new Color(r,g,b,1);
    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b-a);

    static void Forest(Builder b, System.Random r, float edge)
    {
        // Broad overlapping moss and humus islands give the glades their own
        // winding shape. They stop short of the section joins.
        for (int i=0;i<90;i++)
        {
            float x=R(r,-5.5f,5.5f), y=R(r,-edge+1.1f,edge-1.1f);
            float rx=R(r,.35f,1.45f), ry=R(r,.18f,.75f);
            Color moss=i%4==0 ? C(.14f,.18f,.11f) : i%4==1 ? C(.105f,.145f,.095f) : i%4==2 ? C(.12f,.15f,.09f) : C(.095f,.13f,.095f);
            b.Ellipse(x,y,rx,ry,.004f,moss,9);
        }
        for (int i=0;i<250;i++)
        {
            float x=R(r,-5.5f,5.5f), y=R(r,-edge+.15f,edge-.15f);
            float w=R(r,.05f,.19f), h=R(r,.025f,.07f);
            Color ink=i%3==0 ? C(.22f,.24f,.13f) : i%3==1 ? C(.17f,.19f,.12f) : C(.24f,.19f,.12f);
            b.Poly(.012f,ink,new Vector2(x-w,y),new Vector2(x,y+h),new Vector2(x+w,y+.02f),new Vector2(x,y-h));
        }
        // An irregular series of groves alternates across the portrait viewport;
        // small satellite mushrooms knit each grove into the surrounding floor.
        for (int grove=0;grove<12;grove++)
        {
            float y=Mathf.Lerp(-edge+1.45f,edge-1.45f,(grove+.5f)/12f);
            float x=2.8f*Mathf.Sin(grove*2.17f+R(r,-.35f,.35f));
            int count=2+r.Next(4);
            for(int k=0;k<count;k++)
            {
                float xx=Mathf.Clamp(x+R(r,-1.05f,1.05f),-4.8f,4.8f);
                float yy=Mathf.Clamp(y+R(r,-.53f,.53f),-edge+1.3f,edge-1.3f);
                Mushroom(b,xx,yy,R(r,.30f,.80f),(grove+k)%5==0,r);
            }
        }
        // Canopies frame and occasionally enter the glades, with trunks, exposed
        // roots, ferns and fallen wood visible inside the combat viewport.
        for (int i=0;i<24;i++)
        {
            float x=(i%2==0 ? -1 : 1)*R(r,3.5f,5.2f);
            float y=R(r,-edge+1.6f,edge-1.6f);
            Tree(b,x,y,R(r,.6f,.96f));
        }
        for(int i=0;i<48;i++)
        {
            float x=(i%2==0 ? -1 : 1)*R(r,2.7f,5.0f), y=R(r,-edge+.6f,edge-.6f);
            float w=R(r,.18f,.42f);
            Color fern=i%2==0 ? C(.20f,.29f,.13f) : C(.15f,.24f,.12f);
            b.Poly(.11f,fern,new Vector2(x,y-.17f),new Vector2(x-w,y+.13f),new Vector2(x-.08f,y+.04f),new Vector2(x,y+.30f),new Vector2(x+.08f,y+.04f),new Vector2(x+w,y+.13f));
        }
        for(int i=0;i<7;i++)
        {
            float x=R(r,-4.2f,4.2f), y=R(r,-edge+.5f,edge-.5f), w=R(r,.4f,1.0f);
            b.Poly(.105f,C(.20f,.16f,.10f),new Vector2(x-w,y-.09f),new Vector2(x+w,y+.15f),new Vector2(x+w,y+.28f),new Vector2(x-w,y+.04f));
            b.Poly(.11f,C(.32f,.25f,.14f),new Vector2(x-w,y+.04f),new Vector2(x+w,y+.28f),new Vector2(x+w-.12f,y+.32f),new Vector2(x-w,y+.10f));
        }
    }
    static void Tree(Builder b,float x,float y,float s)
    {
        Color bark=C(.16f,.14f,.105f), leaf=C(.11f,.21f,.13f), light=C(.18f,.29f,.16f);
        b.Ellipse(x+.15f*s,y-.13f*s,.85f*s,.25f*s,.075f,C(.035f,.05f,.04f),10);
        b.Poly(.08f,bark,new Vector2(x-.16f*s,y-.22f*s),new Vector2(x+.17f*s,y-.22f*s),new Vector2(x+.10f*s,y+.85f*s),new Vector2(x-.11f*s,y+.85f*s));
        b.Poly(.085f,bark,new Vector2(x-.06f*s,y+.40f*s),new Vector2(x-.65f*s,y+.91f*s),new Vector2(x-.52f*s,y+.94f*s),new Vector2(x+.05f*s,y+.57f*s));
        b.Poly(.085f,bark,new Vector2(x+.04f*s,y+.53f*s),new Vector2(x+.57f*s,y+1.02f*s),new Vector2(x+.68f*s,y+.95f*s),new Vector2(x+.13f*s,y+.38f*s));
        b.Ellipse(x,y+1.02f*s,.83f*s,.49f*s,.09f,leaf,9);
        b.Ellipse(x-.43f*s,y+.91f*s,.48f*s,.35f*s,.095f,light,8);
        b.Ellipse(x+.39f*s,y+1.08f*s,.46f*s,.34f*s,.095f,light,8);
        b.Ellipse(x-.04f*s,y+1.24f*s,.43f*s,.26f*s,.10f,C(.23f,.33f,.18f),8);
    }

    static void Mushroom(Builder b,float x,float y,float s,bool purple,System.Random r)
    {
        Color shadow=C(.035f,.055f,.045f), stem=C(.56f,.53f,.42f), dark=C(.27f,.27f,.22f);
        Color cap=purple?C(.39f,.20f,.49f):C(.70f,.105f,.095f);
        Color light=purple?C(.60f,.34f,.65f):C(.94f,.22f,.15f);
        Color rim=purple?C(.22f,.12f,.30f):C(.37f,.065f,.06f);
        b.Ellipse(x+.15f*s,y-.20f*s,1.08f*s,.28f*s,.02f,shadow,10);
        b.Poly(.035f,dark,new Vector2(x-.24f*s,y-.18f*s),new Vector2(x+.24f*s,y-.18f*s),new Vector2(x+.18f*s,y+.46f*s),new Vector2(x-.16f*s,y+.46f*s));
        b.Poly(.04f,stem,new Vector2(x-.16f*s,y-.14f*s),new Vector2(x+.11f*s,y-.13f*s),new Vector2(x+.10f*s,y+.43f*s),new Vector2(x-.13f*s,y+.43f*s));
        // The broad, scalloped underside anchors a domed, faceted cap.
        b.Ellipse(x,y+.40f*s,.87f*s,.29f*s,.05f,rim,12);
        b.Poly(.06f,cap,new Vector2(x-.91f*s,y+.43f*s),new Vector2(x-.78f*s,y+.72f*s),new Vector2(x-.48f*s,y+.95f*s),new Vector2(x,y+1.04f*s),new Vector2(x+.47f*s,y+.95f*s),new Vector2(x+.77f*s,y+.72f*s),new Vector2(x+.91f*s,y+.43f*s),new Vector2(x+.48f*s,y+.32f*s),new Vector2(x,y+.29f*s),new Vector2(x-.5f*s,y+.32f*s));
        b.Poly(.065f,light,new Vector2(x-.74f*s,y+.68f*s),new Vector2(x-.44f*s,y+.91f*s),new Vector2(x,y+1.00f*s),new Vector2(x+.20f*s,y+.92f*s),new Vector2(x-.15f*s,y+.75f*s));
        if (!purple)
        {
            Color spot=C(.91f,.86f,.72f);
            b.Ellipse(x-.42f*s,y+.63f*s,.13f*s,.075f*s,.07f,spot,6);
            b.Ellipse(x+.15f*s,y+.77f*s,.15f*s,.08f*s,.07f,spot,6);
            b.Ellipse(x+.52f*s,y+.54f*s,.10f*s,.06f*s,.07f,spot,6);
            b.Ellipse(x-.12f*s,y+.43f*s,.09f*s,.045f*s,.07f,spot,6);
        }
        else b.Ellipse(x-.29f*s,y+.72f*s,.24f*s,.055f*s,.07f,C(.71f,.49f,.73f),8);
    }

    static void Dream(Builder b,System.Random r,float edge)
    {
        Color ink=C(.17f,.18f,.23f), slate=C(.27f,.30f,.34f), mauve=C(.35f,.28f,.33f);
        Color ochre=C(.40f,.34f,.27f), blue=C(.26f,.34f,.36f), pale=C(.48f,.41f,.37f);
        float shift=R(r,-.8f,.8f), scale=Mathf.Min(1f,edge/10f);
        // A single layered painting rather than a field of repeated tiles.
        // Its planes fade to the shared dark ground before either scroll seam.
        b.Poly(.015f,slate,new Vector2(-5.8f,-edge+1),new Vector2(-2.9f,-edge+1.1f),new Vector2(3.9f,edge-1.3f),new Vector2(1.7f,edge-1.1f));
        b.Poly(.018f,mauve,new Vector2(-5.6f,edge-1.3f),new Vector2(-1.8f,edge-1.1f),new Vector2(5.7f,-edge+1.3f),new Vector2(2.9f,-edge+1.1f));
        b.Poly(.02f,blue,new Vector2(-5.5f,-5.8f*scale),new Vector2(5.5f,1.9f*scale),new Vector2(5.5f,3.9f*scale),new Vector2(-5.5f,-3.6f*scale));
        b.Poly(.022f,ochre,new Vector2(-5.5f,4.6f*scale),new Vector2(5.5f,-2.1f*scale),new Vector2(5.5f,-.8f*scale),new Vector2(-5.5f,6.2f*scale));
        // Abstract head: independently angled planes, a split profile and two
        // displaced eyes make a coherent cubist figure without text or icons.
        float x=shift, y=.5f*scale;
        b.Poly(.03f,ink,new Vector2(x-2.4f,y-4.3f*scale),new Vector2(x+1.5f,y-4.8f*scale),new Vector2(x+2.7f,y-1.8f*scale),new Vector2(x+2.2f,y+3.5f*scale),new Vector2(x-.2f,y+5.0f*scale),new Vector2(x-2.4f,y+2.6f*scale));
        b.Poly(.035f,pale,new Vector2(x-2.05f,y-3.6f*scale),new Vector2(x-.2f,y-4.35f*scale),new Vector2(x+.10f,y+4.45f*scale),new Vector2(x-1.95f,y+2.35f*scale));
        b.Poly(.036f,mauve,new Vector2(x-.2f,y-4.35f*scale),new Vector2(x+1.3f,y-4.15f*scale),new Vector2(x+2.25f,y-1.45f*scale),new Vector2(x+1.8f,y+3.1f*scale),new Vector2(x+.10f,y+4.45f*scale));
        b.Poly(.04f,ochre,new Vector2(x-.2f,y+2.7f*scale),new Vector2(x+.65f,y-.35f*scale),new Vector2(x-.48f,y-.7f*scale),new Vector2(x+.05f,y+.25f*scale));
        b.Poly(.041f,blue,new Vector2(x+.65f,y-.35f*scale),new Vector2(x+1.6f,y-1.1f*scale),new Vector2(x+.3f,y-1.25f*scale));
        b.Poly(.045f,ink,new Vector2(x-1.85f,y+1.4f*scale),new Vector2(x-.45f,y+1.7f*scale),new Vector2(x-.9f,y+.95f*scale));
        b.Poly(.045f,ink,new Vector2(x+.40f,y+2.0f*scale),new Vector2(x+1.65f,y+1.55f*scale),new Vector2(x+.75f,y+1.22f*scale));
        b.Ellipse(x-1.05f,y+1.35f*scale,.12f,.12f,.05f,pale,8);
        b.Ellipse(x+1.05f,y+1.60f*scale,.12f,.12f,.05f,pale,8);
        b.Poly(.05f,ink,new Vector2(x-1.0f,y-2.20f*scale),new Vector2(x+.8f,y-2.05f*scale),new Vector2(x+.15f,y-2.43f*scale));
        // Short layered strokes interrupt the large planes like dry brush marks.
        for(int i=0;i<65;i++)
        {
            float px=R(r,-5.1f,5.1f), py=R(r,-edge+1.1f,edge-1.1f), w=R(r,.12f,.60f);
            Color c=i%3==0 ? pale : i%3==1 ? blue : ochre;
            b.Poly(.055f,c,new Vector2(px-w,py),new Vector2(px+w,py+.13f),new Vector2(px+w*.8f,py+.18f),new Vector2(px-w,py+.055f));
        }
    }
    sealed class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> indices=new List<int>();
        public void Poly(float z,Color c,params Vector2[] points)
        {
            int start=vertices.Count;
            foreach(var p in points) { vertices.Add(new Vector3(p.x,p.y,-z)); colors.Add(c); }
            for(int i=1;i<points.Length-1;i++) { indices.Add(start); indices.Add(start+i); indices.Add(start+i+1); }
        }
        public void Quad(float x0,float y0,float x1,float y1,float z,Color c) => Poly(z,c,new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1));
        public void Ellipse(float x,float y,float rx,float ry,float z,Color c,int sides)
        {
            int start=vertices.Count;
            vertices.Add(new Vector3(x,y,-z)); colors.Add(c);
            for(int i=0;i<sides;i++) { float a=i*Mathf.PI*2/sides; vertices.Add(new Vector3(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,-z)); colors.Add(c); }
            for(int i=0;i<sides;i++) { indices.Add(start); indices.Add(start+1+i); indices.Add(start+1+(i+1)%sides); }
        }
        public Mesh Finish(string name)
        {
            var mesh=new Mesh { name=name };
            if(vertices.Count>65535) mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices,0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
