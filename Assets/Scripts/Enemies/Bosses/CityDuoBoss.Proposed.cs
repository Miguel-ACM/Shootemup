using System.Collections;
using UnityEngine;

// Proposed City duo book (ArtSource/BossPhaseProposals), flown only with ProposedCards.
// The roles are the shipping ones: in phase one the interceptor circles the hauler and
// both ships fly each card together for the same length; the enraged survivor then flies
// its own two cards, and Hard adds one final spell.
// Phase one (both ships)
//   Green Wave: one burst from the hauler's nose brakes into rows of parked cars, each row
//     with a crossing marked by two lamps in the other tone. The lights turn green from the
//     bottom row up and alternate rows skid off left and right, so the crossings slide
//     apart. The interceptor covers the pair's roof: mirrored arrow fans thrown up behind
//     it rebound off the ceiling, with headlight trains aimed between.
//   Zebra Crossing: the hauler lays level stripes that widen as they fall, alternating
//     tone and silhouette, with a crossing that snakes from stripe to stripe. The
//     interceptor fires headlight trains that brake together and skid sideways.
// Phase two (the enraged survivor)
//   Hauler: Container Drop (crates hoisted to the top edge, dropped from the outside in,
//     each bursting into a downward cone), then Jackknife (mirrored trucks whose trailers
//     fold in and slide through each other).
//   Interceptor: Hairpin Bends (nested fans that brake and turn back up across each other,
//     rebounding off the ceiling), then Skid Marks (its weaving path laid in stalled beads
//     that launch at the pilot in the order they were laid).
// Hard final: hauler Total Gridlock, interceptor Spaghetti Junction.
// Tones: phase one gives the hauler's street dark and the interceptor's roof cover bright,
// then the zebra stripes alternate while the skids lead dark. The survivor's first card
// is bright-led (crates, dark accents), its second dark-led.
public sealed partial class CityDuoBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
            {
                yield return heavy ? TotalGridlock(8) : SpaghettiJunction(8);
                yield return Wait(.5f);
            }
            else if (!enraged)
            {
                yield return heavy ? GreenWave(8) : Overpass(8);
                yield return Wait(.6f);
                yield return heavy ? ZebraCrossing(8) : SkidLights(8);
                yield return Wait(.6f);
            }
            else if (heavy)
            {
                yield return ContainerDrop(7);
                yield return Wait(.5f);
                yield return Jackknife(6);
                yield return Wait(.5f);
            }
            else
            {
                yield return HairpinBends(7);
                yield return Wait(.5f);
                yield return SkidMarks(6);
                yield return Wait(.5f);
            }
        }
    }

    // The rest of a beat that already spent part of its length charging.
    IEnumerator RestAfter(float interval, float spent)
    {
        return Wait(Mathf.Max(0, Rest(interval) - spent));
    }

    // Both wing guns of the hauler charge together.
    IEnumerator ChargeWings(float seconds)
    {
        Phrase(Charge(Hull, WeaponPort(2), seconds));
        yield return Charge(Hull, WeaponPort(0), seconds);
    }

    // Green Wave (hauler, phase one).
    IEnumerator GreenWave(float seconds)
    {
        int cycle = 0;
        for (float t = 0; t < seconds; t += 2.3f)
        {
            yield return Charge(Hull, WeaponPort(1), .35f);
            ParkStreet(Muzzle(1), 6, cycle++, HostileTone.Dark, Rest(1.1f));
            yield return Wait(Rest(.7f));
            HighBeams(HostileTone.Bright);
            yield return Wait(Rest(.7f));
            HighBeams(HostileTone.Bright);
            yield return RestAfter(.9f, .35f);
        }
    }

    // Rows of cars brake onto level rows below the port at the same moment, wait for their
    // green light (bottom row first), then skid off diagonally at one street speed and
    // rebound once off the side walls. Each row's two-car crossing is lit by lamps in the
    // other tone and snakes from row to row.
    void ParkStreet(Vector3 port, int rows, int cycle, HostileTone tone, float green)
    {
        const float park = .8f;
        int cars = Mathf.Max(5, Dense(13));
        float left = Camera.LeftLimit + .45f, right = Camera.RightLimit - .45f;
        for (int row = 0; row < rows; row++)
        {
            float y = port.y - 1.5f - row * 1.2f;
            int skid = row % 2 == 0 ? 1 : -1;
            int gap = Mathf.RoundToInt((cars - 2) * (.5f + .42f * Mathf.Sin(row * 1.3f + cycle * 2.1f)));
            float hold = green + (rows - 1 - row) * .16f;
            for (int car = 0; car < cars; car++)
            {
                if (car == gap || car == gap + 1)
                    continue;
                var delta = new Vector2(Mathf.Lerp(left, right, car / (cars - 1f)), y) - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (park - .175f));
                bool lamp = car == gap - 1 || car == gap + 2;
                UseTone(lamp ? Opposite(tone) : tone);
                var round = lamp ? Fire<GemRound>(port, delta, launch, .22f) : ScoredShot(car % 3 != 0, port, delta, launch, .19f);
                round.Pause(park, hold, CombatRound.Turn.Down, skid * 32, 2.3f / launch).Bounce(1, false);
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

    // Overpass (interceptor, phase one): mirrored arrow fans thrown up behind the circling
    // interceptor rebound off the ceiling and rain back across the top of the arena.
    IEnumerator Overpass(float seconds)
    {
        int n = 0;
        for (float t = 0; t < seconds; t += .45f, n++)
        {
            UseTone(HostileTone.Bright);
            int lanes = Dense(6);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                for (int i = 0; i < lanes; i++)
                {
                    float angle = 12 + i * 9.5f + (n % 3) * 4;
                    ScoredShot(i == lanes - 1, Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.up, -mirror * angle), 3.4f).Bounce(1);
                }
            if (n % 2 == 1)
                NoseTrain(HostileTone.Bright, 0);
            yield return Wait(Rest(.45f));
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

    // Zebra Crossing (hauler, phase one): every round of a stripe shares one descent speed,
    // so the stripe stays level while it widens. Stripes alternate dark arrows and bright
    // circles; the two-round crossing snakes from stripe to stripe.
    IEnumerator ZebraCrossing(float seconds)
    {
        const float descent = 2.3f;
        int n = 0;
        for (float t = 0; t < seconds; t += .3f, n++)
        {
            var port = Muzzle(1);
            int lanes = Mathf.Max(6, Dense(17));
            float slope = 1.6f / (lanes - 1);
            int gap = Mathf.RoundToInt((lanes - 2) * (.5f + .4f * Mathf.Sin(n * .18f)));
            bool dark = n % 2 == 0;
            UseTone(dark ? HostileTone.Dark : HostileTone.Bright);
            for (int i = 0; i < lanes; i++)
            {
                if (i == gap || i == gap + 1)
                    continue;
                var direction = new Vector2((i - (lanes - 1) * .5f) * slope, -1);
                ScoredShot(!dark, port, direction, descent * direction.magnitude, .18f);
            }
            yield return Wait(Rest(.3f));
        }
    }

    // Skid Lights (interceptor, phase one): three headlight trains aimed at the pilot brake
    // together, glint, then skid sideways onto parallel tracks, alternating left and right.
    IEnumerator SkidLights(float seconds)
    {
        int n = 0;
        for (float t = 0; t < seconds; t += .65f, n++)
        {
            yield return Charge(Hull, WeaponPort(0), .2f);
            var port = Muzzle(0);
            var aim = CombatVisuals.Aim(port);
            int cars = Dense(6);
            int skid = n % 2 == 0 ? 1 : -1;
            foreach (float ray in new[] { -18f, 0f, 18f })
                for (int car = 0; car < cars; car++)
                {
                    var heading = CombatVisuals.Rotate(aim, ray);
                    float speed = 5.0f - car * .45f;
                    CombatRound round;
                    if (car % 2 == 0)
                    {
                        UseTone(HostileTone.Dark);
                        round = Fire<ChevronRound>(port, heading, speed, .19f);
                    }
                    else
                    {
                        UseTone(HostileTone.Bright);
                        round = Fire<SimpleRound>(port, heading, speed, .18f);
                    }
                    round.Pause(.75f, .2f, CombatRound.Turn.Rotate, skid * 40, 1);
                }
            yield return RestAfter(.65f, .2f);
        }
    }

    // Container Drop (hauler survivor): crates hoisted from both wings hang on the top edge,
    // then fall from the outside in and burst into downward cones. High beams pick at the
    // pilot while they hang.
    IEnumerator ContainerDrop(float seconds)
    {
        for (float t = 0; t < seconds; t += 2.2f)
        {
            yield return ChargeWings(.3f);
            HoistCrates(5, HostileTone.Bright, Rest(.35f));
            yield return Wait(Rest(.9f));
            HighBeams(HostileTone.Dark);
            yield return Wait(Rest(.6f));
            HighBeams(HostileTone.Dark);
            yield return RestAfter(.7f, .3f);
        }
    }

    // Crates brake onto mirrored stations along the visible top edge, hold, then drop in a
    // ripple from the outside in, each bursting into a forward (downward) shrapnel cone.
    void HoistCrates(int perSide, HostileTone tone, float hold)
    {
        const float lift = .9f;
        float top = CombatRound.Border.yMax - .8f;
        float reach = Camera.RightLimit - 1.1f;
        int shards = Dense(10);
        UseTone(tone);
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var port = Muzzle(mirror < 0 ? 0 : 2);
            for (int k = 0; k < perSide; k++)
            {
                var target = new Vector2(mirror * (.7f + k * reach / Mathf.Max(1, perSide - 1)), top);
                var delta = target - (Vector2)port;
                float launch = WorldSpeed(delta.magnitude / (lift - .175f));
                float wait = hold + (perSide - 1 - k) * .2f;
                Fire<SimpleRound>(port, delta, launch, .42f)
                    .Pause(lift, wait, CombatRound.Turn.Down, 0, 2.6f / launch)
                    .SplitAt(lift + wait + .7f, shards, 8, 3.0f, .17f);
            }
        }
    }

    // Jackknife (hauler survivor): from each wing a gem cab runs outward and down ahead of a
    // trailer of rounds on the same line. The trailer brakes and folds in toward the
    // middle, so the mirrored trailers slide through each other as a moving X. A kunai
    // horn sounds from the nose every third truck.
    IEnumerator Jackknife(float seconds)
    {
        int n = 0;
        for (float t = 0; t < seconds; t += .45f, n++)
        {
            float lean = 25 + (n % 3) * 15;
            int trailer = Dense(7);
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                var port = Muzzle(mirror < 0 ? 0 : 2);
                var heading = CombatVisuals.Rotate(Vector2.down, mirror * lean);
                UseTone(HostileTone.Bright);
                Fire<GemRound>(port, heading, 4.3f, .22f);
                UseTone(HostileTone.Dark);
                for (int k = 0; k < trailer; k++)
                    ScoredShot(k % 2 == 1, port, heading, 3.9f - k * .45f, .18f)
                        .Pause(1.0f, .25f, CombatRound.Turn.Rotate, -mirror * 95, .9f);
            }
            if (n % 3 == 2)
                Horn();
            yield return Wait(Rest(.45f));
        }
    }

    // A narrow aimed spread of kunai from the hauler's nose.
    void Horn()
    {
        UseTone(HostileTone.Bright);
        var port = Muzzle(1);
        var aim = CombatVisuals.Aim(port);
        int count = Dense(5);
        for (int i = 0; i < count; i++)
            Fire<KunaiRound>(port, CombatVisuals.Rotate(aim, (i - (count - 1) * .5f) * 7), 4.4f, .19f);
    }

    // Hairpin Bends (interceptor survivor).
    IEnumerator HairpinBends(float seconds)
    {
        int n = 0;
        for (float t = 0; t < seconds; t += .4f, n++)
        {
            var port = Muzzle(0);
            HairpinFan(port, n % 2 == 0 ? Vector2.down : CombatVisuals.Aim(port));
            yield return Wait(Rest(.4f));
        }
    }

    // Two nested fans open below the interceptor, dark arrows outside and bright circles
    // inside. They brake into arcs, then every round turns back on a hairpin, crossing its
    // mirror twin on the way up, and rebounds once off the ceiling.
    void HairpinFan(Vector3 port, Vector2 centre)
    {
        int lanes = Dense(11);
        float step = 100f / Mathf.Max(1, lanes - 1);
        for (int i = 0; i < lanes; i++)
        {
            float angle = (i - (lanes - 1) * .5f) * step;
            float bend = angle > .01f ? 155 : angle < -.01f ? -155 : 180;
            UseTone(HostileTone.Dark);
            Fire<NeedleRound>(port, CombatVisuals.Rotate(centre, angle), 3.6f, .19f)
                .Pause(.7f, .15f, CombatRound.Turn.Rotate, bend, 1.15f)
                .Bounce(1);
            UseTone(HostileTone.Bright);
            Fire<SimpleRound>(port, CombatVisuals.Rotate(centre, angle * .9f), 2.5f, .19f)
                .Pause(.7f, .15f, CombatRound.Turn.Rotate, bend, 1.15f)
                .Bounce(1);
        }
    }

    // Skid Marks (interceptor survivor): both wings lay stalled beads along the weaving
    // flight path. Each bead waits, then launches at the pilot in the order it was laid,
    // arrows fast and circles slow along the same aims. A kunai fan freezes and snaps
    // onto the pilot every so often.
    IEnumerator SkidMarks(float seconds)
    {
        const float laid = .4f;
        int n = 0;
        for (float t = 0; t < seconds; t += .065f, n++)
        {
            bool fast = n % 2 == 0;
            UseTone(HostileTone.Bright);
            foreach (int port in new[] { 1, 2 })
                ScoredShot(!fast, Muzzle(port), Vector2.down, laid, .18f)
                    .Pause(.15f, 1.3f, CombatRound.Turn.Aim, 0, (fast ? 4.4f : 2.8f) / laid);
            if (n % 12 == 0)
                KunaiSnap();
            yield return Wait(Rest(.065f));
        }
    }

    // A fan of kunai brakes below the interceptor and snaps onto the pilot.
    void KunaiSnap()
    {
        UseTone(HostileTone.Dark);
        int count = Dense(7);
        for (int i = 0; i < count; i++)
            Fire<KunaiRound>(Muzzle(0), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 12), 3.4f, .19f)
                .Pause(.5f, .2f, CombatRound.Turn.Aim, 0, 1.2f);
    }

    // Total Gridlock (hauler, Hard final): a parked street and hanging containers together.
    // The crates hang above the street while it waits for green; the street skids off and
    // the crates drop through it.
    IEnumerator TotalGridlock(float seconds)
    {
        int cycle = 0;
        for (float t = 0; t < seconds; t += 2.3f)
        {
            yield return ChargeWings(.3f);
            ParkStreet(Muzzle(1), 5, cycle++, HostileTone.Dark, Rest(1.1f));
            HoistCrates(4, HostileTone.Bright, Rest(.6f));
            yield return Wait(Rest(.8f));
            HighBeams(HostileTone.Bright);
            yield return Wait(Rest(.6f));
            HighBeams(HostileTone.Bright);
            yield return RestAfter(.9f, .3f);
        }
    }

    // Spaghetti Junction (interceptor, Hard final): hairpin fans whose centre swings a little
    // further each volley, over the skid-mark trail.
    IEnumerator SpaghettiJunction(float seconds)
    {
        Phrase(SkidMarks(seconds));
        int n = 0;
        for (float t = 0; t < seconds; t += .45f, n++)
        {
            HairpinFan(Muzzle(0), CombatVisuals.Rotate(Vector2.down, 35 * Mathf.Sin(n * .6f)));
            yield return Wait(Rest(.45f));
        }
    }
}
