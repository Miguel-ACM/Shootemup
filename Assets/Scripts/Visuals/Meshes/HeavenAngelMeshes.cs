using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh angelCore,angelPreview;
    static readonly Mesh[] angelWheels=new Mesh[3];
    public static Mesh SeraphJudicatorBossMesh()
    {
        if(angelPreview!=null)return angelPreview;
        var parts=new CombineInstance[4];parts[0]=new CombineInstance{mesh=HeavenAngelCore(),transform=Matrix4x4.identity};
        for(int i=0;i<3;i++)parts[i+1]=new CombineInstance{mesh=HeavenAngelWheel(i),transform=Matrix4x4.Rotate(HeavenAngelVisual.Orientation(i,0))};
        angelPreview=new Mesh{name="Ophanim / living core and three eye-covered wheels"};angelPreview.CombineMeshes(parts);return angelPreview;
    }
    public static Mesh HeavenAngelCore()
    {
        if(angelCore!=null)return angelCore;var b=new Builder();
        b.Ball(Vector3.zero,new Vector3(.78f,.78f,.58f),15);
        AngelTorus(b,.81f,.055f,.03f,3);
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI/6;var axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));var tangent=new Vector2(-axis.y,axis.x);
            float length=i%3==0?1.16f:1.0f;
            b.Prism(new[]{axis*.77f+tangent*.07f,axis*length,axis*.77f-tangent*.07f},.08f,8,Vector3.zero,7);
        }
        AngelEye(b,new Vector3(0,0,-.55f),.88f,.43f,0);
        return angelCore=b.Finish("Pearlescent living eye / radiant golden corona");
    }
    public static Mesh HeavenAngelWheel(int wheel)
    {
        if(angelWheels[wheel]!=null)return angelWheels[wheel];var b=new Builder();float radius=HeavenAngelVisual.Radii[wheel];
        AngelTorus(b,radius,.105f,0,3);
        AngelTorus(b,radius-.12f,.022f,-.015f,8);
        AngelTorus(b,radius+.12f,.022f,-.015f,8);
        int count=HeavenAngelVisual.EyeCounts[wheel];
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count;
            AngelEye(b,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,-.08f),.29f,.145f,angle+Mathf.PI*.5f);
            float between=angle+Mathf.PI/count;
            var point=new Vector3(Mathf.Cos(between)*radius,Mathf.Sin(between)*radius,-.115f);
            b.Drum(point,.045f,.025f,8,6,7);
        }
        return angelWheels[wheel]=b.Finish("Golden wheel / "+count+" sculpted eyes");
    }
    static void AngelTorus(Builder b,float radius,float tube,float z,int tile)
    {
        const int segments=64,section=8;
        Vector3 P(int i,int j)
        {
            float angle=i*Mathf.PI*2/segments,cross=j*Mathf.PI*2/section;
            float r=radius+Mathf.Cos(cross)*tube;
            return new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,z+Mathf.Sin(cross)*tube);
        }
        for(int i=0;i<segments;i++)for(int j=0;j<section;j++)b.Face(tile,P(i,j),P(i+1,j),P(i+1,j+1),P(i,j+1));
    }
    static void AngelEye(Builder b,Vector3 center,float width,float height,float angle)
    {
        Vector2 Turn(Vector2 p)=>new Vector2(p.x*Mathf.Cos(angle)-p.y*Mathf.Sin(angle),p.x*Mathf.Sin(angle)+p.y*Mathf.Cos(angle));
        Vector2[] Lid(float scale)
        {
            var points=new Vector2[16];
            for(int i=0;i<16;i++)
            {
                float t=i*Mathf.PI*2/16;
                points[i]=Turn(new Vector2(Mathf.Cos(t)*width,Mathf.Sin(t)*height*Mathf.Pow(Mathf.Abs(Mathf.Sin(t)),.35f))*scale);
            }
            return points;
        }
        b.Prism(Lid(1.12f),.08f,7,center,2,3);
        b.Prism(Lid(1),.07f,11,center+Vector3.back*.035f,7,7);
        b.Prism(Lid(.87f),.075f,15,center+Vector3.back*.065f,1,1);
        float iris=height*.67f;
        b.Drum(center+Vector3.back*.12f,iris,.03f,3,16,7);
        b.Drum(center+Vector3.back*.14f,iris*.78f,.025f,9,16,9);
        b.Drum(center+Vector3.back*.16f,iris*.42f,.025f,11,16,11);
        var gleam=Turn(new Vector2(-iris*.23f,iris*.25f));
        b.Drum(center+new Vector3(gleam.x,gleam.y,-.18f),iris*.16f,.012f,15,8,15);
    }
}
