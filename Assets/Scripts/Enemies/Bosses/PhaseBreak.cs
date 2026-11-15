using UnityEngine;

// The spell break between boss phases, deliberately overblown: a short implosion in which the
// arena dims and a ring collapses into the boss as it shrinks, then a rupture with a screen-wide
// white wash, five pressure fronts (the late ones in the district colour), rotating rays, two waves
// of sparks, a heavy shake, a deep boom and the boss punching out before it settles.
// One shared quad and one pass, with no per-ray objects or lights.
public sealed class PhaseBreak : MonoBehaviour
{
    public const float Implosion = .3f, Duration = 2.2f;
    static Mesh quad;
    static Material surface;
    MaterialPropertyBlock ink;
    MeshRenderer visual;
    Transform hull;
    Vector3 hullScale;
    Color accent;
    float age;
    bool ruptured, echoed;

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
        PSXImpactEffects.SpellBreak(at);
        ClearEffects.ScreenClear(Color.white);
        var boom = SoundCueCatalog.Find("bossPhaseBreak");
        if (boom != null)
            SoundManager.PlaySoundPitch(boom.Id, boom.Volume, boom.Pitch);
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(9.5f, 9, .03f, 1.4f);
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || GameRules.playerDisabled)
            return;
        age += Time.deltaTime;
        if (!ruptured && age >= Implosion)
            Rupture();
        if (!echoed && age >= Implosion + .14f)
        {
            // A second, coloured spark wave chases the first.
            echoed = true;
            PSXImpactEffects.SpellBreak(transform.position, accent);
        }
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
            factor = Mathf.Lerp(1, .84f, Mathf.SmoothStep(0, 1, age / Implosion));
        else
        {
            float e = age - Implosion;
            factor = 1 + .3f * Mathf.Exp(-e * 4.2f) * Mathf.Cos(e * 15);
        }
        hull.localScale = hullScale * factor;
    }

    void OnDestroy()
    {
        if (hull != null && age < Duration)
            hull.localScale = hullScale;
    }
}
