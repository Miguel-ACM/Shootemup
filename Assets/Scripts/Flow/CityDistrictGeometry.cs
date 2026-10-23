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
        void Face(Color c,params Vector3[] points)
        {
            int first=vertices.Count;
            foreach(var p in points){vertices.Add(new Vector3(p.x+p.z*.14f,p.y+p.z*.24f,-p.z));colors.Add(c);}
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
        public Mesh Finish(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
    static Color C(float r,float g,float b)=>new Color(r,g,b,1);
    public static Mesh Build(int seed,int section)
    {
        var rng=new System.Random(seed+section*7919);var b=new Builder();
        float R(float a,float z)=>(float)(a+(z-a)*rng.NextDouble());
        var roof=C(.28f,.32f,.34f);var trim=C(.39f,.43f,.44f);var glass=C(.14f,.27f,.32f);
        for(int row=0;row<8;row++)for(int column=0;column<8;column++)
        {
            float x=-9.1f+column*2.6f,y=-8.75f+row*2.5f;
            int district=(column/2+section)%4;int use=rng.Next(12);
            b.Box(x,y,.02f,2.12f,2.04f,.06f,C(.27f,.29f,.29f));
            if(use==0) // A planted square creates breathing room between dense blocks.
            {
                b.Box(x,y,.09f,1.8f,1.7f,.02f,C(.19f,.28f,.23f));
                b.Box(x,y,.12f,.22f,1.8f,.015f,trim);b.Box(x,y,.12f,1.8f,.22f,.015f,trim);
                for(int t=0;t<6;t++)b.Cylinder(x+R(-.75f,.75f),y+R(-.7f,.7f),.13f,.13f,R(.12f,.25f),C(.17f,.25f,.21f),7);
                continue;
            }
            if(use==1) // Parking lot, marked bays and varied parked vehicles.
            {
                b.Box(x,y,.09f,1.85f,1.8f,.02f,C(.15f,.19f,.22f));
                for(int lot=0;lot<7;lot++)for(int side=-1;side<=1;side+=2)
                {
                    float xx=x-.76f+lot*.25f,yy=y+side*.52f;
                    b.Box(xx,yy,.115f,.018f,.5f,.005f,C(.49f,.49f,.42f));
                    if(rng.NextDouble()<.65)b.Box(xx+.1f,yy,.13f,.12f,.3f,.08f,rng.NextDouble()<.2 ? C(.45f,.34f,.16f) : glass);
                }
                continue;
            }
            int lots=use==2 || district==2 ? 1 : use==3 ? 2 : 4;
            for(int lot=0;lot<lots;lot++)
            {
                float xx=x+(lots==1 ? 0 : (lot%2==0 ? -.5f : .5f));
                float yy=y+(lots<4 ? 0 : (lot<2 ? -.46f : .46f));
                float w=lots==1 ? R(1.3f,1.65f) : R(.69f,.86f),d=lots<4 ? R(1.35f,1.7f) : R(.68f,.84f);
                float h=district==2 ? R(.85f,1.5f) : district==0 ? R(.2f,.5f) : R(.4f,.85f);
                var facade=Color.Lerp(C(.23f,.29f,.32f),C(.4f,.36f,.3f),(float)rng.NextDouble());
                int style=rng.Next(9);
                if(style==0) // Stepped office tower with a recessed crown.
                {
                    b.Box(xx,yy,.09f,w,d,h*.58f,facade);b.Box(xx,yy,.09f+h*.58f,w*.78f,d*.8f,h*.28f,glass);
                    b.Box(xx,yy,.09f+h*.86f,w*.55f,d*.6f,h*.14f,facade);
                }
                else if(style==1) // L-shaped perimeter block.
                {b.Box(xx-w*.23f,yy,.09f,w*.54f,d,h,facade);b.Box(xx+w*.2f,yy-d*.3f,.09f,w*.45f,d*.4f,h*.72f,facade);}
                else if(style==2 && lots==1) // Courtyard with an inner garden.
                {
                    b.Box(xx-w*.4f,yy,.09f,w*.2f,d,h,facade);b.Box(xx+w*.4f,yy,.09f,w*.2f,d,h,facade);
                    b.Box(xx,yy-d*.4f,.09f,w*.6f,d*.2f,h,facade);b.Box(xx,yy+d*.4f,.09f,w*.6f,d*.2f,h,facade);
                    b.Box(xx,yy,.1f,w*.52f,d*.5f,.01f,C(.16f,.25f,.21f));
                }
                else if(style==3) // Round apartment tower on a square podium.
                {b.Box(xx,yy,.09f,w,d,.12f,roof);b.Cylinder(xx,yy,.21f,Mathf.Min(w,d)*.44f,h,facade,12);h+=.12f;}
                else if(style==4) // Warehouse with raised skylights.
                {
                    h*=.55f;b.Box(xx,yy,.09f,w,d,h,facade);
                    for(int j=0;j<3;j++)b.Box(xx+(j-1)*w*.24f,yy,.09f+h,w*.13f,d*.7f,.045f,glass);
                }
                else if(style==7) // Communications tower: tiered antenna on a small service building.
                {
                    h*=.55f;b.Box(xx,yy,.09f,w,d,h,facade);
                    b.Cylinder(xx,yy,.1f+h,Mathf.Min(w,d)*.3f,.12f,trim,8);
                    b.Cylinder(xx,yy,.22f+h,.035f,.64f,trim,6);
                    for(int dish=0;dish<3;dish++)b.Box(xx,yy,.35f+h+dish*.16f,w*.52f,.045f,.04f,glass);
                }
                else if(style==8) // Solar-covered factory sheds.
                {
                    h*=.5f;b.Box(xx,yy,.09f,w,d,h,facade);
                    for(int solar=0;solar<4;solar++)b.Box(xx+(solar-1.5f)*w*.2f,yy,.1f+h,w*.16f,d*.75f,.06f,glass);
                }
                else b.Box(xx,yy,.09f,w,d,h,facade);
                if(style!=1 && style!=2 && style!=3 && style!=7 && style!=8)
                {
                    // Parapets, lift housing, HVAC banks and service piping on each roof.
                    // A stepped tower's crown must not get a floating full-width roof.
                    float rw=style==0 ? w*.55f : w,rd=style==0 ? d*.6f : d;
                    b.Box(xx,yy,.09f+h,rw*.94f,rd*.94f,.025f,roof);
                    foreach(int side in new[]{-1,1})
                    {b.Box(xx+side*rw*.46f,yy,.11f+h,.035f,rd,.045f,trim);b.Box(xx,yy+side*rd*.46f,.11f+h,rw,.035f,.045f,trim);}
                    b.Box(xx-rw*.18f,yy+rd*.16f,.12f+h,rw*.28f,rd*.3f,.09f,trim);
                    for(int j=0;j<2;j++)
                    {b.Box(xx+rw*.23f,yy+(j-.5f)*rd*.25f,.12f+h,rw*.18f,rd*.17f,.06f,trim);b.Cylinder(xx+rw*.23f,yy+(j-.5f)*rd*.25f,.18f+h,.035f,.012f,roof,8);}
                    b.Box(xx,yy,.12f+h,.025f,rd*.55f,.025f,trim);
                    if(rng.NextDouble()<.18)b.Cylinder(xx,yy,.13f+h,.025f,.28f,trim,6);
                }
                // Window rows and vertical facade mullions are geometry, not random speckles.
                if(style!=1 && style!=2 && style!=3)
                for(float floor=.18f;floor<h;floor+=.16f)
                {
                    float fw=style==0 ? floor>h*.86f ? w*.55f : floor>h*.58f ? w*.78f : w : w;
                    float fd=style==0 ? floor>h*.86f ? d*.6f : floor>h*.58f ? d*.8f : d : d;
                    b.Box(xx,yy-fd*.505f,.09f+floor,fw*.82f,.012f,.055f,glass);
                    b.Box(xx+fw*.505f,yy,.09f+floor,.012f,fd*.82f,.055f,glass);
                }
                if(use==2 && lots==1) // Rooftop landing pad landmark.
                {
                    b.Cylinder(xx,yy,.15f+h,.38f,.01f,C(.2f,.25f,.26f),16);
                    b.Box(xx-.1f,yy,.17f+h,.035f,.26f,.006f,trim);b.Box(xx+.1f,yy,.17f+h,.035f,.26f,.006f,trim);b.Box(xx,yy,.17f+h,.2f,.035f,.006f,trim);
                }
            }
        }
        return b.Finish("Seeded city architecture "+section);
    }
}
