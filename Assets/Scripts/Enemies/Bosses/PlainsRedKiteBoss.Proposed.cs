using System;
using System.Collections;
using UnityEngine;

// Proposed card book for the Red Kite (ArtSource/BossPhaseProposals). It flies only while
// DistrictBoss.ProposedCards is set, so it can be recorded beside Cards(). One idea per
// phase, developing over the phase rather than alternating with another card.
// Phase one, Kite Tail: the kite sweeps across the upper arena trailing a tail from both
//   wing guns, five strands of dark beads per gun with a bright petal bow every few beads.
//   The tail hangs along the flight path and lets go while the next one is written. Pass by
//   pass the letting-go changes: all at once fanning out, a whip that follows the kite,
//   all at once crossing inward, a crossing whip; the second half of the cycle flies arched
//   passes. A bright kite string runs from the nose to the pilot, with a forked talon at
//   every release.
// Phase two, Scissor Kite: dives plant hanging bright arrows from both wing guns along the
//   kite's path; when the figure is complete it opens like shears, every arrow leaving square
//   to its line in three ranks by speed. The figure develops: an X that opens, a V, an X that
//   closes through itself, a W. Dark nose payloads open flat along every dive.
// Phase three (Hard), Hunting Stoop: the kite hunts the pilot's column in stoop after stoop,
//   hooking left and right, shedding a bright feather wake that thickens over the first dives;
//   every third stoop doubles back for a second dive. Dark chevrons at each bottom.
// Tones: dark tail and payloads, bright string, blades and feathers.
public sealed partial class PlainsRedKiteBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return HuntingStoop();
            else if (!phaseTwo)
                yield return KiteTail(transform.position.x > 0 ? -1 : 1);
            else
                yield return ScissorKite();
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

    // How a tail lets go: all together or as a whip, its strands fanning out or crossing in.
    enum TailRelease
    {
        Together, Whip, TogetherInward, WhipInward
    }

    // Kite Tail: eight passes, high and low in turn, each letting go a different way;
    // passes five to eight arch upward instead of dipping.
    IEnumerator KiteTail(int side)
    {
        for (int pass = 0; pass < 8; pass++)
        {
            yield return PrimePair(.25f);
            float height = Camera.UpLimit - (pass % 2 == 0 ? 2.1f : 1.4f);
            Phrase(KiteString(2.0f));
            yield return TailPass(new Vector3(Lane(side), height, 0), 2.0f, pass < 4 ? .7f : -.35f, .55f,
                Mathf.Max(3, Dense(5)), (TailRelease)(pass % 4));
            side = -side;
        }
    }

    // One pass of the tail. Each beat both wing guns leave a few beads that peel back and up
    // from the flight line and brake almost at once, so the tail hangs where the kite flew.
    // Together, it all lets go `linger` after the pass; as a whip, each bead lets go a fixed
    // time after it was written. Every strand falls at its own angle and speed and sways.
    IEnumerator TailPass(Vector3 end, float duration, float depth, float linger, int strands, TailRelease release)
    {
        int beats = Mathf.Max(5, Dense(18));
        bool whip = release == TailRelease.Whip || release == TailRelease.WhipInward;
        bool inward = release == TailRelease.TogetherInward || release == TailRelease.WhipInward;
        float together = duration / MovementScale + linger;
        Phrase(TalonAfter(whip ? together + .5f : together));
        yield return Glide(end, duration, depth, beats, (t, tangent, beat) =>
        {
            float written = t * duration / MovementScale;
            float letGo = whip ? written + linger + .5f : together;
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
                    float lean = inward ? -mirror * (4 + strand * 7) : mirror * (6 + strand * 10);
                    var bead = bow
                        ? (CombatRound)Fire<PetalRound>(Muzzle(wing), back, speed, .26f)
                        : Fire<SimpleRound>(Muzzle(wing), back, speed, .21f);
                    bead.Pause(.35f, Mathf.Max(.1f, letGo - written - .35f), CombatRound.Turn.Down, lean, fall / speed)
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

    // Scissor Kite: the four figures in turn.
    IEnumerator ScissorKite()
    {
        for (int figure = 0; figure < 4; figure++)
            yield return Shears(figure);
    }

    // One figure. The kite climbs to its first point and charges, then flies the route,
    // planting along the legs marked to plant; climbs between legs plant nothing. When the
    // route is done every planted arrow lets go together.
    IEnumerator Shears(int figure)
    {
        int side = transform.position.x > 0 ? 1 : -1;
        float high = Camera.UpLimit - 2.0f, low = Camera.UpLimit - 7.0f;
        Vector2[] route;
        bool[] plants;
        switch (figure % 4)
        {
            case 1:
                // V: down to the centre and straight back up the other diagonal.
                route = new[] { new Vector2(Lane(side), high), new Vector2(0, low), new Vector2(Lane(-side), high) };
                plants = new[] { true, true };
                break;
            case 3:
                // W: down, up, down; the last climb plants nothing.
                route = new[] { new Vector2(Lane(side), high), new Vector2(Lane(side / 3f), low), new Vector2(Lane(-side / 3f), high),
                    new Vector2(Lane(-side), low), new Vector2(Lane(-side), high) };
                plants = new[] { true, true, true, false };
                break;
            default:
                // X: two crossing dives with a straight climb after each.
                route = new[] { new Vector2(Lane(side), high), new Vector2(Lane(-side), low), new Vector2(Lane(-side), high),
                    new Vector2(Lane(side), low), new Vector2(Lane(side), high) };
                plants = new[] { true, false, true, false };
                break;
        }
        // The second X closes: its blades cross back through each other.
        bool closing = figure % 4 == 2;
        yield return Glide(route[0], .6f, 0, 0, null);
        yield return PrimePair(.4f);
        float scale = 1 / MovementScale;
        float flight = 0;
        for (int leg = 0; leg < plants.Length; leg++)
            flight += LegTime(route[leg], route[leg + 1]);
        float release = flight * scale + .35f;
        Phrase(KiteString(flight * scale));
        float clock = 0;
        for (int leg = 0; leg < plants.Length; leg++)
        {
            Vector2 from = route[leg], to = route[leg + 1];
            float time = LegTime(from, to);
            if (plants[leg])
            {
                Vector2 path = (to - from).normalized;
                float legStart = clock;
                int beats = Mathf.Max(4, Mathf.RoundToInt(Dense(14) * Vector2.Distance(from, to) / 7.8f));
                yield return Glide(to, time, 0, beats, (t, tangent, beat) =>
                {
                    PlantBlade(path, release - legStart - t * time * scale, closing);
                    if (beat % 2 == 1)
                        FlatPayload();
                });
                Phrase(TalonAfter(.2f));
            }
            else
                yield return Glide(to, time, 0, 0, null);
            clock += time * scale;
        }
        yield return Wait(.35f);
    }

    // Diving legs run at about seven units a second; straight climbs a little slower.
    static float LegTime(Vector2 from, Vector2 to) => Mathf.Abs(from.x - to.x) < .1f ? .6f : .14f * Vector2.Distance(from, to);

    // Each wing gun plants three bright arrows on one spot, aimed square off the line on its
    // own side (or, closing, toward the other side); they brake at once and wait. On release
    // they leave at three speeds.
    void PlantBlade(Vector2 path, float wait, bool closing)
    {
        Vector2 across = new Vector2(-path.y, path.x);
        UseTone(HostileTone.Bright);
        for (int wing = 1; wing <= 2; wing++)
        {
            Vector2 outward = (wing == 1) == (across.x < 0) ? across : -across;
            if (closing)
                outward = -outward;
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

    // Hunting Stoop: nine stoops. The wake grows from four feathers a gun to six over the
    // first three; every third stoop doubles back for a second dive. The string never stops.
    IEnumerator HuntingStoop()
    {
        for (int dive = 0; dive < 9; dive++)
        {
            bool twice = dive % 3 == 2;
            Phrase(KiteString(twice ? 3.6f : 2.3f));
            yield return StoopDive(dive, Dense(24), Mathf.Min(6, 4 + dive), Dense(6), twice);
            yield return Wait(Rest(.1f));
        }
    }

    // Take station over the pilot, charge the nose, then a U-shaped dive that hooks off to
    // one side (and, twice, back again). Each beat both wing guns shed bright feathers fanned
    // from down-outward to up-outward at layered speeds, all quickening; dark chevrons leave
    // at every bottom.
    IEnumerator StoopDive(int dive, int beats, int feathers, int chevronRows, bool twice)
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
        Action<float, Vector2, int> shed = (t, tangent, beat) =>
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
        };
        yield return Glide(new Vector3(hook, high, 0), 1.3f, high - bottom, beats, shed);
        if (twice)
            yield return Glide(new Vector3(x, high, 0), 1.3f, high - bottom, beats, shed);
    }
}
