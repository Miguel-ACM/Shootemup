using UnityEngine;

// An inside view of six joined panels. The shader intersects the rotating cube
// per pixel so its far corners stay straight at the game's portrait resolution.
public sealed class WitnessChamber : MonoBehaviour
{
    Material interior;
    public static void Open()
    {
        var old = FindAnyObjectByType<VoidBackdrop>();
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Silver chamber";
        if (old != null)
        {
            go.transform.SetParent(old.transform.parent, false);
            old.gameObject.SetActive(false);
        }
        Destroy(go.GetComponent<Collider>());
        var chamber = go.AddComponent<WitnessChamber>();
        chamber.interior = new Material(Resources.Load<Shader>("WitnessChamber"));
        go.transform.position = new Vector3(0, 0, 24);
        go.transform.localScale = new Vector3(56, 42, 1);
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 12;
        renderer.sharedMaterial = chamber.interior;
    }
    float clock;
    void Update()
    {
        if (!WorldTimeStop.Frozen)
            clock += Time.deltaTime;
        interior.SetFloat("_Clock", clock);
    }
    void OnDestroy()
    {
        if (interior != null)
            Destroy(interior);
    }
}
