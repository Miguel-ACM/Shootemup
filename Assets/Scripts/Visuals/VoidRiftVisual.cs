using UnityEngine;

// The boss keeps two rifts for the whole encounter. A pointed tear, luminous lips,
// inward streaks and a dark interior remain distinct from the round hostile sprites.
public sealed class VoidRiftVisual : MonoBehaviour
{
    static Mesh mesh;
    static Material surface;
    MeshRenderer visual;
    MaterialPropertyBlock ink;

    public static VoidRiftVisual Create(Transform parent)
    {
        if (mesh == null)
        {
            mesh = new Mesh { name = "Void dimensional slit" };
            mesh.vertices = new[] { new Vector3(-1,-1), new Vector3(1,-1), new Vector3(1,1), new Vector3(-1,1) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            mesh.RecalculateBounds();
        }
        if (surface == null) surface = new Material(Resources.Load<Shader>("VoidRift"));
        var go = new GameObject("Purple dimensional tear");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rift = go.AddComponent<VoidRiftVisual>();
        rift.ink = new MaterialPropertyBlock();
        rift.visual = go.AddComponent<MeshRenderer>();
        rift.visual.sharedMaterial = surface;
        rift.visual.sortingLayerName = "Enemy";
        rift.visual.sortingOrder = 20;
        rift.visual.enabled = false;
        return rift;
    }

    public void Pose(Vector3 position, float opening, float clock, bool arrival)
    {
        transform.position = position + Vector3.back * 2;
        transform.localScale = new Vector3(2.8f, 3.6f, 1);
        ink.SetFloat("_Open", Mathf.Clamp01(opening));
        ink.SetFloat("_Clock", clock);
        ink.SetFloat("_Arrival", arrival ? 1 : 0);
        visual.SetPropertyBlock(ink);
        visual.enabled = opening > .001f;
    }
    public void Hide() => visual.enabled = false;
}
