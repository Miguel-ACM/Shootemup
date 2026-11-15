using UnityEngine;

// The spell break between boss phases: a short implosion in which the arena dims and a ring
// collapses into the boss as it shrinks, then a rupture with a white wash, pressure fronts (the
// last in the district colour), rotating rays, a spark wave, a shake, a boom and a small punch.
// Toned down from the first overblown version (Miguel, 15 November 2026).
// One shared quad and one pass, with no per-ray objects or lights.
public sealed class PhaseBreak : MonoBehaviour
{
    public const float Implosion = .3f, Duration = 1.8f;
    static Mesh quad;
    static Material surface;
    MaterialPropertyBlock ink;
    MeshRenderer visual;
    Transform hull;
    Vector3 hullScale;
    Color accent;
    float age;
    bool ruptured;

    public static void Warm()
    {
        if (quad == null)
        {
            quad = new Mesh { name = "Spell break / shared arena quad" };
            quad.vertices = new[] { new Vector3(-24, -24), new Vector3(24, -24), new Vector3(24, 24), new Vector3(-24, 24) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateBounds();
        }
        if (surface == null)
            surface = new Material(Resources.Load<Shader>("BossSpellBreak"));
    }

    // hull: the boss model to squeeze and punch; null leaves the model alone (Void's own unfolding).
    public static void Play(Vector3 at, Transform hull = null)
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
        effect.accent = Color.Lerp(LevelProjectilePalette.Accent(LevelRun.Current), Color.white, .2f);
        effect.hull = hull;
        if (hull != null)
            effect.hullScale = hull.localScale;
        effect.Pose(0);
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(1.6f, 18, Implosion * .8f, .2f);
    }

    public void Pose(float seconds)
    {
        ink.SetFloat("_Age", seconds);
        ink.SetColor("_Accent", accent);
        visual.SetPropertyBlock(ink);
    }

    void Rupture()
    {
        ruptured = true;
        var at = transform.position;
        PSXImpactEffects.SpellBreak(at, accent);
        var boom = SoundCueCatalog.Find("bossPhaseBreak");
        if (boom != null)
            SoundManager.PlaySoundPitch(boom.Id, boom.Volume, boom.Pitch);
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(5, 9, .03f, 1);
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || GameRules.playerDisabled)
            return;
        age += Time.deltaTime;
        if (!ruptured && age >= Implosion)
            Rupture();
        PunchHull();
        if (age >= Duration)
        {
            if (hull != null)
                hull.localScale = hullScale;
            Destroy(gameObject);
        }
        else
            Pose(age);
    }

    // Squeezed small during the implosion, thrown out to 1.3x at the rupture, then a damped wobble.
    void PunchHull()
    {
        if (hull == null)
            return;
        float factor;
        if (age < Implosion)
            factor = Mathf.Lerp(1, .9f, Mathf.SmoothStep(0, 1, age / Implosion));
        else
        {
            float e = age - Implosion;
            factor = 1 + .16f * Mathf.Exp(-e * 5) * Mathf.Cos(e * 15);
        }
        hull.localScale = hullScale * factor;
    }

    void OnDestroy()
    {
        if (hull != null && age < Duration)
            hull.localScale = hullScale;
    }
}
