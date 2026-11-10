using System.Collections;
using UnityEngine;

// Proposed card book (ArtSource/BossPhaseProposals/Ideas/SeraphJudicatorBoss.md),
// flown only while DistrictBoss.ProposedCards is set so it can be recorded beside
// the shipping Cards(). The angel's own anatomy draws every card: the tumbling
// wheels shape the fans, the eyes carry the lances, and unbodied eyes open in
// empty air. Tones follow Body/Accent: Wheel Within a Wheel bright, Six Wings
// dark; Feather Fall dark, Many Eyes bright; Wheels of Fire bright, Sea of Glass
// dark. Lances stay bright.
public sealed partial class SeraphJudicatorBoss
{
    readonly Vector3[] sites = new Vector3[6];
    const float GlassFlight = .8f;

    protected override IEnumerator Proposed()
    {
        while (true)
        {
            CardIndex = 0;
            Pray(true);
            yield return Charge(Angel.Emitter(0), Vector3.zero, .65f);
            if (CurrentPhase == 1)
                yield return WheelWithinAWheel();
            else if (CurrentPhase == 2)
                yield return FeatherFall();
            else
                yield return WheelsOfFire();
            Pray(false);
            yield return Wait(Rest(1.25f));
            CardIndex = 1;
            Pray(true);
            yield return Charge(Angel.Emitter(0), Vector3.zero, .65f);
            if (CurrentPhase == 1)
                yield return SixWings();
            else if (CurrentPhase == 2)
                yield return ManyEyes();
            else
                yield return SeaOfGlass();
            Pray(false);
            yield return Wait(Rest(1.45f));
        }
    }

    // Wheel Within a Wheel: the middle wheel's lower eyes cast feather lashes along
    // the wheel's own tumbling outline, so every fan is drawn anew by the wheel, with
    // one moving aisle. The outer wheel's side eyes open mirrored rim fans on the off
    // beats, the inner wheel answers with slow pearls, the core eye lays a wide glance
    // along the pilot's line twice, and watchers open in the corners.
    IEnumerator WheelWithinAWheel()
    {
        Phrase(CornerWatchers(0, 4));
        yield return Every(4.6f, .22f, beat =>
        {
            if (beat % 10 == 0)
                Glance(1.0f, 2.0f, 1.3f);
            if (beat % 4 == 2)
                Gaze();
            UseTone(Body(0));
            WheelLash(1, Dense(9), 3, 1.35f, beat % 4 == 0, 46 * Mathf.Sin(beat * .17f));
            if (beat % 2 == 1)
            {
                RimFans(3, Dense(9), 30, 50, 2.0f, beat, false);
                UseTone(Accent(0));
                WheelPearls(2, 1.2f);
            }
        });
    }

    // Six Wings (Isaiah's seraph): the upper pair are lances held out to the walls
    // from the riding side eyes, beating down and back; the middle pair are feathers
    // thrown sideways that halt as drawn wings and then shed downward; the lower pair
    // sweep beneath the body. Three pairs of unbodied eyes along the top edge cry Holy
    // and stare, and the side eyes stare back at a pilot who stays still.
    IEnumerator SixWings()
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var beam = Beam(s < 0 ? 3 : 4, s * 88, .8f, 3.6f, 1.6f);
            if (beam != null)
            {
                beam.sweep = -s * 22;
                beam.reversalArc = 36;
            }
        }
        Phrase(Holy(1));
        yield return Every(4.8f, .36f, beat =>
        {
            if (beat % 3 == 0)
                Gaze();
            if (beat % 2 == 1)
                Stare(3, 5);
            UseTone(Body(1));
            float stroke = 7 * Mathf.Sin(beat * .9f);
            int wing = Dense(14);
            int under = Dense(9);
            for (int s = -1; s <= 1; s += 2)
            {
                var port = Eye(s < 0 ? 1 : 2);
                for (int i = 0; i < wing; i++)
                {
                    float u = wing <= 1 ? 1 : i / (wing - 1f);
                    // Tips fly farther and higher, so the halt draws the wing's curve.
                    float speed = 2.0f + 2.6f * u;
                    Fire<FeatherRound>(port, CombatVisuals.Rotate(Vector2.down, s * (62 + 46 * u + stroke)), speed, .17f)
                        .Pause(.75f, .35f, CombatRound.Turn.Down, s * (6 + 20 * u), (1.9f + .4f * (1 - u)) / speed)
                        .FadeAt(8.5f);
                }
                for (int i = 0; i < under; i++)
                {
                    float u = Across(i, under);
                    ScoredShot(i % 3 == 0, port, CombatVisuals.Rotate(Vector2.down, s * (10 + 18 * u - stroke)), 2.5f + .3f * u, .17f)
                        .FadeAt(8);
                }
            }
        });
    }

    // Feather Fall: two gate lances cross beneath the angel from the riding side eyes
    // and breathe apart and back (the cherubim and the turning sword). Along the whole
    // top edge unbodied plumes shed swaying feathers, offset by half a plume each verse
    // so the gaps drift. Below, the middle wheel lashes around one moving aisle, the
    // gate eyes open mirrored fans into the side aisles outside the X, the inner wheel
    // throws stars that halt and stare, and the gate eyes stare at a resting pilot.
    IEnumerator FeatherFall()
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var beam = Beam(s < 0 ? 3 : 4, -s * 22, .85f, 3.4f, 1.6f);
            if (beam != null)
            {
                // Outward only: the lower aisle widens; the crossing never sinks onto the floor.
                beam.sweep = -s * 9;
                beam.reversalArc = 10;
            }
        }
        Phrase(Plumes(0, 5));
        yield return Every(5.0f, .22f, beat =>
        {
            if (beat % 5 == 1)
                Gaze();
            if (beat % 3 == 2)
                Stare(3, 5);
            UseTone(Body(0));
            WheelLash(1, Dense(9), 3, 1.3f, false, -40 * Mathf.Sin(beat * .21f));
            RimFans(3, Dense(6), 18, 40, 1.9f, beat, false);
            UseTone(Accent(0));
            StaringStars(2, Dense(3));
        });
    }

    // Many Eyes: pairs of eyes on the outer wheel's lower rim open in turn and lay
    // thin lances that cross on the pilot; every lancing eye opens two layered
    // feather lids along its line of sight, never on it. The inner wheel weeps
    // pearls and the upper rim sends a few feathers toward the top edge.
    IEnumerator ManyEyes()
    {
        yield return Every(5.4f, .52f, beat =>
        {
            if (beat % 3 == 2)
                Gaze();
            float reach = 34 + 14 * Mathf.Sin(beat * .8f);
            for (int s = -1; s <= 1; s += 2)
            {
                var eye = EyeFacing(0, CombatVisuals.Rotate(Vector2.down, s * reach));
                Vector2 sight = CombatVisuals.Aim(eye.position);
                // Aimed lances stay where the eye opened: a riding origin would slide the
                // thin beam sideways onto a pilot who has already stepped clear of it.
                PinnedLance(eye.position, sight, .46f, .7f, .85f);
                UseTone(Body(1));
                EyeLids(eye.position, sight, Dense(12));
            }
            UseTone(Body(1));
            WheelLash(1, 0, 0, 1.2f, beat % 2 == 0, 0);
            UseTone(Accent(1));
            WheelPearls(2, 1.1f);
        });
    }

    // Wheels of Fire (Hard): two wheels cast their own lashes at a time, outer
    // feathers, middle feathers and stars, inner pearls, sharing one moving aisle.
    // The middle wheel's side eyes add mirrored rim fans, the outer side eyes stare.
    // A lance pair through the core eye turns like a slow propeller, one arm across
    // the lower arena and the other over the top; watchers close the corners.
    IEnumerator WheelsOfFire()
    {
        for (int arm = 0; arm < 2; arm++)
        {
            var beam = Beam(0, arm == 0 ? -62 : 118, 1.0f, 3.4f, 1.7f);
            if (beam != null)
                beam.sweep = 16;
        }
        Phrase(CornerWatchers(0, 4));
        yield return Every(5.2f, .2f, beat =>
        {
            if (beat % 5 == 0)
                Gaze();
            if (beat % 4 == 3)
                Stare(3, 5);
            float aisle = 50 * Mathf.Sin(beat * .15f);
            int first = beat % 3;
            for (int k = 0; k < 2; k++)
            {
                int ring = (first + k) % 3;
                UseTone(ring == 2 ? Accent(0) : Body(0));
                WheelLash(ring, Dense(8), 3.5f, 1.4f - .1f * ring, k == 0 && beat % 6 == 3, aisle);
            }
            if (beat % 2 == 0)
            {
                UseTone(Body(0));
                RimFans(1, Dense(8), 34, 46, 2.1f, beat, false);
            }
        });
    }

    // Sea of Glass (Hard): six unbodied eyes draw three glass sheets at a time across
    // the arena; every feather flies to its exact place, the sheet hangs, then falls
    // as a woven lattice of opposed diagonals with one open seam per sheet. The outer
    // wheel keeps lashing around an aisle, the middle side eyes throw refracted rim
    // fans that halt and turn down, and the core eye and side eyes answer a still pilot.
    IEnumerator SeaOfGlass()
    {
        Phrase(GlassSheets(1, 5));
        yield return Every(5.4f, .3f, beat =>
        {
            if (beat % 4 == 0)
                Gaze();
            if (beat % 7 == 3)
                Glance(.5f, .9f, .9f);
            if (beat % 3 == 1)
                Stare(3, 5);
            UseTone(Body(1));
            WheelLash(0, Dense(10), 2.6f, 1.5f, false, 40 * Mathf.Sin(beat * .23f));
            RimFans(1, Dense(8), 20, 44, 2.0f, beat, true);
            UseTone(Accent(1));
            WheelPearls(2, 1.15f);
        });
    }

    // Mirrored rim fans from a pair of side eyes (1/2 middle wheel, 3/4 outer wheel),
    // opening outward and down from the inner angle across the arc, with one lane open
    // that drifts beat by beat. Refracted fans halt briefly and turn straight down.
    void RimFans(int leftPort, int count, float inner, float arc, float speed, int beat, bool refract)
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
                var shot = FeatherShot(i % 3 == 0, port, CombatVisuals.Rotate(Vector2.down, s * (inner + arc * (u + 1) * .5f)), speed + .5f * (1 - u * u), .17f)
                    .Ramp(1.3f, .8f, .3f)
                    .FadeAt(8);
                if (refract)
                    shot.Pause(.8f, .2f, CombatRound.Turn.Down, s * 8 * u, 1);
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

    // A lance pinned to the body where an eye stood at launch, so a turning wheel
    // cannot drag its aisle closed while the pilot is inside it.
    PulseLaser PinnedLance(Vector3 eye, Vector2 axis, float width, float hold, float charge)
        => HeavenArmory.Lance(Angel.transform, Angel.transform.InverseTransformPoint(eye), axis, width, hold, charge);

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

    // A wheel's lower eyes throw small star chords that halt and dart at the pilot.
    void StaringStars(int ring, int count)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (axis.y > -.3f)
                continue;
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                Fire<StarRound>(eye.position, CombatVisuals.Rotate(axis, u * 14), 2.1f, .2f)
                    .Pause(.7f, .3f, CombatRound.Turn.Aim, u * 6, 1.25f)
                    .FadeAt(8);
            }
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

    // Plumes: six unbodied eyes along the top edge shed sparse, swaying feather fans;
    // the crossing arcs leave diamond gaps, and every verse shifts by half a plume.
    IEnumerator Plumes(int card, int verses)
    {
        for (int verse = 0; verse < verses; verse++)
        {
            Rect r = CombatRound.Border;
            for (int i = 0; i < 6; i++)
            {
                float x = Mathf.Lerp(r.xMin + .7f, r.xMax - .7f, (i + (verse % 2) * .5f) / 5.5f);
                sites[i] = new Vector3(x, r.yMax - .55f - (i % 2) * .35f, -.8f);
            }
            yield return Apparition(6, .45f);
            int count = Dense(9);
            UseTone(Body(card));
            for (int i = 0; i < 6; i++)
            {
                if (Near(sites[i]))
                    continue;
                for (int k = 0; k < count; k++)
                {
                    float u = Across(k, count);
                    // The middle of each plume falls first; the sway rate differs plume to plume.
                    Fire<FeatherRound>(sites[i], CombatVisuals.Rotate(Vector2.down, u * 40), 1.5f + .55f * (1 - u * u), .17f)
                        .Sway(.26f, .42f + .06f * (i % 3))
                        .Ramp(1.35f, 1.4f, .8f)
                        .FadeAt(9);
                }
            }
            yield return Wait(Rest(.3f));
        }
    }

    // Glass sheets: three rows of two unbodied eyes, at the top edge, below the angel
    // and mid-arena. Each eye flings feathers to exact places along its half of the
    // row; every feather of a sheet halts together, hangs, then falls on alternating
    // diagonals. The rows trade heights each verse.
    IEnumerator GlassSheets(int card, int verses)
    {
        for (int verse = 0; verse < verses; verse++)
        {
            Rect r = CombatRound.Border;
            for (int row = 0; row < 3; row++)
            {
                float y = r.yMax - .8f - row * 2.6f - (verse % 2) * .9f;
                if (row > 0)
                    y -= 2.6f;
                for (int c = 0; c < 2; c++)
                    sites[row * 2 + c] = new Vector3(Mathf.Lerp(r.xMin, r.xMax, c * .5f + .25f), y, -.8f);
            }
            yield return Apparition(6, .5f);
            int count = Dense(26);
            UseTone(Body(card));
            for (int row = 0; row < 3; row++)
            {
                float seam = r.center.x + (r.width * .5f - 1.2f) * Mathf.Sin(verse * 1.7f + row * 2.4f);
                for (int j = 0; j < count; j++)
                {
                    float x = Mathf.Lerp(r.xMin + .35f, r.xMax - .35f, count <= 1 ? .5f : j / (count - 1f));
                    if (Mathf.Abs(x - seam) < .6f)
                        continue;
                    var site = sites[row * 2 + (x < r.center.x ? 0 : 1)];
                    if (Near(site))
                        continue;
                    Vector2 path = new Vector2(x - site.x, 0);
                    // Launch speed in world units that brakes to a stop exactly on the place.
                    float raw = Mathf.Max(.3f, path.magnitude / (GlassFlight - .175f));
                    float launch = WorldSpeed(raw);
                    Vector2 heading = path.sqrMagnitude > .0001f ? path.normalized : Vector2.down;
                    Fire<FeatherRound>(site, heading, launch, .17f)
                        .Pause(GlassFlight, .45f, CombatRound.Turn.Down, j % 2 == 0 ? 18 : -18, 1.9f / launch)
                        .FadeAt(9);
                }
            }
            yield return Wait(Rest(.3f));
        }
    }
}
