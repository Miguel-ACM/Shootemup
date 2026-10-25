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
            Patch(x+.20f,y-.22f,radius*1.10f,radius*.78f,-.12f,Shadow,rng);
            var root=new Vector3(x,y,-.16f);var fork=new Vector3(x-.06f,y+.19f,-.72f);
            Beam(root,fork,radius*.18f,new Color(.27f,.265f,.17f));
            for(int branch=0;branch<3;branch++)
            {
                float a=branch*2.094f+Range(rng,-.4f,.4f);
                Beam(fork,new Vector3(x+Mathf.Cos(a)*radius*.56f,y+.2f+Mathf.Sin(a)*radius*.46f,-.83f),
                    radius*.085f,new Color(.29f,.29f,.185f));
            }
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
        void Beam(Vector3 a,Vector3 b,float width,Color color)
        {
            var direction=(b-a).normalized;
            var cross=Vector3.Cross(direction,Vector3.forward).normalized*width*.5f;
            if(cross.sqrMagnitude<.00001f)cross=Vector3.right*width*.5f;
            var depth=Vector3.Cross(direction,cross).normalized*width*.5f;
            var p=new[]{a-cross-depth,a+cross-depth,a+cross+depth,a-cross+depth};
            var q=new[]{b-cross-depth,b+cross-depth,b+cross+depth,b-cross+depth};
            for(int i=0;i<4;i++)Face(color*(i==0 ? 1 : .78f),p[i],q[i],q[(i+1)%4],p[(i+1)%4]);
            Face(color,p);Face(color,q);
        }
        public void Bridge(float x,float y,float halfWidth,System.Random rng)
        {
            float span=halfWidth*2+3f;
            Color concrete=new Color(.42f,.43f,.365f),steel=new Color(.29f,.36f,.325f),edge=new Color(.46f,.49f,.405f);
            // Bank abutments, battered wing walls and pointed cutwaters anchor the crossing.
            Rect(x+.12f,y-.55f,span,1.65f,-.13f,new Color(.085f,.155f,.145f));
            foreach(float side in new[]{-1f,1f})
            {
                Block(x+side*(halfWidth+.30f),y,.68f,2.12f,-.17f,.38f,concrete);
                foreach(float bankSide in new[]{-1f,1f})
                    Beam(new Vector3(x+side*(halfWidth+.1f),y+bankSide*.91f,-.30f),
                         new Vector3(x+side*(halfWidth+.8f),y+bankSide*1.34f,-.14f),.18f,concrete*.87f);
                float px=x+side*halfWidth*.74f;
                Block(px,y-.3f,.35f,1.8f,-.12f,.44f,concrete*.8f);
                Face(concrete*.87f,new Vector3(px-.175f,y+.65f,-.3f),new Vector3(px,y+.97f,-.3f),new Vector3(px+.175f,y+.65f,-.3f));
            }
            Block(x,y,span,1.58f,-.48f,.16f,concrete);
            Rect(x,y,span-.08f,1.0f,-.66f,Asphalt*.94f);
            foreach(float side in new[]{-1f,1f})
            {
                Rect(x,y+side*.49f,span-.1f,.035f,-.68f,new Color(.48f,.47f,.345f));
                Block(x,y+side*.65f,span,.19f,-.65f,.10f,edge);
                // Low arched steel side trusses: eight open bays with diagonal bracing.
                for(int bay=0;bay<8;bay++)
                {
                    float t=bay/8f,n=(bay+1)/8f;
                    float h=.30f+.42f*Mathf.Sin(t*Mathf.PI),nh=.30f+.42f*Mathf.Sin(n*Mathf.PI);
                    var a=new Vector3(x-span/2+span*t,y+side*.67f,-.78f);
                    var b=new Vector3(x-span/2+span*n,y+side*.67f,-.78f);
                    var top=a+new Vector3(-h*.10f,h*.48f,-h);
                    var next=b+new Vector3(-nh*.10f,nh*.48f,-nh);
                    Beam(a,b,.065f,steel);Beam(top,next,.095f,edge);
                    Beam(a,top,.065f,steel);
                    Beam(bay%2==0 ? a : top,bay%2==0 ? next : b,.055f,steel);
                    if(bay==7)Beam(b,next,.065f,steel);
                }
                // Stone end posts keep the small truss readable from the overhead camera.
                foreach(float endSide in new[]{-1f,1f})
                    Block(x+endSide*(span/2-.08f),y+side*.68f,.20f,.27f,-.65f,.36f,concrete);
            }
            for(float mark=-span/2+.3f;mark<span/2-.2f;mark+=.74f)
                Rect(x+mark,y,.34f,.038f,-.68f,new Color(.50f,.48f,.32f));
            foreach(float joint in new[]{-.34f,.34f})Rect(x+span*joint,y,.035f,1.03f,-.69f,new Color(.16f,.20f,.18f));
            // Worn patching on the deck, kept quieter than the moving ships.
            Rect(x+.45f,y-.21f,.48f,.17f,-.675f,Asphalt*1.08f);
            float west=x-halfWidth-2.3f;
            Block(west,y+1.75f,1.1f,1.35f,-.12f,.16f,concrete*.87f);
            Block(west,y+1.8f,.8f,.95f,-.29f,.40f,new Color(.38f,.42f,.36f));
            Face(steel,new Vector3(west-.48f,y+1.24f,-.72f),new Vector3(west-.48f,y+2.35f,-.72f),
                 new Vector3(west,y+2.35f,-.92f),new Vector3(west,y+1.24f,-.92f));
            Face(edge*.8f,new Vector3(west,y+1.24f,-.92f),new Vector3(west,y+2.35f,-.92f),
                 new Vector3(west+.48f,y+2.35f,-.72f),new Vector3(west+.48f,y+1.24f,-.72f));
            Rect(x+halfWidth+1.9f,y,1.5f,1.05f,-.12f,Asphalt);
            Block(x+halfWidth+2.3f,y+1,.6f,.6f,-.15f,.2f,new Color(.31f,.38f,.34f));
        }
        public Mesh Finish()
        {
            var mesh=new Mesh{name="Composed PSX valley "+origin};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
