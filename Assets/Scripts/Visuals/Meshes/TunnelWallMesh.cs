using System.Collections.Generic;
using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<int,Mesh> tunnelWalls=new();
    public static Mesh TunnelWallMesh(float width)
    {
        int key=Mathf.RoundToInt(width*1000);
        if(tunnelWalls.TryGetValue(key,out var mesh))return mesh;
        var b=new Builder();
        b.Box(Vector3.zero,new Vector3(width,4.75f,.8f),0,11);
        foreach(float side in new[]{-1f,1f})
        {
            b.Box(new Vector3(side*(width*.5f-.09f),0,-.47f),new Vector3(.18f,4.75f,.16f),6,7);
            for(int row=0;row<4;row++)
                b.Box(new Vector3(side*(width*.5f-.12f),-1.7f+row*1.1f,-.58f),new Vector3(.20f,.35f,.06f),13,7);
        }
        for(int row=0;row<3;row++)
        {
            float y=-1.6f+row*1.6f;
            b.Box(new Vector3(0,y,-.45f),new Vector3(width-.3f,.12f,.16f),6,11);
            b.Box(new Vector3(0,y+.5f,-.43f),new Vector3(Mathf.Max(.15f,width-.65f),.55f,.08f),11,6);
        }
        return tunnelWalls[key]=b.Finish("Solid tunnel wall with ribs and brass edge markers");
    }
}
