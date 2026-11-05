using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // Separate vertices on every face: the facets are structural, never smooth-shaded.
    static void CutStone(Builder b,Vector3 center,Vector3 size,int tile,int sides=6,float angle=0)
    {
        Vector3 P(int i,float radius,float z)
        {
            float a=i*Mathf.PI*2/sides+angle;
            return center+new Vector3(Mathf.Cos(a)*size.x*radius,Mathf.Sin(a)*size.y*radius,z*size.z);
        }
        var crown=new Vector3[sides];
        for(int i=0;i<sides;i++)
        {
            int j=(i+1)%sides;crown[sides-1-i]=P(i,.64f,-1);
            b.Face(tile,P(i,1,-.2f),P(i,.64f,-1),P(j,.64f,-1),P(j,1,-.2f));
            b.Face(tile,P(i,1,-.2f),P(j,1,-.2f),P(j,.75f,.7f),P(i,.75f,.7f));
            b.Face(tile,center+Vector3.forward*size.z,P(i,.75f,.7f),P(j,.75f,.7f));
        }
        b.Face(tile,crown);
    }
    static void CutBlade(Builder b,Vector3[] path,float width,float depth,int tile)
    {
        var left=new Vector3[path.Length];var right=new Vector3[path.Length];var ridge=new Vector3[path.Length];
        for(int i=0;i<path.Length;i++)
        {
            Vector3 tangent=(path[Mathf.Min(i+1,path.Length-1)]-path[Mathf.Max(0,i-1)]).normalized;
            Vector3 side=new Vector3(-tangent.y,tangent.x,0).normalized;
            float taper=i==path.Length-1?0:1-i/(float)(path.Length-1)*.65f;
            left[i]=path[i]-side*width*taper;right[i]=path[i]+side*width*taper;
            ridge[i]=path[i]+Vector3.back*depth*taper;
        }
        b.Face(tile,left[0],right[0],ridge[0]);
        for(int i=0;i<path.Length-1;i++)
        {
            int j=i+1;
            if(j==path.Length-1)
            {
                b.Face(tile,left[i],ridge[i],path[j]);b.Face(tile,right[i],path[j],ridge[i]);
                b.Face(11,left[i],path[j],right[i]);
            }
            else
            {
                // Shared mitered sections meet exactly, with no cracks or overlapping caps.
                b.Face(tile,left[i],ridge[i],ridge[j],left[j]);
                b.Face(tile,right[i],right[j],ridge[j],ridge[i]);
                b.Face(11,left[i],left[j],right[j],right[i]);
            }
        }
    }
    static void CutBand(Builder b,float radius,float width,float depth,int sides,int tile)
    {
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
            Vector3 P(float t,float r,float z)=>new Vector3(Mathf.Cos(t)*r,Mathf.Sin(t)*r,z);
            float inner=radius-width,outer=radius+width;
            b.Face(tile,P(a,inner,-depth),P(c,inner,-depth),P(c,outer,-depth),P(a,outer,-depth));
            b.Face(7,P(a,outer,-depth),P(c,outer,-depth),P(c,outer,depth),P(a,outer,depth));
            b.Face(7,P(a,inner,depth),P(c,inner,depth),P(c,inner,-depth),P(a,inner,-depth));
            b.Face(tile,P(a,inner,depth),P(a,outer,depth),P(c,outer,depth),P(c,inner,depth));
        }
    }
    static void CutCurve(Builder b,Vector3 a,Vector3 p,Vector3 q,Vector3 end,float width,float depth,int tile,bool taper=true,int rows=4)
        =>CutBlade(b,new[]{a,p,q,end},width,depth,tile);
    static void CutHalo(Builder b,Vector3 center,float radius,float tube,Vector3 scale,Quaternion rotation,int tile,int sides=10)
    {
        sides=Mathf.Min(sides,10);
        Vector3 P(float angle,float r,float z)=>center+rotation*Vector3.Scale(new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,z),scale);
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides,c=(i+1)*Mathf.PI*2/sides;
            b.Face(tile,P(a,radius-tube,-tube),P(c,radius-tube,-tube),P(c,radius+tube,-tube),P(a,radius+tube,-tube));
            b.Face(tile,P(a,radius+tube,-tube),P(c,radius+tube,-tube),P(c,radius+tube,tube),P(a,radius+tube,tube));
            b.Face(7,P(a,radius-tube,tube),P(c,radius-tube,tube),P(c,radius-tube,-tube),P(a,radius-tube,-tube));
            b.Face(tile,P(a,radius-tube,tube),P(a,radius+tube,tube),P(c,radius+tube,tube),P(c,radius-tube,tube));
        }
    }
}
