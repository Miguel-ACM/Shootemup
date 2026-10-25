using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// Buildings retain the street grid at section seams; block use and architecture vary with the run seed.
public static class CityDistrictGeometry
{
    sealed class Builder
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        Vector2 blockCenter;
        bool quarterTurn;
        public void OrientBlock(float x,float y,bool turn){blockCenter=new Vector2(x,y);quarterTurn=turn;}
        void Face(Color c,params Vector3[] points)
        {
            c.a=1; // Facade shading changes light, never the solidity of a building.
            int first=vertices.Count;
            foreach(var p in points)
            {
                var q=quarterTurn ? new Vector3(blockCenter.x-(p.y-blockCenter.y),blockCenter.y+(p.x-blockCenter.x),p.z) : p;
                vertices.Add(new Vector3(q.x+q.z*.14f,q.y+q.z*.24f,-q.z));colors.Add(c);
            }
            for(int i=1;i<points.Length-1;i++){triangles.Add(first);triangles.Add(first+i);triangles.Add(first+i+1);}
        }
        public void Box(float x,float y,float z,float w,float d,float h,Color c)
        {
            float a=x-w/2,b=x+w/2,f=y-d/2,k=y+d/2,t=z+h;
            Face(c*.65f,new Vector3(a,f,z),new Vector3(b,f,z),new Vector3(b,f,t),new Vector3(a,f,t));
            Face(c*.78f,new Vector3(b,f,z),new Vector3(b,k,z),new Vector3(b,k,t),new Vector3(b,f,t));
            Face(c*.87f,new Vector3(a,k,z),new Vector3(a,f,z),new Vector3(a,f,t),new Vector3(a,k,t));
            Face(c,new Vector3(a,f,t),new Vector3(b,f,t),new Vector3(b,k,t),new Vector3(a,k,t));
        }
        public void Cylinder(float x,float y,float z,float radius,float h,Color c,int sides=10)
        {
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                var p=new Vector3(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,z);
                var q=new Vector3(x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,z);
                Face(c*(.65f+.25f*i/sides),p,q,q+Vector3.forward*h,p+Vector3.forward*h);
                Face(c,new Vector3(x,y,z+h),p+Vector3.forward*h,q+Vector3.forward*h);
            }
        }
        public void Chamfer(float x,float y,float z,float w,float d,float h,Color c)
        {
            float cut=Mathf.Min(w,d)*.18f;
            var ring=new[]{new Vector2(-w/2+cut,-d/2),new Vector2(w/2-cut,-d/2),new Vector2(w/2,-d/2+cut),new Vector2(w/2,d/2-cut),
                new Vector2(w/2-cut,d/2),new Vector2(-w/2+cut,d/2),new Vector2(-w/2,d/2-cut),new Vector2(-w/2,-d/2+cut)};
            var top=new Vector3[ring.Length];
            for(int i=0;i<ring.Length;i++)
            {
                var a=new Vector3(x+ring[i].x,y+ring[i].y,z);var q=ring[(i+1)%ring.Length];var b=new Vector3(x+q.x,y+q.y,z);
                Face(c*(i<3 ? .66f : .83f),a,b,b+Vector3.forward*h,a+Vector3.forward*h);top[i]=a+Vector3.forward*h;
            }
            Face(c,top);
        }
        public void Ridge(float x,float y,float z,float w,float d,float rise,Color c)
        {
            var a=new Vector3(x-w/2,y-d/2,z);var q=new Vector3(x+w/2,y-d/2,z);
            var r=new Vector3(x+w/2,y+d/2,z);var s=new Vector3(x-w/2,y+d/2,z);
            var front=new Vector3(x,y-d/2,z+rise);var back=new Vector3(x,y+d/2,z+rise);
            Face(c*.72f,a,q,front);Face(c*.86f,s,a,front,back);Face(c,q,r,back,front);Face(c*.7f,r,s,back);
        }
        public Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
    static Color C(float r,float g,float b)=>new Color(r,g,b,1);
    static int BlockHash(int seed,int x,int y)
    {
        unchecked{uint h=(uint)seed^(uint)x*374761393u^(uint)y*668265263u;h=(h^(h>>13))*1274126177u;return (int)((h^(h>>16))&0x7fffffff);}
    }
    public static Mesh Build(int seed,int section)
    {
        var rng=new System.Random(seed+section*7919);var b=new Builder();
        float R(float a,float z)=>(float)(a+(z-a)*rng.NextDouble());
        var roof=C(.235f,.28f,.30f);var glass=C(.15f,.235f,.27f);
        for(int row=0;row<8;row++)for(int column=0;column<8;column++)
        {
            float x=-9.1f+column*2.6f,y=-8.75f+row*2.5f;
            b.OrientBlock(x,y,false);
            // Coarse zones make neighborhoods, not a different random building in every cell.
            int zoneHash=BlockHash(seed,column/2,(section*8+row)/2),zone=zoneHash%4;
            bool landmark=column%2==((zoneHash>>3)&1) && row%2==((zoneHash>>4)&1);
            int use=rng.Next(100);
            var paving=C(.235f,.27f,.28f);b.Box(x,y,.02f,2.05f,1.98f,.06f,paving);
            if(use<12 && !landmark)
            {
                b.Box(x,y,.09f,1.82f,1.73f,.02f,C(.19f,.265f,.23f));
                b.Box(x+.24f,y,.12f,.18f,1.75f,.015f,paving);
                b.Box(x,y-.22f,.12f,1.8f,.17f,.015f,paving);
                for(int t=0;t<5;t++)b.Cylinder(x+R(-.72f,.72f),y+R(-.65f,.65f),.13f,R(.14f,.24f),R(.16f,.32f),C(.17f,.245f,.21f),7);
                continue;
            }
            if(use<19 && !landmark)
            {
                b.Box(x,y,.09f,1.85f,1.8f,.02f,C(.18f,.215f,.235f));
                for(int lot=0;lot<5;lot++)for(int side=-1;side<=1;side+=2)
                {
                    float xx=x-.72f+lot*.35f,yy=y+side*.52f;
                    b.Box(xx,yy,.115f,.018f,.43f,.005f,C(.32f,.34f,.31f));
                    if(rng.NextDouble()<.6)b.Box(xx+.13f,yy,.13f,.14f,.29f,.07f,rng.NextDouble()<.2 ? C(.35f,.31f,.21f) : glass);
                }
                continue;
            }
            // Adjacent blocks share an axis, while independent zone bits vary it between neighborhoods.
            // Streets and paving retain their section-safe grid; only the architecture turns.
            b.OrientBlock(x,y,((zoneHash>>11)&1)!=0);
            int lots=landmark || zone==3 ? 1 : zone==0 && use>65 ? 4 : 2;
            var districtColor=zone==3 ? C(.26f,.31f,.30f) : zone==2 ? C(.27f,.31f,.34f) : zone==1 ? C(.34f,.345f,.31f) : C(.29f,.32f,.32f);
            for(int lot=0;lot<lots;lot++)
            {
                float xx=x+(lots==1 ? 0 : (lot%2==0 ? -.5f : .5f));
                float yy=y+(lots<4 ? 0 : (lot<2 ? -.46f : .46f));
                float w=lots==1 ? R(1.4f,1.76f) : R(.70f,.86f),d=lots<4 ? R(1.35f,1.72f) : R(.68f,.84f);
                float h=zone==2 && landmark ? R(1.9f,2.65f) : zone==1 && landmark ? R(.9f,1.35f) : zone==0 ? R(.26f,.58f) : R(.42f,.84f);
                var facade=Color.Lerp(districtColor,roof,R(.05f,.25f));
                var trim=Color.Lerp(facade,C(.43f,.46f,.45f),.22f);
                int style=zone==3 ? (use%2==0 ? 4 : 8) : zone==2 && landmark ? (((zoneHash>>7)&1)==0 ? 0 : 9) :
                    zone==1 && landmark ? (((zoneHash>>7)&1)==0 ? 3 : 2) : zone==0 ? (use%3==0 ? 1 : 5) : (use%3==0 ? 0 : 6);
                // A ground-level shadow reinforces the rare tall shapes without bright outlines.
                if(h>1)b.Box(xx+.13f,yy+.24f,.085f,w+.1f,d+.18f,.003f,C(.16f,.20f,.22f));
                if(style==0)
                {
                    b.Box(xx,yy,.09f,w,d,h*.50f,facade);
                    b.Box(xx-w*.07f,yy+d*.06f,.09f+h*.50f,w*.78f,d*.78f,h*.29f,Color.Lerp(facade,glass,.35f));
                    b.Chamfer(xx-w*.07f,yy+d*.06f,.09f+h*.79f,w*.52f,d*.57f,h*.21f,facade);
                }
                else if(style==1)
                {b.Box(xx-w*.23f,yy,.09f,w*.54f,d,h,facade);b.Box(xx+w*.2f,yy-d*.3f,.09f,w*.45f,d*.4f,h*.72f,facade);}
                else if(style==2)
                {
                    b.Box(xx-w*.4f,yy,.09f,w*.2f,d,h,facade);b.Box(xx+w*.4f,yy,.09f,w*.2f,d,h,facade);
                    b.Box(xx,yy-d*.4f,.09f,w*.6f,d*.2f,h,facade);b.Box(xx,yy+d*.4f,.09f,w*.6f,d*.2f,h,facade);
                    b.Box(xx,yy,.1f,w*.52f,d*.5f,.01f,C(.16f,.25f,.21f));
                }
                else if(style==3) // A civic rotunda surrounded by a quiet low podium.
                {
                    b.Box(xx,yy,.09f,w,d,.18f,roof);
                    b.Cylinder(xx,yy,.27f,Mathf.Min(w,d)*.44f,h*.68f,facade,12);
                    b.Cylinder(xx,yy,.27f+h*.68f,Mathf.Min(w,d)*.32f,h*.20f,trim,12);
                }
                else if(style==4) // Industrial halls use broad pitched roof bays.
                {
                    h*=.65f;b.Box(xx,yy,.09f,w,d,h,facade);
                    for(int j=0;j<2;j++)b.Ridge(xx+(j-.5f)*w*.5f,yy,.09f+h,w*.49f,d,.16f,roof);
                }
                else if(style==8)
                {
                    h*=.65f;b.Box(xx,yy,.09f,w,d,h,facade);
                    for(int solar=0;solar<2;solar++)b.Box(xx+(solar-.5f)*w*.35f,yy,.1f+h,w*.25f,d*.66f,.055f,Color.Lerp(roof,glass,.65f));
                }
                else if(style==9)
                {
                    b.Chamfer(xx,yy,.09f,w,d,h*.22f,facade);
                    b.Chamfer(xx,yy,.09f+h*.22f,w*.74f,d*.76f,h*.68f,Color.Lerp(facade,glass,.30f));
                    b.Chamfer(xx,yy,.09f+h*.90f,w*.57f,d*.58f,h*.10f,facade);
                }
                else if(style==5)
                {b.Box(xx,yy,.09f,w,d,h,facade);b.Ridge(xx,yy,.09f+h,w,d,.12f,roof);}
                else b.Box(xx,yy,.09f,w,d,h,facade);
                if(style==0 || style==6 || style==9)
                {
                    float rw=style==0 ? w*.52f : style==9 ? w*.57f : w,rd=style==0 ? d*.57f : style==9 ? d*.58f : d;
                    float rx=xx-(style==0 ? w*.07f : 0),ry=yy+(style==0 ? d*.06f : 0);
                    if(style!=9)b.Box(rx,ry,.09f+h,rw*.94f,rd*.94f,.025f,roof);
                    // A single service feature on selected roofs; large surfaces do the visual work.
                    if(use%3==0)b.Box(rx-rw*.13f,ry+rd*.14f,.12f+h,rw*.27f,rd*.30f,.10f,trim);
                    else if(landmark && use%3==1)
                    {b.Cylinder(rx,ry,.12f+h,.035f,.40f,trim,6);b.Box(rx,ry,.42f+h,.25f,.035f,.025f,trim);}
                    if(landmark && style==0)b.Box(rx,ry+rd*.47f,.11f+h,rw,.03f,.035f,trim);
                }
                if(style==0 || style==6)
                for(float floor=.25f;floor<h;floor+=.31f)
                {
                    float fw=style==0 ? floor>h*.79f ? w*.52f : floor>h*.50f ? w*.78f : w : w;
                    float fd=style==0 ? floor>h*.79f ? d*.57f : floor>h*.50f ? d*.78f : d : d;
                    float shift=style==0 && floor>h*.5f ? 1 : 0;
                    b.Box(xx-w*.07f*shift,yy+d*.06f*shift-fd*.505f,.09f+floor,fw*.70f,.012f,.045f,Color.Lerp(facade,glass,.55f));
                }
            }
        }
        return b.Finish("Seeded city architecture "+section);
    }
}
