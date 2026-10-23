using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static void Armor(Builder b,Vector2[] shape,float depth,int paint,float z=0)
    {b.Prism(shape,depth,paint,new Vector3(0,0,z),6,11);}
    static void Motor(Builder b,float x,float y,float width=.28f,float length=.7f,int paint=0)
    {
        b.Prism(new[]{V(-width*.5f,-length*.5f),V(width*.5f,-length*.5f),V(width*.62f,length*.3f),V(width*.4f,length*.5f),V(-width*.4f,length*.5f),V(-width*.62f,length*.3f)},.28f,paint,new Vector3(x,y,-.04f),6);
        b.Box(new Vector3(x,y+length*.49f,-.02f),new Vector3(width*.72f,.12f,.22f),11,6);
        for(int i=0;i<4;i++)b.Box(new Vector3(x,y+(i-1.5f)*length*.15f,-.21f),new Vector3(width*.7f,.04f,.035f),11);
    }
    static void Glass(Builder b,float x,float y,float w,float h,float z=-.25f,int tile=9)
    {
        b.Prism(new[]{V(-w*.5f,h*.3f),V(-w*.34f,-h*.5f),V(w*.34f,-h*.5f),V(w*.5f,h*.3f),V(0,h*.5f)},.14f,11,new Vector3(x,y,z),6);
        b.Prism(new[]{V(-w*.38f,h*.23f),V(-w*.24f,-h*.37f),V(w*.24f,-h*.37f),V(w*.38f,h*.23f),V(0,h*.36f)},.035f,tile,new Vector3(x,y,z-.09f),6);
    }
    static void Barrel(Builder b,float x,float y,float length=.55f,float width=.12f,float z=-.15f)
    {
        b.Box(new Vector3(x,y,z),new Vector3(width,length,width),11,6);
        b.Box(new Vector3(x,y-length*.45f,z),new Vector3(width*1.5f,.12f,width*1.5f),6);
        b.Box(new Vector3(x,y-length*.52f,z-.03f),new Vector3(width*.7f,.025f,width*.7f),11);
    }
    static void ArcPlate(Builder b,float inner,float outer,float from,float to,float z,float depth,int paint)
    {
        int count=Mathf.Max(2,Mathf.CeilToInt((to-from)/12));
        for(int i=0;i<count;i++)
        {
            float a=Mathf.Lerp(from,to,i/(float)count)*Mathf.Deg2Rad,c=Mathf.Lerp(from,to,(i+1f)/count)*Mathf.Deg2Rad;
            Armor(b,new[]{V(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner),V(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer),V(Mathf.Cos(c)*outer,Mathf.Sin(c)*outer),V(Mathf.Cos(c)*inner,Mathf.Sin(c)*inner)},depth,paint,z);
        }
    }
}
