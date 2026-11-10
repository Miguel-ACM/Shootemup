using System.Collections;
using UnityEngine;

// Proposed card book (ArtSource/BossPhaseProposals/Ideas/SeraphJudicatorBoss.md),
// flown only while DistrictBoss.ProposedCards is set so it can be recorded beside
// the shipping Cards(). One concept per phase, developed in movements: the movement
// number chooses the variation, and every completed cycle of variations escalates.
//   Phase one: Wheel Within a Wheel. Phase two: Six Wings. Phase three: Sea of Glass.
// Tones: the movement's body takes Body(CardIndex), its accents Accent(CardIndex), and
// CardIndex alternates movement by movement, so the dominant tone flips with each
// variation (phase one opens bright, phase two dark, phase three bright). Lances
// stay bright.
public sealed partial class SeraphJudicatorBoss
{
    readonly Vector3[] sites = new Vector3[6];
    const float GlassFlight = .8f;

    protected override IEnumerator Proposed()
    {
        // The book restarts with each phase, so every concept opens on its first movement.
        for (int movement = 0; ; movement++)
        {
            CardIndex = movement % 2;
            Pray(true);
            yield return Charge(Angel.Emitter(0), Vector3.zero, .55f);
            if (CurrentPhase == 1)
                yield return WheelWithinAWheel(movement);
            else if (CurrentPhase == 2)
                yield return SixWings(movement);
            else
                yield return SeaOfGlass(movement);
            Pray(false);
            // A longer breath closes each full cycle of variations.
            yield return Wait(Rest(movement % 4 == 3 ? 1.3f : .7f));
        }
    }

    // Wheel Within a Wheel (phase one): the eyes of the tumbling wheels are the weapon.
    // Each lash leaves an eye along the wheel's own projected outline, so the fans are
    // redrawn by the tumble, and one aisle sways through them. Movements: the middle
    // wheel lashes while the outer side eyes answer; the outer wheel takes it up, wider
    // and slower, over weeping pearls; the middle and inner wheels trade beats with
    // mirrored aisles; then every eye opens, the upper rims hanging rain along the top
    // edge and watchers closing the corners. Mid-movement the core eye lays a wide
    // glance along the pilot's line. Each later cycle sways the aisle faster and widens
    // the lashes.
    IEnumerator WheelWithinAWheel(int movement)
    {
        int verse = movement % 4;
        int cycle = Mathf.Min(movement / 4, 2);
        float sway = .17f * (1 + .25f * cycle);
        if (verse == 3)
            Phrase(CornerWatchers(CardIndex, 3));
        yield return Every(4.4f, .2f, beat =>
        {
            float aisle = 46 * Mathf.Sin(beat * sway + movement * 1.3f);
            if (beat == 9)
                Glance(1.0f, 1.8f, 1.3f);
            if (beat % 4 == 2)
                Gaze();
            UseTone(Body(CardIndex));
            if (verse == 0)
            {
                WheelLash(1, Dense(8 + cycle), 3, 1.35f, false, aisle);
                if (beat % 2 == 1)
                    RimFans(3, Dense(9), 30, 50, 2.0f, beat);
            }
            else if (verse == 1)
            {
                WheelLash(0, Dense(8 + cycle), 2.6f, 1.2f, false, aisle);
                UseTone(Accent(CardIndex));
                if (beat % 2 == 1)
                    WheelPearls(2, 1.2f);
            }
            else if (verse == 2)
            {
                bool middle = beat % 2 == 0;
                WheelLash(middle ? 1 : 2, Dense(10 + cycle), 3.5f, 1.4f, false, middle ? aisle : -aisle);
            }
            else
            {
                WheelLash(1, Dense(8 + cycle), 3, 1.35f, false, aisle);
                UseTone(Accent(CardIndex));
                if (beat % 3 == 0)
                    UpperRain(0, Dense(2));
            }
        });
    }

    // Six Wings (phase two): Isaiah's seraph, "with twain he covered his face, and with
    // twain he covered his feet, and with twain he did fly". Each movement opens one
    // pair of wings, and the fourth opens all six under the Holy chorus. Flight draws two
    // layered wings sideways that shed curtains down the sides, with a beat of rest in
    // every four as a passage; the feet are a two-layer skirt beneath the body that opens
    // and folds while the side eyes stare; the face is two lances held out to the walls
    // that beat, with raised wings hanging along the top edge. Each later cycle beats the
    // wings faster.
    IEnumerator SixWings(int movement)
    {
        int verse = movement % 4;
        float tempo = 1 + .2f * Mathf.Min(movement / 4, 3);
        if (verse == 0 || verse == 3)
            Phrase(Holy(CardIndex));
        if (verse >= 2)
            WingLances(tempo);
        yield return Every(4.4f, .2f, beat =>
        {
            float stroke = 7 * Mathf.Sin(beat * .45f * tempo);
            if (beat % 5 == 2)
                Gaze();
            UseTone(Body(CardIndex));
            if (verse == 0)
            {
                if (beat % 4 != 3)
                    MiddleWings(stroke, Dense(16), 2);
            }
            else if (verse == 1)
            {
                LowerWings(stroke, Dense(12), 2);
                if (beat % 2 == 1)
                    Stare(3, 5);
            }
            else if (verse == 2)
            {
                RaisedWings(stroke, Dense(14));
                LowerWings(stroke, Dense(6), 1);
            }
            else
            {
                if (beat % 4 != 3)
                    MiddleWings(stroke, Dense(16), 1);
                LowerWings(stroke, Dense(8), 1);
                if (beat % 3 == 0)
                    RaisedWings(stroke, Dense(10));
            }
        });
    }

    // Sea of Glass (phase three, Hard): "a sea of glass like unto crystal". Unbodied eyes
    // draw panes of pearls; every pearl flies to its exact place, the pane hangs, then
    // moves as one: sheets fall as a woven lattice whose channels shift half a step each
    // pane, columns slide sideways, and every pane keeps one open seam. Beneath, the
    // outer wheel's lashes stand for the waves around a moving aisle. Movements: a still
    // sea of falling sheets; sheets with the many eyes laying thin aimed lances through
    // them; the sea standing up as sliding columns; sheets and columns crossed with the
    // lances. Each later cycle moves the aisle faster.
    IEnumerator SeaOfGlass(int movement)
    {
        int verse = movement % 4;
        float drift = .2f + .04f * Mathf.Min(movement / 4, 3);
        bool eyes = verse == 1 || verse == 3;
        Phrase(Glass(verse, 5));
        yield return Every(4.4f, .2f, beat =>
        {
            if (beat % 5 == 0)
                Gaze();
            UseTone(Body(CardIndex));
            WheelLash(0, Dense(10), 2.4f, 1.5f, false, 40 * Mathf.Sin(beat * drift + movement));
            if (eyes && beat % 5 == 1)
                ManyEyes(Dense(10), beat);
            UseTone(Accent(CardIndex));
            if (beat % 2 == 0)
                WheelPearls(2, 1.15f);
            if (!eyes && beat % 4 == 3)
                Stare(3, 5);
        });
    }

    bool Near(Vector3 point) => player != null && Vector2.Distance(point, player.position) < 1.6f;

    // A living eye of one wheel: 0 outer, 1 middle, 2 inner.
    Transform WheelEye(int ring, int index)
    {
        var wheel = Angel.Wheel(ring);
        int count = wheel.childCount;
        return wheel.GetChild((index % count + count) % count);
    }

    // The eye's direction from the core on the wheel's projected, tumbling outline.
    Vector2 Outward(Transform eye)
    {
        Vector2 offset = eye.position - Angel.transform.position;
        return offset.sqrMagnitude > .0001f ? offset.normalized : Vector2.down;
    }

    // The eye of a wheel that currently looks most nearly along a direction.
    Transform EyeFacing(int ring, Vector2 direction)
    {
        var wheel = Angel.Wheel(ring);
        Transform best = wheel.GetChild(0);
        float score = float.NegativeInfinity;
        for (int i = 0; i < wheel.childCount; i++)
        {
            var eye = wheel.GetChild(i);
            float facing = Vector2.Dot(Outward(eye), direction);
            if (facing > score)
            {
                score = facing;
                best = eye;
            }
        }
        return best;
    }

    // The core eye looks: a lance laid along the pilot's line at the charge.
    void Glance(float width, float hold, float charge)
    {
        Vector2 aim = CombatVisuals.Aim(Eye(0));
        Beam(0, Vector2.SignedAngle(Vector2.down, aim), width, hold, charge);
    }

    // Each wheel speaks in its own material: outer feathers, middle feathers with
    // star beads, inner pearls.
    CombatRound RingShot(int ring, int lane, Vector3 port, Vector2 axis, float speed)
    {
        if (ring == 0)
            return Fire<FeatherRound>(port, axis, speed, .17f);
        if (ring == 1)
            return lane % 3 == 1 ? (CombatRound)Fire<StarRound>(port, axis, speed, .2f) : Fire<FeatherRound>(port, axis, speed, .17f);
        return Fire<SimpleRound>(port, axis, speed, .21f);
    }

    // Every lower eye of a wheel casts a small lash along its own outward line, except
    // the eyes facing the moving aisle (degrees from straight down). The upper rim
    // speaks only when asked, one slow round per eye toward the top edge, no bounce.
    void WheelLash(int ring, int lash, float spread, float speed, bool upper, float aisle)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            bool lower = axis.y < -.15f;
            if (lower && Mathf.Abs(Vector2.SignedAngle(Vector2.down, axis) - aisle) < 15)
                continue;
            if (!lower && !(upper && axis.y > .35f))
                continue;
            int count = lower ? lash : 1;
            // Eyes deeper below the core throw a little faster, bending each fan into a crescent.
            float pace = speed * (lower ? 1 - .3f * axis.y : .8f);
            for (int i = 0; i < count; i++)
                RingShot(ring, i, eye.position, CombatVisuals.Rotate(axis, Across(i, count) * spread), pace)
                    .Ramp(1.9f, .8f, .45f)
                    .FadeAt(lower ? 8 : 4.5f);
        }
    }

    // The upper rim: each upper eye sends a few feathers toward the top edge that halt
    // there and fall back as a light rain; nothing bounces along the ceiling.
    void UpperRain(int ring, int count)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (axis.y < .35f)
                continue;
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                Fire<FeatherRound>(eye.position, CombatVisuals.Rotate(axis, u * 6), 1.6f, .17f)
                    .Pause(.9f, .45f, CombatRound.Turn.Down, u * 10 - axis.x * 14, .75f)
                    .FadeAt(8);
            }
        }
    }

    // A wheel's lowest eyes weep pearls in two speeds.
    void WheelPearls(int ring, float speed)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (axis.y > -.45f)
                continue;
            for (int layer = 0; layer < 2; layer++)
                Fire<SimpleRound>(eye.position, axis, speed + layer * .55f, .21f).FadeAt(8);
        }
    }

    // Mirrored rim fans from a pair of side eyes (1/2 middle wheel, 3/4 outer wheel),
    // opening outward and down from the inner angle across the arc, with one lane open
    // that drifts beat by beat.
    void RimFans(int leftPort, int count, float inner, float arc, float speed, int beat)
    {
        float lane = .3f * Mathf.Sin(beat * .31f);
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? leftPort : leftPort + 1);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                if (Mathf.Abs(u - lane) < .2f)
                    continue;
                FeatherShot(i % 3 == 0, port, CombatVisuals.Rotate(Vector2.down, s * (inner + arc * (u + 1) * .5f)), speed + .5f * (1 - u * u), .17f)
                    .Ramp(1.3f, .8f, .3f)
                    .FadeAt(8);
            }
        }
    }

    // A stare: both side eyes throw a short aimed chord, a circular bead between
    // needles, at a pilot who has stopped moving.
    void Stare(int leftPort, int count)
    {
        UseTone(Accent(CardIndex));
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? leftPort : leftPort + 1);
            Vector2 aim = CombatVisuals.Aim(port);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                ScoredShot(i == count / 2, port, CombatVisuals.Rotate(aim, u * 7), 2.9f - .25f * Mathf.Abs(u), .19f)
                    .FadeAt(7);
            }
        }
    }

    // Flight: feathers thrown sideways from the middle side eyes, tips fastest, halt as
    // a drawn wing and shed straight down as curtains beside the walls. A second layer
    // halts inside the first, so each wing has primaries and coverts.
    void MiddleWings(float stroke, int count, int layers)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? 1 : 2);
            for (int layer = 0; layer < layers; layer++)
                for (int i = 0; i < count; i++)
                {
                    float u = count <= 1 ? 1 : i / (count - 1f);
                    float speed = (2.0f + 2.6f * u) * (layer == 0 ? 1 : .78f);
                    Fire<FeatherRound>(port, CombatVisuals.Rotate(Vector2.down, s * (62 + 46 * u + stroke - layer * 8)), speed, .17f)
                        .Pause(.75f, .35f, CombatRound.Turn.Down, s * (6 + 20 * u), (1.9f + .4f * (1 - u)) / speed)
                        .FadeAt(8.5f);
                }
        }
    }

    // The feet: a skirt of arrows (and a bead layer offset by half a step) beneath the
    // body from the middle side eyes, opening outward and folding in with the stroke.
    void LowerWings(float stroke, int count, int layers)
    {
        float step = 36f / Mathf.Max(1, count - 1);
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? 1 : 2);
            for (int layer = 0; layer < layers; layer++)
                for (int i = 0; i < count; i++)
                {
                    float u = Across(i, count);
                    float angle = s * (10 + 18 * u - stroke + layer * step * .5f);
                    ScoredShot(layer == 1, port, CombatVisuals.Rotate(Vector2.down, angle), 2.5f + .3f * u - layer * .35f, .17f)
                        .FadeAt(8);
                }
        }
    }

    // The face: feathers thrown up and out from the outer side eyes, tips fastest; they
    // halt as a raised wing near the top edge, then shed down the upper sides.
    void RaisedWings(float stroke, int count)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? 3 : 4);
            for (int i = 0; i < count; i++)
            {
                float u = count <= 1 ? 1 : i / (count - 1f);
                float speed = 1.6f + 2.0f * u;
                Fire<FeatherRound>(port, CombatVisuals.Rotate(Vector2.down, s * (112 + 40 * u + stroke)), speed, .17f)
                    .Pause(.8f, .4f, CombatRound.Turn.Down, s * (4 + 14 * u), (1.7f + .3f * (1 - u)) / speed)
                    .FadeAt(8.5f);
            }
        }
    }

    // The face's lances: held out to the walls from the riding outer side eyes, they
    // beat down and back. A rising eye lifts its beam toward the ceiling, shrinking but
    // never closing the corner pocket above it; the charge shows the line first.
    void WingLances(float tempo)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var beam = Beam(s < 0 ? 3 : 4, s * 88, .8f, 3.0f, 1.6f);
            if (beam != null)
            {
                beam.sweep = -s * 22 * tempo;
                beam.reversalArc = 36;
            }
        }
    }

    // The many eyes: a pair on the outer wheel's lower rim lays thin lances that cross on
    // the pilot, each opening two layered feather lids along its line of sight, never on
    // it. Aimed lances stay where the eye opened: riding, a thin beam would slide sideways
    // onto a pilot who had already stepped clear of it.
    void ManyEyes(int lids, int beat)
    {
        float reach = 34 + 14 * Mathf.Sin(beat * .8f);
        for (int s = -1; s <= 1; s += 2)
        {
            var eye = EyeFacing(0, CombatVisuals.Rotate(Vector2.down, s * reach));
            Vector2 sight = CombatVisuals.Aim(eye.position);
            HeavenArmory.Lance(Angel.transform, Angel.transform.InverseTransformPoint(eye.position), sight, .46f, .7f, .85f);
            EyeLids(eye.position, sight, lids);
        }
    }

    // Two feather lids open along an eye's line of sight, never on it: the inner
    // layer slower, the outer layer faster and offset by half a step.
    void EyeLids(Vector3 port, Vector2 sight, int count)
    {
        for (int side = -1; side <= 1; side += 2)
            for (int layer = 0; layer < 2; layer++)
                for (int i = 0; i < count; i++)
                {
                    float u = (i + layer * .5f) / Mathf.Max(1, count);
                    float speed = 1.5f + .9f * u + layer * .55f;
                    FeatherShot(i % 4 == 3, port, CombatVisuals.Rotate(sight, side * (20 + 52 * u)), speed, .17f)
                        .Ramp(1.45f, .9f, .4f)
                        .FadeAt(8);
                }
    }

    // Unbodied eyes open at the first sites in empty air and glow for the charge.
    IEnumerator Apparition(int count, float charge)
    {
        for (int i = 0; i < apparitions.Length; i++)
        {
            apparitions[i].enabled = false;
            if (i < count)
                apparitions[i].transform.position = sites[i];
        }
        for (float t = 0; t < charge;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            float k = Mathf.Clamp01(t / charge);
            for (int i = 0; i < count; i++)
            {
                var glow = apparitions[i];
                glow.enabled = true;
                glow.transform.localScale = Vector3.one * Mathf.Lerp(.65f, .15f, k);
                var ink = LevelProjectilePalette.Accent(LevelId.Heaven);
                ink.a = .2f + k * .55f;
                glow.color = ink;
            }
            yield return null;
        }
        for (int i = 0; i < count; i++)
            apparitions[i].enabled = false;
    }

    // Watchers: unbodied eyes open in the upper and middle corners and throw feather
    // fans toward a point near the floor on the far side, closing every corner.
    IEnumerator CornerWatchers(int card, int verses)
    {
        for (int verse = 0; verse < verses; verse++)
        {
            Rect r = CombatRound.Border;
            float drop = verse * .7f;
            sites[0] = new Vector3(r.xMin + .8f, r.yMax - .9f - drop, -.8f);
            sites[1] = new Vector3(r.xMax - .8f, r.yMax - .9f - drop, -.8f);
            sites[2] = new Vector3(r.xMin + .7f, r.center.y - .4f - drop, -.8f);
            sites[3] = new Vector3(r.xMax - .7f, r.center.y - .4f - drop, -.8f);
            yield return Apparition(4, .7f);
            int count = Dense(9);
            UseTone(Accent(card));
            for (int i = 0; i < 4; i++)
            {
                var p = sites[i];
                if (Near(p))
                    continue;
                float side = p.x < r.center.x ? -1 : 1;
                var target = new Vector3(r.center.x - side * (1.2f + verse * .8f), r.yMin + 1.5f, p.z);
                Vector2 aim = ((Vector2)(target - p)).normalized;
                for (int k = 0; k < count; k++)
                {
                    float u = Across(k, count);
                    Fire<FeatherRound>(p, CombatVisuals.Rotate(aim, u * 24), 1.6f + .5f * (1 - u * u), .17f)
                        .Ramp(1.4f, 1, .3f)
                        .FadeAt(7.5f);
                }
            }
            yield return Wait(Rest(.35f));
        }
    }

    // Holy, holy, holy: three pairs of unbodied eyes along the top edge; each drops
    // a short chord that halts, stares, and is thrown at the pilot.
    IEnumerator Holy(int card)
    {
        for (int verse = 0; verse < 3; verse++)
        {
            Rect r = CombatRound.Border;
            for (int i = 0; i < 6; i++)
            {
                int side = i < 3 ? -1 : 1;
                int lane = i % 3;
                float x = r.center.x + side * (1.0f + lane * 1.5f + (verse % 2) * .35f);
                sites[i] = new Vector3(x, r.yMax - .6f - lane * .45f, -.8f);
            }
            yield return Apparition(6, .6f);
            int count = Dense(6);
            UseTone(Accent(card));
            for (int i = 0; i < 6; i++)
            {
                if (Near(sites[i]))
                    continue;
                for (int k = 0; k < count; k++)
                {
                    float u = Across(k, count);
                    Fire<SimpleRound>(sites[i], CombatVisuals.Rotate(Vector2.down, u * 34), 1.7f, .2f)
                        .Pause(.55f, .3f, CombatRound.Turn.Aim, u * 9, 1.35f)
                        .FadeAt(7.5f);
                }
            }
            yield return Wait(Rest(.8f));
        }
    }

    // Glass panes for the Sea of Glass: two panes per verse, three unbodied eyes each.
    // Sheets (rows) cycle through the top edge, below the angel and mid-arena; columns
    // stand left and right of the middle. A verse of the still sea lays two sheets, the
    // standing sea two columns, the crossed sea one of each.
    IEnumerator Glass(int verse, int verses)
    {
        for (int v = 0; v < verses; v++)
        {
            Rect r = CombatRound.Border;
            for (int pane = 0; pane < 2; pane++)
            {
                bool column = verse == 2 || verse == 3 && pane == 1;
                for (int c = 0; c < 3; c++)
                {
                    if (column)
                    {
                        float x = r.center.x + (pane == 0 ? -1 : 1) * (r.width * .26f + (v % 2) * .45f);
                        float y = Mathf.Lerp(r.yMax - .8f, r.yMin + 2.5f, (c * 2 + 1) / 6f);
                        sites[pane * 3 + c] = new Vector3(x, y, -.8f);
                    }
                    else
                        sites[pane * 3 + c] = new Vector3(Mathf.Lerp(r.xMin, r.xMax, (c * 2 + 1) / 6f), SheetHeight(r, (v + pane) % 3), -.8f);
                }
            }
            yield return Apparition(6, .45f);
            UseTone(Body(CardIndex));
            for (int pane = 0; pane < 2; pane++)
            {
                bool column = verse == 2 || verse == 3 && pane == 1;
                if (column)
                    Column(r, pane, v);
                else
                    Sheet(r, pane, v);
            }
            yield return Wait(Rest(.3f));
        }
    }

    static float SheetHeight(Rect r, int level) => level == 0 ? r.yMax - .8f : level == 1 ? r.yMax - 6.0f : r.yMax - 8.6f;

    // One pane's pearl, flung to its exact place: it brakes to a stop there, hangs,
    // then leaves along an absolute heading (degrees from straight down) at a speed.
    void Place(Vector3 site, Vector3 target, float hang, float leave, float speed)
    {
        Vector2 path = target - site;
        float raw = Mathf.Max(.3f, path.magnitude / (GlassFlight - .175f));
        float launch = WorldSpeed(raw);
        Vector2 heading = path.sqrMagnitude > .0001f ? path.normalized : Vector2.down;
        Fire<SimpleRound>(site, heading, launch, .19f)
            .Pause(GlassFlight, hang, CombatRound.Turn.Down, leave, speed / launch)
            .FadeAt(9);
    }

    // A sheet falls on alternating diagonals. The woven channels between diverging
    // pairs move half a step every verse, and one seam drifts along the row.
    void Sheet(Rect r, int pane, int verse)
    {
        int count = Dense(26);
        float seam = r.center.x + (r.width * .5f - 1.2f) * Mathf.Sin(verse * 1.7f + pane * 2.4f);
        for (int j = 0; j < count; j++)
        {
            float x = Mathf.Lerp(r.xMin + .35f, r.xMax - .35f, count <= 1 ? .5f : j / (count - 1f));
            if (Mathf.Abs(x - seam) < .6f)
                continue;
            var site = sites[pane * 3 + Mathf.Clamp(Mathf.FloorToInt((x - r.xMin) / r.width * 3), 0, 2)];
            if (Near(site))
                continue;
            Place(site, new Vector3(x, site.y, site.z), .45f, (j + verse) % 2 == 0 ? 18 : -18, 1.9f);
        }
    }

    // A column slides sideways as one wall, outward on even verses and inward on odd
    // ones, with a seam that climbs or sinks verse by verse.
    void Column(Rect r, int pane, int verse)
    {
        int count = Dense(18);
        float top = r.yMax - .8f;
        float bottom = r.yMin + 2.5f;
        float seam = Mathf.Lerp(bottom + 1, top - 1, .5f + .45f * Mathf.Sin(verse * 1.3f + pane * 2.0f));
        int side = pane == 0 ? -1 : 1;
        float leave = (verse % 2 == 0 ? side : -side) * 90;
        for (int j = 0; j < count; j++)
        {
            float y = Mathf.Lerp(top, bottom, count <= 1 ? .5f : j / (count - 1f));
            if (Mathf.Abs(y - seam) < .75f)
                continue;
            var site = sites[pane * 3 + Mathf.Clamp(Mathf.FloorToInt((top - y) / (top - bottom) * 3), 0, 2)];
            var target = new Vector3(site.x, y, site.z);
            if (Near(target))
                continue;
            Place(site, target, .5f, leave, 1.4f);
        }
    }
}
