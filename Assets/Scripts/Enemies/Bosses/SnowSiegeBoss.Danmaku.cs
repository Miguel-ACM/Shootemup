using System.Collections;
using UnityEngine;

// Snow fortress: time freezes around it. It flies its attacks as spell cards.
// Each phase owns one sky: phase one and the Hard phase three rain, phase two snows.
// Phase one (rain)
//   Frozen Petals: mirrored arrow fans fly out, stop dead and glint, then re-aim at
//     the player, crawl and accelerate.
//   Icicle Comb: the comb lines the whole top edge, a column on each screen edge,
//     and rains arrows straight down in a woven lattice.
// Phase two (snow)
//   Whiteout: mirrored gusts thrown sideways halt and turn down and inward, with
//     frozen petals cutting across them and snow shells bursting overhead.
//   Snowdrift: fans of snow shells spread across the whole width; their shards fly
//     up out of sight and fall back as snow from the top, while the mouth fires slow
//     orb arcs that stop and resume.
public sealed partial class SnowSiegeBoss
{
    Vector3 Mouth => Hull.TransformPoint(MouthLocal);
    static readonly Vector3 MouthLocal = new Vector3(0, -.65f, -.25f);
    protected override void Attack()
    {
        OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
            {
                yield return FrozenPetals();
                yield return Wait(1);
                yield return IcicleComb();
                yield return Wait(1);
            }
            else
            {
                yield return Whiteout();
                yield return Wait(.9f);
                yield return Snowdrift();
                yield return Wait(.9f);
            }
        }
    }

    IEnumerator FrozenPetals()
    {
        for (int bloom = 0; bloom < 4; bloom++)
        {
            yield return Charge(Hull, MouthLocal, .45f);
            FreezeFrame(bloom % 2 == 0 ? 1 : -1);
            yield return Wait(1.5f);
        }
    }

    // Two mirrored arrow fans fly out, stop dead and glint, then every arrow re-aims
    // at the player and crawls before accelerating. The re-aimed fans keep their spacing.
    void FreezeFrame(int sign)
    {
        int count = Dense(phaseTwo ? 21 : 17);
        float spread = 130f / (count - 1);
        for (int mirror = -1; mirror <= 1; mirror += 2)
        {
            var port = Hull.TransformPoint(new Vector3(mirror * .55f, -.55f, -.25f));
            for (int i = 0; i < count; i++)
            {
                float lane = i - (count - 1) * .5f;
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.down, mirror * (35 + sign * 6 + lane * spread * .5f)), 4.4f - Mathf.Abs(lane) * .06f, .19f)
                    .Pause(.7f, .7f + Mathf.Abs(lane) * .03f, CombatRound.Turn.Aim, mirror * lane * 3.5f, .22f)
                    .Ramp(4.5f, 1.4f, 1.45f);
            }
        }
    }

    void CrystalShells(int count, float spread)
    {
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            var shell = Fire<BurstShell>(Mouth, CombatVisuals.Rotate(Vector2.down, lane * spread), 2.7f, .5f);
            if (shell != null)
                shell.SetTravelDistance(Mathf.Abs(lane) < 1 ? 6.2f : 4.6f);
        }
    }

    // The comb lines the top edge and rains arrows straight down, alternate columns
    // half a beat apart so the rain forms a woven lattice; snow shells burst between.
    IEnumerator IcicleComb()
    {
        DeployComb();
        yield return Wait(.6f);
        for (int wave = 0; wave < 3; wave++)
        {
            for (int k = 0; k < 10; k++)
            {
                for (int i = 0; i < combCount; i++)
                {
                    if (i % 2 != k % 2 || Difficulty.Easy && i % 3 == 2)
                        continue;
                    Fire<NeedleRound>(comb[i].TransformPoint(new Vector3(0, -.26f, -.55f)), Vector2.down, 3.4f, .19f);
                    releases[i] = .1f;
                }
                yield return Wait(Rest(.2f));
            }
            yield return Wait(.7f);
            if (wave == 1)
            {
                // Slide the comb half a column; the column pushed off one edge reappears on the other.
                deployment = 0;
                float width = Camera.RightLimit - Camera.LeftLimit - .4f;
                for (int i = 0; i < combCount; i++)
                {
                    launch[i] = comb[i].position;
                    lanes[i].x = Camera.LeftLimit + .2f + Mathf.Repeat(lanes[i].x - Camera.LeftLimit - .2f + width / (combCount - 1) * .5f, width + .001f);
                }
            }
        }
        RecallComb();
    }

    void RecallComb()
    {
        deployment = 0;
        returning = true;
        for (int i = 0; i < combCount; i++)
            launch[i] = comb[i].position;
    }

    IEnumerator Whiteout()
    {
        Phrase(Every(7, 2.2f, n =>
        {
            FreezeFrame(n % 2 == 0 ? 1 : -1);
            CrystalShells(Dense(4), 34);
        }));
        int gusts = Dense(60);
        for (int g = 0; g < gusts; g++)
        {
            float lift = 8 + (g % 4) * 7;
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * .9f, -.3f, -.25f));
                Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.right * side, -side * lift), 3.0f + (g % 3) * .3f, .19f)
                    .Pause(.6f, .35f, CombatRound.Turn.Rotate, -side * (100 + (g % 3) * 10), .8f);
            }
            yield return Wait(Rest(.11f));
        }
    }

    IEnumerator Snowdrift()
    {
        yield return Every(7, 1.0f, n =>
        {
            // A wide fan of shells, offset every beat, so the snow covers the whole top.
            int shells = Dense(6);
            for (int i = 0; i < shells; i++)
            {
                float lane = i - (shells - 1) * .5f + (n % 2 == 0 ? -.25f : .25f);
                var shell = Fire<BurstShell>(Mouth, CombatVisuals.Rotate(Vector2.down, lane * 26), 2.7f, .5f);
                if (shell != null)
                    shell.SetTravelDistance(Mathf.Abs(lane) < 1.5f ? 6.2f : 4.4f);
            }
            if (n % 2 == 1)
            {
                int lanes = Dense(13);
                for (int i = 0; i < lanes; i++)
                {
                    float lane = i - (lanes - 1) * .5f;
                    Fire<SimpleRound>(Mouth, CombatVisuals.Rotate(Vector2.down, lane * 11), 3.0f, .24f)
                        .Pause(.8f, .6f + Mathf.Abs(lane) * .05f, CombatRound.Turn.Keep, 0, .8f);
                }
            }
        });
    }

    // Hard final phase, Absolute Zero: rain only. The comb alternates every three
    // seconds between the woven lattice and fast and slow pairs from every column,
    // while mirrored petals freeze and re-aim.
    IEnumerator FinalSpell()
    {
        DeployComb();
        yield return Wait(.5f);
        yield return Every(9, .3f, n =>
        {
            bool woven = n / 10 % 2 == 0;
            for (int i = 0; i < combCount; i++)
            {
                if (Difficulty.Easy && i % 3 == 2)
                    continue;
                var p = comb[i].TransformPoint(new Vector3(0, -.26f, -.55f));
                if (woven && i % 2 == n % 2)
                {
                    Fire<NeedleRound>(p, Vector2.down, 3.6f, .19f);
                    releases[i] = .1f;
                }
                else if (!woven && n % 2 == 0)
                {
                    Fire<NeedleRound>(p, Vector2.down, 3.6f, .19f);
                    Fire<NeedleRound>(p, Vector2.down, 2.4f, .19f);
                    releases[i] = .1f;
                }
            }
            if (n % 6 == 0)
                FreezeFrame(n % 12 == 0 ? 1 : -1);
        });
        RecallComb();
        yield return Wait(.6f);
    }
}
