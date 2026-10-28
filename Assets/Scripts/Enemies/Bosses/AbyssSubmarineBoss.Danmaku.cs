using System.Collections;
using UnityEngine;

// Abyss submarine: water does the work. Torpedoes leave the tubes almost
// still and then accelerate at the player; volleys are caught by a current and
// swept sideways in arcs; each eruption throws a plume that collapses outward
// under its own weight; the charge leaves a wake that hangs, then sinks away.
public sealed partial class AbyssSubmarineBoss
{
    int plumes;
    protected override bool PatternEmissionAllowed => !Submerged && Mathf.Repeat(cycleClock, cycleSeconds) < 3.5f;
    protected override void Attack()
    {
        if (Submerged || Mathf.Repeat(cycleClock, cycleSeconds) >= 3.5f)
            return;
        if (volley % 4 == 1)
            Torpedoes();
        else if (volley % 4 == 2)
            Phrase(Current(volley / 4 % 2 == 0 ? 1 : -1));
    }

    // Big slow torpedoes that wind up to full speed along their aim.
    void Torpedoes()
    {
        int tubes = Dense(phaseTwo ? 3 : 2);
        for (int side = 1; side <= 2; side++)
        {
            var port = Muzzle(side);
            var aim = CombatVisuals.Aim(port);
            for (int i = 0; i < tubes; i++)
                Fire<SimpleRound>(port, CombatVisuals.Rotate(aim, (i - (tubes - 1) * .5f) * 22), .45f, .32f)
                    .Ramp(9, 1.1f, .45f + i * .18f);
        }
    }

    // A volley caught by a sideways current, bending into a sweeping arc.
    IEnumerator Current(int sign)
    {
        int shots = Dense(phaseTwo ? 14 : 10), cycle = lastCycle;
        for (int i = 0; i < shots; i++)
        {
            float lane = i / (shots - 1f) * 2 - 1;
            Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, -sign * (10 + lane * 22)), 2.6f, .2f)
                .Fall(new Vector2(sign * 1.5f, 0));
            yield return Wait(Rest(.07f));
            // A dive pauses the phrase; abandon it rather than finish on the next surfacing.
            if (lastCycle != cycle)
                yield break;
        }
    }

    // Each eruption throws a plume upward that falls back outward over the arena.
    void Crown(float offset, float speed)
    {
        EmergenceBursts++;
        plumes++;
        PSXImpactEffects.Burst(transform.position, new Color(.42f, .83f, .94f), true);
        int count = Dense(CrownSize * 3 / 5);
        float tilt = plumes % 3 == 1 ? -18 : plumes % 3 == 2 ? 18 : 0;
        for (int i = 0; i < count; i++)
        {
            int side = i % 2 == 0 ? -1 : 1, lane = (i / 2) % 3 - 1;
            var port = Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .35f, -.3f));
            float angle = tilt + (i - (count - 1) * .5f) * 110f / (count - 1);
            Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.up, angle), speed + Random.Range(.6f, 1.8f), .22f)
                .Fall(new Vector2(0, -2.5f));
        }
    }

    // The charge's wake hangs beside the hull, then sinks away diagonally.
    void Wake(Vector3 port, Vector2 direction, int lane, int side)
    {
        if (Difficulty.Easy && lane == 0)
            return;
        Fire<SimpleRound>(port, direction, 2.0f + lane * .2f, .19f)
            .Pause(.45f, .55f + (lane + 1) * .15f, CombatRound.Turn.Down, -side * 18, .8f);
    }

    protected override void PrepareScore(float seconds)
    {
        float t = Mathf.Repeat(cycleClock, cycleSeconds);
        if (t < 3.5f)
        {
            if ((volley + 1) % 4 == 1)
            {
                Cue(Muzzle(1), seconds);
                Cue(Muzzle(2), seconds);
            }
            else if ((volley + 1) % 4 == 2)
                Cue(Muzzle(), seconds);
        }
        else if (t >= 5.9f && t < 7.7f || t >= 10.85f && t < 13.1f)
        {
            float nextCrown = t < 6.4f ? 6.4f : t < 7.05f ? 7.05f : 7.7f;
            float warning = t < 7.7f ? (nextCrown - t) / (phaseTwo ? 1.1f : 1) : (11.35f - t) / (phaseTwo ? 1.1f : 1);
            if (t >= 11.35f)
                warning = Mathf.Max(0, (nextWake - age) / LevelRun.EnemyTempoScale);
            for (int side = -1; side <= 1; side += 2)
                for (int lane = -1; lane <= 1; lane++)
                    Cue(Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .35f, -.3f)), warning);
        }
    }
}
