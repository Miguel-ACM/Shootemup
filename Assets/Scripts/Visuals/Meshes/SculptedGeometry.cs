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
        public void Sweep(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,float depth,int tile,bool taper=true,int rows=24)
        {
            Surface(12,rows,(u,t)=>{
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
        public void Halo(Vector3 center,float radius,float tube,Vector3 scale,Quaternion rotation,int tile,int sides=64)
        {
            Surface(sides,10,(u,v)=>{
                float a=u*Mathf.PI*2,b=-v*Mathf.PI*2;
                return center+rotation*Vector3.Scale(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(a)*(radius+Mathf.Cos(b)*tube),Mathf.Sin(b)*tube),scale);
            },tile);
        }
    }
    static void Feather(Builder b,Vector3 root,Vector3 tip,float width,float bend,int tile=15)
    {
        var delta=tip-root;var side=new Vector3(-delta.y,delta.x,0).normalized*bend;
        var p=root+delta*.30f+side+Vector3.back*.12f;var q=root+delta*.77f+side+Vector3.back*.19f;
        b.Sweep(root,p,q,tip,width,.095f,tile);
        b.Sweep(root,p+Vector3.back*.10f,q+Vector3.back*.07f,tip,.022f,.024f,3);
    }
    static void CelestialMuzzle(Builder b,Vector3 lip,float radius)
    {
        var rear=lip+Vector3.up*.62f;
        b.Sweep(rear,rear+Vector3.down*.16f,lip+Vector3.up*.2f,lip,radius,radius,7,false,10);
        b.Halo(lip,radius,.04f,Vector3.one,Quaternion.Euler(90,0,0),3,24);
        b.Pearl(lip+new Vector3(0,.045f,-.045f),new Vector3(radius*.68f,.045f,radius*.68f),8,20,8);
        for(int side=-1;side<=1;side+=2)
            b.Sweep(rear+Vector3.right*side*radius,rear+new Vector3(side*radius*1.35f,-.2f,-.1f),lip+new Vector3(side*radius*1.2f,.15f,-.08f),lip+Vector3.right*side*radius,.035f,.03f,3,false,12);
    }
    static void CelestialKeel(Builder b,float width,float length,float depth)
    {
        b.Sweep(new Vector3(0,length*.52f,0),new Vector3(0,length*.12f,-.15f),new Vector3(0,-length*.3f,-.06f),new Vector3(0,-length*.55f,0),width,depth,15);
        for(int s=-1;s<=1;s+=2)
            b.Sweep(new Vector3(0,length*.45f,-.12f),new Vector3(s*width*.88f,length*.16f,-depth*.8f),new Vector3(s*width*.72f,-length*.18f,-depth*.8f),new Vector3(0,-length*.5f,-.08f),.025f,.025f,3,false);
    }
}
