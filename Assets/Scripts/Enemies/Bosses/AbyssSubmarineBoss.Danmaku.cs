using System.Collections;
using UnityEngine;

// Abyss submarine: water does the work. Its dive cycle is the spell sequence.
//   Surfaced, alternate dives: Torpedo Salvo (racks of torpedoes that leave almost
//     still and wind up at the player, between mirrored volleys swept outward by
//     opposite currents) or Pressure Lace (a three-arm mirrored lace from the
//     launch channels with aimed arrow wavefronts).
//   Eruption: three dense mirrored plumes of arrows thrown up, falling back under gravity.
//   Charge: the wake hangs beside the hull in arrows, then sinks away diagonally.
public sealed partial class AbyssSubmarineBoss
{
    int plumes;
    protected override bool PatternEmissionAllowed => !Submerged && Mathf.Repeat(cycleClock, cycleSeconds) < 3.5f;
    protected override void Attack()
    {
        if (Submerged || Mathf.Repeat(cycleClock, cycleSeconds) >= 3.5f)
            return;
        bool salvo = lastCycle % 2 == 0;
        if (salvo && volley % 3 == 1)
            Torpedoes();
        else if (salvo && volley % 3 == 2)
            Phrase(Current(volley / 3 % 2 == 0 ? 1 : -1));
        else if (!salvo && volley % 3 == 1)
            Phrase(MirrorLace(Hull, new Vector3(-1.02f, .3f, -.3f), new Vector3(1.02f, .3f, -.3f), Dense(phaseTwo ? 32 : 26), 3, 60, 18, 3.4f, .06f, volley % 2 == 0 ? 1 : -1));
        else if (!salvo && volley % 3 == 2)
            ArrowWavefront(Muzzle(), Aim, Dense(phaseTwo ? 19 : 15), 90, 3, 2.8f, .8f);
    }

    // Big slow torpedoes that wind up to full speed along their aim.
    void Torpedoes()
    {
        int tubes = Dense(phaseTwo ? 5 : 4);
        for (int side = 1; side <= 2; side++)
        {
            var port = Muzzle(side);
            var aim = CombatVisuals.Aim(port);
            for (int i = 0; i < tubes; i++)
                Fire<SimpleRound>(port, CombatVisuals.Rotate(aim, (i - (tubes - 1) * .5f) * 22), .45f, .32f)
                    .Ramp(9, 1.1f, .45f + i * .18f);
        }
    }

    // Mirrored arrow volleys caught by opposite currents, curling outward in arcs.
    IEnumerator Current(int sign)
    {
        int shots = Dense(phaseTwo ? 18 : 14), cycle = lastCycle;
        for (int i = 0; i < shots; i++)
        {
            float lane = i / (shots - 1f) * 2 - 1;
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<NeedleRound>(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, mirror * sign * (12 + lane * 20)), 2.8f, .19f)
                    .Fall(new Vector2(-mirror * sign * 1.5f, 0));
            yield return Wait(Rest(.06f));
            // A dive pauses the phrase; abandon it rather than finish on the next surfacing.
            if (lastCycle != cycle)
                yield break;
        }
    }

    // Each eruption throws a dense mirrored plume upward in three speed layers;
    // it falls back under gravity as nested arcs spreading over the arena.
    void Crown(float offset, float speed)
    {
        EmergenceBursts++;
        plumes++;
        PSXImpactEffects.Burst(transform.position, new Color(.42f, .83f, .94f), true);
        int lanes = Dense(CrownSize - 6 + (phaseTwo ? 2 : 0)) | 1;
        float tilt = plumes % 3 == 1 ? -12 : plumes % 3 == 2 ? 12 : 0;
        for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float lane = i - (lanes - 1) * .5f;
                float angle = tilt + lane * 150f / (lanes - 1) + (layer == 1 ? 75f / (lanes - 1) : 0);
                var port = Hull.TransformPoint(new Vector3(Mathf.Sign(lane) * 1.02f * Mathf.Min(1, Mathf.Abs(lane)), .3f, -.3f));
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.up, angle), speed + .8f + layer * .9f, .19f)
                    .Fall(new Vector2(0, -2.6f));
            }
    }

    // The charge's wake hangs beside the hull, then sinks away diagonally.
    void Wake(Vector3 port, Vector2 direction, int lane, int side)
    {
        if (Difficulty.Easy && lane == 0)
            return;
        Fire<NeedleRound>(port, direction, 2.0f + lane * .2f, .19f)
            .Pause(.45f, .55f + (lane + 1) * .15f, CombatRound.Turn.Down, -side * 18, .8f);
    }

    protected override void PrepareScore(float seconds)
    {
        float t = Mathf.Repeat(cycleClock, cycleSeconds);
        if (t < 3.5f)
        {
            if ((volley + 1) % 3 != 0)
            {
                Cue(Muzzle(), seconds);
                Cue(Muzzle(1), seconds);
                Cue(Muzzle(2), seconds);
            }
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
