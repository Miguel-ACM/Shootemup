using System.Collections;
using UnityEngine;

// Proposed Snow fortress card book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set. One concept per phase, developed in movements
// separated by rests that belong to it. Same hull, ports and comb.
// Phase one, Sleet Lattice (rain): the comb lines the top edge and rains two crossing
//   families of slanted arrows, so the rain falls as a diamond lattice. Movement by
//   movement the lattice is still, then swung by a crosswind (its holes drift), then
//   breathes (the diamonds stretch and squash), then gusts: the families swap every few
//   beats and the comb slides half a column. From the second round of movements the
//   side ports take up the lattice beneath the hull. Loosely aimed mouth rounds fall
//   between beats while the sides are quiet.
// Phase two, Avalanche (snow): shell fans stop along a line and burst in order, so the
//   snow sweeps across the arena; beneath it the gust ports pour layered arrows downhill
//   with the slide, starting slow and accelerating. The avalanche runs from the left,
//   from the right, closes in from both sides, then bursts out from the centre; later
//   rounds add mouth fans that freeze and re-aim.
// Hard phase three, Diamond Dust: the comb becomes a harp. Each strung column drops a
//   fast arrow and a slower echo, so a strum falls as two separating diagonal slashes
//   with muted strings for a gap. Arpeggio (single strums and mouth fans), crossing
//   strums from both ends with dendrites from the sides, then a fast glissando; later
//   rounds strum tighter and the comb shifts between movements.
// Every round thrown out of the hull's sides is an arrow; the comb rains arrows straight
// from the top edge; snow is thrown up harmlessly and falls from the top. Tones: dark
// rain with bright mouth rounds in phase one; bright shells and snow over dark slides
// in phase two; dark and bright strums crossing in phase three, bright fans and dendrites.
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
                yield return SleetLattice();
            else
                yield return Avalanche();
        }
    }

    // ---- Phase one: Sleet Lattice ----

    IEnumerator SleetLattice()
    {
        DeployComb();
        for (int movement = 0; !phaseTwo; movement++)
        {
            // The rest belongs to the comb: every aperture gathers before the next movement.
            yield return ChargeComb(movement == 0 ? .6f : Rest(.85f));
            int kind = movement % 4;
            bool sides = movement >= 4 || kind == 3;
            const int Beats = 18;
            for (int k = 0; k < Beats; k++)
            {
                // Still air, then a swinging crosswind, then a breathing lattice, then gusts.
                float wind = kind == 0 ? 0 : 13 * Mathf.Sin((k + movement * 5) * .26f);
                float lean = kind == 2 ? 21 + 9 * Mathf.Sin(k * .55f) : 21;
                bool swapped = kind == 3 ? k / 6 % 2 == 1 : movement % 2 == 1;
                UseTone(HostileTone.Dark);
                for (int i = 0; i < combCount; i++)
                {
                    // Lighter difficulties drop a diagonal of strings, never a whole column.
                    if (!Difficulty.Hard && (i + k) % 4 == 0 || Difficulty.Easy && (i + k) % 4 == 2)
                        continue;
                    int family = (i % 2 == 0) != swapped ? 1 : -1;
                    Fire<NeedleRound>(CombPort(i), CombatVisuals.Rotate(Vector2.down, wind + family * lean), 2.7f, .19f);
                    releases[i] = .1f;
                }
                if (sides)
                {
                    // The side ports carry both families on beneath the hull.
                    for (int mirror = -1; mirror <= 1; mirror += 2)
                    {
                        int family = (k % 2 == 0) == (mirror < 0) ? 1 : -1;
                        Fire<NeedleRound>(Hull.TransformPoint(SideLocal(mirror)), CombatVisuals.Rotate(Vector2.down, wind + family * lean), 2.7f, .19f);
                    }
                }
                else if (k % 2 == 1)
                {
                    // Between the rain beats: a loose spray around the pilot from the mouth.
                    UseTone(HostileTone.Bright);
                    int loose = Dense(3);
                    var aim = CombatVisuals.Aim(Mouth);
                    for (int j = 0; j < loose; j++)
                        Fire<SimpleRound>(Mouth, CombatVisuals.Rotate(aim, (j - (loose - 1) * .5f) * 12 + Random.Range(-8f, 8f)), 1.3f, .21f)
                            .Ramp(2.2f, 1.0f, .2f);
                }
                yield return Wait(Rest(.55f));
            }
            if (kind == 3)
                SlideComb();
        }
    }

    // ---- Phase two: Avalanche ----

    IEnumerator Avalanche()
    {
        for (int movement = 0; !phaseThree; movement++)
        {
            // 0 from the left, 1 from the right, 2 closing in from both sides, 3 out from the centre.
            int kind = movement % 4;
            yield return Charge(Hull, MouthLocal, .45f);
            Phrase(Toned(HostileTone.Dark, Slides(kind, Rest(2.0f) + .6f)));
            yield return Cascade(kind);
            if (movement >= 4 && kind % 2 == 1)
                FrostFan();
            // The slope settles after the centre bursts out: a longer rest.
            yield return Wait(Rest(kind == 3 ? 2.6f : 2.0f));
        }
    }

    // Shells lobbed onto a line, the first to burst landing near and high, the last far
    // and low. Each bursts as it settles, so the snow sweeps in the avalanche's order.
    IEnumerator Cascade(int kind)
    {
        int shells = Difficulty.Normal ? 3 : Dense(5);
        float width = CombatRound.Border.xMax - 1.3f;
        for (int c = 0; c < shells; c++)
        {
            float across = shells > 1 ? c / (shells - 1f) : .5f;
            float fromEdge = 1 - Mathf.Abs(across * 2 - 1);
            float order = kind == 0 ? across : kind == 1 ? 1 - across : kind == 2 ? fromEdge : 1 - fromEdge;
            var target = new Vector2(Mathf.Lerp(-width, width, across), Mouth.y - Mathf.Lerp(2.8f, 5.8f, order));
            var path = target - (Vector2)Mouth;
            UseTone(HostileTone.Bright);
            var shell = Fire<BurstShell>(Mouth, path.normalized, 2.7f, .5f);
            if (shell != null)
                shell.SetTravelDistance(path.magnitude);
            yield return Wait(.1f);
        }
    }

    // Layered arrows poured downhill from the gust ports with the avalanche: both down
    // one side, converging (closing in) or diverging (bursting out). The slope ripples,
    // and every arrow starts slow and accelerates.
    IEnumerator Slides(int kind, float seconds)
    {
        float interval = Rest(.095f);
        int layers = Difficulty.Hard ? Dense(3) : 1;
        for (float t = 0; t < seconds; t += interval)
        {
            float slope = 18 + 40 * (.5f + .5f * Mathf.Sin(t * 2.4f));
            for (int mirror = -1; mirror <= 1; mirror += 2)
            {
                int downhill = kind == 0 ? 1 : kind == 1 ? -1 : kind == 2 ? -mirror : mirror;
                var port = Hull.TransformPoint(GustLocal(mirror));
                for (int layer = 0; layer < layers; layer++)
                    Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.down, downhill * (slope + layer * 6)), 1.0f + layer * .35f, .19f)
                        .Ramp(3.2f, 1.2f, .3f);
            }
            yield return Wait(interval);
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

    // ---- Hard phase three: Diamond Dust ----

    IEnumerator DiamondDust()
    {
        DeployComb();
        yield return ChargeComb(.5f);
        for (int movement = 0; ; movement++)
        {
            int kind = movement % 3;
            // Later rounds strum tighter, so the slashes fall flatter and faster.
            float pluck = movement >= 3 ? .075f : .09f;
            if (kind == 0)
            {
                // Arpeggio: single strums, alternating direction, the gap walking; the mouth
                // answers every other strum with a fan that freezes and re-aims.
                for (int s = 0; s < 10; s++)
                {
                    Phrase(Toned(HostileTone.Dark, Strum(s % 2 == 0 ? 1 : -1, Gap(s), pluck)));
                    if (s % 2 == 1)
                        FrostFan();
                    yield return Wait(Rest(.45f));
                }
            }
            else if (kind == 1)
            {
                // Crossing: a dark strum and a bright one from both ends at once fall as an X
                // with a gap in each arm; the side ports grow dendrites between them.
                for (int s = 0; s < 7; s++)
                {
                    Phrase(Toned(HostileTone.Dark, Strum(1, Gap(s), pluck)));
                    Phrase(Toned(HostileTone.Bright, Strum(-1, combCount - 2 - Gap(s), pluck)));
                    if (s % 2 == 1)
                    {
                        yield return ChargeSides(Rest(.65f));
                        UseTone(HostileTone.Bright);
                        float reach = s % 4 == 1 ? 70 : 38;
                        for (int mirror = -1; mirror <= 1; mirror += 2)
                            Dendrite(Hull.TransformPoint(SideLocal(mirror)), CombatVisuals.Rotate(Vector2.down, mirror * reach));
                    }
                    else
                        yield return Wait(Rest(.65f));
                }
            }
            else
            {
                // Glissando: quick strums back and forth, alternating tone.
                for (int s = 0; s < 13; s++)
                {
                    var shade = s % 2 == 0 ? HostileTone.Dark : HostileTone.Bright;
                    Phrase(Toned(shade, Strum(s % 2 == 0 ? 1 : -1, Gap(s + movement), pluck)));
                    yield return Wait(Rest(.34f));
                }
            }
            // The harp is retuned: the last strum finishes, then the comb gathers again.
            yield return Wait(.9f);
            if (movement >= 3)
                SlideComb();
            yield return ChargeComb(Rest(.8f));
        }
    }

    // Two muted strings walk across the comb, strum by strum.
    int Gap(int strum) => 1 + strum * 4 % (combCount - 3);

    // Strings released one after another along the comb. Each drops a fast arrow and a
    // slower echo, so the strum falls as two diagonal slashes that draw apart.
    IEnumerator Strum(int direction, int gap, float pluck)
    {
        for (int n = 0; n < combCount; n++)
        {
            int i = direction > 0 ? n : combCount - 1 - n;
            if (i < gap || i >= gap + 2)
            {
                var port = CombPort(i);
                Fire<NeedleRound>(port, Vector2.down, 3.4f, .19f);
                if (Difficulty.Hard || !Difficulty.Easy && i % 3 == 0)
                    Fire<NeedleRound>(port, Vector2.down, 2.0f, .19f);
                releases[i] = .1f;
            }
            yield return Wait(pluck);
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

    // ---- Helpers ----

    // Slide the comb half a column; the column pushed off one edge reappears on the other.
    void SlideComb()
    {
        deployment = 0;
        float width = Camera.RightLimit - Camera.LeftLimit - .4f;
        for (int i = 0; i < combCount; i++)
        {
            launch[i] = comb[i].position;
            lanes[i].x = Camera.LeftLimit + .2f + Mathf.Repeat(lanes[i].x - Camera.LeftLimit - .2f + width / (combCount - 1) * .5f, width + .001f);
        }
    }

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
