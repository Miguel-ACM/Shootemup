using UnityEngine;

// Two clearly different languages for bullet clears. A full-screen clear flashes the whole
// arena and sweeps a bright band up the screen (drawn by the PSX composite, so it costs nothing
// per bullet); a radius clear is a ring expanding from its source. Blank Bullet has its own burst.
public static class ClearEffects
{
    const float FlashSeconds = .22f, SweepSeconds = .5f;
    static float startedAt = -10;
    static Color tint = Color.white;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        startedAt = -10;
        tint = Color.white;
    }

    /// <summary>Screen flash strength, 1 at the clear and fading to 0.</summary>
    public static float Flash => Mathf.Clamp01(1 - (Time.unscaledTime - startedAt) / FlashSeconds);

    /// <summary>The sweep band's height in screen UV (it runs past the top), or -1 when idle.</summary>
    public static float Sweep
    {
        get
        {
            float t = (Time.unscaledTime - startedAt) / SweepSeconds;
            return t >= 0 && t <= 1 ? Mathf.Lerp(-.1f, 1.2f, 1 - (1 - t) * (1 - t)) : -1;
        }
    }

    public static Color Tint => tint;

    /// <summary>Every hostile round on screen is gone: white flash and a sweep up the arena.</summary>
    public static void ScreenClear(Color? colour = null)
    {
        startedAt = Time.unscaledTime;
        tint = colour ?? Color.white;
    }

    /// <summary>Rounds within a radius are gone: a ring expands from the source.</summary>
    public static void RadiusClear(Vector3 position, float radius, Color colour, float life = .5f)
    {
        PSXFireballs.Shockwave(position, radius, life, colour, 2, .12f);
    }

    /// <summary>Blank Bullet's own cartridge burst: a gunpowder flash and rotating spokes that hold its area.</summary>
    public static void Blank(Vector3 position, float radius, float seconds)
    {
        var go = new GameObject("Blank bullet burst");
        go.transform.position = position;
        go.AddComponent<BlankBurst>().Begin(radius, seconds);
    }
}

public sealed class BlankBurst : MonoBehaviour
{
    const int Spokes = 12, Notches = 24;
    static readonly Color Powder = new Color(1f, .9f, .55f);
    LineRenderer[] spokes, notches;
    float radius, seconds, age;

    public void Begin(float reach, float duration)
    {
        radius = reach;
        seconds = duration;
        spokes = new LineRenderer[Spokes];
        notches = new LineRenderer[Notches];
        for (int i = 0; i < Spokes; i++)
            spokes[i] = CombatVisuals.Line(transform, Powder, .06f);
        for (int i = 0; i < Notches; i++)
            notches[i] = CombatVisuals.Line(transform, Powder, .05f);
        PSXImpactEffects.Burst(transform.position, Powder, true);
        PSXFireballs.Spawn(transform.position, .9f, 3, .15f, .05f, Powder);
        Draw();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= seconds)
        {
            Destroy(gameObject);
            return;
        }
        Draw();
    }

    void Draw()
    {
        // A fast punch outward, then the burst holds its area as a turning cartridge rim.
        float grow = 1 - Mathf.Pow(1 - Mathf.Clamp01(age / .18f), 3);
        float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - seconds * .6f) / (seconds * .4f)));
        float reach = radius * grow, turn = age * 40 * Mathf.Deg2Rad;
        Vector3 p = transform.position + Vector3.back * .15f;
        for (int i = 0; i < Spokes; i++)
        {
            float a = turn + i * Mathf.PI * 2 / Spokes;
            var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
            float inner = Mathf.Lerp(.2f, .78f, grow) * reach;
            spokes[i].SetPosition(0, p + d * inner);
            spokes[i].SetPosition(1, p + d * reach * .92f);
            spokes[i].startColor = new Color(1, 1, 1, .9f * fade);
            spokes[i].endColor = new Color(Powder.r, Powder.g, Powder.b, .25f * fade);
        }
        for (int i = 0; i < Notches; i++)
        {
            float a = -turn * .5f + i * Mathf.PI * 2 / Notches;
            float half = Mathf.PI / Notches * .55f;
            notches[i].SetPosition(0, p + new Vector3(Mathf.Cos(a - half), Mathf.Sin(a - half), 0) * reach);
            notches[i].SetPosition(1, p + new Vector3(Mathf.Cos(a + half), Mathf.Sin(a + half), 0) * reach);
            notches[i].startColor = notches[i].endColor = new Color(Powder.r, Powder.g, Powder.b, .75f * fade);
        }
    }
}
