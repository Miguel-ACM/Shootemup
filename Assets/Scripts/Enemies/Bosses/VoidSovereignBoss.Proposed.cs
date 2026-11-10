using System.Collections;
using UnityEngine;

// Proposed card book (ArtSource/BossPhaseProposals/Ideas/VoidSovereignBoss.md),
// flown only while DistrictBoss.ProposedCards is set so it can be recorded beside
// the shipping Cards(). One concept per form, developed in movements; the teleport
// before each movement, the prime and the fifteen-second one-sided interludes are
// unchanged. The movement number chooses the variation, and every completed cycle
// of variations escalates. Every round leaves the form's visible eyes.
//   Form one: Weeping Mask. Form two: Praying Mantis. Form three: Gnashing Maw.
//   Form four: Prism Lattice.
// Tones: each form leads with one tone (mask dark, mantis bright, maw dark, star
// bright) and flips it movement by movement; giants and aimed replies take the other.
public sealed partial class VoidSovereignBoss
{
    // Seconds a flung tooth or lattice kunai takes to brake onto its exact place.
    const float ToothFlight = .9f;
    const float LatticeFlight = .8f;

    protected override IEnumerator Proposed()
    {
        // The book restarts with each form, so every concept opens on its first movement.
        for (int movement = 0; ; movement++)
        {
            CardIndex = movement % 2;
            yield return Teleport();
            yield return Prime(.65f);
            if (Form == 1)
                yield return WeepingMask(movement);
            else if (Form == 2)
                yield return PrayingMantis(movement);
            else if (Form == 3)
                yield return GnashingMaw(movement);
            else
                yield return PrismLattice(movement);
            yield return Wait(Rest(1.15f));
        }
    }

    // The movement's lead tone: the form's own on even movements, the other on odd ones.
    HostileTone Lead(HostileTone form) => CardIndex == 0 ? form : Other(form);

    // Later cycles of a concept run a little faster, up to fifteen percent.
    static float Quicken(int movement, int verses) => 1 - .05f * Mathf.Min(movement / verses, 3);

    // Weeping Mask (form one): the mask weeps fountains of kunai that rise toward the top
    // edge and arc down under gravity. It wells (inner lanes only, crossing over the
    // mask), weeps (full fountains, outer lanes folding off the walls, lone giant tears
    // down the empty middle) and sobs (the eyes take turns in doubled pulses). A slit
    // moves through every fountain; gem pairs answer a resting pilot. Each later cycle
    // swings the breath wider and narrows the slit.
    IEnumerator WeepingMask(int movement)
    {
        int verse = movement % 3;
        int cycle = Mathf.Min(movement / 3, 3);
        var body = Lead(HostileTone.Dark);
        float swing = 9 + 4 * cycle;
        float slit = .22f - .025f * cycle;
        yield return Every(5.6f, .34f * Quicken(movement, 3), beat =>
        {
            if (beat % 7 == 6)
                return;
            if (beat % 3 == 0)
            {
                UseTone(Other(body));
                for (int eye = 0; eye < 2; eye++)
                    Fire<GemRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.7f, .22f).FadeAt(6);
            }
            float breath = swing * Mathf.Sin(beat * .4f);
            UseTone(body);
            if (verse == 0)
            {
                for (int eye = 0; eye < 2; eye++)
                    Fountain(eye, Dense(9), -18, 30, breath, slit, beat, 2.5f);
            }
            else if (verse == 1)
            {
                for (int eye = 0; eye < 2; eye++)
                    Fountain(eye, Dense(11), -18, 62, breath, slit, beat, 2.5f);
                if (beat % 4 == 2)
                    Tear(beat / 4 % 2, body);
            }
            else
            {
                int eye = beat / 3 % 2;
                Fountain(eye, Dense(14), -18, 62, breath, slit, beat, 2.5f);
                Fountain(eye, Dense(14), -10, 54, -breath, slit, beat + 3, 2.0f);
                if (beat % 6 == 4)
                    Tear(1 - eye, body);
            }
        });
    }

    // One fountain from one eye: lanes lean outward from straight up (negative leans cross
    // over the mask), fall under gravity and fold once off a side wall, never the ceiling.
    void Fountain(int eye, int count, float inner, float outer, float breath, float slit, int beat, float speed)
    {
        int side = eye == 0 ? -1 : 1;
        var port = Port(eye);
        for (int i = 0; i < count; i++)
        {
            float u = Across(i, count);
            if (OpenSeam(u, beat, slit))
                continue;
            float lean = Mathf.Lerp(inner, outer, (u + 1) * .5f) + breath;
            Fire<KunaiRound>(port, CombatVisuals.Rotate(Vector2.up, -side * lean), speed + .45f * (1 - u * u), .17f)
                .Fall(new Vector2(0, -1.0f))
                .Bounce(1, false)
                .FadeAt(7.5f);
        }
    }

    // A lone slow giant tear straight down the fountains' empty middle.
    void Tear(int eye, HostileTone body)
    {
        UseTone(Other(body));
        Fire<GiantOrb>(Port(eye), Vector2.down, 1.15f, .62f).Ramp(1.5f, 1.6f, 1);
        UseTone(body);
    }

    // Praying Mantis (form two): the mantis hunts. It stalks (forelegs held down as rays
    // that open and fold, wings running the ceiling and walls, pearls dripping between
    // the forelegs), strikes (hooked kunai blades from alternate arms), strikes from
    // above (ricochet blades off the ceiling between low cuts) and feeds (forelegs,
    // crossed blades from both arms and the wings together). Crescents answer a rest;
    // each later cycle cuts faster.
    IEnumerator PrayingMantis(int movement)
    {
        int verse = movement % 4;
        var body = Lead(HostileTone.Bright);
        if (verse == 0 || verse == 3)
            Forelegs();
        float interval = (verse == 0 ? .62f : verse == 3 ? .5f : .32f) * Quicken(movement, 4);
        yield return Every(5.4f, interval, beat =>
        {
            if (verse == 0 ? beat % 5 == 4 : beat % 8 == 7)
                return;
            if (beat % 4 == 0)
            {
                UseTone(Other(body));
                for (int eye = 0; eye < 2; eye++)
                    Fire<CrescentRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.9f, .22f);
            }
            UseTone(body);
            if (verse == 0)
            {
                Wings(Dense(7), beat);
                Drips();
            }
            else if (verse == 1)
                Blade(beat % 2 == 0 ? -1 : 1, Dense(9), beat, body);
            else if (verse == 2)
            {
                if (beat % 3 == 2)
                    for (int side = -1; side <= 1; side += 2)
                        RicochetBlade(side, Dense(9));
                else
                    Blade(beat % 2 == 0 ? -1 : 1, Dense(9), beat, body);
            }
            else
            {
                if (beat % 2 == 0)
                    for (int side = -1; side <= 1; side += 2)
                        Blade(side, Dense(9), beat, body);
                if (beat % 3 == 1)
                    Wings(Dense(6), beat);
            }
        });
    }

    // The forelegs: rays held almost straight down from the scythe eyes that open outward
    // and fold back, never closing the aisle between them.
    void Forelegs()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            var ray = VoidRay(side < 0 ? 0 : 1, CombatVisuals.Rotate(Vector2.down, side * 3), .5f, 3.4f, side * 12);
            if (ray != null)
                ray.reversalArc = 22;
        }
    }

    // A curved blade from one scythe eye, bowed forward because its middle flies fastest.
    // It halts, swings through forty degrees and carries on as the same curve. Every
    // sixth beat a lone giant takes the outer tip, with the lanes beside it empty.
    void Blade(int side, int count, int beat, HostileTone body)
    {
        var port = Port(side < 0 ? 0 : 1);
        var axis = CombatVisuals.Rotate(Vector2.down, -side * (30 + 8 * Mathf.Sin(beat * .5f)));
        bool crown = beat % 6 == 0;
        for (int i = 0; i < count; i++)
        {
            if (CrownSpacer(i, count, crown))
                continue;
            float u = Across(i, count);
            var direction = CombatVisuals.Rotate(axis, u * 26);
            float speed = 2.0f + 1.1f * (1 - u * u);
            bool tip = crown && i == count - 1;
            UseTone(tip ? Other(body) : body);
            var blade = tip ? (CombatRound)Fire<GiantOrb>(port, direction, speed * .9f, .6f) : Fire<KunaiRound>(port, direction, speed, .17f);
            blade.Pause(.85f, .22f, CombatRound.Turn.Rotate, side * 40, 1.15f).FadeAt(6.5f);
        }
        UseTone(body);
    }

    // A blade thrown up and outward as ricochets that come back off the ceiling.
    void RicochetBlade(int side, int count)
    {
        var port = Port(side < 0 ? 0 : 1);
        var axis = CombatVisuals.Rotate(Vector2.up, -side * 28);
        for (int i = 0; i < count; i++)
        {
            float u = Across(i, count);
            Fire<RicochetBolt>(port, CombatVisuals.Rotate(axis, u * 26), 2.0f + 1.1f * (1 - u * u), .19f).Bounce(1).FadeAt(6.5f);
        }
    }

    // The wings open upward as chevrons and needles that reach the ceiling or a wall and
    // run along it into the corners and down the sides: the left wing anticlockwise,
    // the right wing clockwise.
    void Wings(int count, int beat)
    {
        float flutter = 8 * Mathf.Sin(beat * .6f);
        for (int side = -1; side <= 1; side += 2)
        {
            var port = Port(side < 0 ? 0 : 1);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                var axis = CombatVisuals.Rotate(Vector2.up, -side * (14 + 26 * u + flutter));
                float speed = 3.0f + .4f * u;
                var wing = i % 2 == 0 ? (CombatRound)Fire<ChevronRound>(port, axis, speed, .18f) : Fire<NeedleRound>(port, axis, speed, .17f);
                wing.RunWalls(2.4f, side < 0 ? -1 : 1);
            }
        }
    }

    // Pearls drip and sway between the forelegs.
    void Drips()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            var port = Port(side < 0 ? 0 : 1);
            for (int drop = 0; drop < 2; drop++)
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, -side * (10 + 12 * drop)), 1.6f + .3f * drop, .21f)
                    .Sway(.25f, .5f)
                    .FadeAt(7);
        }
    }

    // Gnashing Maw (form three): the mandibles bite and set teeth. Low bites throw a
    // sideways jaw that halts and snaps inward below the maw; high bites open over the
    // maw and snap down from near the top; then the maw sets three rows of teeth across
    // the upper arena that hang and fall jagged, each with its own moving gap; finally it
    // gnashes, rows and bites in turn. Crescents answer a rest; later cycles rest less.
    IEnumerator GnashingMaw(int movement)
    {
        int verse = movement % 4;
        var body = Lead(HostileTone.Dark);
        int restEvery = movement / 4 >= 1 ? 6 : 5;
        float interval = (verse == 2 ? .9f : verse == 3 ? .45f : .4f) * Quicken(movement, 4);
        yield return Every(5.6f, interval, beat =>
        {
            if (beat % restEvery == restEvery - 1)
                return;
            if (beat % 3 == 0)
            {
                UseTone(Other(body));
                for (int eye = 0; eye < 2; eye++)
                    Fire<CrescentRound>(Port(eye), CombatVisuals.Aim(Port(eye)), 2.8f, .22f);
            }
            if (verse == 0)
                Bite(false, Dense(13), beat, body);
            else if (verse == 1)
                Bite(true, Dense(13), beat, body);
            else if (verse == 2)
                Rows(Dense(18), beat, body);
            else if (beat % 3 == 0)
                Rows(Dense(18), beat, body);
            else
                Bite(beat % 2 == 1, Dense(13), beat, body);
        });
    }

    // Each mandible eye throws a jaw of kunai sideways (or up and out) that halts and
    // snaps back inward, the two jaws closing across each other. Every fourth bite a
    // lone giant leaves one mandible outside its jaw, which drops its outer tooth.
    void Bite(bool high, int count, int beat, HostileTone body)
    {
        bool giant = beat % 4 == 3;
        int giantSide = beat / 4 % 2 == 0 ? -1 : 1;
        // Degrees from straight down, outward; the snap turns each tooth back inward.
        float centre = high ? 132 : 88;
        float snap = high ? 150 : 118;
        UseTone(body);
        for (int side = -1; side <= 1; side += 2)
        {
            var port = Port(side < 0 ? 0 : 1);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                if (OpenSeam(u, beat, .18f))
                    continue;
                if (giant && side == giantSide && i == count - 1)
                    continue;
                var axis = CombatVisuals.Rotate(Vector2.down, side * (centre + 30 * u));
                Fire<KunaiRound>(port, axis, (high ? 3.4f : 2.3f) + .5f * Mathf.Abs(u), .17f)
                    .Pause(.62f, .28f, CombatRound.Turn.Rotate, -side * snap, 1.1f)
                    .FadeAt(6.5f);
            }
            if (giant && side == giantSide)
            {
                UseTone(Other(body));
                Fire<GiantOrb>(port, CombatVisuals.Rotate(Vector2.down, side * 20), 1.2f, .66f).Ramp(1.4f, 1.5f, 1);
                UseTone(body);
            }
        }
    }

    // Three rows of teeth flung to exact places across the upper arena. Each row hangs,
    // then falls with teeth alternating fast and slow, leaving a gap at its own seam.
    // A lone giant ends one row in three; no tooth lands on the pilot.
    void Rows(int count, int beat, HostileTone body)
    {
        Rect r = CombatRound.Border;
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
                UseTone(tip ? Other(body) : body);
                CombatRound tooth = tip ? Fire<GiantOrb>(port, heading, launch, .6f)
                    : i % 2 == 0 ? (CombatRound)Fire<GemRound>(port, heading, launch, .2f)
                    : Fire<SimpleRound>(port, heading, launch, .2f);
                tooth.Pause(ToothFlight, .4f, CombatRound.Turn.Down, 0, (i % 2 == 0 ? 2.1f : 1.45f) / launch).FadeAt(9);
            }
        }
        UseTone(body);
    }

    // Prism Lattice (form four): the crystal star refracts and shatters. Its four rays
    // turn as a slow cross while the points refract gem fans into the quadrants; then the
    // points lay crystal bars that shatter into travelling dotted walls; then the cross
    // turns back and sheds bars from beneath its own rays; finally the whole star
    // shatters at once. Charged crossfire from the side tips closes the body's shadow
    // throughout; later cycles quicken.
    IEnumerator PrismLattice(int movement)
    {
        int verse = movement % 4;
        var body = Lead(HostileTone.Bright);
        float turnRate = verse == 0 ? 9 : verse == 2 ? -9 : 0;
        if (turnRate != 0)
            for (int port = 0; port < 4; port++)
                VoidRay(port, PointAxis(port), .42f, 3.0f, turnRate);
        float interval = (verse == 0 ? .6f : verse == 3 ? 1.1f : .85f) * Quicken(movement, 4);
        yield return Every(5.4f, interval, beat =>
        {
            if (beat % 5 == 4)
                return;
            if (beat % 3 == 0)
                Phrase(CrossedGaze(Other(body)));
            if (verse == 0)
                Refract(beat, interval, turnRate, Dense(3), body);
            else if (verse == 3)
            {
                Shatter(true, beat, body);
                Shatter(false, beat, body);
            }
            else
                Shatter(beat % 2 == 0, beat, body);
        });
    }

    // Each star point refracts a gem fan into the quadrant anticlockwise of it, following
    // the cross's turn; the gems halt and bend twenty degrees with it. Now and then a lone
    // giant replaces the lower point's fan.
    void Refract(int beat, float interval, float turnRate, int count, HostileTone body)
    {
        float turn = Mathf.Clamp(beat * Rest(interval) - Difficulty.Rest(1.25f), 0, 3.0f) * turnRate;
        bool giant = beat % 4 == 2;
        for (int port = 0; port < 4; port++)
        {
            var bisector = CombatVisuals.Rotate(PointAxis(port), 45 + turn);
            if (giant && port == 3)
            {
                UseTone(Other(body));
                Fire<GiantOrb>(Port(port), bisector, 1.3f, .66f).Ramp(1.4f, 1.4f, .9f);
                continue;
            }
            UseTone(body);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                Fire<GemRound>(Port(port), CombatVisuals.Rotate(bisector, u * 16), 2.2f + .3f * (1 - u * u), .2f)
                    .Pause(.7f, .25f, CombatRound.Turn.Rotate, 20 * Mathf.Sign(turnRate), 1.1f)
                    .FadeAt(6.5f);
            }
        }
    }

    // Two star points lay crystal bars of kunai along their axes, the side points to the
    // walls (level) or the top and bottom points to the ceiling and floor. Each bar hangs,
    // then shatters: alternate teeth turn ninety degrees each way, opening the bar into
    // two dotted walls that travel apart. Every long bar keeps an open slit; on alternate
    // level beats a lone giant ends each side bar at the wall.
    void Shatter(bool level, int beat, HostileTone body)
    {
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
                float raw = spacing * (i + 1) / (LatticeFlight - .175f);
                float launch = WorldSpeed(raw);
                bool tip = crown && i == teeth - 1;
                float travel = i % 4 < 2 ? 1.7f : 1.25f;
                UseTone(tip ? Other(body) : body);
                var tooth = tip ? (CombatRound)Fire<GiantOrb>(from, axis, launch, .6f) : Fire<KunaiRound>(from, axis, launch, .17f);
                tooth.Pause(LatticeFlight, .38f, CombatRound.Turn.Rotate, i % 2 == 0 ? 90 : -90, travel / launch).FadeAt(8);
            }
        }
        UseTone(body);
    }

    // The star's points: left, right, top and bottom, matching LocalPort in form four.
    static Vector2 PointAxis(int port) => port == 0 ? Vector2.left : port == 1 ? Vector2.right : port == 2 ? Vector2.up : Vector2.down;
}
