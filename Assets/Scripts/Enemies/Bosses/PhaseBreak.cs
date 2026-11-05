using UnityEngine;

// A hard white ignition, a broad pressure front, then tapered rays and delayed rings.
// One shared quad and one pass, with no per-ray objects or lights.
public sealed class PhaseBreak : MonoBehaviour
{
    public const float Duration = 1.35f;
    static Mesh quad;
    static Material surface;
    MaterialPropertyBlock ink;
    MeshRenderer visual;
    float age;

    public static void Warm()
    {
        if (quad == null)
        {
            quad = new Mesh { name = "Spell break / shared arena quad" };
            quad.vertices = new[] { new Vector3(-24,-24), new Vector3(24,-24), new Vector3(24,24), new Vector3(-24,24) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.triangles = new[] { 0,2,1,0,3,2 };
            quad.RecalculateBounds();
        }
        if (surface == null) surface = new Material(Resources.Load<Shader>("BossSpellBreak"));
    }

    public static void Play(Vector3 at)
    {
        Warm();
        var go = new GameObject("White spell rupture");
        go.transform.position = new Vector3(at.x, at.y, -3);
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        var effect = go.AddComponent<PhaseBreak>();
        effect.visual = go.AddComponent<MeshRenderer>();
        effect.visual.sharedMaterial = surface;
        effect.visual.sortingLayerName = "Bullet";
        effect.visual.sortingOrder = 80;
        effect.ink = new MaterialPropertyBlock();
        effect.Pose(0);
        PSXImpactEffects.SpellBreak(go.transform.position);
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(5.2f, 8, .035f, .85f);
    }

    public void Pose(float seconds)
    {
        ink.SetFloat("_Age", seconds);
        visual.SetPropertyBlock(ink);
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || GameRules.playerDisabled) return;
        age += Time.deltaTime;
        if (age >= Duration) Destroy(gameObject);
        else Pose(age);
    }
}
