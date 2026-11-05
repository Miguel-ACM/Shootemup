using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh angelCore,angelPreview,angelIris;
    static readonly Mesh[] angelWheels=new Mesh[3];
    public static Mesh SeraphJudicatorBossMesh()
    {
        if(angelPreview!=null)return angelPreview;
        var parts=new CombineInstance[5];parts[0]=new CombineInstance{mesh=HeavenAngelCore(),transform=Matrix4x4.identity};
        for(int i=0;i<3;i++)parts[i+1]=new CombineInstance{mesh=HeavenAngelWheel(i),transform=Matrix4x4.Rotate(HeavenAngelVisual.Orientation(i,0))};
        parts[4]=new CombineInstance{mesh=HeavenAngelIris(),transform=Matrix4x4.Translate(Vector3.back*.77f)};
        angelPreview=new Mesh{name="Ophanim / cut gold and ivory",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};angelPreview.CombineMeshes(parts);return angelPreview;
    }
    public static Mesh HeavenAngelCore()
    {
        if(angelCore!=null)return angelCore;var b=new Builder();
        CutStone(b,new Vector3(0,0,.1f),new Vector3(.72f,.90f,.48f),15,8,Mathf.PI/8);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;var u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            var v=new Vector3(-u.y,u.x,0);float length=i%3==0?1.29f:1.08f;
            CutBlade(b,new[]{u*.51f+Vector3.forward*.12f,u*.80f+v*.06f,u*length},.115f,.13f,i%2==0?15:3);
        }
        AngelEye(b,new Vector3(0,0,-.48f),.86f,.43f,0,false);
        return angelCore=b.Finish("Ivory reliquary / faceted living eye");
    }
    public static Mesh HeavenAngelIris()
    {
        if(angelIris!=null)return angelIris;var b=new Builder();
        CutStone(b,Vector3.zero,new Vector3(.275f,.275f,.045f),9,10);
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI/5;var u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            CutBlade(b,new[]{u*.14f+Vector3.back*.05f,u*.24f+Vector3.back*.04f},.018f,.008f,3);
        }
        CutStone(b,Vector3.back*.065f,new Vector3(.105f,.15f,.015f),11,6);
        CutStone(b,new Vector3(-.05f,.07f,-.087f),new Vector3(.035f,.045f,.009f),15,4);
        return angelIris=b.Finish("Cut turquoise iris / black slit");
    }
    public static Mesh HeavenAngelWheel(int wheel)
    {
        if(angelWheels[wheel]!=null)return angelWheels[wheel];var b=new Builder();
        float radius=HeavenAngelVisual.Radii[wheel];int count=HeavenAngelVisual.EyeCounts[wheel];
        CutBand(b,radius,.105f,.075f,count,3);
        CutBand(b,radius-.17f,.026f,.045f,count,7);
        for(int i=0;i<count;i++)
        {
            float angle=i*Mathf.PI*2/count;
            var u=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            CutStone(b,u*radius,new Vector3(.16f,.16f,.10f),3,4,angle);
            AngelEye(b,u*radius+Vector3.back*.105f,.285f,.14f,angle+Mathf.PI*.5f);
            float a=angle+Mathf.PI/count;var v=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            CutBlade(b,new[]{v*(radius+.01f),v*(radius+.21f)+Vector3.back*.07f,v*(radius+.34f)},.07f,.07f,3);
        }
        return angelWheels[wheel]=b.Finish("Articulated polygonal gold wheel / "+count+" inset eyes");
    }
    static void AngelEye(Builder b,Vector3 center,float width,float height,float angle,bool withIris=true)
    {
        Vector3 P(float x,float y,float z)=>center+new Vector3(x*Mathf.Cos(angle)-y*Mathf.Sin(angle),x*Mathf.Sin(angle)+y*Mathf.Cos(angle),z);
        // Six-sided almond rim and a shallow crystalline sclera. No spherical cap.
        var rim=new[]{P(-width,0,0),P(-width*.38f,height,0),P(width*.38f,height,0),P(width,0,0),P(width*.38f,-height,0),P(-width*.38f,-height,0)};
        var pupil=P(0,0,-width*.24f);
        for(int i=0;i<6;i++)
        {
            int j=(i+1)%6;b.Face(15,rim[i],rim[j],pupil);
            b.Face(3,rim[i],center+(rim[i]-center)*1.13f+Vector3.forward*.025f,center+(rim[j]-center)*1.13f+Vector3.forward*.025f,rim[j]);
            b.Face(7,center+(rim[i]-center)*1.13f+Vector3.forward*.025f,center+(rim[i]-center)*1.13f+Vector3.forward*.09f,center+(rim[j]-center)*1.13f+Vector3.forward*.09f,center+(rim[j]-center)*1.13f+Vector3.forward*.025f);
        }
        if(!withIris)return;
        float r=height*.63f;
        CutStone(b,pupil+Vector3.back*.013f,new Vector3(r,r,.023f),9,8);
        CutStone(b,pupil+Vector3.back*.04f,new Vector3(r*.32f,r*.65f,.012f),11,4);
    }
}
