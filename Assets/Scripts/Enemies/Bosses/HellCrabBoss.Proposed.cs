using System.Collections;
using UnityEngine;

// Proposed card book for the Hell crab (review only, DistrictBoss.ProposedCards).
// Ideas and comparisons: ArtSource/BossPhaseProposals/Ideas/HellCrabBoss.md
// Every card is timed to the claw stroke the crab already flies: the pincers fire as
// they come down to the pilot's height, sweep and return, the eyes stare and the mouth
// throws the brimstone that covers the ceiling. No minions, no vents.
// Phase one
//   Ember Cage: as the pincers come down, they and the eyes throw dark embers that stop
//     as a cage around the pilot, a bright second frame just outside. It stares, then
//     closes: the bars slide together and cross, the lid comes down; the floor is the way
//     out. Bright eye glares (graded arrow lines) answer while the claws rest.
//   Lantern Garland: the eyes string a garland of bright souls and dark embers across the
//     whole upper arena, sagging under the crab, then arching over it. The souls dart at
//     the pilot from the middle outward while the embers unzip off the ends; through the
//     claw sweep the pincers snip bright arrow blades that cross into scissors.
// Phase two
//   Brimstone Braid: each moving pincer pours two dark flame strands swaying in opposite
//     phase toward the far lower corner, so they plait into a braid and the two braids
//     cross under the crab, a bright knot running down each axis and gaps opening every
//     few beats. The mouth hangs a bright brimstone lattice along the ceiling that falls
//     away in crossing diagonals.
//   Searing Trace: through their stroke the pincers leave a bright trail of embers where
//     they pass; each ember stares, then darts at the pilot, so the fuse burns along the
//     claws' path. While the claws rest, the mouth answers with dark falling arrow arcs.
// Inferno (Hard)
//   Ninth Circle: the pincers draw a dark searing trace through their whole stroke while,
//     at the pilot's height, a bright ember cage closes around them; brimstone lattices fall
//     from the ceiling while the claws rest.
public sealed partial class HellCrabBoss
{
    float ClawTime => Mathf.Repeat(clawClock, clawPeriod);
    // World units per authored speed unit, so hanging points follow real travel.
    float Pace => 1 / WorldSpeed(1);

    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return NinthCircle();
            else if (phaseTwo)
            {
                yield return BrimstoneBraid();
                yield return SearingTrace();
            }
            else
            {
                yield return EmberCage();
                yield return LanternGarland();
            }
        }
    }

    // Ember Cage: one cage per claw stroke, two frames; glares while the claws rest.
    IEnumerator EmberCage()
    {
        yield return UntilClaw(1.45f);
        yield return Glow(.45f, true, true, false);
        Cage(CagePoint(), 2, HostileTone.Dark, HostileTone.Bright);
        yield return UntilClaw(4.7f);
        for (int glare = 0; glare < 4; glare++)
        {
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

    // Bars from the pincers either side and a lid from the eyes. Every ember arrives at
    // once, stares, then the bars close sideways and the lid comes down; an outer frame
    // closes a beat later.
    void Cage(Vector2 centre, int frames, HostileTone inner, HostileTone outer)
    {
        int bar = Dense(10);
        int lid = Dense(11);
        for (int frame = 0; frame < frames; frame++)
        {
            UseTone(frame == 0 ? inner : outer);
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
                    Hang<FlameRound>(port, new Vector2(centre.x + sign * width, y), 1.1f, hold, Vector2.right * -sign, 1.5f, .24f)
                        .FadeAt(6);
                }
            }
            for (int k = 0; k < lid; k++)
            {
                float x = centre.x + Mathf.Lerp(-width + .3f, width - .3f, (k + .5f) / lid);
                var eye = eyes[x < centre.x ? 0 : 1];
                if (eye == null)
                    continue;
                Hang<FlameRound>(eye.position, new Vector2(x, centre.y + top), 1.1f, hold, Vector2.down, 1.5f, .24f)
                    .FadeAt(6);
            }
        }
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

    // Lantern Garland: a sagging garland as the claws rise, the snips through the sweep,
    // then an arch while they rest. It follows the cage card straight on.
    IEnumerator LanternGarland()
    {
        yield return Glow(.4f, false, true, false);
        Garland(true);
        yield return UntilClaw(2.45f);
        for (int snip = 0; snip < 4; snip++)
        {
            Snip();
            yield return Wait(Rest(.36f));
        }
        // On Easy's longer rests the snips can run past the rest mark already.
        if (ClawTime < 4.8f)
            yield return UntilClaw(4.8f);
        yield return Glow(.4f, false, true, false);
        Garland(false);
        yield return Wait(Rest(1.2f));
    }

    // Beads strung edge to edge on a parabola: low in the middle (sag) or high (arch).
    void Garland(bool sag)
    {
        var edge = CombatRound.Border;
        int beads = Dense(17) | 1;
        float left = edge.xMin + .45f;
        float right = edge.xMax - .45f;
        float high = Camera.UpLimit - .35f;
        float low = transform.position.y - 3.3f;
        for (int k = 0; k < beads; k++)
        {
            float u = k / (beads - 1f) * 2 - 1;
            float x = Mathf.Lerp(left, right, (u + 1) * .5f);
            float y = sag ? Mathf.Lerp(low, high, u * u) : Mathf.Lerp(high, low, u * u);
            var eye = eyes[u < 0 ? 0 : 1];
            if (eye == null)
                continue;
            // Souls leave from the middle outward; the embers unzip off the ends after them.
            float order = Mathf.Abs(u);
            if (k % 2 == 0)
            {
                UseTone(HostileTone.Bright);
                Stare<SimpleRound>(eye.position, new Vector2(x, y), 1.0f, .45f + order * 1.1f, 3.0f, .34f);
            }
            else
            {
                UseTone(HostileTone.Dark);
                float outward = Mathf.Sign(u);
                Hang<FlameRound>(eye.position, new Vector2(x, y), 1.0f, .75f + order * 1.1f,
                    CombatVisuals.Rotate(Vector2.right * outward, -outward * 25), 1.4f, .24f)
                    .FadeAt(7);
            }
        }
    }

    // A snip: each pincer opens two bright arrow blades either side of its aim, each a
    // line of graded speeds, so the four blades cross into scissors over the pilot.
    void Snip()
    {
        UseTone(HostileTone.Bright);
        int blade = Dense(7);
        for (int side = 0; side < 2; side++)
        {
            if (claws[side] == null)
                continue;
            var port = claws[side].TransformPoint(Pincer);
            if (player != null && Vector2.Distance(port, player.position) < 1.8f)
                continue;
            var aim = CombatVisuals.Aim(port);
            for (int jaw = -1; jaw <= 1; jaw += 2)
                for (int k = 0; k < blade; k++)
                    ScoredShot(k == 0, port, CombatVisuals.Rotate(aim, jaw * 15), 4.6f - k * 1.6f / Mathf.Max(1, blade), .19f);
        }
    }

    // Brimstone Braid: plaited strands from the moving pincers, lattices from the mouth.
    // It runs at least 5.5 s and hands over just before the claws' next stroke.
    IEnumerator BrimstoneBraid()
    {
        yield return Glow(.45f, true, false, false);
        float interval = Rest(.08f);
        float elapsed = 0;
        float before = ClawTime;
        for (int i = 0; ; i++)
        {
            // Eight beats of braid, then three open: the gap travels down each braid.
            if (i % 11 < 8)
                BraidStep(i);
            if (i % 20 == 10)
                BrimstoneLattice(i / 20);
            yield return Wait(interval);
            elapsed += interval;
            float now = ClawTime;
            bool stroke = Crossed(before, now, .5f);
            before = now;
            if (elapsed > 5.5f && stroke)
                yield break;
        }
    }

    void BraidStep(int i)
    {
        var edge = CombatRound.Border;
        for (int side = 0; side < 2; side++)
        {
            if (claws[side] == null)
                continue;
            var port = claws[side].TransformPoint(Pincer);
            if (player != null && Vector2.Distance(port, player.position) < 1.6f)
                continue;
            // Toward the far lower corner, so the two braids cross under the crab.
            var corner = new Vector2(side == 0 ? edge.xMax - 1 : edge.xMin + 1, edge.yMin + 1);
            var axis = (corner - (Vector2)port).normalized;
            UseTone(HostileTone.Dark);
            for (int strand = -1; strand <= 1; strand += 2)
                Fire<FlameRound>(port, axis, 3.2f, .24f)
                    .Sway(strand * .42f, .7f);
            if (i % 4 == 0)
            {
                UseTone(HostileTone.Bright);
                Fire<SimpleRound>(port, axis, 3.2f, .2f);
            }
        }
    }

    // Brimstone lattice: bright arrows from the mouth hang in a row along the ceiling, then
    // fall away in alternating diagonals that cross on the way down.
    void BrimstoneLattice(int n)
    {
        UseTone(HostileTone.Bright);
        var mouth = Hull.TransformPoint(Mouth);
        var edge = CombatRound.Border;
        int count = Dense(12);
        float top = Camera.UpLimit - .3f;
        for (int k = 0; k < count; k++)
        {
            float x = Mathf.Lerp(edge.xMin + .4f, edge.xMax - .4f, (k + .25f + (n % 2) * .5f) / count);
            int lean = k % 2 == 0 ? 1 : -1;
            Hang<NeedleRound>(mouth, new Vector2(x, top), .8f, .3f, CombatVisuals.Rotate(Vector2.down, lean * 24), 2.6f, .19f);
        }
    }

    // Searing Trace: the trail through the stroke, then the mouth's falling arcs.
    IEnumerator SearingTrace()
    {
        yield return UntilClaw(.9f);
        yield return Glow(.4f, true, false, false);
        while (ClawTime < 4.6f)
        {
            TraceStep(HostileTone.Bright);
            yield return Wait(Rest(.05f));
        }
        for (int arc = 0; arc < 3; arc++)
        {
            yield return Glow(.35f, false, false, true);
            UseTone(HostileTone.Dark);
            ArrowWavefront(Hull.TransformPoint(Mouth), CombatVisuals.Rotate(Vector2.down, (arc - 1) * 9), Dense(15) | 1, 110, 3, 2.4f, .5f);
            yield return Wait(Rest(.45f));
        }
    }

    // One ember from each pincer, left almost where the pincer is; it stares, then darts.
    void TraceStep(HostileTone shade)
    {
        UseTone(shade);
        for (int side = 0; side < 2; side++)
        {
            if (claws[side] == null)
                continue;
            var port = claws[side].TransformPoint(Pincer);
            if (player != null && Vector2.Distance(port, player.position) < 1.6f)
                continue;
            Fire<FlameRound>(port, Vector2.down, .5f, .24f)
                .Pause(.3f, 1.1f, CombatRound.Turn.Aim, 0, 6);
        }
    }

    // Ninth Circle (Hard): trace, cage and lattice together, one claw stroke at a time.
    IEnumerator NinthCircle()
    {
        yield return UntilClaw(.9f);
        yield return Glow(.4f, true, true, false);
        bool caged = false;
        while (ClawTime < 4.6f)
        {
            if (!caged && ClawTime >= 1.95f)
            {
                caged = true;
                Cage(CagePoint(), 1, HostileTone.Bright, HostileTone.Bright);
            }
            TraceStep(HostileTone.Dark);
            yield return Wait(Rest(.07f));
        }
        for (int n = 0; n < 2; n++)
        {
            yield return Glow(.35f, false, false, true);
            BrimstoneLattice(n);
            yield return Wait(Rest(.8f));
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
