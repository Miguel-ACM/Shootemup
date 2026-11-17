using System.Collections.Generic;
using UnityEngine;

public static class FableDistrictScenery
{
    public static Mesh Section(LevelId level, int seed, int section, float length)
    {
        if (level == LevelId.Mushroom)
            return MushroomGrove.Build(seed, section, length);
        var b = new Builder();
        var random = new System.Random(unchecked(seed * 7919 + section * 104729 + 1927));
        float edge = length * .5f;
        b.Quad(-19, -edge, 19, edge, 0, C(.19f,.20f,.25f));
        Dream(b, random, edge);
        return b.Finish("Dream painted landscape");
    }

    static Color C(float r, float g, float b) => new Color(r,g,b,1);
    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b-a);

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
