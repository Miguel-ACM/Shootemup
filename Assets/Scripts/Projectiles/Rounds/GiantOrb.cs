using UnityEngine;

// A huge slow orb: a bright rim around a dark spiked core. It uses every authored
// behaviour of an ordinary round and slowly turns so the spikes read as alive.
public sealed class GiantOrb : CombatRound
{
    const float WarningTime = .24f;
    int sideCount;
    float sideSpeed, nextRelease;
    uint sprayRandom;

    protected override void Initialize()
    {
        // The ordinary and side-spraying variants share the warmed giant-orb pool.
        sideCount = 0;
        sideSpeed = nextRelease = 0;
        sprayRandom = 0;
    }

    internal void EnableSideSpray(float velocity)
    {
        sideCount = Difficulty.Dense(3);
        sideSpeed = velocity;
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
        if (sideCount == 0)
            return;

        // Never release unseen side fire, while held, or during a clear/fade.
        var p = transform.position;
        if (!Border.Contains(p) || Paused || fadeAt >= 0 && age >= fadeAt)
        {
            nextRelease = age + WarningTime;
            visual.color = Color.white;
            return;
        }
        float charge = Mathf.Clamp01(1 - (nextRelease - age) / WarningTime);
        float glow = 1 + charge * .38f;
        visual.color = new Color(glow, glow, glow, 1);
        if (age < nextRelease)
            return;

        nextRelease = age + Difficulty.Rest(Between(.60f, 1.05f));
        visual.color = Color.white;
        // Keep the mirrored burst whole when the arena is already at capacity.
        if (Live.Count + sideCount * 2 > Limit)
            return;
        Vector2 side = new Vector2(-heading.y, heading.x);
        float lean = Between(-22, 22), velocity = sideSpeed * Between(.9f, 1.1f);
        float rim = visual.sprite.bounds.extents.x * Mathf.Abs(transform.localScale.x);
        for (int sign = -1; sign <= 1; sign += 2)
            for (int i = 0; i < sideCount; i++)
            {
                float angle = lean + (i - (sideCount - 1) * .5f) * 10;
                var direction = CombatVisuals.Rotate(side * sign, angle * sign);
                var shard = Launch<SimpleRound>(p + (Vector3)direction * (rim + .07f), direction, velocity, .14f);
                if (shard != null)
                {
                    shard.damage = damage;
                    shard.generation = generation + 1;
                }
            }
    }
}
