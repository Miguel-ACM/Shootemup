using System.Collections;
using UnityEngine;

// Proposed Snow fortress card book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set. Same hull, ports and comb; new geometry.
// Phase one (rain)
//   Dendrite Bloom: both side ports grow mirrored ice dendrites. Each frond is a stem
//     of arrows launched together at rising speeds, so it lengthens as it flies; barbs
//     ride beside each link, freeze, then branch off at sixty degrees like a snow
//     crystal and accelerate. The fronds sweep from level (the upper flanks) down
//     toward the centre line and back.
//   Sleet Lattice: the comb lines the top edge and rains two crossing families of
//     slanted arrows, so the rain forms a diamond lattice. A swinging wind tilts both
//     families and the diamond holes drift across the arena; halfway the families swap.
//     Loosely aimed mouth rounds fall between the rain beats.
// Phase two (snow)
//   Avalanche Cascade: a fan of snow shells stops on a slanted line and bursts one
//     after another, so the snow sweeps across the arena from one side, while arrow
//     streams from the sides open and close beneath it like scissors, starting slow
//     and accelerating.
//   Icicle Harp: the comb is strummed from one side to the other. Each string drops a
//     fast and a slow arrow, so a strum falls as two separating diagonal slashes with
//     two muted strings for a gap. Strums alternate direction and the gap walks; the
//     mouth adds fans that freeze and re-aim, and a shell's snow now and then.
// Hard phase three, Diamond Dust: strums from both sides at once fall as crossing Xs,
//   with dendrites between them.
// Every round thrown out of the hull's sides is an arrow, and the comb rains arrows
// straight from the top edge. Tones: Dendrite Bloom bright fronds with dark mouth
// rounds; Sleet Lattice dark rain with bright mouth rounds; Avalanche Cascade bright
// shells and snow with dark scissors; Icicle Harp dark strings with bright mouth fans.
// Diamond Dust crosses a dark strum with a bright one, with bright dendrites.
public sealed partial class SnowSiegeBoss
{
    static Vector3 SideLocal(int mirror) => new Vector3(mirror * .55f, -.55f, -.25f);
    static Vector3 GustLocal(int mirror) => new Vector3(mirror * .9f, -.3f, -.25f);
    static readonly Vector3 ApertureLocal = new Vector3(0, -.26f, -.55f);
    Vector3 CombPort(int i) => comb[i].TransformPoint(ApertureLocal);

    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return DiamondDust();
            else if (!phaseTwo)
            {
                yield return DendriteBloom();
                yield return Wait(1);
                yield return SleetLattice();
                yield return Wait(1);
            }
            else
            {
                yield return AvalancheCascade();
                yield return Wait(.9f);
                yield return IcicleHarp();
                yield return Wait(.9f);
            }
        }
    }

    // ---- Phase one ----

    IEnumerator DendriteBloom()
    {
        for (int bloom = 0; bloom < 6; bloom++)
        {
            yield return ChargeSides(.4f);
            // From nearly level (the upper flanks) toward the centre line and back again.
            float reach = Mathf.Lerp(80, 24, Mathf.PingPong(bloom * .5f, 1));
            UseTone(HostileTone.Bright);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Dendrite(Hull.TransformPoint(SideLocal(mirror)), CombatVisuals.Rotate(Vector2.down, mirror * reach));
            // Slow mouth rounds aimed loosely at the pilot, accelerating, keep the space
            // under the fronds moving.
            UseTone(HostileTone.Dark);
            int loose = Dense(3);
            var aim = CombatVisuals.Aim(Mouth);
            for (int j = 0; j < loose; j++)
                Fire<SimpleRound>(Mouth, CombatVisuals.Rotate(aim, (j - (loose - 1) * .5f) * 16), 1.1f, .22f)
                    .Ramp(2.6f, 1.1f, .25f);
            yield return Wait(Rest(.95f));
        }
    }

    // One snow-crystal arm. The stem's arrows leave together at rising speeds; a pair
    // of barbs rides beside each link, freezes, then branches off at sixty degrees.
    void Dendrite(Vector3 port, Vector2 axis)
    {
        int links = Mathf.Max(3, Dense(5));
        for (int link = 0; link < links; link++)
        {
            float speed = 1.0f + .42f * link;
            Fire<NeedleRound>(port, axis, speed, .19f)
                .Ramp(2.2f, 1.0f, .9f);
            if (link == 0)
                continue;
            float hold = .22f + link * .05f;
            for (int branch = -1; branch <= 1; branch += 2)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, branch * 9), speed, .19f)
                    .Pause(.65f, hold, CombatRound.Turn.Rotate, branch * 51, 1.5f / speed)
                    .Ramp(2.0f, 1.0f, .65f + hold);
        }
    }

    // The comb rains two crossing families of slanted arrows: even columns lean one way,
    // odd columns the other, so the rain falls as a diamond lattice.
    IEnumerator SleetLattice()
    {
        DeployComb();
        yield return ChargeComb(.6f);
        const int Beats = 24;
        for (int k = 0; k < Beats; k++)
        {
            // The wind tilts both families together, so the holes drift sideways; halfway
            // through the families swap and the holes jump half a diamond.
            float wind = 13 * Mathf.Sin(k * .26f);
            bool swapped = k >= Beats / 2;
            UseTone(HostileTone.Dark);
            for (int i = 0; i < combCount; i++)
            {
                // Lighter difficulties drop a diagonal of strings, never a whole column.
                if (!Difficulty.Hard && (i + k) % 3 == 0 || Difficulty.Easy && (i + k) % 3 == 1)
                    continue;
                int family = (i % 2 == 0) != swapped ? 1 : -1;
                Fire<NeedleRound>(CombPort(i), CombatVisuals.Rotate(Vector2.down, wind + family * 21), 2.7f, .19f);
                releases[i] = .1f;
            }
            if (k % 2 == 1)
            {
                // Between the rain beats: a loose spray around the pilot from the mouth.
                UseTone(HostileTone.Bright);
                int loose = Dense(3);
                var aim = CombatVisuals.Aim(Mouth);
                for (int j = 0; j < loose; j++)
                    Fire<SimpleRound>(Mouth, CombatVisuals.Rotate(aim, (j - (loose - 1) * .5f) * 12 + Random.Range(-8f, 8f)), 1.3f, .21f)
                        .Ramp(2.2f, 1.0f, .2f);
            }
            yield return Wait(Rest(.4f));
        }
        yield return Wait(.5f);
        RecallComb();
    }

    // ---- Phase two ----

    IEnumerator AvalancheCascade()
    {
        Phrase(Toned(HostileTone.Dark, Scissors(8)));
        for (int fan = 0; fan < 3; fan++)
        {
            yield return Charge(Hull, MouthLocal, .45f);
            yield return Cascade(fan % 2 == 0 ? 1 : -1);
            yield return Wait(Rest(2.2f));
        }
    }

    // Shells lobbed one after another onto a slanted line: the first lands near and
    // high on one side, the last far and low on the other. Each bursts as it settles,
    // so the snow sweeps across the arena from the side the cascade starts on.
    IEnumerator Cascade(int from)
    {
        int shells = Difficulty.Normal ? 3 : Dense(5);
        var edge = CombatRound.Border;
        for (int c = 0; c < shells; c++)
        {
            float t = shells > 1 ? c / (shells - 1f) : .5f;
            var target = new Vector2(-from * Mathf.Lerp(edge.xMax - 1.3f, -(edge.xMax - 1.3f), t), Mouth.y - Mathf.Lerp(2.8f, 5.8f, t));
            var path = target - (Vector2)Mouth;
            UseTone(HostileTone.Bright);
            var shell = Fire<BurstShell>(Mouth, path.normalized, 2.7f, .5f);
            if (shell != null)
                shell.SetTravelDistance(path.magnitude);
            yield return Wait(.12f);
        }
    }

    // Arrow streams from the gust ports cross under the bow, opening out to the flanks
    // and closing again; each arrow starts slow and accelerates.
    IEnumerator Scissors(float seconds)
    {
        float interval = Rest(.13f);
        for (float t = 0; t < seconds; t += interval)
        {
            float cross = 10 + 34 * Mathf.Sin(t * 1.9f);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<NeedleRound>(Hull.TransformPoint(GustLocal(mirror)), CombatVisuals.Rotate(Vector2.down, -mirror * cross), 1.0f, .19f)
                    .Ramp(3.2f, 1.2f, .3f);
            yield return Wait(interval);
        }
    }

    IEnumerator IcicleHarp()
    {
        DeployComb();
        yield return ChargeComb(.6f);
        const int Strums = 8;
        for (int s = 0; s < Strums; s++)
        {
            // Two muted strings walk across the comb, strum by strum.
            int gap = 1 + s * 4 % (combCount - 3);
            Phrase(Toned(HostileTone.Dark, Strum(s % 2 == 0 ? 1 : -1, gap)));
            if (s % 2 == 1)
                FrostFan();
            if (s % 3 == 1)
            {
                // Now and then a dark shell toward the pilot; its snow falls from the top.
                UseTone(HostileTone.Dark);
                var shell = Fire<BurstShell>(Mouth, CombatVisuals.Aim(Mouth), 2.7f, .5f);
                if (shell != null)
                    shell.SetTravelDistance(5.4f);
            }
            yield return Wait(Rest(.95f));
        }
        yield return Wait(1.2f);
        RecallComb();
    }

    // Strings released one after another along the comb. Each drops a fast arrow and a
    // slower echo, so the strum falls as two diagonal slashes that draw apart.
    IEnumerator Strum(int direction, int gap)
    {
        for (int n = 0; n < combCount; n++)
        {
            int i = direction > 0 ? n : combCount - 1 - n;
            if (i < gap || i >= gap + 2)
            {
                var port = CombPort(i);
                Fire<NeedleRound>(port, Vector2.down, 3.4f, .19f);
                if (Difficulty.Hard || !Difficulty.Easy && i % 2 == 0)
                    Fire<NeedleRound>(port, Vector2.down, 2.0f, .19f);
                releases[i] = .1f;
            }
            yield return Wait(.09f);
        }
    }

    // A bright fan from the mouth: it flies out, stops dead, re-aims at the pilot,
    // crawls and accelerates.
    void FrostFan()
    {
        UseTone(HostileTone.Bright);
        int count = Dense(7);
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<SimpleRound>(Mouth, CombatVisuals.Rotate(Vector2.down, lane * 12), 2.6f, .22f)
                .Pause(.6f, .35f + Mathf.Abs(lane) * .04f, CombatRound.Turn.Aim, lane * 6, .45f)
                .Ramp(3.0f, 1.1f, 1.0f);
        }
    }

    // ---- Hard phase three ----

    // Diamond Dust: the comb is strummed from both ends at once, a dark strum and a
    // bright one, so each pair falls as a crossing X with a gap in each arm. Between
    // the strums the side ports grow bright dendrites.
    IEnumerator DiamondDust()
    {
        DeployComb();
        yield return ChargeComb(.5f);
        for (int s = 0; s < 9; s++)
        {
            int gap = 1 + s * 4 % (combCount - 3);
            Phrase(Toned(HostileTone.Dark, Strum(1, gap)));
            Phrase(Toned(HostileTone.Bright, Strum(-1, combCount - 2 - gap)));
            if (s % 2 == 1)
            {
                yield return ChargeSides(Rest(1.1f));
                UseTone(HostileTone.Bright);
                float reach = s % 4 == 1 ? 70 : 38;
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Dendrite(Hull.TransformPoint(SideLocal(mirror)), CombatVisuals.Rotate(Vector2.down, mirror * reach));
            }
            else
                yield return Wait(Rest(1.1f));
        }
        yield return Wait(1.2f);
        RecallComb();
        yield return Wait(.6f);
    }

    // ---- Helpers ----

    IEnumerator ChargeSides(float seconds)
    {
        for (float t = 0; t < seconds; t += Holding ? 0 : Time.deltaTime * LevelRun.EnemyTempoScale)
        {
            Cue(Hull.TransformPoint(SideLocal(-1)), seconds - t);
            Cue(Hull.TransformPoint(SideLocal(1)), seconds - t);
            yield return null;
        }
    }

    // Every aperture on the comb glows while it settles on the top edge.
    IEnumerator ChargeComb(float seconds)
    {
        for (float t = 0; t < seconds; t += Holding ? 0 : Time.deltaTime * LevelRun.EnemyTempoScale)
        {
            for (int i = 0; i < combCount; i++)
                Cue(CombPort(i), seconds - t);
            yield return null;
        }
    }
}
