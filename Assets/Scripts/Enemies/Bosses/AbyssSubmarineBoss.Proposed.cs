using System.Collections;
using UnityEngine;

// Proposed card book for the abyss submarine (review only, DistrictBoss.ProposedCards).
// Ideas and comparisons: ArtSource/BossPhaseProposals/Ideas/AbyssSubmarineBoss.md
// The dive cycle stays the spell sequence. MoveBoss still dives, marks the emergence,
// erupts with its crowns, charges with its wake and keeps the hull safe while submerged.
// This book fills only the surfaced stretch of each cycle with one concept per phase,
// flying a new variation of it on each surfacing. Wait() already holds through every
// dive, and a verse that a dive interrupts is abandoned rather than finished later.
// Phase one, Torpedo Herringbone: dark torpedoes trailed by bright droplet wakes that hang
//   where the torpedo passed and peel away to alternate sides, so each track opens into a V.
//   Surfacings vary the tracks: a spread, crossing tracks, then wakes peeling forward.
// Phase two, Countercurrent: level fans from both shoulders caught by a current back toward
//   the centre and down, looping out just short of the walls and crossing into a sinking
//   lattice. Surfacings vary the loops: wide, a climbing fountain, then tight and fast.
// Crush Depth (Hard), Hadal Trench: dark cliffs hang along both walls and slide inward so
//   the open water narrows into a sinking trench; herringbones run down it under a bright
//   curtain. Surfacings alternate a diagonal and a level closing.
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
                yield return HadalTrench(played, verse);
            else if (phaseTwo)
                yield return Countercurrent(played, verse);
            else
                yield return TorpedoHerringbone(played, verse);
            verse++;
        }
    }

    // Torpedo Herringbone: three salvos per surfacing, twelve torpedoes in all.
    //   Spread: three tubes aimed, four unaimed and mirrored, then all five aimed.
    //   Crossing: the shoulder tracks cut inward under the bank tracks, an X of wakes.
    //   Arrowheads: the wakes peel forward, so every track trails a chevron.
    IEnumerator TorpedoHerringbone(int cycle, int verse)
    {
        int variant = verse % 3;
        int wake = Dense(12);
        float peel = variant == 2 ? 64 : 112;
        for (int salvo = 0; salvo < 3; salvo++)
        {
            bool aimed = variant == 2 ? salvo == 1 : salvo != 1;
            yield return ChargeAt(salvo == 0 ? .45f : .3f, ProposedAllTubes);
            if (Dived(cycle))
                yield break;
            var aim = aimed ? CombatVisuals.Aim(Hull.TransformPoint(ProposedProw)) : Vector2.down;
            if (variant == 0)
                SpreadSalvo(salvo, aim, wake, peel);
            else if (variant == 1)
                CrossingSalvo(salvo, aim, wake, peel);
            else
                SpreadSalvo(2 - salvo, aim, wake, peel);
            yield return Wait(Rest(.62f));
        }
    }

    // A fan of three, four or five tracks, mirrored about the aim.
    void SpreadSalvo(int size, Vector2 aim, int wake, float peel)
    {
        if (size == 1)
        {
            HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -10), wake, peel);
            HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, 10), wake, peel);
            HerringboneTorpedo(ProposedChannelLeft, CombatVisuals.Rotate(aim, -38), wake, peel);
            HerringboneTorpedo(ProposedChannelRight, CombatVisuals.Rotate(aim, 38), wake, peel);
            return;
        }
        float bank = size == 0 ? 22 : 18;
        HerringboneTorpedo(ProposedProw, aim, wake, peel);
        HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -bank), wake, peel);
        HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, bank), wake, peel);
        if (size == 2)
        {
            HerringboneTorpedo(ProposedChannelLeft, CombatVisuals.Rotate(aim, -42), wake, peel);
            HerringboneTorpedo(ProposedChannelRight, CombatVisuals.Rotate(aim, 42), wake, peel);
        }
    }

    // The shoulder tracks turn inward and cross under the outward bank tracks.
    void CrossingSalvo(int size, Vector2 aim, int wake, float peel)
    {
        float cross = 26 + size * 6;
        HerringboneTorpedo(ProposedChannelLeft, CombatVisuals.Rotate(aim, cross), wake, peel);
        HerringboneTorpedo(ProposedChannelRight, CombatVisuals.Rotate(aim, -cross), wake, peel);
        if (size != 0)
        {
            HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -16), wake, peel);
            HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, 16), wake, peel);
        }
        if (size != 1)
            HerringboneTorpedo(ProposedProw, aim, wake, peel);
    }

    // One dark torpedo and its bright droplet wake, launched together at graded speeds.
    void HerringboneTorpedo(Vector3 local, Vector2 axis, int wake, float peel)
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
                .Pause(1.0f, hold, CombatRound.Turn.Rotate, side * peel, .85f / speed)
                .FadeAt(4.4f + hold);
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

    // Countercurrent: eleven looping fans per surfacing, offset by half a lane each fan.
    //   Wide: level fans whose loops turn just short of the walls and cross mid-arena.
    //   Fountain: raised fans; the steep lanes climb to the top edge and fall back.
    //   Tight: low fans in a stronger current, small loops crossing close under the hull.
    IEnumerator Countercurrent(int cycle, int verse)
    {
        int variant = verse % 3;
        float low = variant == 1 ? 5 : variant == 2 ? -40 : -28;
        float high = variant == 1 ? 55 : variant == 2 ? 20 : 38;
        float sink = variant == 1 ? .9f : .5f;
        float span = variant == 2 ? .5f : 1;
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
                // Each loop turns back short of its own wall, wherever the hull drifts.
                float reach = Mathf.Max(1.2f, ((side < 0 ? port.x - edge.xMin : edge.xMax - port.x) - .45f) * span);
                for (int lane = 0; lane < lanes; lane++)
                {
                    float rise = Mathf.Lerp(low, high, (lane + (fan % 2) * .5f) / lanes);
                    var axis = CombatVisuals.Rotate(Vector2.right * side, side * rise);
                    for (int layer = 0; layer < 2; layer++)
                    {
                        float speed = layer == 0 ? 2.5f : 1.9f;
                        float outward = speed * Pace * Mathf.Cos(rise * Mathf.Deg2Rad);
                        var current = new Vector2(-side * outward * outward / (2 * reach), -sink);
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

    // Hadal Trench (Hard): cliffs along both walls closing inward, herringbones down the
    // middle. Alternate surfacings close the cliffs diagonally or almost level.
    IEnumerator HadalTrench(int cycle, int verse)
    {
        yield return ChargeAt(.4f, ProposedChannels);
        if (Dived(cycle))
            yield break;
        Spout(HostileTone.Bright);
        int rows = Dense(13);
        int wake = Dense(10);
        float slope = verse % 2 == 0 ? 38 : 72;
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
                var inward = CombatVisuals.Rotate(Vector2.down, -side * slope);
                for (int k = 0; k < rows; k++)
                {
                    float y = port.y + .9f - (k + ((wall + verse) % 2) * .5f) * 5.6f / rows;
                    Hang<SimpleRound>(port, new Vector2(x, y), .6f, .2f, inward, 1.0f, .2f)
                        .FadeAt(8);
                }
            }
            if (wall % 3 == 1)
            {
                var aim = CombatVisuals.Aim(Hull.TransformPoint(ProposedProw));
                HerringboneTorpedo(ProposedProw, aim, wake, 112);
                HerringboneTorpedo(ProposedBankLeft, CombatVisuals.Rotate(aim, -16), wake, 112);
                HerringboneTorpedo(ProposedBankRight, CombatVisuals.Rotate(aim, 16), wake, 112);
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
