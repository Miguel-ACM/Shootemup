using System.Collections.Generic;
using UnityEngine;

/// <summary>Low-resolution cutout leaf cards over branched trunks, built into one mesh per section.</summary>
public static class PSXValleyFoliage
{
    public static Texture2D CreateAtlas()
    {
        const int tile=64,size=128;
        var pixels=new Color32[size*size];
        // Original, reproducible leaf art: clustered leaves, branching twigs and holes in the silhouette.
        for(int kind=0;kind<4;kind++)
        {
            var rng=new System.Random(471+kind*713);int ox=(kind%2)*tile,oy=(kind/2)*tile;
            for(int branch=0;branch<(kind==2 ? 11 : 19);branch++)
            {
                float angle=Range(rng,0,Mathf.PI*2),reach=Range(rng,10,24);
                float cx=32+Mathf.Cos(angle)*reach,cy=31+Mathf.Sin(angle)*reach*.85f;
                if(kind==2){cx=Range(rng,10,54);cy=Range(rng,34,56);}
                int steps=kind==2 ? 30 : 24;
                for(int i=0;i<steps;i++)
                {
                    float t=i/(float)(steps-1);
                    int px=Mathf.RoundToInt(Mathf.Lerp(32,cx,t)),py=Mathf.RoundToInt(Mathf.Lerp(kind==2 ? 3 : 26,cy,t));
                    pixels[(oy+py)*size+ox+px]=new Color(.19f,.235f,.12f,1);
                }
                int leaves=kind==2 ? 5 : 12;
                for(int leaf=0;leaf<leaves;leaf++)
                {
                    float lx=cx+Range(rng,-7,7),ly=cy+Range(rng,-6,6);
                    float rx=Range(rng,2,4.5f),ry=kind==1 ? Range(rng,3,7) : Range(rng,1.8f,3.5f);
                    if(kind==2){rx=Range(rng,1.2f,2);ry=Range(rng,4,8);}
                    Color dark=new Color(.16f,.25f,.125f),light=new Color(.43f,.49f,.25f);
                    float tint=Range(rng,.12f,.85f)+(ly-32)*.004f;
                    Color c=Color.Lerp(dark,light,tint);if(kind==1)c=Color.Lerp(c,new Color(.25f,.37f,.24f),.35f);
                    for(int y=Mathf.Max(1,(int)(ly-ry));y<=Mathf.Min(62,(int)(ly+ry));y++)
                    for(int x=Mathf.Max(1,(int)(lx-rx));x<=Mathf.Min(62,(int)(lx+rx));x++)
                    {
                        float dx=(x-lx)/rx,dy=(y-ly)/ry;
                        if(dx*dx+dy*dy>1)continue;
                        Color shaded=c*(y<ly ? .83f : 1);shaded.a=1;
                        pixels[(oy+y)*size+ox+x]=shaded;
                    }
                }
            }
        }
        var atlas=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Plains 64px leaf cards",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        atlas.SetPixels32(pixels);atlas.Apply(false,true);return atlas;
    }

    public static Mesh Build(float origin,float length,PSXValleyLayout layout)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var motion=new List<Vector2>();var triangles=new List<int>();
        foreach(var p in layout.Plants)
        {
            if(p.y<origin || p.y>=origin+length)continue;
            var rng=new System.Random(p.seed);
            float y=p.y-origin;
            if(p.kind==2)
            {
                Card(new Vector3(p.x,y,-.18f),p.radius*2.2f,p.radius*2.9f,p.angle,2,.93f,.16f);
                continue;
            }
            // Offset and tilt each crown card: a volume of leaves with a broken silhouette,
            // including a top-facing card so an overhead camera never sees edge-on foliage.
            int clusters=p.kind==1 ? 5 : 7;
            float height=p.kind==1 ? .95f : .65f;
            for(int cluster=0;cluster<clusters;cluster++)
            {
                float a=p.angle*Mathf.Deg2Rad+cluster*2.39996f;
                float ring=cluster==clusters-1 ? 0 : p.radius*.52f;
                float radius=p.radius*(cluster==clusters-1 ? .77f : Range(rng,.48f,.70f));
                var centre=new Vector3(p.x+Mathf.Cos(a)*ring,y+Mathf.Sin(a)*ring*.80f+height*.3f,-height-cluster*.065f);
                Card(centre,radius*2.25f,radius*(p.kind==1 ? 2.6f : 2.05f),Range(rng,0,360),p.kind,
                    Range(rng,.84f,1.10f),cluster%2==0 ? .3f : -.32f);
            }
            void Card(Vector3 centre,float width,float depth,float angle,int tile,float shade,float tilt)
            {
                int first=vertices.Count;float a=angle*Mathf.Deg2Rad;
                Vector3 right=new Vector3(Mathf.Cos(a),Mathf.Sin(a),tilt)*width*.5f;
                Vector3 up=new Vector3(-Mathf.Sin(a)*.88f,Mathf.Cos(a)*.88f,-tilt)*depth*.5f;
                vertices.AddRange(new[]{centre-right-up,centre-right+up,centre+right+up,centre+right-up});
                float u=(tile%2)*.5f,v=(tile/2)*.5f;
                uv.AddRange(new[]{new Vector2(u,v),new Vector2(u,v+.5f),new Vector2(u+.5f,v+.5f),new Vector2(u+.5f,v)});
                for(int i=0;i<4;i++)
                {
                    colors.Add(new Color(shade,shade,shade,1));
                    // Same phase for a whole tree; the rooted lower edge of reeds moves less.
                    motion.Add(new Vector2(p.angle*.1f,tile==2 && (i==0 || i==3) ? .05f : 1));
                }
                triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
        }
        var mesh=new Mesh{name="PSX cutout groves "+origin};
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,motion);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
        mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(.12f);mesh.bounds=bounds;return mesh;
    }
    static float Range(System.Random rng,float lo,float hi)=>Mathf.Lerp(lo,hi,(float)rng.NextDouble());
}
