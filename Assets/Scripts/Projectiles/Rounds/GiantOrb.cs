using UnityEngine;

// A huge slow orb: a bright rim around a dark spiked core. It uses every authored
// behaviour of an ordinary round and slowly turns so the spikes read as alive.
public sealed class GiantOrb : CombatRound
{
    public override float HitboxScale => .25f;
    const float WarningTime = .24f;
    bool sideSpray;
    float sideSpeed, sideCadence, nextRelease, sideWarning;
    uint sprayRandom;

    protected override void Initialize()
    {
        // The ordinary and side-spraying variants share the warmed giant-orb pool.
        sideSpray = false;
        sideSpeed = sideCadence = nextRelease = sideWarning = 0;
        sprayRandom = 0;
        // Large glows sit behind the smaller hostile sprites, above the hull.
        visual.sortingOrder = 16;
    }

    internal void EnableSideSpray(float velocity)
    {
        sideSpray = true;
        // Thin the stream through its cadence, rather than simultaneous fans.
        sideCadence = Difficulty.Rest(3f / Difficulty.Dense(3));
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
            visual.color = Color.white;
            return;
        }
        float charge = Mathf.Clamp01(1 - (nextRelease - age) / sideWarning);
        float glow = 1 + charge * .38f;
        visual.color = new Color(glow, glow, glow, 1);
        if (age < nextRelease)
            return;

        float interval = Between(.04f, .07f) * sideCadence;
        nextRelease = age + interval;
        sideWarning = Mathf.Min(.035f, interval * .6f);
        visual.color = Color.white;
        if (Live.Count >= Limit)
            return;
        Vector2 side = new Vector2(-heading.y, heading.x);
        int flank = Between(0, 1) < .5f ? -1 : 1;
        float lean = Between(-9, 9), velocity = sideSpeed * Between(.9f, 1.1f);
        float rim = visual.sprite.bounds.extents.x * Mathf.Abs(transform.localScale.x);
        var direction = CombatVisuals.Rotate(side * flank, lean);
        // A short sideways push settles into a slow drift, then clears before
        // repeated carriers can leave stationary walls across the arena.
        var shard = Launch<SimpleRound>(p + (Vector3)direction * (rim + .07f), direction, velocity, .14f)
            .Ramp(.3f, 1.25f).FadeAt(5f);
        if (shard != null)
        {
            shard.damage = damage;
            shard.generation = generation + 1;
        }
    }
}
