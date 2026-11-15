using UnityEngine;

// THUNDERSTORM: a lightning strike down the whole arena. A faint jagged path flickers ever more
// urgently, then the bolt lands along exactly that path and burns for a moment. Only the live
// bolt hurts, along its drawn segments; the warning never does.
public sealed class LightningStrike : MonoBehaviour
{
    const float Warning = 1.15f, Live = .32f, Fade = .3f;
    const int Points = 16;
    static readonly Color Pale = new Color(1, .86f, .55f), Glow = new Color(1, .7f, .3f);
    static Material light;
    Vector3[] path;
    LineRenderer warning, core, halo;
    float age, delay;
    bool struck;

    public static void Begin(float x, int seed, float delay = 0)
    {
        var strike = new GameObject("Lightning strike").AddComponent<LightningStrike>();
        strike.delay = delay;
        strike.Build(x, seed);
    }

    void Build(float x, int seed)
    {
        var random = new System.Random(seed);
        path = new Vector3[Points];
        float top = Camera.UpLimit + 1, bottom = Camera.DownLimit - 1, drift = x;
        for (int i = 0; i < Points; i++)
        {
            float t = i / (float)(Points - 1);
            if (i > 0)
                drift = Mathf.Clamp(drift + (float)(random.NextDouble() - .5) * .7f, x - .55f, x + .55f);
            path[i] = new Vector3(i == 0 ? x : drift, Mathf.Lerp(top, bottom, t), -1.2f);
        }
        if (light == null)
            light = new Material(Resources.Load<Shader>("HazardLine")) { name = "Lightning light" };
        warning = Line(.035f, new Color(Pale.r, Pale.g, Pale.b, 0), 18);
        halo = Line(.9f, new Color(Glow.r, Glow.g, Glow.b, 0), 19);
        core = Line(.24f, new Color(1, 1, 1, 0), 21);
        halo.sharedMaterial = core.sharedMaterial = light;
        foreach (var line in new[] { warning, halo, core })
        {
            line.positionCount = Points;
            line.SetPositions(path);
        }
    }

    LineRenderer Line(float width, Color color, int order)
    {
        var line = CombatVisuals.Line(transform, color, width);
        line.useWorldSpace = true;
        line.sortingOrder = order;
        line.numCornerVertices = 2;
        return line;
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || !GameClock.ActivePlay)
            return;
        if (delay > 0)
        {
            delay -= Time.deltaTime;
            return;
        }
        age += Time.deltaTime;
        if (age < Warning)
        {
            // The tell quickens and brightens as the strike approaches.
            float t = age / Warning;
            float flicker = Mathf.Sin(age * Mathf.Lerp(18, 60, t)) > 0 ? 1 : .35f;
            var color = Pale;
            color.a = Mathf.Lerp(.18f, .75f, t) * flicker;
            warning.startColor = warning.endColor = color;
            warning.widthMultiplier = Mathf.Lerp(.7f, 1.6f, t);
            return;
        }
        if (!struck)
        {
            struck = true;
            warning.enabled = false;
            DistrictHazards.Play("eventLightning");
            LevelEnvironment.StrikeFlash();
            if (EZCameraShake.CameraShaker.Instance != null)
                EZCameraShake.CameraShaker.Instance.ShakeOnce(2.2f, 14, .02f, .3f);
            PSXImpactEffects.Burst(path[Points - 1] + Vector3.up * 1.2f, Glow, true);
        }
        float live = age - Warning;
        float strength = live < Live ? 1 : 1 - Mathf.Clamp01((live - Live) / Fade);
        float jitter = live < Live && Mathf.Sin(live * 90) > 0 ? 1 : .8f;
        core.startColor = core.endColor = new Color(1, 1, 1, strength * jitter);
        halo.startColor = halo.endColor = new Color(Glow.r, Glow.g, Glow.b, .75f * strength);
        if (live < Live)
            Hurt();
        if (live >= Live + Fade)
            Destroy(gameObject);
    }

    void Hurt()
    {
        var pilot = CombatVisuals.Player;
        if (pilot == null)
            return;
        var cockpit = pilot.GetComponent<Collider2D>();
        float radius = .09f * ProjectilePixelContact.HitboxScale;
        for (int i = 1; i < Points; i++)
            if (BeamContact.Touches(cockpit, path[i - 1], path[i], radius, out _))
            {
                pilot.SendMessage("ReceiveHazardDamage", 1f, SendMessageOptions.DontRequireReceiver);
                return;
            }
    }
}
