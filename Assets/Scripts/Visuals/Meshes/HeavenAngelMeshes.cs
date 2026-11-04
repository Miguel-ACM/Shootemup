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
        angelPreview=new Mesh{name="Ophanim / living core and three eye-covered wheels",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};angelPreview.CombineMeshes(parts);return angelPreview;
    }
    public static Mesh HeavenAngelCore()
    {
        if(angelCore!=null)return angelCore;var b=new Builder();
        b.Pearl(Vector3.zero,new Vector3(.73f,.83f,.55f),15,40,24);
        b.Halo(Vector3.zero,.79f,.035f,new Vector3(1,1.1f,1),Quaternion.identity,3);
        for(int i=0;i<12;i++)
        {
            float angle=i*Mathf.PI/6;var axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            float length=i%3==0?1.23f:1.07f;
            Feather(b,(Vector3)(axis*.55f)+Vector3.forward*.18f,(Vector3)(axis*length)+Vector3.forward*.05f,.14f,.08f,i%2==0?15:3);
        }
        AngelEye(b,new Vector3(0,0,-.55f),.88f,.43f,0);
        return angelCore=b.Finish("Pearlescent living eye / radiant golden corona");
    }
    public static Mesh HeavenAngelWheel(int wheel)
    {
        if(angelWheels[wheel]!=null)return angelWheels[wheel];var b=new Builder();float radius=HeavenAngelVisual.Radii[wheel];
        b.Halo(Vector3.zero,radius,.055f,Vector3.one,Quaternion.identity,7);
        for(int braid=0;braid<2;braid++)
        {
            float phase=braid*Mathf.PI;
            b.Surface(128,6,(u,v)=>{
                float a=u*Mathf.PI*2,twist=a*9+phase,c=-v*Mathf.PI*2;
                float r=radius+Mathf.Cos(twist)*.07f+Mathf.Cos(c)*.024f;
                return new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(twist)*.07f+Mathf.Sin(c)*.024f);
            },3);
        }
        b.Halo(Vector3.zero,radius-.13f,.018f,Vector3.one,Quaternion.identity,3);
        b.Halo(Vector3.zero,radius+.13f,.018f,Vector3.one,Quaternion.identity,3);
        int count=HeavenAngelVisual.EyeCounts[wheel];
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count;
            AngelEye(b,new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,-.08f),.29f,.145f,angle+Mathf.PI*.5f);
            float between=angle+Mathf.PI/count;
            var point=new Vector3(Mathf.Cos(between)*radius,Mathf.Sin(between)*radius,-.115f);
            Vector3 outward=new Vector3(Mathf.Cos(between),Mathf.Sin(between),0);
            b.Sweep(point,point+outward*.10f+Vector3.back*.03f,point+outward*.19f,point+outward*.24f,.045f,.027f,3);
        }
        return angelWheels[wheel]=b.Finish("Golden wheel / "+count+" sculpted eyes");
    }
    static void AngelEye(Builder b,Vector3 center,float width,float height,float angle)
    {
        Vector3 Turn(float x,float y,float z)=>center+new Vector3(x*Mathf.Cos(angle)-y*Mathf.Sin(angle),x*Mathf.Sin(angle)+y*Mathf.Cos(angle),z);
        float bulge=width*.24f;
        b.Surface(32,12,(u,v)=>
        {
            float a=-u*Mathf.PI*2;
            return Turn(Mathf.Cos(a)*width*v,Mathf.Sin(a)*height*v,-bulge*Mathf.Sqrt(Mathf.Max(0,1-v*v)));
        },15);
        for(int s=-1;s<=1;s+=2)
            b.Sweep(Turn(-width,0,0),Turn(-width*.42f,s*height*1.37f,-bulge*.2f),Turn(width*.42f,s*height*1.37f,-bulge*.2f),Turn(width,0,0),width*.04f,width*.045f,7,false,24);
        float iris=height*.68f;
        b.Pearl(center+Vector3.back*(bulge+.012f),new Vector3(iris,iris,.028f+width*.02f),9,32,12);
        b.Halo(center+Vector3.back*(bulge+.025f),iris,.012f,Vector3.one,Quaternion.identity,3,32);
        b.Pearl(center+Vector3.back*(bulge+.045f),new Vector3(iris*.4f,iris*.46f,.019f),11,20,10);
        b.Pearl(Turn(-iris*.22f,iris*.27f,-bulge-.064f),new Vector3(iris*.14f,iris*.19f,.009f),15,12,8);
    }
}
