using System.Collections.Generic;
using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<(int,float,float),Mesh> tunnelPassages=new();
    public static Mesh TunnelPassageMesh(int layout)
    {
        var key=(layout,Camera.RightLimit,Camera.UpLimit);
        if(tunnelPassages.TryGetValue(key,out var mesh))return mesh;
        var b=new Builder();
        foreach(var r in TunnelPassage.Sections(layout))
        {
            b.Prism(new[]{new Vector2(r.LeftBottom,r.Bottom),new Vector2(r.RightBottom,r.Bottom),new Vector2(r.RightTop,r.Top),new Vector2(r.LeftTop,r.Top)},.8f,0,new Vector3(0,0,.4f),11);
            // Silver rails and brass marks show the exact sloping collision edge.
            foreach(bool left in new[]{true,false})
            {
                float a=left?r.LeftBottom:r.RightBottom,c=left?r.LeftTop:r.RightTop,inset=left?.16f:-.16f;
                b.Prism(new[]{new Vector2(a,r.Bottom),new Vector2(a+inset,r.Bottom),new Vector2(c+inset,r.Top),new Vector2(c,r.Top)},.14f,6,new Vector3(0,0,-.05f),11);
                for(int j=0;j<8;j++)
                {
                    float t=(j+.5f)/8,y=Mathf.Lerp(r.Bottom,r.Top,t),x=Mathf.Lerp(a,c,t)+inset*.5f;
                    b.Box(new Vector3(x,y,-.15f),new Vector3(.12f,.26f,.06f),13,11);
                }
            }
            for(int j=0;j<6;j++)
            {
                float t=(j+.5f)/6,y=Mathf.Lerp(r.Bottom,r.Top,t);
                float left=Mathf.Lerp(r.LeftBottom,r.LeftTop,t)+.28f,right=Mathf.Lerp(r.RightBottom,r.RightTop,t)-.28f;
                if(right-left<.2f)continue;
                b.Box(new Vector3((left+right)*.5f,y,-.06f),new Vector3(right-left,.1f,.10f),6,11);
                b.Box(new Vector3((left+right)*.5f,y+.3f,-.04f),new Vector3(Mathf.Max(.1f,right-left-.28f),.35f,.08f),11,6);
            }
        }
        return tunnelPassages[key]=b.Finish("Sloped tunnel passage "+layout);
    }
}

