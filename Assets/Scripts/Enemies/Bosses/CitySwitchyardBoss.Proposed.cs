using System.Collections;
using UnityEngine;

// Proposed Switchyard book (ArtSource/BossPhaseProposals), flown only with ProposedCards.
// One concept per phase, developed through four movements of about fifteen seconds. The
// engine still brakes into a signal box, changing platform between cycles or movements.
// Phase one: Signal Gantry. A bloom of lamps from both shoulders brakes into a two-tone
//   row along the top edge, and each aspect falls as short trains when it clears.
//   Movements: Clear, Wave, Double Block, Flashing.
// Phase two: Level Crossing. Mirrored barrier arms of arrows tipped by gem lamps lower
//   across the yard and lift again. Movements: Barriers, Scissors, Boom Gates, Three Gates.
// Hard final: Grand Junction. The level crossing works beneath the signal gantry, both
//   stepping through their movements together.
// Tones: the gantry alternates its aspects with bright expresses; the crossing arms are
// bright arrows with dark gem lamps.
public sealed partial class CitySwitchyardBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return GrandJunction();
            else if (phaseTwo)
                yield return LevelCrossing();
            else
                yield return SignalGantry();
        }
    }

    // Signal Gantry (phase one): a gantry every 3 s from a new box, five gantries per movement.
    IEnumerator SignalGantry()
    {
        for (int cycle = 0; ; cycle++)
        {
            int movement = cycle / 5 % 4;
            int side = movement % 2 == 0 ? -1 : 1;
            station.x = Lane(side * (cycle % 2 == 0 ? .5f : -.5f));
            station.y = Camera.UpLimit - 2.6f;
            yield return PrimePair(.45f);
            HangGantry(movement, cycle, Rest(.55f), Rest(movement == 2 ? .9f : 1.2f));
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
    // Each lamp is a short stack of cars that falls as one train when its aspect clears.
    // Fourteen lamps leave about one unit of visible clearance between same-tone columns on
    // Hard, and the dark aspect clears before the next row parks.
    //   0 Clear: bright falls in a V from the middle out, dark from the edges in.
    //   1 Wave: the aspects sweep across the row, the direction alternating every gantry.
    //   2 Double Block: lamps pair up by tone, opening wider columns, with a quicker change.
    //   3 Flashing: the dark aspect launches its trains at the pilot instead of straight down.
    void HangGantry(int movement, int cycle, float firstDrop, float aspectGap)
    {
        const float lift = .85f;
        int lamps = Mathf.Max(6, Dense(14));
        int cars = Dense(7);
        float top = CombatRound.Border.yMax - .55f;
        float left = Camera.LeftLimit + .4f, right = Camera.RightLimit - .4f;
        for (int i = 0; i < lamps; i++)
        {
            float x = Mathf.Lerp(left, right, i / (lamps - 1f));
            bool bright = movement == 2 ? i / 2 % 2 == 0 : i % 2 == 0;
            float across = Mathf.Abs(x) / Camera.RightLimit;
            float sweep = Mathf.InverseLerp(left, right, x);
            if (cycle % 2 == 1)
                sweep = 1 - sweep;
            float delay = movement == 1 ? (bright ? sweep : 1 - sweep) * .6f : (bright ? across : 1 - across) * .35f;
            float drop = bright ? firstDrop + delay : firstDrop + aspectGap + delay;
            var turn = movement == 3 && !bright ? CombatRound.Turn.Aim : CombatRound.Turn.Down;
            var port = Muzzle(x < transform.position.x ? 1 : 2);
            UseTone(bright ? HostileTone.Bright : HostileTone.Dark);
            for (int car = 0; car < cars; car++)
            {
                var delta = new Vector2(x, top + car * .22f) - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (lift - .175f));
                ScoredShot(car > 0, port, delta, launch, car == 0 ? .19f : .2f)
                    .Pause(lift, drop, turn, 0, (3.4f - car * .1f) / launch);
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

    // Level Crossing (phase two): each movement opens with a prime at a new signal box.
    IEnumerator LevelCrossing()
    {
        int side = -1;
        for (int movement = 0; ; movement = (movement + 1) % 4)
        {
            station.x = Lane(side * .4f);
            station.y = Camera.UpLimit - 2.2f;
            side = -side;
            yield return PrimePair(.5f);
            yield return CrossingArms(14, movement);
        }
    }

    // How far a barrier has lowered at a point in its swing. Boom gates dwell fully lowered
    // and fully raised.
    static float Lowered(float t, bool dwell)
    {
        float x = Mathf.PingPong(t, 1);
        if (dwell)
            x = Mathf.Clamp01((x - .2f) / .6f);
        return Mathf.SmoothStep(0, 1, x);
    }

    // Each step releases one arm from each shoulder: a line of bright arrows at rising speeds
    // tipped by a dark gem lamp, from slightly above level down past straight down and back,
    // rebounding off the walls.
    //   0 Barriers: both arms lower and lift together.
    //   1 Scissors: one arm lowers while the other lifts.
    //   2 Boom Gates: the arms dwell fully lowered and fully raised, standing as walls.
    //   3 Three Gates: quicker sweeps, with a third arm from the nose swinging across the middle.
    IEnumerator CrossingArms(float seconds, int movement)
    {
        float step = movement == 3 ? .2f : .16f;
        float lowerSeconds = movement == 3 ? 1.6f : 2.4f;
        int arm = Dense(movement == 3 ? 3 : 4);
        int steps = Mathf.RoundToInt(seconds / step);
        for (int i = 0; i < steps; i++)
        {
            float t = i * step / lowerSeconds;
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                float lowered = Lowered(movement == 1 && mirror > 0 ? t + 1 : t, movement == 2);
                BarrierArm(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, mirror * Mathf.Lerp(110, -25, lowered)), arm);
            }
            if (movement == 3)
                BarrierArm(Muzzle(0), CombatVisuals.Rotate(Vector2.down, Mathf.Lerp(-60, 60, Lowered(t * .5f, false))), arm);
            yield return Wait(Rest(step));
        }
    }

    void BarrierArm(Vector3 port, Vector2 direction, int arrows)
    {
        UseTone(HostileTone.Bright);
        for (int k = 0; k < arrows; k++)
            Fire<NeedleRound>(port, direction, 1.6f + k * .65f, .19f).Bounce(1);
        UseTone(HostileTone.Dark);
        Fire<GemRound>(port, direction, 1.6f + arrows * .65f, .22f).Bounce(1);
    }

    // Grand Junction (Hard final): every cycle hangs a gantry and works the crossing beneath
    // it. Both step through their movements together, three cycles each, and the engine
    // changes platform every cycle.
    IEnumerator GrandJunction()
    {
        int side = -1;
        for (int cycle = 0; ; cycle++)
        {
            int movement = cycle / 3 % 4;
            station.x = Lane(side * (cycle % 3 - 1) * .6f);
            station.y = Camera.UpLimit - 2.4f;
            if (cycle % 3 == 2)
                side = -side;
            yield return PrimePair(.45f);
            HangGantry(movement, cycle, Rest(.75f), Rest(1.4f));
            yield return CrossingArms(3.2f, movement);
            yield return Express(HostileTone.Dark);
            yield return Wait(Rest(.3f));
        }
    }
}
