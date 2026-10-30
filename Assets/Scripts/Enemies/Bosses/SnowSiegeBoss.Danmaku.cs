using System.Collections;
using UnityEngine;

// Snow fortress: time freezes around it. It flies its attacks as spell cards.
// Phase one
//   Frozen Petals: mirrored arrow fans fly out, stop dead and glint, then re-aim at
//     the player, crawl and accelerate; every bloom also throws snow shells, whose
//     shards fly up out of sight and fall back as snow from the top of the screen.
//   Icicle Comb: the comb lines the top edge and rains arrows straight down in a
//     woven lattice, with snow shells bursting between the waves.
// Phase two
//   Whiteout: mirrored gusts thrown sideways halt and turn down and inward, with
//     frozen petals cutting across them.
//   Glacier: the comb at the top edge rains fast and slow arrows in pairs while the
//     mouth fires slow orb arcs that stop and resume.
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
                yield return Glacier();
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
            yield return Wait(.9f);
            CrystalShells();
            yield return Wait(.6f);
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

    void CrystalShells()
    {
        int count = Dense(phaseTwo ? 5 : 4);
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            var shell = Fire<BurstShell>(Mouth, CombatVisuals.Rotate(Vector2.down, lane * 30), 2.7f, .5f);
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
            CrystalShells();
            yield return Wait(.7f);
            if (wave == 1)
            {
                deployment = 0;
                for (int i = 0; i < combCount; i++)
                {
                    launch[i] = comb[i].position;
                    lanes[i].x += (Camera.RightLimit - Camera.LeftLimit - .7f) / combCount * .5f;
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
        Phrase(Every(7, 2.2f, n => FreezeFrame(n % 2 == 0 ? 1 : -1)));
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

    IEnumerator Glacier()
    {
        DeployComb();
        yield return Wait(.6f);
        yield return Every(7, .7f, n =>
        {
            for (int i = 0; i < combCount; i++)
            {
                if (Difficulty.Easy && i % 3 == 2)
                    continue;
                var p = comb[i].TransformPoint(new Vector3(0, -.26f, -.55f));
                Fire<NeedleRound>(p, Vector2.down, 3.4f, .19f);
                Fire<NeedleRound>(p, Vector2.down, 2.3f, .19f);
                releases[i] = .1f;
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
        RecallComb();
    }

    // Hard final phase, Absolute Zero: the comb rains from the top without pause while
    // mirrored petals freeze and re-aim and snow shells keep bursting.
    IEnumerator FinalSpell()
    {
        DeployComb();
        yield return Wait(.5f);
        yield return Every(9, .3f, n =>
        {
            for (int i = 0; i < combCount; i++)
            {
                if (i % 2 != n % 2 || Difficulty.Easy && i % 3 == 2)
                    continue;
                Fire<NeedleRound>(comb[i].TransformPoint(new Vector3(0, -.26f, -.55f)), Vector2.down, 3.6f, .19f);
                releases[i] = .1f;
            }
            if (n % 6 == 0)
                FreezeFrame(n % 12 == 0 ? 1 : -1);
            if (n % 7 == 3)
                CrystalShells();
        });
        RecallComb();
        yield return Wait(.6f);
    }
}
