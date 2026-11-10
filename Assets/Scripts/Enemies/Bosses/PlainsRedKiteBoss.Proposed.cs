using System;
using System.Collections;
using UnityEngine;

// Proposed card book for the Red Kite (ArtSource/BossPhaseProposals). It flies only while
// DistrictBoss.ProposedCards is set, so it can be recorded beside Cards().
// Phase one
//   Kite Tail: the kite sweeps across the upper arena trailing a tail from both wing guns,
//     five strands of dark beads per gun with a bright petal bow every few beads. The tail hangs
//     along the flight path, then lets go while the kite writes the next one: the strands
//     fan outward at their own speeds and sway, unfurling like a kite's streamers. A bright
//     kite string of arrows runs from the nose to the pilot all the while.
//   Stoop: the kite takes station over the pilot's column with a visible charge, then
//     drops in a hooked dive. Both wing guns shed bright feathers outward the whole way
//     down and back up, so the dive leaves a widening wake, and the nose snaps a dark
//     forked chevron volley at the bottom.
// Phase two
//   Sky Lane: the kite holds high and swings a lane of open air like a searchlight. Each
//     wing gun throws a dark fringe of arrows on its own side of the lane, which swings as
//     a pendulum while the kite drifts; bright beads roll down its middle and an aimed
//     talon answers pilots who leave it for a corner.
//   Scissor Kite: two crossing dives plant an X of hanging bright arrows from both wing
//     guns. When the X is complete it opens like shears: every arrow leaves its line square
//     to it, the two wings' blades peel apart in opposite directions and each splits into
//     three ranks by speed. Dark nose payloads open flat along each dive.
// Phase three (Hard)
//   Red Sky: a tail is written high and keeps hanging through a stoop, then lets go as
//     the kite climbs back out.
// Tones: dark tail, bright stoop, dark lane, bright scissors; accents in the other tone.
public sealed partial class PlainsRedKiteBoss
{
    protected override IEnumerator Proposed()
    {
        int side = transform.position.x > 0 ? -1 : 1;
        int dive = 0;
        while (true)
        {
            if (phaseThree)
            {
                yield return RedSky(side, dive++);
                side = -side;
            }
            else if (!phaseTwo)
            {
                yield return KiteTail(side);
                side = transform.position.x > 0 ? -1 : 1;
                yield return Wait(Rest(.6f));
                yield return Stoop();
                yield return Wait(Rest(.6f));
            }
            else
            {
                yield return SkyLane(side);
                side = -side;
                yield return Wait(Rest(.3f));
                yield return ScissorKite();
                side = transform.position.x > 0 ? -1 : 1;
                yield return Wait(Rest(.3f));
            }
        }
    }

    // Flies to `end` over `duration`, bowing `depth` below the straight line, and calls
    // `beat` with the progress and flight direction at `beats` evenly spaced moments.
    IEnumerator Glide(Vector3 end, float duration, float depth, int beats, Action<float, Vector2, int> beat)
    {
        Vector3 start = transform.position;
        end.z = start.z;
        int emitted = 0;
        for (float t = 0; t < 1;)
        {
            if (!Holding)
            {
                t = Mathf.Min(1, t + Time.deltaTime * LevelRun.EnemyTempoScale * MovementScale / duration);
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t)) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * depth);
                while (beat != null && emitted < beats && t >= (emitted + .5f) / beats)
                {
                    float at = (emitted + .5f) / beats;
                    Vector2 velocity = (Vector2)(end - start) * (6 * at * (1 - at)) + Vector2.down * (Mathf.Cos(at * Mathf.PI) * Mathf.PI * depth);
                    beat(at, velocity.sqrMagnitude > .0001f ? velocity.normalized : Vector2.down, emitted);
                    emitted++;
                }
            }
            yield return null;
        }
    }

    // Kite Tail: three passes, high and low in turn; each tail falls during the next pass.
    IEnumerator KiteTail(int side)
    {
        for (int pass = 0; pass < 3; pass++)
        {
            yield return PrimePair(.25f);
            float height = Camera.UpLimit - (pass % 2 == 0 ? 2.1f : 1.4f);
            Phrase(KiteString(2.0f));
            yield return TailPass(new Vector3(Lane(side), height, 0), 2.0f, .55f, Mathf.Max(3, Dense(5)));
            side = -side;
        }
    }

    // One pass of the tail. Each beat both wing guns leave a few beads that peel back and
    // up from the flight line and brake almost at once, so the tail hangs where the kite
    // flew. All of it lets go `linger` seconds after the pass: each strand falls outward at
    // its own angle and speed and sways, and the bright forked talon strikes at the pilot.
    IEnumerator TailPass(Vector3 end, float duration, float linger, int strands)
    {
        int beats = Mathf.Max(5, Dense(18));
        float release = duration / MovementScale + linger;
        Phrase(TalonAfter(release));
        yield return Glide(end, duration, .7f, beats, (t, tangent, beat) =>
        {
            float written = t * duration / MovementScale;
            for (int wing = 1; wing <= 2; wing++)
            {
                int mirror = wing == 1 ? -1 : 1;
                for (int strand = 0; strand < strands; strand++)
                {
                    bool bow = strand == 1 && beat % 4 == 2;
                    UseTone(bow ? HostileTone.Bright : HostileTone.Dark);
                    Vector2 back = CombatVisuals.Rotate((-tangent + Vector2.up * .8f).normalized, mirror * strand * 9);
                    float speed = 1.0f + strand * .35f;
                    float fall = 1.7f + strand * .45f;
                    var bead = bow
                        ? (CombatRound)Fire<PetalRound>(Muzzle(wing), back, speed, .26f)
                        : Fire<SimpleRound>(Muzzle(wing), back, speed, .21f);
                    bead.Pause(.35f, Mathf.Max(.1f, release - written - .35f), CombatRound.Turn.Down, mirror * (6 + strand * 10), fall / speed)
                        .Sway(.3f, .45f)
                        .LivesFor(12);
                }
            }
        });
    }

    // The kite string: bright arrows from the nose to the pilot, with two loose flanks.
    IEnumerator KiteString(float seconds)
    {
        int flanks = Dense(2) / 2;
        return Every(seconds, .12f, n =>
        {
            UseTone(HostileTone.Bright);
            var aim = CombatVisuals.Aim(Muzzle());
            Fire<NeedleRound>(Muzzle(), aim, 4.4f, .17f);
            if (flanks > 0 || n % 2 == 0)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(aim, mirror * 14), 3.8f, .17f);
        });
    }

    // A bright forked talon at the moment a tail lets go: five arrows, the fork's tines
    // trailing the middle one.
    IEnumerator TalonAfter(float seconds)
    {
        yield return Wait(seconds);
        UseTone(HostileTone.Bright);
        var aim = CombatVisuals.Aim(Muzzle());
        for (int k = -2; k <= 2; k++)
            Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(aim, k * 8), 4.6f - Mathf.Abs(k) * .35f, .19f);
    }

    // Stoop: three dives, the hook alternating sides.
    IEnumerator Stoop()
    {
        for (int dive = 0; dive < 3; dive++)
        {
            yield return StoopDive(dive, Dense(24), 4, Dense(5));
            yield return Wait(Rest(.15f));
        }
    }

    // Take station over the pilot, charge the nose, then a U-shaped dive that hooks off to
    // one side. Each beat both wing guns shed four bright feathers fanned from down-outward
    // to up-outward at layered speeds, all quickening; the dark chevrons leave at the bottom.
    IEnumerator StoopDive(int dive, int beats, int feathers, int chevronRows)
    {
        var pilot = CombatVisuals.Player;
        float x = Mathf.Clamp(pilot != null ? pilot.position.x : 0, Lane(-1), Lane(1));
        float high = Camera.UpLimit - 2.0f;
        yield return Glide(new Vector3(x, high, 0), .55f, 0, 0, null);
        yield return Charge(Hull, WeaponPort(0), .45f);
        float pilotY = pilot != null ? pilot.position.y : Camera.DownLimit;
        float bottom = Mathf.Clamp(pilotY + 3.4f, Camera.UpLimit - 7.5f, high - 1f);
        float hook = Mathf.Clamp(x + (dive % 2 == 0 ? 1.8f : -1.8f), Lane(-1), Lane(1));
        beats = Mathf.Max(6, beats);
        yield return Glide(new Vector3(hook, high, 0), 1.3f, high - bottom, beats, (t, tangent, beat) =>
        {
            UseTone(HostileTone.Bright);
            for (int wing = 1; wing <= 2; wing++)
            {
                int mirror = wing == 1 ? -1 : 1;
                for (int k = 0; k < feathers; k++)
                    Fire<NeedleRound>(Muzzle(wing), CombatVisuals.Rotate(Vector2.down, mirror * (64 + k * 20)), 1.5f + k * .45f, .17f)
                        .Ramp(1.9f, .8f, .35f);
            }
            if (beat == beats / 2)
                Phrase(Toned(HostileTone.Dark, Chevrons(Hull, WeaponPort(0), chevronRows, 3.6f, .12f)));
        });
    }

    // Sky Lane: take a high station on one side, then drift across while the lane swings.
    IEnumerator SkyLane(int side)
    {
        float high = Camera.UpLimit - 1.9f;
        yield return Glide(new Vector3(Lane(-side * .4f), high, 0), .6f, 0, 0, null);
        yield return PrimePair(.5f);
        Phrase(Glide(new Vector3(Lane(side * .4f), high, 0), 6.2f, .45f, 0, null));
        int fringe = Mathf.Max(3, Dense(8));
        yield return Every(6f, .13f, n =>
        {
            // The lane's heading, in degrees from straight down: a slow pendulum.
            float axis = 38 * Mathf.Sin(n * .13f * 1.15f);
            UseTone(HostileTone.Dark);
            for (int wing = 1; wing <= 2; wing++)
            {
                int mirror = wing == 1 ? -1 : 1;
                for (int k = 0; k < fringe; k++)
                    Fire<NeedleRound>(Muzzle(wing), CombatVisuals.Rotate(Vector2.down, axis + mirror * k * 13), 3.8f - k * .2f, .17f);
            }
            UseTone(HostileTone.Bright);
            if (n % 2 == 0)
                Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, axis), 2.2f, .24f);
            if (n % 6 == 4)
            {
                var aim = CombatVisuals.Aim(Muzzle());
                for (int k = -1; k <= 1; k++)
                    Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(aim, k * 7), 4.2f, .17f);
            }
        });
    }

    // Scissor Kite: climb to a top corner, dive across to the far low point planting the
    // first blade, climb straight up, dive back planting the second, climb, and open both.
    IEnumerator ScissorKite()
    {
        int side = transform.position.x > 0 ? 1 : -1;
        float high = Camera.UpLimit - 2.0f, low = Camera.UpLimit - 7.0f;
        yield return Glide(new Vector3(Lane(side), high, 0), .6f, 0, 0, null);
        yield return PrimePair(.4f);
        const float DiveTime = 1.1f, ClimbTime = .6f;
        float scale = 1 / MovementScale;
        float release = (DiveTime * 2 + ClimbTime * 2) * scale + .35f;
        float clock = 0;
        Phrase(KiteString(release - .35f));
        for (int blade = 0; blade < 2; blade++)
        {
            int from = blade == 0 ? side : -side;
            Vector2 path = new Vector2(Lane(-from) - Lane(from), low - high).normalized;
            float bladeStart = clock;
            int beats = Mathf.Max(6, Dense(14));
            yield return Glide(new Vector3(Lane(-from), low, 0), DiveTime, 0, beats, (t, tangent, beat) =>
            {
                float written = bladeStart + t * DiveTime * scale;
                PlantBlade(path, release - written);
                if (beat % 2 == 1)
                    FlatPayload();
            });
            clock += DiveTime * scale;
            Phrase(TalonAfter(.2f));
            yield return Glide(new Vector3(Lane(-from), high, 0), ClimbTime, 0, 0, null);
            clock += ClimbTime * scale;
        }
        yield return Wait(.35f);
    }

    // Each wing gun plants three bright arrows on one spot, aimed square off the dive line on
    // its own side; they brake at once and wait. On release they leave at three speeds.
    void PlantBlade(Vector2 path, float wait)
    {
        Vector2 across = new Vector2(-path.y, path.x);
        UseTone(HostileTone.Bright);
        for (int wing = 1; wing <= 2; wing++)
        {
            Vector2 outward = (wing == 1) == (across.x < 0) ? across : -across;
            for (int rank = 0; rank < 3; rank++)
                Fire<NeedleRound>(Muzzle(wing), outward, 2.0f, .18f)
                    .Pause(.08f, Mathf.Max(.1f, wait - .08f), CombatRound.Turn.Keep, 0, .85f + rank * .35f);
        }
    }

    // A dark nose payload that drops behind the dive, brakes and opens in a flat fan.
    void FlatPayload()
    {
        UseTone(HostileTone.Dark);
        Fire<SimpleRound>(Muzzle(), Vector2.down, 2.6f, .32f)
            .Pause(.45f, .25f)
            .SplitAt(.72f, Dense(15), 10, 2.6f, .16f);
    }

    // Red Sky: a high tail hangs through a full stoop and lets go as the kite climbs out.
    IEnumerator RedSky(int side, int dive)
    {
        yield return PrimePair(.3f);
        Phrase(KiteString(4.3f));
        yield return TailPass(new Vector3(Lane(side), Camera.UpLimit - 1.5f, 0), 1.8f, 2.1f, Mathf.Max(3, Dense(6)));
        yield return StoopDive(dive, Dense(28), 6, Dense(6));
        yield return Wait(Rest(.1f));
    }
}
