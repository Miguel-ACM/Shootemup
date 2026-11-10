using System.Collections;
using UnityEngine;

// Proposed Switchyard book (ArtSource/BossPhaseProposals), flown only with ProposedCards.
// The engine still brakes into a signal box for each card and changes platform between them.
// Phase one
//   Signal Gantry: a bloom of lamps from both shoulders brakes into a row along the top
//     edge, alternate lamps in the two tones. The bright aspect drops first in a V from
//     the middle out, the dark aspect follows from the edges in; each lamp falls as a short
//     train, an arrow leading round cars. Express headlights and rebounding ground signals
//     keep the pilot moving between the aspects.
//   Points Change: rails of cars run from the nose. At every throw all cars on the rails
//     brake together, glint and resume kinked onto the new heading while the nose swings
//     the same way, folding the tracks into zigzags; bright switch lamps are aimed from the
//     shoulders at each throw.
// Phase two
//   Level Crossing: mirrored barrier arms of bright arrows, each tipped by a dark gem lamp,
//     sweep from beside the engine down across the middle and lift again; side rounds
//     rebound off the walls.
//   Shunting: the nose pushes fans of wagons out at four speeds; they brake, back up toward
//     the engine still facing out, stop with a glint and run down the yard as level arcs.
// Hard final
//   Grand Junction: the gantry hangs over the yard while the crossing arms sweep beneath it.
// Tones: the gantry alternates its lamps; Points Change is dark rails with bright lamps;
// Level Crossing is bright arms with dark lamps; Shunting is dark wagons led by bright ones.
public sealed partial class CitySwitchyardBoss
{
    protected override IEnumerator Proposed()
    {
        int side = -1;
        while (true)
        {
            if (phaseThree)
                yield return GrandJunction(side);
            else if (phaseTwo)
            {
                yield return LevelCrossing(side, 7);
                yield return Wait(Rest(.5f));
                yield return Shunting(-side, 6);
            }
            else
            {
                yield return SignalGantry(side, 3);
                yield return Wait(Rest(.5f));
                yield return PointsChange(-side, 7);
            }
            side = -side;
            yield return Wait(Rest(.5f));
        }
    }

    // Signal Gantry (phase one).
    IEnumerator SignalGantry(int side, int cycles)
    {
        for (int cycle = 0; cycle < cycles; cycle++)
        {
            station.x = Lane(side * (cycle % 2 == 0 ? .5f : -.5f));
            station.y = Camera.UpLimit - 2.6f;
            yield return PrimePair(.45f);
            HangGantry(Rest(.55f), Rest(1.2f));
            yield return Wait(Rest(.65f));
            yield return Express(HostileTone.Bright);
            yield return Wait(Rest(.35f));
            GroundSignals(HostileTone.Bright);
            yield return Wait(Rest(.5f));
            yield return Express(HostileTone.Dark);
            yield return Wait(Rest(.25f));
            GroundSignals(HostileTone.Dark);
            yield return Wait(Rest(.3f));
        }
    }

    // One row of lamps braked onto the visible top edge, alternate lamps bright and dark.
    // Each lamp is a short stack of cars that falls as one train when its aspect clears:
    // bright from the middle out, dark from the edges in after the aspect gap. Fourteen
    // lamps leave about one unit of visible clearance between same-tone columns on Hard.
    void HangGantry(float firstDrop, float aspectGap)
    {
        const float lift = .85f;
        int lamps = Mathf.Max(6, Dense(14));
        int cars = Dense(7);
        float top = CombatRound.Border.yMax - .55f;
        float left = Camera.LeftLimit + .4f, right = Camera.RightLimit - .4f;
        for (int i = 0; i < lamps; i++)
        {
            float x = Mathf.Lerp(left, right, i / (lamps - 1f));
            bool bright = i % 2 == 0;
            float across = Mathf.Abs(x) / Camera.RightLimit;
            float drop = bright ? firstDrop + across * .35f : firstDrop + aspectGap + (1 - across) * .35f;
            var port = Muzzle(x < transform.position.x ? 1 : 2);
            UseTone(bright ? HostileTone.Bright : HostileTone.Dark);
            for (int car = 0; car < cars; car++)
            {
                var delta = new Vector2(x, top + car * .22f) - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (lift - .175f));
                ScoredShot(car > 0, port, delta, launch, car == 0 ? .19f : .2f)
                    .Pause(lift, drop, CombatRound.Turn.Down, 0, (3.4f - car * .1f) / launch);
            }
        }
    }

    // Express: a gem headlight with kunai carriages slowing along one locked aim, from the
    // nose and mirrored twenty degrees either side.
    IEnumerator Express(HostileTone tone)
    {
        yield return Charge(Hull, WeaponPort(0), .25f);
        var port = Muzzle(0);
        var aim = CombatVisuals.Aim(port);
        UseTone(tone);
        int cars = Dense(7);
        foreach (float skew in new[] { -20f, 0f, 20f })
        {
            var track = CombatVisuals.Rotate(aim, skew);
            Fire<GemRound>(port, track, 5.0f, .24f);
            for (int car = 1; car < cars; car++)
                Fire<KunaiRound>(port, track, 4.8f - car * .4f, .18f);
        }
    }

    // Ground signals: mirrored arrow fans thrown out from the shoulders toward the walls,
    // rebounding once back across the yard.
    void GroundSignals(HostileTone tone)
    {
        UseTone(tone);
        int count = Dense(7);
        for (int sign = -1; sign <= 1; sign += 2)
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(50, 110, i / Mathf.Max(1f, count - 1));
                Fire<NeedleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, sign * angle), 2.9f, .19f).Bounce(1);
            }
    }

    // Points Change (phase one).
    IEnumerator PointsChange(int side, float seconds)
    {
        station.x = Lane(side * .4f);
        station.y = Camera.UpLimit - 2.3f;
        yield return Charge(Hull, WeaponPort(0), .4f);
        const int perPeriod = 7;
        const float carGap = .11f;
        const float throwAngle = 26;
        float heading = -side * throwAngle * .5f;
        int rails = Dense(5) | 1;
        int periods = Mathf.Max(1, Mathf.RoundToInt(seconds / (perPeriod * carGap)));
        for (int period = 0; period < periods; period++)
        {
            int kink = period % 2 == 0 ? side : -side;
            for (int car = 0; car < perPeriod; car++)
            {
                // Every car on the rails brakes at the next throw, however long it has run.
                float untilThrow = Mathf.Max(.05f, (perPeriod - car) * Rest(carGap));
                UseTone(HostileTone.Dark);
                for (int rail = 0; rail < rails; rail++)
                {
                    var direction = CombatVisuals.Rotate(Vector2.down, heading + (rail - (rails - 1) * .5f) * 16);
                    ScoredShot(car % 3 == 0, Muzzle(0), direction, 3.4f, .18f)
                        .Pause(untilThrow, .22f, CombatRound.Turn.Rotate, kink * throwAngle, 1);
                }
                yield return Wait(Rest(carGap));
            }
            heading += kink * throwAngle;
            SwitchLamps();
            if (period % 3 == 2)
                station.x = Lane(-Mathf.Sign(station.x) * .4f);
        }
    }

    // Bright switch lamps: five gems aimed from each shoulder.
    void SwitchLamps()
    {
        UseTone(HostileTone.Bright);
        int lamps = Dense(5);
        for (int port = 1; port <= 2; port++)
        {
            var origin = Muzzle(port);
            var aim = CombatVisuals.Aim(origin);
            for (int i = 0; i < lamps; i++)
                Fire<GemRound>(origin, CombatVisuals.Rotate(aim, (i - (lamps - 1) * .5f) * 7), 3.8f, .22f);
        }
    }

    // Level Crossing (phase two).
    IEnumerator LevelCrossing(int side, float seconds)
    {
        station.x = Lane(side * .3f);
        station.y = Camera.UpLimit - 2.2f;
        yield return PrimePair(.5f);
        yield return CrossingArms(seconds, 2.4f);
    }

    // Mirrored barrier arms: each step releases one arm from each shoulder, a line of arrows
    // at rising speeds tipped by a gem lamp. The arms lower from beside the engine across
    // the middle (past straight down) and lift again, rebounding off the walls.
    IEnumerator CrossingArms(float seconds, float lowerSeconds)
    {
        const float step = .16f;
        int arm = Dense(4);
        int steps = Mathf.RoundToInt(seconds / step);
        for (int i = 0; i < steps; i++)
        {
            float lowered = Mathf.SmoothStep(0, 1, Mathf.PingPong(i * step / lowerSeconds, 1));
            float angle = Mathf.Lerp(110, -25, lowered);
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var port = Muzzle(mirror < 0 ? 1 : 2);
                var direction = CombatVisuals.Rotate(Vector2.down, mirror * angle);
                UseTone(HostileTone.Bright);
                for (int k = 0; k < arm; k++)
                    Fire<NeedleRound>(port, direction, 1.6f + k * .65f, .19f).Bounce(1);
                UseTone(HostileTone.Dark);
                Fire<GemRound>(port, direction, 1.6f + arm * .65f, .22f).Bounce(1);
            }
            yield return Wait(Rest(step));
        }
    }

    // Shunting (phase two): fans of wagons at four speeds brake, back up toward the engine
    // with their arrows still facing out, stop with a glint and run down the yard as level
    // arcs at one speed. Alternate fans are centred on the pilot.
    IEnumerator Shunting(int side, float seconds)
    {
        station.x = Lane(side * .5f);
        station.y = Camera.UpLimit - 2.5f;
        const float backing = -.6f;
        int n = 0;
        for (float t = 0; t < seconds; t += .75f, n++)
        {
            yield return Charge(Hull, WeaponPort(0), .25f);
            var port = Muzzle(0);
            Vector2 centre = n % 2 == 0 ? Vector2.down : CombatVisuals.Aim(port);
            int lanes = Dense(11);
            float spread = 130f / Mathf.Max(1, lanes - 1);
            for (int i = 0; i < lanes; i++)
            {
                var direction = CombatVisuals.Rotate(centre, (i - (lanes - 1) * .5f) * spread + (n % 2) * 4);
                for (int k = 0; k < 4; k++)
                {
                    float speed = 2.4f + k;
                    UseTone(k == 3 ? HostileTone.Bright : HostileTone.Dark);
                    // A negative resume speed undoes the reversed ramp: the arcs run downward.
                    ScoredShot(k == 0, port, direction, speed, .19f)
                        .Ramp(backing, 1.2f, .5f)
                        .Pause(1.9f, .25f, CombatRound.Turn.Down, 0, 2.6f / (backing * speed));
                }
            }
            yield return Wait(Mathf.Max(0, Rest(.75f) - .25f));
        }
    }

    // Grand Junction (Hard final): the gantry hangs over the yard while the crossing arms
    // sweep beneath it; the engine changes platform every cycle.
    IEnumerator GrandJunction(int side)
    {
        for (int cycle = 0; cycle < 3; cycle++)
        {
            station.x = Lane(side * (cycle - 1) * .6f);
            station.y = Camera.UpLimit - 2.4f;
            yield return PrimePair(.45f);
            HangGantry(Rest(.75f), Rest(1.4f));
            yield return CrossingArms(3.2f, 1.6f);
            yield return Express(HostileTone.Dark);
            yield return Wait(Rest(.3f));
        }
    }
}
