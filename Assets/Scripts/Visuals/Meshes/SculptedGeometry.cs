using System;
using UnityEngine;

public static partial class IndustrialMeshFactory
{
    sealed partial class Builder
    {
        // Shared vertices give the authored curved surfaces continuous normals.
        public void Surface(int slices,int rows,Func<float,float,Vector3> point,int tile)
        {
            int start=vertices.Count;
            for(int row=0;row<=rows;row++)for(int col=0;col<=slices;col++)
            {
                vertices.Add(point(col/(float)slices,row/(float)rows));
                uv.Add(new Vector2((tile%4+.5f)/4,(tile/4+.5f)/4));
                if(row==rows||col==slices)continue;
                int a=start+row*(slices+1)+col,b=a+1,c=a+slices+1,d=c+1;
                indices.Add(a);indices.Add(c);indices.Add(b);indices.Add(b);indices.Add(c);indices.Add(d);
            }
        }
        public void Pearl(Vector3 center,Vector3 radius,int tile,int sides=28,int rows=16)
        {
            Surface(sides,rows,(u,v)=>{
                float a=u*Mathf.PI*2,b=(v-.5f)*Mathf.PI;
                return center+Vector3.Scale(new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(b),Mathf.Sin(a)*Mathf.Cos(b)),radius);
            },tile);
        }
        public void Sweep(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,float depth,int tile,bool taper=true,int rows=24,int sides=12)
        {
            Surface(sides,rows,(u,t)=>{
                float r=1-t;Vector3 p=r*r*r*a+3*r*r*t*b+3*r*t*t*c+t*t*t*d;
                Vector3 tangent=(3*r*r*(b-a)+6*r*t*(c-b)+3*t*t*(d-c)).normalized;
                Vector3 side=Vector3.Cross(tangent,Vector3.forward).normalized;
                if(side.sqrMagnitude<.1f)side=Vector3.right;
                Vector3 normal=Vector3.Cross(side,tangent).normalized;
                float envelope=taper?Mathf.Max(.008f,Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)),.65f)*(1-.55f*t)):1;
                float angle=u*Mathf.PI*2;
                return p+side*(Mathf.Cos(angle)*width*envelope)+normal*(Mathf.Sin(angle)*depth*envelope);
            },tile);
        }
        public void Halo(Vector3 center,float radius,float tube,Vector3 scale,Quaternion rotation,int tile,int sides=64,int tubeSides=10)
        {
            Surface(sides,tubeSides,(u,v)=>{
                float a=u*Mathf.PI*2,b=-v*Mathf.PI*2;
                return center+rotation*Vector3.Scale(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(b)*tube),scale);
            },tile);
        }
    }
    static void Feather(Builder b,Vector3 root,Vector3 tip,float width,float bend,int tile=15)
    {
        var delta=tip-root;var side=new Vector3(-delta.y,delta.x,0).normalized*bend;
        var p=root+delta*.30f+side+Vector3.back*.12f;var q=root+delta*.77f+side+Vector3.back*.19f;
        CutBlade(b,new[]{root,p,q,tip},width,.095f,tile);
        CutBlade(b,new[]{root,p+Vector3.back*.10f,q+Vector3.back*.07f,tip},.018f,.018f,3);
    }
    static void CelestialMuzzle(Builder b,Vector3 lip,float radius)
    {
        var rear=lip+Vector3.up*.62f;
        BevelArmor(b,new[]{V(rear.x-radius*.7f,rear.y),V(rear.x+radius*.7f,rear.y),V(lip.x+radius,lip.y),V(lip.x-radius,lip.y)},radius*1.6f,15,lip.z,.18f);
        b.Box(lip,new Vector3(radius*1.7f,.075f,radius*1.5f),7,3);
        b.Box(lip+Vector3.down*.043f,new Vector3(radius*1.2f,.025f,radius),11,11);
        CutStone(b,lip+new Vector3(0,.04f,-radius*.85f),new Vector3(radius*.58f,.11f,.035f),8,4);
        for(int side=-1;side<=1;side+=2)
            CutBlade(b,new[]{rear+Vector3.right*side*radius,rear+new Vector3(side*radius*1.35f,-.2f,-.1f),lip+new Vector3(side*radius*1.2f,.15f,-.08f),lip+Vector3.right*side*radius},.035f,.03f,3);
    }
    static void CelestialKeel(Builder b,float width,float length,float depth)
    {
        BevelArmor(b,new[]{V(0,length*.52f),V(width,length*.20f),V(width*.7f,-length*.24f),V(0,-length*.55f),V(-width*.7f,-length*.24f),V(-width,length*.20f)},depth*1.7f,15,0,.38f);
        for(int s=-1;s<=1;s+=2)
            CutBlade(b,new[]{new Vector3(0,length*.45f,-.12f),new Vector3(s*width*.88f,length*.16f,-depth*.8f),new Vector3(s*width*.72f,-length*.18f,-depth*.8f),new Vector3(0,-length*.5f,-.08f)},.025f,.025f,3);
    }
}
