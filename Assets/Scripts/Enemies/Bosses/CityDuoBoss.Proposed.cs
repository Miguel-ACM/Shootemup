using System.Collections;
using UnityEngine;

// Proposed City duo book (ArtSource/BossPhaseProposals), flown only with ProposedCards.
// One concept per phase, developed through four movements of about fifteen seconds.
// Phase one, both ships: Green Wave. Every round is a car that brakes onto a street, waits
//   for its light and skids off. The hauler parks the street below the pair; the circling
//   interceptor parks the overpass along the top edge above it, and both go green together.
//   Movements: Weave, Shear, Avenue, Gridlock.
// Phase two, the enraged survivor:
//   Hauler: Container Drop. Crates hoisted to the top edge hang, fall and burst into cones.
//     Movements: Outside In, Inside Out, Two Tiers, Swinging Loads.
//   Interceptor: Hairpin Bends. Nested fans brake into arcs and turn back on themselves.
//     Movements: Hairpins, Switchbacks, Slalom, U-Turns.
// Hard final:
//   Hauler: Total Gridlock, the parked street under hanging containers.
//   Interceptor: Spaghetti Junction, three-layer hairpins from a swinging centre.
// Tones: phase one's street is dark with bright crossing lamps and the overpass bright with
// dark lamps; the survivor's crates are bright under dark high beams, and its hairpins are
// dark arrows over bright circles.
public sealed partial class CityDuoBoss
{
    // Each movement lasts seven cycles of about 2.2 s.
    const int CyclesPerMovement = 7;

    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return heavy ? TotalGridlock() : SpaghettiJunction();
            else if (!enraged)
                yield return heavy ? GreenWaveStreet() : GreenWaveOverpass();
            else
                yield return heavy ? ContainerDrop() : HairpinBends();
        }
    }

    // The rest of a beat that already spent part of its length charging.
    IEnumerator RestAfter(float interval, float spent)
    {
        return Wait(Mathf.Max(0, Rest(interval) - spent));
    }

    // Both wing guns charge together.
    IEnumerator ChargeWings(float seconds)
    {
        Phrase(Charge(Hull, WeaponPort(2), seconds));
        yield return Charge(Hull, WeaponPort(0), seconds);
    }

    // Green Wave, the hauler's part: one street per cycle, flashing its high beams twice
    // while it waits for green.
    IEnumerator GreenWaveStreet()
    {
        for (int cycle = 0; ; cycle++)
        {
            int movement = cycle / CyclesPerMovement % 4;
            yield return Charge(Hull, WeaponPort(1), .35f);
            ParkStreet(Muzzle(1), 7, cycle, movement, HostileTone.Dark, Rest(1.1f));
            yield return Wait(Rest(.6f));
            HighBeams(HostileTone.Bright);
            yield return Wait(Rest(.6f));
            HighBeams(HostileTone.Bright);
            yield return RestAfter(1.0f, .35f);
        }
    }

    // Green Wave, the interceptor's part: the overpass above the pair, on the same cycle,
    // with a headlight train either side of the pilot while it waits.
    IEnumerator GreenWaveOverpass()
    {
        for (int cycle = 0; ; cycle++)
        {
            int movement = cycle / CyclesPerMovement % 4;
            Phrase(Charge(Hull, WeaponPort(2), .35f));
            yield return Charge(Hull, WeaponPort(1), .35f);
            ParkOverpass(cycle, movement, Rest(1.1f));
            yield return Wait(Rest(.6f));
            NoseTrain(HostileTone.Bright, -8);
            yield return Wait(Rest(.6f));
            NoseTrain(HostileTone.Bright, 8);
            yield return RestAfter(1.0f, .35f);
        }
    }

    // How the street skids off on green:
    //   0 Weave: alternate rows skid opposite ways, green from the bottom row up.
    //   1 Shear: every row skids the same way (alternating each cycle), green from the top.
    //   2 Avenue: rows run straight down; the crossing snakes faster from row to row.
    //   3 Gridlock: alternate cars within each row skid opposite ways, splitting it into a lattice.
    static float StreetSkid(int movement, int row, int car, int cycle)
    {
        switch (movement)
        {
            case 0: return row % 2 == 0 ? 32 : -32;
            case 1: return cycle % 2 == 0 ? 32 : -32;
            case 2: return 0;
            default: return car % 2 == 0 ? 32 : -32;
        }
    }

    // Rows of cars brake onto level rows below the port at the same moment, wait for their
    // light, then skid off at one street speed and rebound once off the side walls. Each
    // row's two-car crossing is lit by lamps in the other tone and snakes from row to row.
    void ParkStreet(Vector3 port, int rows, int cycle, int movement, HostileTone tone, float green)
    {
        const float park = .8f;
        int cars = Mathf.Max(5, Dense(14));
        float left = Camera.LeftLimit + .45f, right = Camera.RightLimit - .45f;
        float snake = movement == 2 ? 2.0f : 1.3f;
        for (int row = 0; row < rows; row++)
        {
            float y = port.y - 1.5f - row * 1.2f;
            int gap = Mathf.RoundToInt((cars - 2) * (.5f + .42f * Mathf.Sin(row * snake + cycle * 2.1f)));
            // Shear goes green from the top, after the overpass; the others from the bottom.
            int order = movement == 1 ? row + 2 : rows - 1 - row;
            float hold = green + order * .16f;
            for (int car = 0; car < cars; car++)
            {
                if (car == gap || car == gap + 1)
                    continue;
                var delta = new Vector2(Mathf.Lerp(left, right, car / (cars - 1f)), y) - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (park - .175f));
                bool lamp = car == gap - 1 || car == gap + 2;
                UseTone(lamp ? Opposite(tone) : tone);
                var round = lamp ? Fire<GemRound>(port, delta, launch, .22f) : ScoredShot(car % 3 != 0, port, delta, launch, .19f);
                round.Pause(park, hold, CombatRound.Turn.Down, StreetSkid(movement, row, car, cycle), 2.3f / launch).Bounce(1, false);
            }
        }
    }

    // The overpass: two rows of bright cars braked along the visible top edge, thrown from
    // the interceptor's wings, with dark lamps at a crossing. They skid down against the
    // street's top row (Weave), with the shear (Shear), straight down (Avenue) or onto the
    // pilot (Gridlock).
    void ParkOverpass(int cycle, int movement, float green)
    {
        const float park = .8f;
        int cars = Mathf.Max(5, Dense(15));
        float top = CombatRound.Border.yMax - .5f;
        float left = Camera.LeftLimit + .4f, right = Camera.RightLimit - .4f;
        for (int row = 0; row < 2; row++)
        {
            float y = top - row * .75f;
            int gap = Mathf.RoundToInt((cars - 2) * (.5f + .42f * Mathf.Sin(row * 1.7f + cycle * 1.6f + 1)));
            // Bottom-up greens reach the overpass last, top-down greens first.
            float hold = green + (movement == 1 ? row : 8 - row) * .16f;
            float skid = movement == 0 ? (row == 0 ? -20 : 20) : movement == 1 ? (cycle % 2 == 0 ? 32 : -32) : 0;
            var turn = movement == 3 ? CombatRound.Turn.Aim : CombatRound.Turn.Down;
            for (int car = 0; car < cars; car++)
            {
                if (car == gap || car == gap + 1)
                    continue;
                float x = Mathf.Lerp(left, right, car / (cars - 1f));
                var port = Muzzle(x < transform.position.x ? 1 : 2);
                var delta = new Vector2(x, y) - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (park - .175f));
                bool lamp = car == gap - 1 || car == gap + 2;
                UseTone(lamp ? HostileTone.Dark : HostileTone.Bright);
                var round = lamp ? Fire<GemRound>(port, delta, launch, .22f) : ScoredShot(car % 3 != 0, port, delta, launch, .19f);
                round.Pause(park, hold, turn, skid, (movement == 3 ? 2.8f : 2.4f) / launch);
            }
        }
    }

    // High beams: one aimed train from each wing of the hauler, a fast kunai headlight
    // ahead of round cars slowing along the same line.
    void HighBeams(HostileTone tone)
    {
        UseTone(tone);
        foreach (int port in new[] { 0, 2 })
        {
            var origin = Muzzle(port);
            var aim = CombatVisuals.Aim(origin);
            Fire<KunaiRound>(origin, aim, 5.2f, .2f);
            int cars = Dense(6);
            for (int car = 1; car < cars; car++)
                Fire<SimpleRound>(origin, aim, 5.0f - car * .45f, .18f);
        }
    }

    // A headlight train from the interceptor's nose: a fast chevron, then round cars
    // slowing along the same aim.
    void NoseTrain(HostileTone tone, float skew)
    {
        var port = Muzzle(0);
        var aim = CombatVisuals.Rotate(CombatVisuals.Aim(port), skew);
        UseTone(tone);
        Fire<ChevronRound>(port, aim, 5.4f, .19f);
        int cars = Dense(5);
        for (int car = 1; car < cars; car++)
            Fire<SimpleRound>(port, aim, 5.0f - car * .55f, .18f);
    }

    // Container Drop (hauler survivor), one crane cycle after another.
    IEnumerator ContainerDrop()
    {
        for (int cycle = 0; ; cycle++)
        {
            int movement = cycle / CyclesPerMovement % 4;
            yield return ChargeWings(.3f);
            HoistCrates(5, movement, HostileTone.Bright, Rest(.35f));
            yield return Wait(Rest(.9f));
            HighBeams(HostileTone.Dark);
            yield return Wait(Rest(.6f));
            HighBeams(HostileTone.Dark);
            yield return RestAfter(.7f, .3f);
        }
    }

    // Crates brake onto mirrored stations along the visible top edge, hold, drop and burst
    // into a forward shrapnel cone:
    //   0 Outside In: the outer crates fall first, cones straight down.
    //   1 Inside Out: the middle crates fall first, cones aimed at the pilot.
    //   2 Two Tiers: alternate crates hang lower and the two tiers fall in turn.
    //   3 Swinging Loads: crates fall on alternate diagonals, crossing before they burst.
    void HoistCrates(int perSide, int movement, HostileTone tone, float hold)
    {
        const float lift = .9f;
        float top = CombatRound.Border.yMax - .8f;
        float reach = Camera.RightLimit - 1.1f;
        int shards = Dense(10);
        bool aimed = movement == 1;
        UseTone(tone);
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var port = Muzzle(mirror < 0 ? 0 : 2);
            for (int k = 0; k < perSide; k++)
            {
                bool lower = movement == 2 && k % 2 == 1;
                var target = new Vector2(mirror * (.7f + k * reach / Mathf.Max(1, perSide - 1)), lower ? top - 1.3f : top);
                var delta = target - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (lift - .175f));
                float order = movement == 1 ? k : movement == 2 ? (k % 2) * 3 : perSide - 1 - k;
                float wait = hold + order * .2f;
                float lean = movement == 3 ? (k % 2 == 0 ? 22 : -22) * mirror : 0;
                Fire<SimpleRound>(port, delta, launch, .42f)
                    .Pause(lift, wait, CombatRound.Turn.Down, lean, 2.6f / launch)
                    .SplitAt(lift + wait + .7f, shards, aimed ? 6 : 8, 3.0f, .17f, aimed);
            }
        }
    }

    // Hairpin Bends (interceptor survivor), a fan every 0.4 s:
    //   0 Hairpins: fans alternately straight down and on the pilot, bending back 155 degrees.
    //   1 Switchbacks: later brakes and sharper 125-degree turns that throw wider loops.
    //   2 Slalom: the fan centre swings from side to side.
    //   3 U-Turns: every round reverses up its own line and rebounds off the ceiling.
    IEnumerator HairpinBends()
    {
        for (int n = 0; ; n++)
        {
            int movement = n / 35 % 4;
            var port = Muzzle(0);
            Vector2 centre = movement == 2 ? CombatVisuals.Rotate(Vector2.down, 35 * Mathf.Sin(n * .5f))
                : n % 2 == 0 ? Vector2.down : CombatVisuals.Aim(port);
            float bend = movement == 1 ? 125 : movement == 3 ? 180 : 155;
            HairpinFan(port, centre, bend, movement == 1 ? .9f : .7f, false);
            yield return Wait(Rest(.4f));
        }
    }

    // Nested fans open below the interceptor: dark arrows outside, bright circles inside and,
    // when asked, bright kunai beyond. They brake into arcs, then every round turns back
    // through the bend, crossing its mirror twin on the way up, and rebounds once off the ceiling.
    void HairpinFan(Vector3 port, Vector2 centre, float bend, float brake, bool kunai)
    {
        int lanes = Dense(11);
        float step = 100f / Mathf.Max(1, lanes - 1);
        for (int i = 0; i < lanes; i++)
        {
            float angle = (i - (lanes - 1) * .5f) * step;
            float turn = angle > .01f ? bend : angle < -.01f ? -bend : 180;
            UseTone(HostileTone.Dark);
            Fire<NeedleRound>(port, CombatVisuals.Rotate(centre, angle), 3.6f, .19f)
                .Pause(brake, .15f, CombatRound.Turn.Rotate, turn, 1.15f)
                .Bounce(1);
            UseTone(HostileTone.Bright);
            Fire<SimpleRound>(port, CombatVisuals.Rotate(centre, angle * .9f), 2.5f, .19f)
                .Pause(brake, .15f, CombatRound.Turn.Rotate, turn, 1.15f)
                .Bounce(1);
            if (kunai)
                Fire<KunaiRound>(port, CombatVisuals.Rotate(centre, angle * 1.08f), 4.6f, .19f)
                    .Pause(brake, .15f, CombatRound.Turn.Rotate, turn, 1.15f)
                    .Bounce(1);
        }
    }

    // Total Gridlock (hauler, Hard final): every cycle parks a street with containers hanging
    // above it. The street skids off by the same movements as Green Wave, and the crates
    // drop through it by the Container Drop movements.
    IEnumerator TotalGridlock()
    {
        for (int cycle = 0; ; cycle++)
        {
            int movement = cycle / CyclesPerMovement % 4;
            yield return ChargeWings(.3f);
            ParkStreet(Muzzle(1), 5, cycle, movement, HostileTone.Dark, Rest(1.1f));
            HoistCrates(4, movement, HostileTone.Bright, Rest(.6f));
            yield return Wait(Rest(.8f));
            HighBeams(HostileTone.Bright);
            yield return Wait(Rest(.6f));
            HighBeams(HostileTone.Bright);
            yield return RestAfter(.8f, .3f);
        }
    }

    // Spaghetti Junction (interceptor, Hard final): hairpin fans from an ever-swinging centre,
    // with a kunai layer on alternate fans. The bend changes every forty fans.
    IEnumerator SpaghettiJunction()
    {
        float[] bends = { 155, 125, 180, 140 };
        for (int n = 0; ; n++)
        {
            int movement = n / 40 % 4;
            var centre = CombatVisuals.Rotate(Vector2.down, 35 * Mathf.Sin(n * .6f));
            HairpinFan(Muzzle(0), centre, bends[movement], movement == 1 ? .85f : .7f, n % 2 == 0);
            yield return Wait(Rest(.33f));
        }
    }
}
