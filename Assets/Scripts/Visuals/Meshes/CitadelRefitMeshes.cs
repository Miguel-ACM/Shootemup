using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh citadelRefit, citadelReactor;
    public static Mesh CitadelRefit() => citadelRefit != null ? citadelRefit : citadelRefit = BlackHolePlane(3, 2, "Black hole accretion disk");
    public static Mesh CitadelReactor() => citadelReactor != null ? citadelReactor : citadelReactor = BlackHolePlane(.76f, .76f, "Dark event horizon");
    static Mesh BlackHolePlane(float x, float y, string name)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = new[] { new Vector3(-x,-y,0),new Vector3(x,-y,0),new Vector3(x,y,0),new Vector3(-x,y,0) };
        mesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
        mesh.triangles = new[] { 0,2,1,0,3,2 };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }
}
