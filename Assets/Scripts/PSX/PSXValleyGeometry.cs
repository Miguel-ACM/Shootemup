using System.Collections.Generic;
using UnityEngine;

/// <summary>Low-poly valley kit: continuous banks, a service road, clustered canopies and one bridge landmark.</summary>
public static class PSXValleyGeometry
{
    const float Period=96, Width=7;
    static readonly Color Grass=new Color(.30f,.39f,.23f), Water=new Color(.12f,.285f,.32f),
        Bank=new Color(.39f,.405f,.31f), Stone=new Color(.48f,.53f,.50f), Asphalt=new Color(.225f,.27f,.27f),
        Shadow=new Color(.135f,.205f,.175f);

    public static Mesh Build(float origin,float length,float brightness=1,int seed=17381)
        =>Build(origin,length,brightness,new PSXValleyLayout(seed),seed);

    public static Mesh Build(float origin,float length,float brightness,PSXValleyLayout layout,int seed)
    {
        var b=new Builder(origin,brightness);
        // Geometry at each section boundary uses identical world-space samples.
        for(float y=0;y<length-.01f;y+=.75f)
        {
            float next=Mathf.Min(length,y+.75f),a=origin+y,c=origin+next;
            float l0=layout.Centre(a)-layout.HalfWidth(a),l1=layout.Centre(c)-layout.HalfWidth(c);
            float r0=layout.Centre(a)+layout.HalfWidth(a),r1=layout.Centre(c)+layout.HalfWidth(c);
            Color grass=Grass*(.98f+.025f*Mathf.Sin(a*Mathf.PI*2/Period));grass.a=1;
            b.Meadow=true;
            b.Strip(-Width,-Width,l0-layout.BankWidth(a,-1),l1-layout.BankWidth(c,-1),y,next,0,grass);
            b.Strip(r0+layout.BankWidth(a,1),r1+layout.BankWidth(c,1),Width,Width,y,next,0,grass);
            b.Meadow=false;
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
        // Broad changes in turf colour sit underneath the finer blade texture.
        b.Meadow=true;
        for(int i=0;i<16;i++)
        {
            float y=Range(rng,.7f,length-.7f),worldY=origin+y,side=i%2==0 ? -1 : 1;
            float x=layout.Centre(worldY)+side*(layout.HalfWidth(worldY)+Range(rng,2.0f,4.5f));
            b.Patch(x,y,Range(rng,.5f,1.3f),Range(rng,.35f,.8f),-.015f,
                i%3==0 ? new Color(.32f,.405f,.24f) : new Color(.28f,.365f,.21f),rng);
        }
        b.Meadow=false;
        var grassRng=new System.Random(unchecked(seed*713+(int)origin));
        for(int tuft=0;tuft<185;tuft++)
        {
            float x=Range(grassRng,-6.7f,6.7f),y=Range(grassRng,.25f,length-.25f),worldY=origin+y;
            if(!layout.Clear(x,worldY,.14f))continue;
            if(Mathf.Abs(x-layout.Centre(worldY))<layout.HalfWidth(worldY)+layout.BankWidth(worldY,x<layout.Centre(worldY) ? -1 : 1)+.16f)continue;
            b.GrassTuft(x,y,Range(grassRng,.12f,.24f),grassRng);
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
        if(bridgeY>=origin && bridgeY<origin+length)b.Bridge(layout.Centre(bridgeY),bridgeY-origin,layout.HalfWidth(bridgeY),rng,layout);
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
        readonly List<Vector2> surfaces=new List<Vector2>();
        public bool Meadow;
        readonly float origin;
        readonly float brightness;
        public Builder(float origin,float brightness){this.origin=origin;this.brightness=brightness;}
        void Face(Color color,params Vector3[] points)
        {
            int start=vertices.Count;
            var normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
            if(normal.z>0)normal=-normal;
            foreach(var p in points){vertices.Add(p);normals.Add(normal);colors.Add(color*brightness);uv.Add(new Vector2(p.x,p.y+origin));surfaces.Add(new Vector2(Meadow ? 1 : 0,0));}
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
        public void GrassTuft(float x,float y,float size,System.Random rng)
        {
            Color low=new Color(.25f,.335f,.17f),tip=new Color(.37f,.445f,.24f);
            for(int blade=0;blade<5;blade++)
            {
                float bx=x+Range(rng,-size*.55f,size*.55f),by=y+Range(rng,-size*.3f,size*.3f);
                float height=size*Range(rng,.65f,1.35f),lean=Range(rng,-.11f,.11f),width=Range(rng,.018f,.035f);
                Face(Color.Lerp(low,tip,Range(rng,.1f,.85f)),new Vector3(bx-width,by,-.04f),
                    new Vector3(bx+lean,by+height*.65f,-.04f-height),new Vector3(bx+width,by,-.04f));
            }
        }
        public void Tree(float x,float y,float radius,System.Random rng)
        {
            Patch(x+.20f,y-.22f,radius*1.10f,radius*.78f,-.12f,Color.Lerp(Grass,Shadow,.48f),rng);
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
        public void Bridge(float x,float y,float halfWidth,System.Random rng,PSXValleyLayout layout)
        {
            float span=halfWidth*2+1;
            Color wood=new Color(.46f,.355f,.22f),timber=new Color(.31f,.255f,.165f),endGrain=new Color(.39f,.31f,.20f);
            Color path=new Color(.385f,.365f,.255f);
            // A pedestrian route: the west ramp meets the actual curved asphalt edge.
            float roadLo=layout.Centre(origin+y-.39f)-layout.HalfWidth(origin+y-.39f)-.82f;
            float roadHi=layout.Centre(origin+y+.39f)-layout.HalfWidth(origin+y+.39f)-.82f;
            Face(wood*.86f,new Vector3(roadLo,y-.39f,-.115f),new Vector3(roadHi,y+.39f,-.115f),
                new Vector3(x-span/2,y+.39f,-.29f),new Vector3(x-span/2,y-.39f,-.29f));
            Face(wood*.86f,new Vector3(x+span/2,y-.39f,-.29f),new Vector3(x+span/2,y+.39f,-.29f),
                new Vector3(x+span/2+.35f,y+.39f,-.10f),new Vector3(x+span/2+.35f,y-.39f,-.10f));
            // Narrow worn dirt trail continues into the meadow, rather than ending in a paved apron.
            float startX=x+span/2+.32f;
            for(int step=0;step<12;step++)
            {
                float t=step/12f,n=(step+1)/12f;
                float ax=Mathf.Lerp(startX,7.2f,t),bx=Mathf.Lerp(startX,7.2f,n);
                float ay=y+Mathf.SmoothStep(0,.95f,t),by=y+Mathf.SmoothStep(0,.95f,n);
                float aw=Mathf.Lerp(.38f,.27f,t),bw=Mathf.Lerp(.38f,.27f,n);
                Face(path,new Vector3(ax,ay-aw,-.085f),new Vector3(ax,ay+aw,-.085f),
                    new Vector3(bx,by+bw,-.085f),new Vector3(bx,by-bw,-.085f));
            }
            Rect(x+.10f,y-.28f,span,.9f,-.13f,new Color(.115f,.19f,.165f));
            // Small bank pads and timber trestles support the span. The current wakes
            // remain attached to the same two pile stations as the previous crossing.
            foreach(float side in new[]{-1f,1f})
            {
                Block(x+side*(halfWidth+.32f),y,.36f,1.02f,-.10f,.12f,new Color(.36f,.385f,.30f));
                float px=x+side*halfWidth*.74f;
                float pileTop=DeckZ((px-x+span/2)/span)+.085f;
                foreach(float rail in new[]{-1f,1f})
                    Beam(new Vector3(px,y+rail*.34f-.13f,-.045f),new Vector3(px,y+rail*.34f,pileTop),.14f,timber*.8f);
                Beam(new Vector3(px-.18f,y-.39f,-.22f),new Vector3(px+.18f,y+.39f,pileTop+.025f),.065f,timber);
                Beam(new Vector3(px+.18f,y-.39f,-.22f),new Vector3(px-.18f,y+.39f,pileTop+.025f),.065f,timber);
            }
            int boards=Mathf.CeilToInt(span/.16f);float pitch=span/boards;
            for(int board=0;board<boards;board++)
            {
                float t=(board+.5f)/boards,px=x-span/2+(board+.5f)*pitch,z=DeckZ(t);
                float tint=Range(rng,.87f,1.12f),length=Range(rng,.83f,.88f);
                Block(px,y,pitch-.012f,length,z+.045f,.045f,wood*tint);
                // A few dark grain cuts and nail heads sell the planks at gameplay scale.
                if(board%3==0)
                {
                    Rect(px-.02f,y+.05f,.012f,.32f,z-.004f,timber*.93f);
                    foreach(float side in new[]{-1f,1f})Rect(px,y+side*.31f,.025f,.025f,z-.007f,timber*.72f);
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                const int bays=7;
                for(int bay=0;bay<bays;bay++)
                {
                    float t=bay/(float)bays,n=(bay+1f)/bays;
                    var a=new Vector3(x-span/2+t*span,y+side*.44f,DeckZ(t));
                    var b=new Vector3(x-span/2+n*span,y+side*.44f,DeckZ(n));
                    var top=a+new Vector3(0,.19f,-.45f);var next=b+new Vector3(0,.19f,-.45f);
                    Beam(a,b,.075f,timber);Beam(top,next,.075f,endGrain);
                    Beam(a,top,.085f,timber);
                    Beam(a+new Vector3(0,.095f,-.225f),b+new Vector3(0,.095f,-.225f),.045f,timber);
                    if(bay==bays-1)Beam(b,next,.085f,timber);
                }
            }
        }
        static float DeckZ(float t)=>-.29f-.18f*Mathf.Sin(t*Mathf.PI);
        public Mesh Finish()
        {
            var mesh=new Mesh{name="Composed PSX valley "+origin};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetUVs(1,surfaces);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
