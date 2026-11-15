using UnityEngine;

// SPEAR RAIN: a celestial spear hurled straight down at very high speed. Like Heaven's lasers it is
// announced first: a gold column draws in from the top and narrows to a bright line, then the spear
// falls along it. The spear is an ordinary pooled needle round, so clears and absorption apply.
public sealed class SpearStrike : MonoBehaviour
{
    const float Warning = 1.05f;
    LineRenderer column, line, tip;
    float x, age, delay;
    Color gold;

    public static void Begin(float x, float delay = 0)
    {
        var strike = new GameObject("Spear strike").AddComponent<SpearStrike>();
        strike.x = x;
        strike.delay = delay;
        strike.Build();
    }

    void Build()
    {
        gold = Color.Lerp(LevelProjectilePalette.Accent(LevelId.Heaven), Color.white, .2f);
        float top = Camera.UpLimit + .4f, bottom = Camera.DownLimit - .4f;
        column = Line(.5f, 17, new Vector3(x, top, -1.1f), new Vector3(x, bottom, -1.1f));
        line = Line(.03f, 18, new Vector3(x, top, -1.15f), new Vector3(x, bottom, -1.15f));
        // A small chevron at the top edge marks where the spear will enter.
        tip = Line(.06f, 19, new Vector3(x - .22f, Camera.UpLimit - .25f, -1.2f), new Vector3(x, Camera.UpLimit - .55f, -1.2f));
        tip.positionCount = 3;
        tip.SetPosition(2, new Vector3(x + .22f, Camera.UpLimit - .25f, -1.2f));
        foreach (var l in new[] { column, line, tip })
            l.startColor = l.endColor = Color.clear;
    }

    LineRenderer Line(float width, int order, Vector3 a, Vector3 b)
    {
        var l = CombatVisuals.Line(transform, gold, width);
        l.useWorldSpace = true;
        l.sortingOrder = order;
        l.SetPosition(0, a);
        l.SetPosition(1, b);
        return l;
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
        float t = Mathf.Clamp01(age / Warning);
        // The column gathers into a line, as a laser's charge does.
        float pulse = .75f + .25f * Mathf.Sin(age * Mathf.Lerp(10, 34, t));
        column.widthMultiplier = Mathf.Lerp(1.6f, .25f, t);
        column.startColor = column.endColor = new Color(gold.r, gold.g, gold.b, Mathf.Lerp(.05f, .18f, t));
        line.startColor = line.endColor = new Color(gold.r, gold.g, gold.b, Mathf.Lerp(.2f, .9f, t) * pulse);
        tip.startColor = tip.endColor = new Color(1, 1, 1, Mathf.Lerp(.3f, 1, t) * pulse);
        if (age < Warning)
            return;
        CombatRound.LaunchTone = HostileTone.Bright;
        var spear = CombatRound.Launch<NeedleRound>(new Vector3(x, Camera.UpLimit + .8f, 0), Vector2.down, 26, .2f);
        if (spear != null)
            spear.transform.localScale *= 1.7f;
        DistrictHazards.Play("eventSpear");
        Destroy(gameObject);
    }
}
