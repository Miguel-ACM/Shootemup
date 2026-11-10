using System.Collections;
using UnityEngine;

// Proposed card book (ArtSource/BossPhaseProposals/Ideas/VoidSovereignBoss.md),
// flown only while DistrictBoss.ProposedCards is set so it can be recorded beside
// the shipping Cards(). Teleports, primes and the fifteen-second one-sided
// interludes are unchanged; only the form cards are new. Every round leaves the
// form's visible eyes. Tones flip card to card and form to form:
//   Weeping Mask dark, Ringed Horns bright; Mantis Strike bright, Praying Mantis
//   dark; Mandible Bite dark, Gnashing Rows bright; Prism Cross bright, Shattered
//   Lattice dark. Each card's giants and aimed replies take the other tone.
public sealed partial class VoidSovereignBoss
{
    // Seconds a flung tooth or lattice kunai takes to brake onto its exact place.
    const float ToothFlight = .9f;
    const float LatticeFlight = .8f;

    protected override IEnumerator Proposed()
    {
        while (true)
        {
            CardIndex = 0;
            yield return Teleport();
            yield return Prime(.65f);
            if (Form == 1)
                yield return WeepingMask();
            else if (Form == 2)
                yield return MantisStrike();
            else if (Form == 3)
                yield return MandibleBite();
            else
                yield return PrismCross();
            yield return Wait(Rest(1.15f));
            CardIndex = 1;
            yield return Teleport();
            yield return Prime(.65f);
            if (Form == 1)
                yield return RingedHorns();
            else if (Form == 2)
                yield return PrayingMantis();
            else if (Form == 3)
                yield return GnashingRows();
            else
                yield return ShatteredLattice();
            yield return Wait(Rest(1.15f));
        }
    }

    // Weeping Mask: each eye wells a fountain of kunai that rises toward the top edge
    // and arcs down under gravity; inner lanes cross over the mask, outer lanes fold
    // off the side walls. A slit moves through each fountain. Slow giant tears fall
    // alone down the empty middle, and gem pairs answer a resting pilot.
    IEnumerator WeepingMask() => Every(6.2f, .34f, beat =>
    {
        if (beat % 7 == 6)
            return;
        if (beat % 3 == 0)
        {
            UseTone(HostileTone.Bright);
            for (int eye = 0; eye < 2; eye++)
                Fire<GemRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.7f, .22f).FadeAt(6);
        }
        int count = Dense(11);
        float breath = 9 * Mathf.Sin(beat * .4f);
        UseTone(HostileTone.Dark);
        for (int side = -1; side <= 1; side += 2)
        {
            var port = Port(side < 0 ? 0 : 1);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                if (OpenSeam(u, beat, .2f))
                    continue;
                // Lean outward from straight up: negative leans cross over the mask.
                float lean = 22 + 40 * u + breath;
                Fire<KunaiRound>(port, CombatVisuals.Rotate(Vector2.up, -side * lean), 2.5f + .45f * (1 - u * u), .17f)
                    .Fall(new Vector2(0, -1.0f))
                    .Bounce(1, false)
                    .FadeAt(7.5f);
            }
        }
        if (beat % 4 == 2)
        {
            UseTone(HostileTone.Bright);
            Fire<GiantOrb>(Port(beat / 4 % 2), Vector2.down, 1.15f, .62f).Ramp(1.5f, 1.6f, 1);
        }
    });

    // Ringed Horns: each eye draws a horn, one stream sweeping from straight up over
    // the outside to straight down and back, upper lanes fast and lower lanes slow so
    // the line curls; every fifth step a crescent pair rings it. Down-strokes end in
    // one lone giant tip with the stream beside it left empty; up-strokes end in an
    // aimed gem reply.
    IEnumerator RingedHorns()
    {
        for (int stroke = 0; stroke < 4; stroke++)
        {
            int steps = Mathf.Max(4, Dense(20));
            bool down = stroke % 2 == 0;
            int tipSide = stroke / 2 % 2 == 0 ? -1 : 1;
            for (int step = 0; step < steps; step++)
            {
                float t = step / (steps - 1f);
                float arc = 180 * (down ? t : 1 - t);
                for (int side = -1; side <= 1; side += 2)
                {
                    // The tip's lane stays empty around the giant that ends this horn.
                    if (down && side == tipSide && arc > 158)
                        continue;
                    var port = Port(side < 0 ? 0 : 1);
                    var axis = CombatVisuals.Rotate(Vector2.up, -side * arc);
                    float speed = 2.0f + 1.1f * (1 - arc / 180);
                    UseTone(HostileTone.Bright);
                    CrownedShot(false, step % 3 == 0, port, axis, speed, .17f, .6f, step % 3 == 1)
                        .Ramp(1.5f, 1.2f, 1.1f)
                        .FadeAt(6.5f);
                    if (step % 5 == 2)
                    {
                        UseTone(HostileTone.Dark);
                        for (int ring = -1; ring <= 1; ring += 2)
                            Fire<CrescentRound>(port, CombatVisuals.Rotate(axis, ring * 8), speed * .85f, .2f).FadeAt(6.5f);
                    }
                }
                yield return Wait(Rest(1.25f / steps));
            }
            UseTone(HostileTone.Dark);
            if (down)
            {
                var port = Port(tipSide < 0 ? 0 : 1);
                Fire<GiantOrb>(port, CombatVisuals.Rotate(Vector2.down, -tipSide * 12), 1.3f, .62f).Ramp(1.4f, 1.4f, .8f);
            }
            else
                for (int eye = 0; eye < 2; eye++)
                    Fire<GemRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.8f, .22f).FadeAt(6);
            yield return Wait(Rest(.55f));
        }
    }

    // Mantis Strike: the scythe eyes throw curved blades of kunai, bowed forward
    // because each blade's middle flies fastest. A blade halts, swings through forty
    // degrees and carries on as the same curve. The arms alternate, crossing the
    // arena; every third strike both arms hurl ricochet blades up off the ceiling.
    // Every sixth blade ends in a lone giant tip; crescent replies answer a rest.
    IEnumerator MantisStrike() => Every(5.6f, .32f, beat =>
    {
        if (beat % 8 == 7)
            return;
        if (beat % 4 == 0)
        {
            UseTone(HostileTone.Dark);
            for (int eye = 0; eye < 2; eye++)
                Fire<CrescentRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.9f, .22f);
        }
        int count = Dense(9);
        bool upward = beat % 3 == 2;
        for (int side = -1; side <= 1; side += 2)
        {
            if (!upward && (side < 0) != (beat % 2 == 0))
                continue;
            var port = Port(side < 0 ? 0 : 1);
            // Downward strikes cut across the middle; upward blades lean out to the ceiling.
            var axis = upward
                ? CombatVisuals.Rotate(Vector2.up, -side * 28)
                : CombatVisuals.Rotate(Vector2.down, -side * (30 + 8 * Mathf.Sin(beat * .5f)));
            bool crown = !upward && beat % 6 == 0;
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                if (CrownSpacer(i, count, crown))
                    continue;
                var direction = CombatVisuals.Rotate(axis, u * 26);
                float speed = 2.0f + 1.1f * (1 - u * u);
                if (upward)
                {
                    UseTone(HostileTone.Bright);
                    Fire<RicochetBolt>(port, direction, speed, .19f).Bounce(1).FadeAt(6.5f);
                    continue;
                }
                bool tip = crown && i == count - 1;
                UseTone(tip ? HostileTone.Dark : HostileTone.Bright);
                var blade = tip ? (CombatRound)Fire<GiantOrb>(port, direction, speed * .9f, .6f) : Fire<KunaiRound>(port, direction, speed, .17f);
                blade.Pause(.85f, .22f, CombatRound.Turn.Rotate, side * 40, 1.15f).FadeAt(6.5f);
            }
        }
    });

    // Praying Mantis: the forelegs are two rays held down from the scythe eyes that
    // open outward and fold back. The wings open upward as fans of chevrons and
    // needles that reach the ceiling or walls and run along them into the corners and
    // down the sides. Between the forelegs pearls drip and sway; crescents reply.
    IEnumerator PrayingMantis()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            var ray = VoidRay(side < 0 ? 0 : 1, CombatVisuals.Rotate(Vector2.down, side * 3), .5f, 3.4f, side * 12);
            if (ray != null)
                ray.reversalArc = 22;
        }
        yield return Every(5.4f, .62f, beat =>
        {
            if (beat % 5 == 4)
                return;
            if (beat % 3 == 0)
            {
                UseTone(HostileTone.Bright);
                for (int eye = 0; eye < 2; eye++)
                    Fire<CrescentRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.8f, .22f);
            }
            int count = Dense(6);
            float flutter = 8 * Mathf.Sin(beat * .6f);
            UseTone(HostileTone.Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Port(side < 0 ? 0 : 1);
                for (int i = 0; i < count; i++)
                {
                    float u = Across(i, count);
                    var axis = CombatVisuals.Rotate(Vector2.up, -side * (14 + 26 * u + flutter));
                    float speed = 3.0f + .4f * u;
                    var wing = i % 2 == 0 ? (CombatRound)Fire<ChevronRound>(port, axis, speed, .18f) : Fire<NeedleRound>(port, axis, speed, .17f);
                    // Left wing folds anticlockwise along the top, the right wing clockwise.
                    wing.RunWalls(2.4f, side < 0 ? -1 : 1);
                }
                for (int drop = 0; drop < 2; drop++)
                    Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, -side * (10 + 12 * drop)), 1.6f + .3f * drop, .21f)
                        .Sway(.25f, .5f)
                        .FadeAt(7);
            }
        });
    }

    // Mandible Bite: each mandible eye throws a sideways jaw of kunai that halts and
    // snaps inward, the two jaws closing across each other below the maw; alternate
    // bites open upward above the maw and snap down. Lone giant swallowed stars leave
    // alternate mandibles outside the jaws; crescent replies answer a rest.
    IEnumerator MandibleBite() => Every(6.2f, .5f, beat =>
    {
        if (beat % 5 == 4)
            return;
        if (beat % 3 == 0)
        {
            UseTone(HostileTone.Bright);
            for (int eye = 0; eye < 2; eye++)
                Fire<CrescentRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.8f, .22f);
        }
        int count = Dense(11);
        bool high = beat % 2 == 1;
        bool giant = beat % 4 == 3;
        // Degrees from straight down, outward; the snap turns each tooth back inward.
        float centre = high ? 132 : 88;
        float snap = high ? 150 : 118;
        UseTone(HostileTone.Dark);
        for (int side = -1; side <= 1; side += 2)
        {
            var port = Port(side < 0 ? 0 : 1);
            int giantSide = beat / 4 % 2 == 0 ? -1 : 1;
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                if (OpenSeam(u, beat, .18f))
                    continue;
                // The giant's own mandible leaves its outermost tooth out.
                if (giant && side == giantSide && i == count - 1)
                    continue;
                var axis = CombatVisuals.Rotate(Vector2.down, side * (centre + 30 * u));
                Fire<KunaiRound>(port, axis, (high ? 3.4f : 2.3f) + .5f * Mathf.Abs(u), .17f)
                    .Pause(.62f, .28f, CombatRound.Turn.Rotate, -side * snap, 1.1f)
                    .FadeAt(6.5f);
            }
            if (giant && side == giantSide)
            {
                UseTone(HostileTone.Bright);
                Fire<GiantOrb>(port, CombatVisuals.Rotate(Vector2.down, side * 20), 1.2f, .66f).Ramp(1.4f, 1.5f, 1);
                UseTone(HostileTone.Dark);
            }
        }
    });

    // Gnashing Rows: the mandibles fling teeth to exact places, building three
    // straight rows across the upper arena. Each row hangs, then falls with its teeth
    // alternating fast and slow, leaving a gap where that row's own seam sits. A lone
    // giant ends one row in three; crescent replies answer a rest.
    IEnumerator GnashingRows() => Every(6.2f, .9f, beat =>
    {
        if (beat % 5 == 4)
            return;
        Rect r = CombatRound.Border;
        if (beat % 2 == 0)
        {
            UseTone(HostileTone.Dark);
            for (int eye = 0; eye < 2; eye++)
                Fire<CrescentRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.7f, .22f);
        }
        int count = Dense(18);
        for (int row = 0; row < 3; row++)
        {
            float y = r.yMax - .8f - row * 1.9f;
            float seam = r.center.x + (r.width * .5f - 1.4f) * Mathf.Sin(beat * .9f + row * 2.1f);
            bool crown = (beat + row) % 3 == 0;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(r.xMin + .45f, r.xMax - .45f, count <= 1 ? .5f : i / (count - 1f));
                if (Mathf.Abs(x - seam) < .75f || CrownSpacer(i, count, crown))
                    continue;
                var target = new Vector3(x, y, 0);
                if (player != null && Vector2.Distance(target, player.position) < 1.3f)
                    continue;
                var port = Port(x < transform.position.x ? 0 : 1);
                Vector2 path = target - port;
                // Launch speed in world units that brakes to a stop exactly on the place.
                float raw = Mathf.Max(.3f, path.magnitude / (ToothFlight - .175f));
                float launch = WorldSpeed(raw);
                Vector2 heading = path.sqrMagnitude > .0001f ? path.normalized : Vector2.down;
                bool tip = crown && i == count - 1;
                UseTone(tip ? HostileTone.Dark : HostileTone.Bright);
                CombatRound tooth = tip ? Fire<GiantOrb>(port, heading, launch, .6f)
                    : i % 2 == 0 ? (CombatRound)Fire<GemRound>(port, heading, launch, .2f)
                    : Fire<SimpleRound>(port, heading, launch, .2f);
                tooth.Pause(ToothFlight, .4f, CombatRound.Turn.Down, 0, (i % 2 == 0 ? 2.1f : 1.45f) / launch).FadeAt(9);
            }
        }
    });

    // Prism Cross: the four star points hold rays along their own axes, a cross that
    // turns slowly so every quadrant drifts. Each point refracts a gem fan into the
    // quadrant anticlockwise of it; the gems halt and bend twenty degrees with the
    // turn. A lone giant replaces the lower point's fan now and then, and the side
    // tips cross charged aims through the pilot to close the body's shadow.
    IEnumerator PrismCross()
    {
        for (int port = 0; port < 4; port++)
            VoidRay(port, PointAxis(port), .42f, 3.0f, 9);
        yield return Every(6.2f, .56f, beat =>
        {
            if (beat % 6 == 5)
                return;
            if (beat % 4 == 0)
                Phrase(CrossedGaze(HostileTone.Dark));
            int count = Dense(4);
            // Follow the rays' slow turn so each fan stays inside its quadrant.
            float turn = Mathf.Clamp(beat * Rest(.56f) - Difficulty.Rest(1.25f), 0, 3.0f) * 9;
            bool giant = beat % 4 == 2;
            for (int port = 0; port < 4; port++)
            {
                var bisector = CombatVisuals.Rotate(PointAxis(port), 45 + turn);
                if (giant && port == 3)
                {
                    UseTone(HostileTone.Dark);
                    Fire<GiantOrb>(Port(port), bisector, 1.3f, .66f).Ramp(1.4f, 1.4f, .9f);
                    continue;
                }
                UseTone(HostileTone.Bright);
                for (int i = 0; i < count; i++)
                {
                    float u = Across(i, count);
                    Fire<GemRound>(Port(port), CombatVisuals.Rotate(bisector, u * 16), 2.2f + .3f * (1 - u * u), .2f)
                        .Pause(.7f, .25f, CombatRound.Turn.Rotate, 20, 1.1f)
                        .FadeAt(6.5f);
                }
            }
        });
    }

    // Shattered Lattice: the side points lay horizontal bars of kunai to the walls,
    // the top and bottom points vertical bars to the ceiling and floor. Each bar hangs
    // as a crystal seam, then shatters: alternate teeth turn ninety degrees each way,
    // so the bar opens into two dotted walls travelling apart. Every bar keeps an open
    // slit; on every other side-bar beat a lone giant ends both bars at the walls;
    // crossfire closes the body's shadow.
    IEnumerator ShatteredLattice() => Every(6.2f, .85f, beat =>
    {
        if (beat % 5 == 4)
            return;
        if (beat % 3 == 0)
            Phrase(CrossedGaze(HostileTone.Bright));
        bool level = beat % 2 == 0;
        int count = Dense(10);
        Rect r = CombatRound.Border;
        for (int k = 0; k < 2; k++)
        {
            int port = level ? k : 2 + k;
            Vector2 axis = PointAxis(port);
            Vector3 from = Port(port);
            float room = axis.x < 0 ? from.x - r.xMin : axis.x > 0 ? r.xMax - from.x : axis.y > 0 ? r.yMax - from.y : from.y - r.yMin;
            // A short bar (toward the ceiling, or a side wall) keeps its teeth at least .38 apart.
            int teeth = Mathf.Min(count, Mathf.FloorToInt((room - .3f) / .38f));
            if (teeth < 3)
                continue;
            float spacing = (room - .3f) / teeth;
            int seam = teeth >= 6 ? (beat * 3 + k * 5) % teeth : -2;
            bool crown = level && beat % 4 == 0 && teeth >= 5;
            for (int i = 0; i < teeth; i++)
            {
                if (i == seam || i == seam + 1 || CrownSpacer(i, teeth, crown))
                    continue;
                float distance = spacing * (i + 1);
                float raw = distance / (LatticeFlight - .175f);
                float launch = WorldSpeed(raw);
                bool tip = crown && i == teeth - 1;
                float travel = i % 4 < 2 ? 1.7f : 1.25f;
                UseTone(tip ? HostileTone.Bright : HostileTone.Dark);
                var tooth = tip ? (CombatRound)Fire<GiantOrb>(from, axis, launch, .6f) : Fire<KunaiRound>(from, axis, launch, .17f);
                tooth.Pause(LatticeFlight, .38f, CombatRound.Turn.Rotate, i % 2 == 0 ? 90 : -90, travel / launch).FadeAt(8);
            }
        }
    });

    // The star's points: left, right, top and bottom, matching LocalPort in form four.
    static Vector2 PointAxis(int port) => port == 0 ? Vector2.left : port == 1 ? Vector2.right : port == 2 ? Vector2.up : Vector2.down;
}
