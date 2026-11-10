using System.Collections;
using UnityEngine;

// Proposed card book for the Hell crab (review only, DistrictBoss.ProposedCards).
// Ideas and comparisons: ArtSource/BossPhaseProposals/Ideas/HellCrabBoss.md
// One concept per phase, developed stroke by stroke on the claw stroke the crab already
// flies: the pincers fire as they come down to the pilot's height, sweep and return; the
// eyes and the mouth belong to the same idea. No minions, no vents.
// Phase one, Ember Cage: each stroke the pincers and eyes throw dark embers that stop as a
//   cage around the pilot, with a bright frame outside. It closes, stares and darts, or
//   bursts while the outer frame closes, in turn; the eyes glare while the claws rest.
// Phase two, Searing Trace: the pincers draw their stroke in a bright ribbon, a flame from
//   the tip between arrows from the jaws. The
//   ribbon burns like a fuse at the pilot, brands the whole drawn shape at the pilot at
//   the rest, or tears open into two drifting walls, in turn.
// Inferno (Hard), Ninth Circle: descending, the pincers draw the walls of a fiery pit at
//   either side; the eyes seal its ceiling and the walls close with the claws' sweep, whose
//   trail darts at the pilot; while the claws rest the ceiling keeps falling, with one gap.
public sealed partial class HellCrabBoss
{
    // Ribbon rounds leave the pincer tip (a flame) and either jaw (arrows). Flames stay
    // within the district's warmed reserve even when a whole drawn stroke hangs at once.
    static readonly Vector3[] Ribbon = { new Vector3(-.28f, -.32f, -.4f), new Vector3(0, -.32f, -.4f), new Vector3(.28f, -.32f, -.4f) };
    float ClawTime => Mathf.Repeat(clawClock, clawPeriod);
    float ClawRate => phaseTwo ? 1.18f : 1;
    // World units per authored speed unit, so hanging points follow real travel.
    float Pace => 1 / WorldSpeed(1);

    protected override IEnumerator Proposed()
    {
        int stroke = 0;
        while (true)
        {
            if (phaseThree)
                yield return NinthCircle(stroke);
            else if (phaseTwo)
                yield return SearingTrace(stroke);
            else
                yield return EmberCage(stroke);
            stroke++;
        }
    }

    // Ember Cage, one claw stroke: the cage at the pilot's height, glares at the rest.
    //   Closing: the bars slide together and cross, the lid comes down, a frame a beat later.
    //   Staring: every ember stares, then darts at the pilot, bar by bar, then the lid.
    //   Bursting: the inner cage flies apart while the bright outer frame closes through it.
    IEnumerator EmberCage(int stroke)
    {
        int variant = stroke % 3;
        yield return UntilClaw(1.45f);
        yield return Glow(.45f, true, true, false);
        Cage(CagePoint(), variant);
        if (ClawTime < 4.7f)
            yield return UntilClaw(4.7f);
        // The jailer's glare: one more line for each turn of the cage.
        for (int glare = 0; glare < 3 + variant; glare++)
        {
            // Never run into the next stroke.
            if (ClawTime < 4f)
                break;
            yield return Glow(.35f, false, true, false);
            Glare(HostileTone.Bright);
            yield return Wait(Rest(.36f));
        }
    }

    // The cage's centre: the pilot, kept inside the walls and clear below the crab.
    Vector2 CagePoint()
    {
        var at = player != null ? (Vector2)player.position : new Vector2(0, -3);
        float side = Camera.RightLimit - 2.1f;
        float under = transform.position.y - 3.4f;
        return new Vector2(Mathf.Clamp(at.x, -side, side), Mathf.Clamp(at.y, Camera.DownLimit + 1.6f, under));
    }

    // Bars from the pincers either side and a lid from the eyes, all arriving at once: a
    // dark inner frame and a bright outer one.
    void Cage(Vector2 centre, int variant)
    {
        int bar = Dense(10);
        int lid = Dense(11);
        for (int frame = 0; frame < 2; frame++)
        {
            UseTone(frame == 0 ? HostileTone.Dark : HostileTone.Bright);
            float width = 1.45f + frame * .5f;
            float top = 1.35f + frame * .5f;
            float hold = .5f + frame * .35f;
            for (int side = 0; side < 2; side++)
            {
                if (claws[side] == null)
                    continue;
                int sign = side == 0 ? -1 : 1;
                var port = claws[side].TransformPoint(Pincer);
                for (int k = 0; k < bar; k++)
                {
                    float y = centre.y + Mathf.Lerp(-1.25f, top - .25f, (k + .5f + frame * .5f) / bar);
                    CageEmber(port, new Vector2(centre.x + sign * width, y), hold + (variant == 1 ? side * .3f : 0), Vector2.right * -sign, variant, frame);
                }
            }
            for (int k = 0; k < lid; k++)
            {
                float x = centre.x + Mathf.Lerp(-width + .3f, width - .3f, (k + .5f) / lid);
                var eye = eyes[x < centre.x ? 0 : 1];
                if (eye != null)
                    CageEmber(eye.position, new Vector2(x, centre.y + top), hold + (variant == 1 ? .6f : 0), Vector2.down, variant, frame);
            }
        }
    }

    // How one cage ember leaves: inward, at the pilot, or (inner frame) outward.
    void CageEmber(Vector3 port, Vector2 point, float hold, Vector2 inward, int variant, int frame)
    {
        if (variant == 1)
        {
            Stare<FlameRound>(port, point, 1.1f, hold, 2.6f, .24f)
                .FadeAt(7);
            return;
        }
        var away = variant == 2 && frame == 0 ? -inward : inward;
        Hang<FlameRound>(port, point, 1.1f, hold, away, 1.5f, .24f)
            .FadeAt(6);
    }

    // A glare: each eye looses a line of arrows at graded speeds straight at the pilot.
    void Glare(HostileTone shade)
    {
        UseTone(shade);
        int line = Dense(8);
        for (int e = 0; e < 2; e++)
        {
            if (eyes[e] == null)
                continue;
            var port = eyes[e].position;
            var aim = CombatVisuals.Aim(port);
            for (int k = 0; k < line; k++)
                ScoredShot(k == 0, port, aim, 4.8f - k * 1.8f / Mathf.Max(1, line), k == 0 ? .26f : .19f);
        }
    }

    // Searing Trace, one claw stroke drawn in fire from the descent to the return.
    //   Fuse: each ember darts at the pilot 1.1 s after it was laid.
    //   Brand: the whole drawn shape hangs until the claws rest, then darts in drawn order.
    //   Rift: the ribbon tears open, its edges drifting apart from the path, the centre sinking.
    IEnumerator SearingTrace(int stroke)
    {
        int variant = stroke % 3;
        yield return UntilClaw(.9f);
        yield return Glow(.4f, true, false, false);
        var last = new Vector3[2];
        for (int side = 0; side < 2; side++)
            last[side] = claws[side] != null ? claws[side].position : transform.position;
        int laid = 0;
        while (ClawTime < 4.6f)
        {
            for (int side = 0; side < 2; side++)
            {
                if (claws[side] == null)
                    continue;
                var motion = (Vector2)(claws[side].position - last[side]);
                last[side] = claws[side].position;
                TraceStep(side, variant, laid, motion, HostileTone.Bright);
            }
            laid++;
            yield return Wait(Rest(.055f));
        }
        // The rest belongs to the fire: the fuse and the brand burn out before the next stroke.
        yield return Wait(Rest(variant == 1 ? 1.2f : .6f));
    }

    // One ribbon step from a pincer, left almost where it is, leaving by the variant.
    void TraceStep(int side, int variant, int laid, Vector2 motion, HostileTone shade)
    {
        var claw = claws[side];
        if (player != null && Vector2.Distance(claw.TransformPoint(Pincer), player.position) < 1.6f)
            return;
        UseTone(shade);
        // Until the claws reach the end of the stroke, in the round's own clock.
        float untilRest = Mathf.Max(0, (4.6f - ClawTime) / ClawRate);
        var across = motion.sqrMagnitude > .00001f ? new Vector2(-motion.y, motion.x).normalized : Vector2.right;
        for (int k = 0; k < Ribbon.Length; k++)
        {
            var round = RibbonRound(claw.TransformPoint(Ribbon[k]), k);
            if (variant == 0)
                round.Pause(.3f, 1.1f, CombatRound.Turn.Aim, 0, 7.5f);
            else if (variant == 1)
                round.Pause(.3f, untilRest + laid * .012f, CombatRound.Turn.Aim, 0, 8);
            else
            {
                // The jaws part to either side of the path; the tip sinks down it.
                var leave = k == 1 ? Vector2.down : across * (k - 1);
                round.Pause(.3f, untilRest, CombatRound.Turn.Rotate, Vector2.SignedAngle(Vector2.down, leave), 3)
                    .FadeAt(untilRest + 5);
            }
        }
    }

    CombatRound RibbonRound(Vector3 port, int k)
        => k == 1 ? (CombatRound)Fire<FlameRound>(port, Vector2.down, .4f, .24f) : Fire<NeedleRound>(port, Vector2.down, .4f, .19f);

    // Ninth Circle (Hard), one claw stroke.
    //   The descent draws the pit walls; the eyes seal the ceiling with a falling lid; the
    //   walls close as the claws sweep (slide, stare-and-dart, or slow slide in turn); the
    //   sweep's ribbon burns like a fuse; at the rest two more lids fall, each with one gap.
    IEnumerator NinthCircle(int stroke)
    {
        int variant = stroke % 3;
        yield return UntilClaw(.9f);
        yield return Glow(.4f, true, true, false);
        // The walls close with the claws' sweep, at claw time 2.9.
        while (ClawTime < 2.4f)
        {
            for (int side = 0; side < 2; side++)
                PitWall(side, variant, (2.9f - ClawTime) / ClawRate);
            yield return Wait(Rest(.04f));
        }
        Lid(stroke * 3);
        int laid = 0;
        while (ClawTime < 4.6f)
        {
            for (int side = 0; side < 2; side++)
                if (claws[side] != null)
                    TraceStep(side, 0, laid, Vector2.zero, HostileTone.Dark);
            laid++;
            yield return Wait(Rest(.04f));
        }
        for (int n = 1; n <= 2; n++)
        {
            if (ClawTime < 4f)
                break;
            yield return Glow(.35f, false, true, false);
            Lid(stroke * 3 + n);
            yield return Wait(Rest(.8f));
        }
    }

    // A wall step: the descending pincer leaves a dark ribbon that hangs until the sweep,
    // then closes inward, darts at the pilot, or closes slowly.
    void PitWall(int side, int variant, float hold)
    {
        var claw = claws[side];
        if (claw == null)
            return;
        if (player != null && Vector2.Distance(claw.TransformPoint(Pincer), player.position) < 1.6f)
            return;
        UseTone(HostileTone.Dark);
        float inward = side == 0 ? 90 : -90;
        for (int k = 0; k < Ribbon.Length; k++)
        {
            var round = RibbonRound(claw.TransformPoint(Ribbon[k]), k);
            if (variant == 1)
                round.Pause(.3f, hold + k * .15f, CombatRound.Turn.Aim, 0, 7);
            else
                round.Pause(.3f, hold, CombatRound.Turn.Rotate, inward, variant == 0 ? 4.5f : 2.5f)
                    .FadeAt(hold + 6);
        }
    }

    // The pit's ceiling: three offset rows of embers and arrows hang along the top edge,
    // then fall slanting together, open at one gap that moves from lid to lid.
    void Lid(int n)
    {
        var edge = CombatRound.Border;
        int count = Dense(16);
        float top = Camera.UpLimit - .3f;
        float gap = Mathf.Lerp(edge.xMin + 1.2f, edge.xMax - 1.2f, Mathf.Repeat(n * .37f + .2f, 1));
        var fall = CombatVisuals.Rotate(Vector2.down, n % 2 == 0 ? 12 : -12);
        for (int row = 0; row < 3; row++)
        {
            UseTone(row == 1 ? HostileTone.Dark : HostileTone.Bright);
            for (int k = 0; k < count; k++)
            {
                float x = Mathf.Lerp(edge.xMin + .4f, edge.xMax - .4f, (k + row / 3f) / count);
                if (Mathf.Abs(x - gap) < .65f)
                    continue;
                var eye = eyes[x < transform.position.x ? 0 : 1];
                if (eye == null)
                    continue;
                var point = new Vector2(x, top - row * .4f);
                if (row == 1)
                    Hang<FlameRound>(eye.position, point, .8f, .3f, fall, 1.8f, .24f);
                else
                    Hang<NeedleRound>(eye.position, point, .8f, .3f, fall, 1.8f, .19f);
            }
        }
    }

    // Waits until the claw stroke passes a given time (and fire is allowed).
    IEnumerator UntilClaw(float at)
    {
        float before = ClawTime;
        bool due = false;
        while (!due || Holding)
        {
            yield return null;
            float now = ClawTime;
            due |= Crossed(before, now, at);
            before = now;
        }
    }

    static bool Crossed(float before, float now, float at) => before <= now ? before < at && at <= now : before < at || at <= now;

    // A visible charge at the pincers, the eyes and/or the mouth for the whole wait.
    IEnumerator Glow(float seconds, bool pincers, bool eyesToo, bool mouth)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            for (int i = 0; i < 2; i++)
            {
                if (pincers && claws[i] != null)
                    Cue(claws[i].TransformPoint(Pincer), seconds - t);
                if (eyesToo && eyes[i] != null)
                    Cue(eyes[i].position, seconds - t);
            }
            if (mouth)
                Cue(Hull.TransformPoint(Mouth), seconds - t);
            yield return null;
        }
    }

    // A round that brakes to a stop at a chosen point after `arrive` seconds of its own
    // clock, hangs for `hold`, then leaves along `then` at an authored speed.
    T Hang<T>(Vector3 port, Vector2 point, float arrive, float hold, Vector2 then, float speed, float radius) where T : CombatRound
    {
        Vector2 path = point - (Vector2)port;
        float distance = Mathf.Max(.05f, path.magnitude);
        Vector2 axis = path.sqrMagnitude > .0001f ? path / distance : Vector2.down;
        // The brake eases over the last .35 s, so the path takes arrive - .175 s of travel.
        float travel = distance / (arrive - .175f);
        return Fire<T>(port, axis, WorldSpeed(travel), radius)
            .Pause(arrive, hold, CombatRound.Turn.Rotate, Vector2.SignedAngle(axis, then), speed * Pace / travel);
    }

    // The same, but the round leaves toward the pilot: a soul that stares, then darts.
    T Stare<T>(Vector3 port, Vector2 point, float arrive, float hold, float speed, float radius) where T : CombatRound
    {
        Vector2 path = point - (Vector2)port;
        float distance = Mathf.Max(.05f, path.magnitude);
        Vector2 axis = path.sqrMagnitude > .0001f ? path / distance : Vector2.down;
        float travel = distance / (arrive - .175f);
        return Fire<T>(port, axis, WorldSpeed(travel), radius)
            .Pause(arrive, hold, CombatRound.Turn.Aim, 0, speed * Pace / travel);
    }
}
