using System.Collections.Generic;
using UnityEngine;

/// <summary>Low-poly valley kit: continuous banks, a service road, clustered canopies and one bridge landmark.</summary>
public static class PSXValleyGeometry
{
    const float Period=96, Width=7;
    static readonly Color Grass=new Color(.31f,.385f,.285f), Water=new Color(.12f,.285f,.32f),
        Bank=new Color(.39f,.405f,.31f), Stone=new Color(.48f,.53f,.50f), Asphalt=new Color(.225f,.27f,.27f),
        Shadow=new Color(.135f,.205f,.175f), Foliage=new Color(.255f,.385f,.30f);
    public static float RiverCentre(float y)=>1.25f*Mathf.Sin(y*Mathf.PI*4/Period)+.38f*Mathf.Sin(y*Mathf.PI*8/Period+.7f);
    public static float RiverHalfWidth(float y)=>1.35f+.25f*Mathf.Sin(y*Mathf.PI*4/Period+.5f);

    public static Mesh Build(float origin,float length,float brightness=1,int seed=17381)
    {
        var b=new Builder(origin,brightness);
        var layout=new PSXValleyLayout(seed);
        // Geometry at each section boundary uses identical world-space samples.
        for(float y=0;y<length-.01f;y+=.75f)
        {
            float next=Mathf.Min(length,y+.75f),a=origin+y,c=origin+next;
            float l0=layout.Centre(a)-layout.HalfWidth(a),l1=layout.Centre(c)-layout.HalfWidth(c);
            float r0=layout.Centre(a)+layout.HalfWidth(a),r1=layout.Centre(c)+layout.HalfWidth(c);
            Color grass=Grass*(.98f+.025f*Mathf.Sin(a*Mathf.PI*2/Period));grass.a=1;
            b.Strip(-Width,-Width,l0-layout.BankWidth(a,-1),l1-layout.BankWidth(c,-1),y,next,0,grass);
            b.Strip(r0+layout.BankWidth(a,1),r1+layout.BankWidth(c,1),Width,Width,y,next,0,grass);
            // Eroded shoulders and darker steep bank faces give the river a visible bed.
            b.Strip(l0-layout.BankWidth(a,-1),l1-layout.BankWidth(c,-1),l0,l1,y,next,-.03f,Bank);
            b.Strip(r0,r1,r0+layout.BankWidth(a,1),r1+layout.BankWidth(c,1),y,next,-.03f,Bank*.91f);
            b.Strip(l0,l1,l0+.12f,l1+.12f,y,next,-.04f,new Color(.15f,.235f,.215f));
            b.Strip(r0-.12f,r1-.12f,r0,r1,y,next,-.04f,new Color(.18f,.265f,.24f));
            b.Strip(l0+.12f,l1+.12f,r0-.12f,r1-.12f,y,next,.02f,Water);
            // A narrow road follows the west bank. Long shoulders read clearly at a small scale.
            b.Strip(l0-1.65f,l1-1.65f,l0-.65f,l1-.65f,y,next,-.08f,Bank*.89f);
            b.Strip(l0-1.5f,l1-1.5f,l0-.8f,l1-.8f,y,next,-.10f,Asphalt);
            if(((int)((a+96)/1.5f))%3==0)
                b.Strip(l0-1.17f,l1-1.17f,l0-1.13f,l1-1.13f,y,next,-.11f,new Color(.43f,.46f,.37f));
        }
        var rng=new System.Random(unchecked(seed*397+(int)origin));
        // Broad painted ground patches replace the old high-frequency speckled tile.
        for(int i=0;i<16;i++)
        {
            float y=Range(rng,.7f,length-.7f),worldY=origin+y,side=i%2==0 ? -1 : 1;
            float x=layout.Centre(worldY)+side*(layout.HalfWidth(worldY)+Range(rng,2.0f,4.5f));
            b.Patch(x,y,Range(rng,.5f,1.3f),Range(rng,.35f,.8f),-.015f,
                i%3==0 ? new Color(.335f,.40f,.29f) : new Color(.285f,.36f,.27f),rng);
        }
        foreach(var plant in layout.Plants)
        {
            if(plant.y<origin || plant.y>=origin+length)continue;
            if(plant.kind<2)b.Tree(plant.x,plant.y-origin,plant.radius,new System.Random(plant.seed));
        }
        // Gravel outcrops occupy dry ground and follow the road/landmark clearances.
        for(int i=0;i<23;i++)
        {
            float y=Range(rng,.5f,length-.5f),x=Range(rng,-6.5f,6.5f),radius=Range(rng,.12f,.38f);
            if(layout.Clear(x,origin+y,radius))b.Rock(x,y,radius,rng);
        }
        // One recognizable landmark per 96 units, fixed to the terrain rather than independently recycled.
        float bridgeY=layout.BridgeY;
        if(bridgeY>=origin && bridgeY<origin+length)b.Bridge(layout.Centre(bridgeY),bridgeY-origin,layout.HalfWidth(bridgeY),rng);
        // A quieter concrete lookout further along the river establishes a second composed beat.
        float lookoutY=layout.LookoutY;
        if(lookoutY>=origin && lookoutY<origin+length)
        {
            float x=layout.Centre(lookoutY)+layout.HalfWidth(lookoutY)+1.6f,y=lookoutY-origin;
            b.Block(x,y,1.5f,2,-.1f,.2f,new Color(.34f,.40f,.37f));
            b.Block(x+.15f,y+.35f,.8f,.8f,-.3f,.5f,Stone);
            b.Rect(x+.15f,y+.35f,.55f,.4f,-.82f,Asphalt);
            b.Rect(x-.45f,y-.65f,.1f,.3f,-.32f,new Color(.5f,.43f,.24f));
        }
        return b.Finish();
    }

    static float Range(System.Random rng,float lo,float hi)=>Mathf.Lerp(lo,hi,(float)rng.NextDouble());

    sealed class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector3> normals=new List<Vector3>();
        readonly List<int> triangles=new List<int>();
        readonly List<Color> colors=new List<Color>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly float origin;
        readonly float brightness;
        public Builder(float origin,float brightness){this.origin=origin;this.brightness=brightness;}
        void Face(Color color,params Vector3[] points)
        {
            int start=vertices.Count;
            var normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
            if(normal.z>0)normal=-normal;
            foreach(var p in points){vertices.Add(p);normals.Add(normal);colors.Add(color*brightness);uv.Add(new Vector2(p.x,p.y+origin));}
            for(int i=1;i<points.Length-1;i++){triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
        }
        public void Strip(float left0,float left1,float right0,float right1,float y0,float y1,float z,Color color)
        {Face(color,new Vector3(left0,y0,z),new Vector3(left1,y1,z),new Vector3(right1,y1,z),new Vector3(right0,y0,z));}
        public void Rect(float x,float y,float w,float h,float z,Color color)
        {Strip(x-w/2,x-w/2,x+w/2,x+w/2,y-h/2,y+h/2,z,color);}
        public void Patch(float x,float y,float rx,float ry,float z,Color color,System.Random rng)
        {
            var centre=new Vector3(x,y,z);
            var ring=new Vector3[7];
            for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7,s=Range(rng,.85f,1.15f);ring[i]=new Vector3(x+Mathf.Cos(a)*rx*s,y+Mathf.Sin(a)*ry*s,z);}
            for(int i=0;i<7;i++)Face(color,centre,ring[i],ring[(i+1)%7]);
        }
        public void Block(float x,float y,float w,float h,float z,float height,Color color)
        {
            var lo=new[]{new Vector3(x-w/2,y-h/2,z),new Vector3(x-w/2,y+h/2,z),new Vector3(x+w/2,y+h/2,z),new Vector3(x+w/2,y-h/2,z)};
            var hi=new Vector3[4];
            for(int i=0;i<4;i++)hi[i]=new Vector3(Mathf.Lerp(lo[i].x,x,.1f)-height*.08f,Mathf.Lerp(lo[i].y,y,.1f)+height*.12f,z-height);
            for(int i=0;i<4;i++)Face(color*.8f,lo[i],lo[(i+1)%4],hi[(i+1)%4],hi[i]);
            Face(color,hi);
        }
        public void Tree(float x,float y,float radius,System.Random rng)
        {
            Patch(x+.22f,y-.28f,radius*1.3f,radius*.9f,-.12f,Shadow,rng);
            // Three overlapping, asymmetrical faceted crowns; no tiled leaf patterns.
            Crown(x,y,radius,-.45f,Foliage,rng);
            Crown(x-radius*.35f,y+radius*.4f,radius*.68f,-.75f,Foliage*1.12f,rng);
            Crown(x+radius*.38f,y+radius*.2f,radius*.58f,-.65f,Foliage*.94f,rng);
        }
        void Crown(float x,float y,float radius,float z,Color color,System.Random rng)
        {
            int sides=7;var ring=new Vector3[sides];var inner=new Vector3[sides];
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,r=radius*Range(rng,.83f,1.1f);
                ring[i]=new Vector3(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r*.83f,z);
                inner[i]=new Vector3(x-.1f+Mathf.Cos(a)*r*.46f,y+.12f+Mathf.Sin(a)*r*.4f,z-radius*.5f);
            }
            var top=new Vector3(x-.1f,y+.15f,z-radius*.66f);
            for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;
                Face(color,ring[i],ring[j],inner[j],inner[i]);
                Face(color*1.08f,inner[i],inner[j],top);
            }
        }
        public void Rock(float x,float y,float radius,System.Random rng)
        {Patch(x+.12f,y-.13f,radius*1.3f,radius*.7f,-.13f,Shadow,rng);Crown(x,y,radius,-.23f,new Color(.34f,.40f,.37f),rng);}
        public void Bridge(float x,float y,float halfWidth,System.Random rng)
        {
            float span=halfWidth*2+3.0f;
            // Cast shadow and visible piers establish height over the water.
            Rect(x+.2f,y-.48f,span,1.35f,-.16f,new Color(.075f,.16f,.18f));
            foreach(float side in new[]{-1f,1f})Block(x+side*(halfWidth-.25f),y-.55f,.45f,1.7f,-.2f,.25f,new Color(.24f,.31f,.30f));
            Block(x,y,span,1.55f,-.35f,.3f,Stone);
            Rect(x,y,span-.15f,.88f,-.67f,Asphalt);
            // Expansion joints and worn shoulders make the concrete read as a small model.
            foreach(float joint in new[]{-.28f,.28f})Rect(x+span*joint,y,.025f,1.3f,-.695f,new Color(.26f,.33f,.32f));
            for(float wear=-span/2+.5f;wear<span/2;wear+=1.4f)Rect(x+wear,y-.47f,.42f,.035f,-.695f,new Color(.57f,.58f,.48f));
            for(float mark=-span/2+.35f;mark<span/2;mark+=.7f)Rect(x+mark,y,.34f,.045f,-.69f,new Color(.55f,.55f,.40f));
            foreach(float side in new[]{-1f,1f})
            {
                Block(x,y+side*.64f,span,.12f,-.67f,.14f,new Color(.48f,.53f,.49f));
                for(float pier=-span/2+.15f;pier<span/2;pier+=1.1f)Block(x+pier,y+side*.64f,.13f,.2f,-.65f,.24f,Stone);
                Rect(x+side*(span/2-.25f),y,.12f,.7f,-.71f,new Color(.53f,.44f,.24f));
            }
            float west=x-halfWidth-2.3f;
            Block(west,y+1.8f,1.25f,1.6f,-.12f,.16f,new Color(.34f,.39f,.35f));
            Block(west,y+1.9f,.85f,1.1f,-.3f,.45f,new Color(.40f,.45f,.41f));
            Rect(west,y+1.7f,.52f,.27f,-.77f,new Color(.16f,.23f,.24f));
            Rect(west+.31f,y+2.15f,.08f,.18f,-.78f,new Color(.54f,.45f,.25f));
            // The far approach ends in a loading apron, rather than floating in the grass.
            Rect(x+halfWidth+2,y,1.6f,1.0f,-.12f,Asphalt);
            Block(x+halfWidth+2.3f,y+1,.6f,.6f,-.15f,.2f,new Color(.31f,.38f,.34f));
        }
        public Mesh Finish()
        {
            var mesh=new Mesh{name="Composed PSX valley "+origin};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
