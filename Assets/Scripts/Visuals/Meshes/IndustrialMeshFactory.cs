using System.Collections.Generic;
using UnityEngine;

/// <summary>Small, explicit low-poly meshes with trim UVs into the ship's atlas.</summary>
public static partial class IndustrialMeshFactory
{
    sealed class Builder
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> indices = new List<int>();
        readonly List<Vector2> uv = new List<Vector2>();

        public void Face(int tile, params Vector3[] points)
        {
            int start = vertices.Count;
            var normal = Vector3.Cross(points[1]-points[0], points[2]-points[0]).normalized;
            int axis = Mathf.Abs(normal.z) > Mathf.Abs(normal.x) && Mathf.Abs(normal.z) > Mathf.Abs(normal.y) ? 2 : Mathf.Abs(normal.y) > Mathf.Abs(normal.x) ? 1 : 0;
            var coordinates = new Vector2[points.Length];
            var lo = new Vector2(float.MaxValue,float.MaxValue);
            var hi = new Vector2(float.MinValue,float.MinValue);
            for (int i=0;i<points.Length;i++)
            {
                var p=points[i];
                coordinates[i]=axis==2 ? new Vector2(p.x,p.y) : axis==1 ? new Vector2(p.x,p.z) : new Vector2(p.y,p.z);
                lo=Vector2.Min(lo,coordinates[i]); hi=Vector2.Max(hi,coordinates[i]);
            }
            for (int i=0;i<points.Length;i++)
            {
                vertices.Add(points[i]);
                Vector2 q=coordinates[i]-lo, span=hi-lo;
                uv.Add(new Vector2((tile%4*64+4+55*q.x/Mathf.Max(span.x,0.0001f))/256f,(tile/4*64+4+55*q.y/Mathf.Max(span.y,0.0001f))/256f));
            }
            // Ear clipping preserves the cutouts of swept wings and forked hulls.
            // A triangle fan fills across concave outlines and destroys their silhouette.
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            float area=0;for(int i=0;i<coordinates.Length;i++)area+=Cross(coordinates[i],coordinates[(i+1)%coordinates.Length]);
            float winding=area>=0 ? 1 : -1;
            var remaining=new List<int>();for(int i=0;i<points.Length;i++)remaining.Add(i);
            while(remaining.Count>3)
            {
                bool clipped=false;
                for(int i=0;i<remaining.Count;i++)
                {
                    int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                    var pa=coordinates[a];var pb=coordinates[b];var pc=coordinates[c];
                    if(Cross(pb-pa,pc-pb)*winding<=.0000001f)continue;
                    bool occupied=false;
                    foreach(int candidate in remaining)
                    {
                        if(candidate==a || candidate==b || candidate==c)continue;
                        var p=coordinates[candidate];
                        if(Cross(pb-pa,p-pa)*winding>=-.0000001f && Cross(pc-pb,p-pb)*winding>=-.0000001f && Cross(pa-pc,p-pc)*winding>=-.0000001f){occupied=true;break;}
                    }
                    if(occupied)continue;
                    indices.Add(start+a);indices.Add(start+b);indices.Add(start+c);remaining.RemoveAt(i);clipped=true;break;
                }
                if(!clipped)break; // Degenerate faces have no valid area to fill.
            }
            if(remaining.Count==3){foreach(int i in remaining)indices.Add(start+i);}
        }

        public void Box(Vector3 center, Vector3 size, int front, int side=6)
        {
            var a=center-size*0.5f; var b=center+size*0.5f;
            Face(front,new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,a.y,a.z));
            Face(side,new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,a.y,b.z));
            Face(side,new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),new Vector3(a.x,a.y,a.z));
            Face(side,new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,a.y,b.z));
            Face(side,new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z));
            Face(side,new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z));
        }

        public Mesh Finish(string name)
        {
            var mesh=new Mesh {name=name}; mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        public void Prism(Vector2[] outline, float depth, int tile, Vector3 offset,int sideTile=-1,int backTile=-1)
        {
            var front=new Vector3[outline.Length];var back=new Vector3[outline.Length];
            float area=0;
            for(int i=0;i<outline.Length;i++){var a=outline[i];var c=outline[(i+1)%outline.Length];area+=a.x*c.y-c.x*a.y;}
            for(int i=0;i<outline.Length;i++)
            {
                var p=outline[area>0 ? outline.Length-1-i : i];
                front[i]=offset+new Vector3(p.x,p.y,-depth*.5f);
                back[outline.Length-1-i]=offset+new Vector3(p.x,p.y,depth*.5f);
            }
            Face(tile,front);Face(backTile<0 ? 11 : backTile,back);
            for(int i=0;i<front.Length;i++)
            {
                int j=(i+1)%front.Length;
                Face(sideTile<0 ? 6 : sideTile,front[i],front[i]+Vector3.forward*depth,front[j]+Vector3.forward*depth,front[j]);
            }
        }

        public void Drum(Vector3 center,float radius,float depth,int tile,int sides=6,int sideTile=-1)
        {
            var points=new Vector2[sides];
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}
            Prism(points,depth,tile,center,sideTile,sideTile);
        }

        public void Ball(Vector3 center,Vector3 size,int tile)
        {
            Vector3 P(int ring,int segment){float lat=-Mathf.PI*.5f+ring*Mathf.PI/4,lon=segment*Mathf.PI/4;return center+Vector3.Scale(new Vector3(Mathf.Cos(lat)*Mathf.Cos(lon),Mathf.Sin(lat),Mathf.Cos(lat)*Mathf.Sin(lon)),size);}
            for(int ring=0;ring<4;ring++)for(int s=0;s<8;s++)
            {
                Vector3[] points=ring==0 ? new[]{P(0,s),P(1,s),P(1,s+1)} : ring==3 ? new[]{P(3,s),P(4,s),P(3,s+1)} : new[]{P(ring,s),P(ring+1,s),P(ring+1,s+1),P(ring,s+1)};
                var normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]);var middle=Vector3.zero;foreach(var p in points)middle+=p;middle=middle/points.Length-center;
                if(Vector3.Dot(normal,middle)<0)System.Array.Reverse(points);Face(tile,points);
            }
        }

        // Continuous longitude/latitude UVs, with a split seam and individual pole UVs.
        public void LunarSphere(Vector3 center,float radius,int tile)
        {
            Vector3 P(int r,int s){float a=-Mathf.PI*.5f+r*Mathf.PI/6,b=s*Mathf.PI/4;return center+radius*new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a),Mathf.Cos(a)*Mathf.Sin(b));}
            void Triangle(Vector3 a,Vector3 b,Vector3 c,Vector2 ua,Vector2 ub,Vector2 uc)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c)/3-center)<0){var p=b;b=c;c=p;var q=ub;ub=uc;uc=q;}
                int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);
                foreach(var q in new[]{ua,ub,uc})uv.Add(new Vector2((tile%4*64+4+55*q.x)/256f,(tile/4*64+4+55*q.y)/256f));
                indices.Add(n);indices.Add(n+1);indices.Add(n+2);
            }
            for(int r=0;r<6;r++)for(int s=0;s<8;s++)
            {
                var a=new Vector2(s/8f,r/6f);var b=new Vector2(s/8f,(r+1)/6f);var c=new Vector2((s+1)/8f,(r+1)/6f);var d=new Vector2((s+1)/8f,r/6f);
                if(r==0)Triangle(P(r,s),P(r+1,s),P(r+1,s+1),new Vector2((s+.5f)/8,r/6f),b,c);
                else if(r==5)Triangle(P(r,s),P(r+1,s),P(r,s+1),a,new Vector2((s+.5f)/8,(r+1)/6f),d);
                else{Triangle(P(r,s),P(r+1,s),P(r+1,s+1),a,b,c);Triangle(P(r,s),P(r+1,s+1),P(r,s+1),a,c,d);}
            }
        }

        public void Can(Vector3 center,float radius,float height,int side,int top,bool coffee=false)
        {
            var bottom=new Vector3[8];var lid=new Vector3[8];
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;bottom[i]=center+new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius);
                lid[i]=center+new Vector3(Mathf.Cos(a)*radius,height*.5f,Mathf.Sin(a)*radius);
            }
            for(int i=0;i<8;i++){int j=(i+1)%8;Face(side,bottom[i],lid[i],lid[j],bottom[j]);}
            Face(side,bottom);var cap=(Vector3[])lid.Clone();System.Array.Reverse(cap);Face(top,cap);
            if(coffee)
            {
                var rim=new Vector3[8];for(int i=0;i<8;i++)rim[i]=center+new Vector3(Mathf.Cos(i*Mathf.PI/4)*radius*.78f,height*.5f+.002f,Mathf.Sin(i*Mathf.PI/4)*radius*.78f);
                for(int i=0;i<8;i++){int j=(i+1)%8;Face(0,lid[i],rim[i],rim[j],lid[j]);}
            }
        }
    }

    public static Mesh[] CreateFieldMeshes()
    {
        var meshes=new Mesh[6];
        var b=new Builder(); b.Box(Vector3.zero,new Vector3(1,1,0.13f),0,11); meshes[0]=b.Finish("Armour plate - 12 tris");
        b=new Builder();
        b.Box(Vector3.zero,new Vector3(0.17f,1.8f,0.22f),11);
        b.Box(new Vector3(0,0,-0.15f),new Vector3(0.45f,1.8f,0.10f),6);
        b.Box(new Vector3(0,0,0.15f),new Vector3(0.45f,1.8f,0.10f),11);
        meshes[1]=b.Finish("I-beam - 36 tris");
        b=new Builder(); b.Box(Vector3.zero,new Vector3(0.9f,0.9f,0.65f),14);
        b.Box(new Vector3(0,0,-0.36f),new Vector3(0.8f,0.12f,0.08f),3);
        b.Box(new Vector3(0,0,-0.37f),new Vector3(0.12f,0.8f,0.08f),10);
        meshes[2]=b.Finish("Cargo block - 36 tris");
        b=new Builder(); b.Box(Vector3.zero,new Vector3(1.1f,1.5f,0.30f),7);
        b.Box(new Vector3(-0.50f,0,-0.20f),new Vector3(0.10f,1.6f,0.10f),6);
        b.Box(new Vector3(0.50f,0,-0.20f),new Vector3(0.10f,1.6f,0.10f),6);
        meshes[3]=b.Finish("Vent cassette - 36 tris");
        b=new Builder();
        b.Face(12,new Vector3(-.5f,-.5f,0),new Vector3(-.4f,.6f,0),new Vector3(.5f,.2f,0),new Vector3(.2f,-.6f,0));
        b.Face(11,new Vector3(.2f,-.6f,.14f),new Vector3(.5f,.2f,.14f),new Vector3(-.4f,.6f,.14f),new Vector3(-.5f,-.5f,.14f));
        b.Box(new Vector3(0,0,.08f),new Vector3(.10f,.65f,.10f),10);
        meshes[4]=b.Finish("Broken sheet - 16 tris");
        b=new Builder();
        b.Box(Vector3.zero,new Vector3(.26f,1.4f,.25f),11,10);
        b.Box(new Vector3(0,-.45f,0),new Vector3(.42f,.12f,.40f),3);
        b.Box(new Vector3(0,.45f,0),new Vector3(.42f,.12f,.40f),6);
        meshes[5]=b.Finish("Conduit - 36 tris");
        return meshes;
    }

    public static Mesh CreateProjectile()
    {
        var b=new Builder();
        b.Box(Vector3.zero,new Vector3(.085f,.27f,.085f),10,6);
        b.Box(new Vector3(0,-.08f,0),new Vector3(.105f,.075f,.105f),3);
        b.Face(5,new Vector3(-.0425f,.135f,-.0425f),new Vector3(0,.26f,0),new Vector3(.0425f,.135f,-.0425f));
        b.Face(5,new Vector3(.0425f,.135f,.0425f),new Vector3(0,.26f,0),new Vector3(-.0425f,.135f,.0425f));
        b.Face(5,new Vector3(-.0425f,.135f,.0425f),new Vector3(0,.26f,0),new Vector3(-.0425f,.135f,-.0425f));
        b.Face(5,new Vector3(.0425f,.135f,-.0425f),new Vector3(0,.26f,0),new Vector3(.0425f,.135f,.0425f));
        return b.Finish("GR07 armour-piercing tracer - 28 tris");
    }

    public static Mesh CreateFlame()
    {
        var mesh=new Mesh {name="Low-poly exhaust plume"};
        mesh.vertices=new[] {new Vector3(-.08f,0,0),new Vector3(.08f,0,0),new Vector3(.10f,-.10f,0),new Vector3(0,-.65f,0),new Vector3(-.10f,-.10f,0),new Vector3(-.038f,0,-.005f),new Vector3(.038f,0,-.005f),new Vector3(0,-.40f,-.005f)};
        mesh.triangles=new[] {0,1,2,0,2,3,0,3,4,5,6,7};
        mesh.colors=new[] {new Color(1,.48f,.12f,.8f),new Color(1,.48f,.12f,.8f),new Color(.85f,.19f,.025f,.45f),new Color(.50f,.09f,.01f,0),new Color(.85f,.19f,.025f,.45f),new Color(1,.95f,.70f,1),new Color(1,.95f,.70f,1),new Color(1,.65f,.18f,.05f)};
        mesh.uv=new Vector2[8];mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }

    public static Mesh CreateEnemy(int kind)
    {
        var b=new Builder();
        if(kind==3)
        {
            // Heavy industrial carrier: recessed hangar, stepped bridge, armored outriggers,
            // four gun barrels and an exposed reactor. 272 triangles, independent of collider.
            b.Prism(new[]{new Vector2(-.55f,1.0f),new Vector2(.55f,1.0f),new Vector2(.72f,.1f),new Vector2(.43f,-1.0f),new Vector2(-.43f,-1.0f),new Vector2(-.72f,.1f)},.58f,0,Vector3.zero);
            b.Box(new Vector3(0,.60f,-.44f),new Vector3(.80f,.48f,.34f),11);
            b.Box(new Vector3(0,.63f,-.64f),new Vector3(.58f,.10f,.07f),9,10);
            b.Box(new Vector3(0,-.44f,-.34f),new Vector3(.64f,.58f,.12f),10,10);
            b.Box(new Vector3(0,-.76f,-.43f),new Vector3(.79f,.12f,.14f),3);
            b.Drum(new Vector3(0,.03f,-.43f),.27f,.20f,7,8,11);
            foreach(float side in new[]{-1f,1f})
            {
                b.Prism(new[]{new Vector2(0,.45f),new Vector2(side*1.12f,.72f),new Vector2(side*1.48f,.14f),new Vector2(side*.90f,-.27f)},.23f,12,new Vector3(side*.40f,.08f,.05f));
                b.Prism(new[]{new Vector2(-.25f,.89f),new Vector2(.25f,.89f),new Vector2(.31f,.45f),new Vector2(.25f,-.70f),new Vector2(-.25f,-.70f),new Vector2(-.31f,.45f)},.60f,0,new Vector3(side*1.35f,0,-.02f));
                b.Box(new Vector3(side*1.35f,.38f,-.39f),new Vector3(.40f,.52f,.13f),7,11);
                b.Box(new Vector3(side*1.35f,-.16f,-.37f),new Vector3(.48f,.12f,.12f),3);
                foreach(float offset in new[]{-.12f,.12f})b.Box(new Vector3(side*1.35f+offset,-.76f,-.24f),new Vector3(.10f,.92f,.13f),10,10);
                b.Prism(new[]{new Vector2(-.13f,0),new Vector2(.13f,0),new Vector2(0,-.52f)},.26f,11,new Vector3(side*.58f,-.63f,-.06f));
            }
            return b.Finish("Freight Warden - armored reactor carrier");
        }
        if(kind==2)
        {
            // Open twin-nacelle gunboat: separated engines, narrow keel and long cannons.
            b.Prism(new[]{new Vector2(-.26f,.88f),new Vector2(.26f,.88f),new Vector2(.38f,.05f),new Vector2(.18f,-.85f),new Vector2(-.18f,-.85f),new Vector2(-.38f,.05f)},.30f,0,Vector3.zero);
            b.Box(new Vector3(0,.32f,.06f),new Vector3(1.65f,.20f,.16f),10);
            b.Box(new Vector3(0,.28f,-.22f),new Vector3(.31f,.52f,.19f),11);
            b.Box(new Vector3(0,-.08f,-.33f),new Vector3(.16f,.12f,.06f),9);
            foreach(float side in new[]{-1f,1f})
            {
                b.Prism(new[]{new Vector2(-.22f,.94f),new Vector2(.22f,.94f),new Vector2(.28f,.48f),new Vector2(.18f,-.62f),new Vector2(-.18f,-.62f),new Vector2(-.28f,.48f)},.42f,0,new Vector3(side*.73f,0,0));
                b.Box(new Vector3(side*1.04f,-.35f,-.08f),new Vector3(.14f,1.12f,.18f),10);
                b.Box(new Vector3(side*.73f,.65f,-.26f),new Vector3(.30f,.35f,.10f),7);
                b.Box(new Vector3(side*.73f,.03f,-.25f),new Vector3(.40f,.09f,.08f),3);
            }
            return b.Finish("Twin-keel assault gunboat");
        }
        float width=kind==3 ? 1.5f : kind==2 ? 1.05f : .72f;
        b.Prism(new[]{new Vector2(-width,.48f),new Vector2(-width*.5f,.72f),new Vector2(width*.5f,.72f),new Vector2(width,.48f),new Vector2(width*.72f,-.23f),new Vector2(.22f,-.7f),new Vector2(-.22f,-.7f),new Vector2(-width*.72f,-.23f)},.34f,kind==1 ? 12 : 0,Vector3.zero);
        b.Box(new Vector3(0,.1f,-.25f),new Vector3(.34f,.62f,.2f),7);
        b.Box(new Vector3(0,-.25f,-.37f),new Vector3(.28f,.10f,.07f),5);
        for(int i=-1;i<=1;i+=2)
        {
            float x=i*width*.72f;
            b.Box(new Vector3(x,-.14f,-.1f),new Vector3(.20f,.9f,.22f),10);
            b.Box(new Vector3(x,.43f,-.18f),new Vector3(.27f,.22f,.25f),3);
        }
        if(kind==1)
            foreach(float side in new[]{-1f,1f}) b.Prism(new[]{new Vector2(0,.15f),new Vector2(side*.63f,.55f),new Vector2(side*.55f,-.24f)},.15f,12,new Vector3(side*.55f,0,.08f));
        if(kind>=2)
            foreach(float side in new[]{-1f,1f}){b.Drum(new Vector3(side*.66f,.1f,-.34f),.24f,.20f,14);b.Box(new Vector3(side*.66f,-.34f,-.35f),new Vector3(.12f,.64f,.12f),11);}
        if(kind==3)
        {
            b.Drum(new Vector3(0,.3f,-.48f),.36f,.22f,3,8);
            b.Box(new Vector3(0,-.45f,-.48f),new Vector3(.18f,.9f,.16f),10);
            b.Box(new Vector3(0,.7f,-.2f),new Vector3(1.8f,.15f,.2f),7);
        }
        return b.Finish(new[]{"Sentry interceptor","Scythe drone","Siege gunship","Freight warden"}[kind]);
    }

    public static readonly string[] ItemKinds={"life","mana","money","RabbitsFoot","BlankBullet","TwerpItem","Magnet","AK45Item","Caffeine","ProteinSupplement","AMoonItem","BabyYouItem","ElectricityItem","AnotherArm","AngryMoonItem","Telekinesis","ThreeMoonsItem"};

    static Mesh CreateLegacyItem(int kind)
    {
        var b=new Builder();string name=ItemKinds[kind];
        if(kind==0)
        {
            b.Box(Vector3.zero,new Vector3(.65f,.52f,.28f),0);
            b.Box(new Vector3(0,0,-.18f),new Vector3(.38f,.10f,.07f),5);
            b.Box(new Vector3(0,0,-.19f),new Vector3(.10f,.34f,.07f),5);
            b.Box(new Vector3(0,.31f,0),new Vector3(.24f,.08f,.12f),11);
        }
        else if(kind==1 || kind==8 || kind==9)
        {
            b.Drum(Vector3.zero,.29f,.42f,kind==1 ? 9 : 0,8);
            b.Box(new Vector3(0,0,-.25f),new Vector3(.13f,.40f,.08f),kind==1 ? 9 : 3);
            b.Box(new Vector3(0,.29f,0),new Vector3(.22f,.10f,.25f),11);
            if(kind==9)b.Box(new Vector3(0,-.28f,0),new Vector3(.4f,.12f,.32f),3);
        }
        else if(kind==2 || kind==3)
        {
            b.Prism(new[]{new Vector2(-.3f,-.12f),new Vector2(-.3f,.12f),new Vector2(.0f,.3f),new Vector2(.3f,.12f),new Vector2(.3f,-.12f),new Vector2(0,-.3f)},.14f,3,Vector3.zero);
            b.Box(new Vector3(0,0,-.12f),new Vector3(.12f,.34f,.10f),kind==2 ? 10 : 9);
            if(kind==3)b.Drum(new Vector3(0,.3f,0),.12f,.16f,11);
        }
        else if(kind==6)
        {
            b.Box(new Vector3(-.22f,0,0),new Vector3(.17f,.6f,.25f),11);b.Box(new Vector3(.22f,0,0),new Vector3(.17f,.6f,.25f),11);
            b.Box(new Vector3(0,-.25f,0),new Vector3(.55f,.17f,.25f),0);
            foreach(float x in new[]{-.22f,.22f})b.Box(new Vector3(x,.24f,0),new Vector3(.18f,.15f,.26f),3);
        }
        else if(kind==7)
        {
            b.Box(Vector3.zero,new Vector3(.7f,.19f,.22f),7);b.Box(new Vector3(.44f,0,0),new Vector3(.4f,.09f,.09f),11);
            b.Box(new Vector3(-.05f,-.18f,0),new Vector3(.14f,.32f,.18f),3);
            b.Box(new Vector3(-.3f,-.13f,0),new Vector3(.1f,.24f,.14f),10);
        }
        else if(kind==13)
        {
            b.Box(new Vector3(-.1f,.16f,0),new Vector3(.4f,.19f,.22f),0);
            b.Drum(new Vector3(.13f,.16f,0),.16f,.28f,3);
            b.Box(new Vector3(.13f,-.1f,0),new Vector3(.16f,.42f,.2f),11);
            b.Box(new Vector3(.13f,-.32f,0),new Vector3(.34f,.14f,.26f),10);
        }
        else if(kind==12 || kind==15)
        {
            b.Box(Vector3.zero,new Vector3(.48f,.3f,.3f),7);
            foreach(float x in new[]{-.2f,0,.2f}){b.Box(new Vector3(x,.27f,0),new Vector3(.07f,.3f,.07f),kind==15 ? 9 : 5);b.Box(new Vector3(x,-.27f,0),new Vector3(.07f,.3f,.07f),11);}
            b.Drum(new Vector3(0,0,-.22f),.13f,.16f,9);
        }
        else if(kind==16)
        {
            b.Drum(Vector3.zero,.23f,.26f,7,8);
            for(int i=0;i<3;i++){float a=i*2*Mathf.PI/3;b.Drum(new Vector3(Mathf.Cos(a)*.39f,Mathf.Sin(a)*.39f,0),.14f,.2f,10);}
        }
        else
        {
            b.Drum(Vector3.zero,.30f,.30f,kind==4 ? 3 : 0,8);
            b.Drum(new Vector3(0,0,-.21f),.15f,.14f,kind==14 ? 5 : 7);
            foreach(float side in new[]{-1f,1f})b.Box(new Vector3(side*.37f,0,0),new Vector3(.28f,.16f,.18f),kind==5 || kind==11 ? 3 : 11);
            if(kind==11)b.Box(new Vector3(0,.32f,0),new Vector3(.08f,.35f,.08f),10);
        }
        return b.Finish("Upgrade - "+name);
    }
}
