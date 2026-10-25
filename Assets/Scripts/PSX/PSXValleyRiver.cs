using System.Collections.Generic;
using UnityEngine;

public static class PSXValleyRiver
{
    // Channel-space UVs bend the current with the river, and agree at every section seam.
    public static Mesh Build(float origin,float length,PSXValleyLayout layout)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        int rows=Mathf.CeilToInt(length/.75f);
        for(int row=0;row<=rows;row++)
        {
            float y=Mathf.Min(length,row*.75f),worldY=origin+y;
            for(int lane=0;lane<=8;lane++)
            {
                float u=lane/8f;
                vertices.Add(new Vector3(layout.Centre(worldY)+(u*2-1)*(layout.HalfWidth(worldY)-.07f),y,.005f));
                uv.Add(new Vector2(u,worldY));
                if(row==rows || lane==8)continue;
                int k=row*9+lane;triangles.AddRange(new[]{k,k+9,k+1,k+1,k+9,k+10});
            }
        }
        var mesh=new Mesh{name="Flowing Plains river "+origin};
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }
}
