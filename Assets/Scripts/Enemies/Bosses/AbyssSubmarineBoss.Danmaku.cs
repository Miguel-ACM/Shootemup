using System.Collections;
using UnityEngine;

// Abyss submarine: water does the work. Its dive cycle is the spell sequence.
//   Surfaced: it fires from the top of the arena. Phase one alternates torpedo racks
//     (rounds that leave almost still and wind up) with rain: plumes of arrows thrown
//     up that fall back over the arena. Phase two replaces the racks with a three-arm
//     mirrored pressure lace and aimed arrow wavefronts between the rain.
//   Emergence: it bursts out of the water in an explosion of 360 arrow rings, one per
//     crown in phase one and two (zigzagging) in phase two.
//   Charge: it rams straight down the arena, shedding a dense wake of arrows sideways
//     the whole way, never stopping or aiming; foam marks the hull, which hurts on
//     contact. In phase two it also drops depth charges that explode 360.
public sealed partial class AbyssSubmarineBoss
{
    int plumes, emergences;
    protected override bool PatternEmissionAllowed => !Submerged && Mathf.Repeat(cycleClock, cycleSeconds) < 3.5f;
    protected override void Attack()
    {
        if (Submerged || Mathf.Repeat(cycleClock, cycleSeconds) >= 3.5f)
            return;
        int beat = volley % 3;
        if (beat == 0)
            Rain();
        else if (!phaseTwo && beat == 1)
            Torpedoes();
        else if (!phaseTwo)
            Phrase(Current(volley / 3 % 2 == 0 ? 1 : -1));
        else if (beat == 1)
            Phrase(MirrorLace(Hull, new Vector3(-1.02f, .3f, -.3f), new Vector3(1.02f, .3f, -.3f), Dense(30), 3, 60, 18, 3.4f, .06f, volley % 2 == 0 ? 1 : -1));
        else
            ArrowWavefront(Muzzle(), Aim, Dense(19), 90, 3, 2.8f, .8f);
    }

    // Big slow torpedoes that wind up to full speed along their aim.
    void Torpedoes()
    {
        int tubes = Dense(5);
        for (int side = 1; side <= 2; side++)
        {
            var port = Muzzle(side);
            var aim = CombatVisuals.Aim(port);
            for (int i = 0; i < tubes; i++)
                Fire<SimpleRound>(port, CombatVisuals.Rotate(aim, (i - (tubes - 1) * .5f) * 20), .45f, .32f)
                    .Ramp(9, 1.1f, .45f + i * .15f);
        }
    }

    // Mirrored arrow volleys caught by opposite currents, curling outward in arcs.
    IEnumerator Current(int sign)
    {
        int shots = Dense(16), cycle = lastCycle;
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

    // Rain from the surface: a mirrored plume of arrows thrown up in three speed
    // layers, falling back under gravity as nested arcs spreading over the arena.
    void Rain()
    {
        plumes++;
        int lanes = Dense(phaseTwo ? 17 : 15) | 1;
        float tilt = plumes % 3 == 1 ? -12 : plumes % 3 == 2 ? 12 : 0;
        for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float lane = i - (lanes - 1) * .5f;
                float angle = tilt + lane * 150f / (lanes - 1) + (layer == 1 ? 75f / (lanes - 1) : 0);
                var port = Hull.TransformPoint(new Vector3(Mathf.Sign(lane) * 1.02f * Mathf.Min(1, Mathf.Abs(lane)), .3f, -.3f));
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.up, angle), 3.0f + layer * .9f, .19f)
                    .Fall(new Vector2(0, -2.6f));
            }
    }

    // Emergence: an explosion of 360 arrow rings from the hull as it breaks the surface.
    void Crown(float offset, float speed)
    {
        EmergenceBursts++;
        emergences++;
        PSXImpactEffects.Burst(transform.position, new Color(.42f, .83f, .94f), true);
        int beads = Dense(phaseTwo ? 32 : 28);
        int rings = phaseTwo ? 2 : 1;
        for (int ring = 0; ring < rings; ring++)
            for (int i = 0; i < beads; i++)
            {
                var axis = CombatVisuals.Rotate(Vector2.down, offset * beads + (i + ring * .5f) * 360f / beads + emergences * 5);
                var round = Fire<NeedleRound>(transform.position + (Vector3)axis * .8f, axis, speed + .6f + ring * .8f, .19f);
                if (phaseTwo && round != null)
                    round.Zigzag(.4f, 10);
            }
    }

    // The charge sheds a dense sideways wake of arrows on both flanks, never stopping.
    void ChargeWake(int tick)
    {
        var foam = new Color(.42f, .83f, .94f);
        for (int side = -1; side <= 1; side += 2)
        {
            var flank = Hull.TransformPoint(new Vector3(side * 1.02f, .3f, -.3f));
            if (tick % 2 == 0)
                PSXImpactEffects.Vanish(flank, foam, .35f);
            for (int lane = -2; lane <= 2; lane++)
            {
                if (Difficulty.Easy && (lane + tick) % 2 != 0)
                    continue;
                var port = Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .3f, -.3f));
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.right * side, lane * 14 + (tick % 2) * 7), 2.4f + Mathf.Abs(lane) * .3f, .19f);
            }
        }
        if (tick % 3 == 0)
            PSXImpactEffects.Burst(transform.position + Vector3.down * 1.8f * hullScale, foam, false);
        if (phaseTwo && tick % 5 == 0)
        {
            // Depth charges left in the path, exploding all round a moment later.
            int shards = Dense(14);
            Fire<SimpleRound>(transform.position + Vector3.up * 1.5f, Vector2.up, .3f, .4f)
                .SplitAt(1.1f, shards, 360f / shards, 2.6f, .19f);
        }
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
