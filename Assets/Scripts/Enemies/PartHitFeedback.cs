using UnityEngine;

// Only the struck module flashes and recoils. Never move its parent hull or collider.
[DefaultExecutionOrder(150)]
public sealed class PartHitFeedback : MonoBehaviour
{
    Transform visual;
    MeshRenderer[] meshes;
    Vector3 rest, scale;
    Vector2 kick;
    float heat;
    MaterialPropertyBlock ink;
    void Awake()
    {
        ink = new MaterialPropertyBlock();
    }

    public float Heat => heat;
    public static PartHitFeedback Bind(Transform visual)
    {
        var hit = visual.gameObject.AddComponent<PartHitFeedback>();
        hit.visual = visual;
        hit.rest = visual.localPosition;
        hit.scale = visual.localScale;
        hit.meshes = visual.GetComponentsInChildren<MeshRenderer>();
        return hit;
    }

    public void Pulse(Vector2 direction)
    {
        heat = 1;
        kick = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.up;
    }

    void LateUpdate()
    {
        heat = Mathf.Max(0, heat - Time.deltaTime * 7.5f);
        visual.localPosition = rest + (Vector3)kick * (Mathf.Sin(heat * Mathf.PI) * .055f);
        visual.localScale = scale * (1 + Mathf.Sin(heat * Mathf.PI) * .10f);
        foreach (var mesh in meshes)
            if (mesh != null && mesh.enabled)
            {
                mesh.GetPropertyBlock(ink);
                ink.SetFloat("_HitFlash", heat > .68f ? 1 : heat * .55f);
                mesh.SetPropertyBlock(ink);
            }
    }
}
