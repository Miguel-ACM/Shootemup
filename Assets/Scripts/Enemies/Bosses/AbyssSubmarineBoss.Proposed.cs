using System.Collections;
using UnityEngine;

// Proposed card book for the abyss submarine (review only, DistrictBoss.ProposedCards).
// Ideas and comparisons: ArtSource/BossPhaseProposals/Ideas/AbyssSubmarineBoss.md
// The dive cycle stays the spell sequence. MoveBoss still dives, marks the emergence,
// erupts with its crowns, charges with its wake and keeps the hull safe while submerged;
// this book only fills the surfaced stretch of each cycle, one verse per surfacing,
// taking the phase's two cards in turn. Wait() already holds through every dive, and a
// verse that a dive interrupts is abandoned rather than finished on the next surfacing.
// Phase one
//   Torpedo Herringbone: dark torpedoes from three, four, then five tubes, each trailed by a
//     bright line of droplets that hang where it passed and peel away to alternate sides,
//     the farthest first, so every track opens into a widening V.
//   Caustic Net: each shoulder channel pours two dark arrow strands swaying in opposite
//     phase, braiding chains of lens cells that ripple as the hull drifts, with bright
//     pearls down each chain and two-beat gaps to slip through; a bright spout hangs along
//     the top edge and sinks as a swaying curtain.
// Phase two
//   Countercurrent: level fans from both shoulders, every round caught by a current back
//     toward the centre and down. Each fan loops out just short of its wall and returns
//     across the middle, so both sides' loops cross into a sinking lattice; bright arrows
//     loop wide around dark beads, and a dark jet from the prow answers a still pilot.
//   Kelp Forest: the launch banks plant dark droplet stalks down into the arena and the
//     upper channels plant them out along the top; each stalk hangs, then sinks swaying,
//     every droplet a little out of step, so it ripples like kelp. Bright arrow shoals
//     dart at the pilot between plantings.
// Crush Depth (Hard)
//   Hadal Trench: dark circular cliffs hang along both walls, then slide down and inward so
//     the open water narrows into a sinking trench; torpedo herringbones run down it and a
//     bright curtain closes the surface.
public sealed partial class AbyssSubmarineBoss
{
    static readonly Vector3 ProposedProw = new Vector3(0, -1.65f, -.5f);
    static readonly Vector3 ProposedBankLeft = new Vector3(-.65f, -.6f, -.4f);
    static readonly Vector3 ProposedBankRight = new Vector3(.65f, -.6f, -.4f);
    static readonly Vector3 ProposedChannelLeft = new Vector3(-1.02f, .3f, -.3f);
    static readonly Vector3 ProposedChannelRight = new Vector3(1.02f, .3f, -.3f);
    // The upper lane of each launch channel, the highest port with its own charge cue.
    static readonly Vector3 ProposedHighLeft = new Vector3(-1.02f, .65f, -.3f);
    static readonly Vector3 ProposedHighRight = new Vector3(1.02f, .65f, -.3f);
    static readonly Vector3[] ProposedTubes = { ProposedProw, ProposedBankLeft, ProposedBankRight };
    static readonly Vector3[] ProposedAllTubes = { ProposedProw, ProposedBankLeft, ProposedBankRight, ProposedChannelLeft, ProposedChannelRight };
    static readonly Vector3[] ProposedChannels = { ProposedChannelLeft, ProposedChannelRight };
    static readonly Vector3[] ProposedPlanters = { ProposedBankLeft, ProposedBankRight, ProposedHighLeft, ProposedHighRight };

    // World units per authored speed unit, so curves and hanging points follow real travel.
    float Pace => 1 / WorldSpeed(1);
    bool SurfacedNow => !Submerged && Mathf.Repeat(cycleClock, cycleSeconds) < 3.5f;
    bool Dived(int cycle) => lastCycle != cycle;

    protected override IEnumerator Proposed()
    {
        int played = -1;
        int verse = 0;
        while (true)
        {
            // Wait() only counts surfaced time, so this returns during a surfacing.
            yield return Wait(.05f);
            if (lastCycle == played || !SurfacedNow)
                continue;
            played = lastCycle;
            if (phaseThree)
                yield return HadalTrench(played);
            else if (phaseTwo)
                yield return verse % 2 == 0 ? Countercurrent(played) : KelpForest(played);
            else
                yield return verse % 2 == 0 ? TorpedoHerringbone(played) : CausticNet(played);
            verse++;
        }
    }

    // Torpedo Herringbone: three salvos, from three, four, then five tubes.
    IEnumerator TorpedoHerringbone(int cycle)
    {
        int wake = Dense(12);
        for (int salvo = 0; salvo < 3; salvo++)
        {
            yield return ChargeAt(salvo == 0 ? .45f : .3f, salvo == 1 ? ProposedChannels : salvo == 0 ? ProposedTubes : ProposedAllTubes);
            if (Dived(cycle))
                yield break;
            var aim = CombatVisuals.Aim(Hull.TransformPoint(ProposedProw));
            if (salvo == 0)
            {
                HerringboneTorpedo(ProposedProw, aim, wake);
                HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -22), wake);
                HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, 22), wake);
            }
            else if (salvo == 1)
            {
                // Unaimed: two inner tracks and two wide ones, mirrored about straight down.
                HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(Vector2.down, -10), wake);
                HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(Vector2.down, 10), wake);
                HerringboneTorpedo(ProposedChannelLeft, CombatVisuals.Rotate(Vector2.down, -38), wake);
                HerringboneTorpedo(ProposedChannelRight, CombatVisuals.Rotate(Vector2.down, 38), wake);
            }
            else
            {
                HerringboneTorpedo(ProposedProw, aim, wake);
                HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -18), wake);
                HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, 18), wake);
                HerringboneTorpedo(ProposedChannelLeft, CombatVisuals.Rotate(aim, -42), wake);
                HerringboneTorpedo(ProposedChannelRight, CombatVisuals.Rotate(aim, 42), wake);
            }
            yield return Wait(Rest(.62f));
        }
    }

    // One dark torpedo and its bright droplet wake, launched together at graded speeds.
    void HerringboneTorpedo(Vector3 local, Vector2 axis, int wake)
    {
        var port = Hull.TransformPoint(local);
        UseTone(HostileTone.Dark);
        Fire<SimpleRound>(port, axis, 3.0f, .32f)
            .Ramp(2.1f, .8f, 1.0f);
        UseTone(HostileTone.Bright);
        for (int j = 0; j < wake; j++)
        {
            float u = j / Mathf.Max(1f, wake - 1);
            float speed = Mathf.Lerp(2.75f, .85f, u);
            // The farthest droplets peel away first, so the wake opens into a V.
            float hold = .2f + (1 - u) * .5f;
            int side = j % 2 == 0 ? 1 : -1;
            Fire<DropletRound>(port, axis, speed, .22f)
                .Pause(1.0f, hold, CombatRound.Turn.Rotate, side * 112, .85f / speed)
                .FadeAt(4.4f + hold);
        }
    }

    // Caustic Net: two counter-swaying strands per shoulder, pearls on each chain's axis.
    IEnumerator CausticNet(int cycle)
    {
        yield return ChargeAt(.45f, ProposedChannels);
        if (Dived(cycle))
            yield break;
        Spout(HostileTone.Bright);
        int steps = Dense(32);
        float interval = Rest(.07f) * 32f / steps;
        for (int i = 0; i < steps; i++)
        {
            // Every chain breaks for two beats in eight: a moving gap to slip through.
            if (i % 8 < 6)
            {
                float splay = 9 + 9 * Mathf.Sin(i * .3f);
                for (int side = -1; side <= 1; side += 2)
                {
                    var port = Hull.TransformPoint(side < 0 ? ProposedChannelLeft : ProposedChannelRight);
                    var axis = CombatVisuals.Rotate(Vector2.down, side * splay);
                    UseTone(HostileTone.Dark);
                    for (int strand = -1; strand <= 1; strand += 2)
                        Fire<NeedleRound>(port, axis, 3.0f, .19f)
                            .Sway(strand * .5f, .55f);
                    if (i % 4 == 0)
                    {
                        UseTone(HostileTone.Bright);
                        Fire<SimpleRound>(port, axis, 3.0f, .2f);
                    }
                }
            }
            if (i == steps / 2)
                Spout(HostileTone.Bright);
            yield return Wait(interval);
            if (Dived(cycle))
                yield break;
        }
    }

    // A spout from both upper channels: droplets hang in a line along the top edge, the
    // inner ones first, then sink as a swaying curtain, so the surface is never a refuge.
    void Spout(HostileTone shade)
    {
        UseTone(shade);
        int drops = Dense(8);
        float top = Camera.UpLimit - .35f;
        var edge = CombatRound.Border;
        for (int side = -1; side <= 1; side += 2)
        {
            var port = Hull.TransformPoint(side < 0 ? ProposedHighLeft : ProposedHighRight);
            float inner = transform.position.x + side * .1f;
            float wall = side < 0 ? edge.xMin + .35f : edge.xMax - .35f;
            for (int k = 0; k < drops; k++)
            {
                float x = Mathf.Lerp(inner, wall, (k + .5f) / drops);
                Hang<DropletRound>(port, new Vector2(x, top), .7f, .3f + k * .06f, Vector2.down, 1.3f, .22f)
                    .Sway(.22f, .45f)
                    .FadeAt(9);
            }
        }
    }

    // Countercurrent: looping fans from both shoulders, offset by half a lane each fan.
    IEnumerator Countercurrent(int cycle)
    {
        yield return ChargeAt(.45f, ProposedChannels);
        int lanes = Dense(10);
        for (int fan = 0; fan < 11; fan++)
        {
            if (Dived(cycle))
                yield break;
            var edge = CombatRound.Border;
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(side < 0 ? ProposedChannelLeft : ProposedChannelRight);
                // Each loop turns back just short of its own wall, wherever the hull drifts.
                float reach = Mathf.Max(1.2f, (side < 0 ? port.x - edge.xMin : edge.xMax - port.x) - .45f);
                for (int lane = 0; lane < lanes; lane++)
                {
                    float rise = Mathf.Lerp(-28, 38, (lane + (fan % 2) * .5f) / lanes);
                    var axis = CombatVisuals.Rotate(Vector2.right * side, side * rise);
                    for (int layer = 0; layer < 2; layer++)
                    {
                        float speed = layer == 0 ? 2.5f : 1.9f;
                        float outward = speed * Pace * Mathf.Cos(rise * Mathf.Deg2Rad);
                        var current = new Vector2(-side * outward * outward / (2 * reach), -.5f);
                        UseTone(layer == 0 ? HostileTone.Bright : HostileTone.Dark);
                        ScoredShot(layer == 1, port, axis, speed, layer == 0 ? .19f : .2f)
                            .Fall(current);
                    }
                }
            }
            if (fan % 3 == 1)
                PressureJet();
            yield return Wait(Rest(.21f));
        }
    }

    // A dark jet from the prow: a short line of droplets at graded speeds, aimed.
    void PressureJet()
    {
        var port = Hull.TransformPoint(ProposedProw);
        var aim = CombatVisuals.Aim(port);
        UseTone(HostileTone.Dark);
        for (int j = 0; j < 5; j++)
            Fire<DropletRound>(port, aim, 3.6f - j * .3f, .22f);
    }

    // Kelp Forest: six plantings, each four stalks, with mirrored shoals on both half-beats.
    IEnumerator KelpForest(int cycle)
    {
        yield return ChargeAt(.4f, ProposedPlanters);
        for (int planting = 0; planting < 6; planting++)
        {
            if (Dived(cycle))
                yield break;
            // The stalks lean alternately narrow and wide, so the clearings move.
            float lean = planting % 2 == 0 ? 10 + planting * 6 : 30 + planting * 6;
            UseTone(HostileTone.Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                Stalk(side < 0 ? ProposedBankLeft : ProposedBankRight, CombatVisuals.Rotate(Vector2.down, side * lean), Dense(12), 1.0f, .3f);
                Stalk(side < 0 ? ProposedHighLeft : ProposedHighRight, CombatVisuals.Rotate(Vector2.up, -side * (62 + planting * 4)), Dense(9), .8f, .2f);
            }
            for (int beat = 0; beat < 2; beat++)
            {
                yield return Wait(Rest(.22f));
                if (Dived(cycle))
                    yield break;
                Shoal(ProposedChannelLeft);
                Shoal(ProposedChannelRight);
            }
        }
    }

    // A stalk: droplets along one lane at graded speeds hang as a line, then sink, each
    // swaying at a slightly different rate so the line ripples.
    void Stalk(Vector3 local, Vector2 lane, int drops, float slowest, float step)
    {
        var port = Hull.TransformPoint(local);
        for (int j = 0; j < drops; j++)
        {
            float speed = slowest + j / Mathf.Max(1f, drops - 1) * step * 11;
            Fire<DropletRound>(port, lane, speed, .22f)
                .Pause(.9f, .45f, CombatRound.Turn.Down, 0, 1.1f / speed)
                .Sway(.3f, .4f + j * .165f / Mathf.Max(1f, drops - 1))
                .FadeAt(7.5f);
        }
    }

    // A shoal: a diamond school of bright arrows, rows by speed and width by angle, aimed.
    void Shoal(Vector3 local)
    {
        var port = Hull.TransformPoint(local);
        var aim = CombatVisuals.Aim(port);
        UseTone(HostileTone.Bright);
        for (int row = 0; row < 5; row++)
        {
            int width = row < 3 ? row : 4 - row;
            for (int k = -width; k <= width; k += 2)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, k * 3.5f), 4.4f - row * .18f, .19f);
        }
    }

    // Hadal Trench (Hard): cliffs along both walls closing inward, herringbones down the middle.
    IEnumerator HadalTrench(int cycle)
    {
        yield return ChargeAt(.4f, ProposedChannels);
        if (Dived(cycle))
            yield break;
        Spout(HostileTone.Bright);
        int rows = Dense(13);
        int wake = Dense(10);
        for (int wall = 0; wall < 11; wall++)
        {
            if (Dived(cycle))
                yield break;
            var edge = CombatRound.Border;
            UseTone(HostileTone.Dark);
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(side < 0 ? ProposedChannelLeft : ProposedChannelRight);
                float x = side < 0 ? edge.xMin + .5f : edge.xMax - .5f;
                var inward = CombatVisuals.Rotate(Vector2.down, -side * 38);
                for (int k = 0; k < rows; k++)
                {
                    float y = port.y + .9f - (k + (wall % 2) * .5f) * 5.6f / rows;
                    Hang<SimpleRound>(port, new Vector2(x, y), .6f, .2f, inward, 1.0f, .2f)
                        .FadeAt(8);
                }
            }
            if (wall % 3 == 1)
            {
                var aim = CombatVisuals.Aim(Hull.TransformPoint(ProposedProw));
                HerringboneTorpedo(ProposedProw, aim, wake);
                HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -16), wake);
                HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, 16), wake);
            }
            // The tubes glow through the beat before each herringbone salvo.
            if (wall % 3 == 0)
                yield return ChargeAt(Rest(.2f), ProposedTubes);
            else
                yield return Wait(Rest(.2f));
        }
    }

    // A visible charge at several hull ports at once for the whole wait.
    IEnumerator ChargeAt(float seconds, Vector3[] locals)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            foreach (var local in locals)
                Cue(Hull.TransformPoint(local), seconds - t);
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
}
