using UnityEngine;

// A huge slow orb: a bright rim around a dark spiked core. It uses every authored
// behaviour of an ordinary round and slowly turns so the spikes read as alive.
public sealed class GiantOrb : CombatRound
{
    public override float HitboxScale => .125f;
    const float WarningTime = .24f;
    bool sideSpray;
    float sideSpeed, sideCadence, nextRelease, sideWarning, flash;
    int burstLeft, burstFlank;
    uint sprayRandom;

    protected override void Initialize()
    {
        // The ordinary and side-spraying variants share the warmed giant-orb pool.
        sideSpray = false;
        sideSpeed = sideCadence = nextRelease = sideWarning = flash = 0;
        burstLeft = burstFlank = 0;
        sprayRandom = 0;
        // Large glows sit behind the smaller hostile sprites, above the hull.
        visual.sortingOrder = 16;
    }

    internal void EnableSideSpray(float velocity)
    {
        sideSpray = true;
        // Thin the spurts through their cadence, rather than through simultaneous fans.
        sideCadence = Difficulty.Rest(3f / Difficulty.Dense(3));
        burstLeft = 0;
        sideSpeed = velocity;
        sideWarning = WarningTime;
        sprayRandom = (uint)Random.Range(1, int.MaxValue);
        nextRelease = age + Difficulty.Rest(Between(.65f, .95f));
    }

    float Between(float low, float high)
    {
        // Local randomness keeps the irregular releases independent of other rounds.
        sprayRandom ^= sprayRandom << 13;
        sprayRandom ^= sprayRandom >> 17;
        sprayRandom ^= sprayRandom << 5;
        return Mathf.Lerp(low, high, (sprayRandom & 0xFFFFFF) / 16777215f);
    }

    protected override void Tick(float dt)
    {
        Advance(dt);
        transform.rotation = Quaternion.Euler(0, 0, age * 40);
        if (!sideSpray)
            return;

        // Never release unseen side fire, while held, or during a clear/fade.
        var p = transform.position;
        if (!Border.Contains(p) || Paused || fadeAt >= 0 && age >= fadeAt)
        {
            nextRelease = age + WarningTime;
            sideWarning = WarningTime;
            burstLeft = 0;
            flash = 0;
            visual.color = Color.white;
            return;
        }
        // Each spurt swells the orb's glow first; every release flashes it.
        flash = Mathf.Max(0, flash - dt * 9);
        float charge = burstLeft > 0 ? 0 : Mathf.Clamp01(1 - (nextRelease - age) / sideWarning);
        float glow = 1 + Mathf.Max(charge * .4f, flash * .6f);
        visual.color = new Color(glow, glow, glow, 1);
        if (age < nextRelease)
            return;

        // Irregular spurts (Miguel, 15 November 2026): one to five quick shots from one flank,
        // now and then crossing to the other, then a ragged gap. Each shot picks its own angle
        // across the flank (leaning mostly forward), its own speed and its own size.
        if (burstLeft <= 0)
        {
            float roll = Between(0, 1);
            burstLeft = roll < .2f ? 1 : roll < .5f ? 2 : roll < .75f ? 3 : roll < .9f ? 4 : 5;
            burstFlank = Between(0, 1) < .5f ? -1 : 1;
        }
        int flank = Between(0, 1) < .2f ? -burstFlank : burstFlank;
        burstLeft--;
        if (burstLeft > 0)
            nextRelease = age + Between(.025f, .055f);
        else
        {
            float gap = Between(.06f, .22f) * sideCadence;
            nextRelease = age + gap;
            sideWarning = Mathf.Min(.12f, gap * .7f);
        }
        flash = 1;
        if (Live.Count >= Limit)
            return;
        Vector2 side = new Vector2(-heading.y, heading.x) * flank;
        // Positive lean turns the shot toward the orb's own heading, on either flank.
        float lean = Between(-35, 60);
        var direction = CombatVisuals.Rotate(side, -flank * lean);
        float velocity = sideSpeed * Between(.65f, 1.55f);
        float rim = visual.sprite.bounds.extents.x * Mathf.Abs(transform.localScale.x);
        // A quick throw that keeps going: some ease off a little, some pick up, and all of
        // them leave the arena instead of hanging in a slow drift.
        var shard = Launch<SimpleRound>(p + (Vector3)direction * (rim + .07f), direction, velocity, Between(.11f, .18f))
            .Ramp(Between(.7f, 1.3f), Between(.5f, 1.1f), .15f);
        if (shard != null)
        {
            shard.damage = damage;
            shard.generation = generation + 1;
        }
    }
}
