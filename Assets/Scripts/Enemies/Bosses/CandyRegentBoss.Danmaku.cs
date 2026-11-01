using System.Collections;
using UnityEngine;

// Candy regent: a sugar rush that never stops darting. It has no minions: in phase
// one its hatches swell and spray wiggling bonbons, and both phase-one cards keep a
// candy canopy over the top of the arena.
// Phase one
//   Taffy Pull: both hatches stretch a line of rounds from slow to fast along one aim,
//     bending in mirror, so each pull arrives as two curling ribbons.
//   Pinball Parlour: mirrored sprinkles ricochet off the walls and ceiling while
//     gumdrops brake and pop into tight aimed shotguns.
// Phase two: one idea at a time, every card mirrored.
//   Sugar Rush: a two-arm mirrored lace folds open and shut.
//   Candy Worms: wiggling streams in antiphase from both hatches.
//   Gumdrop Barrage: steady volleys of three gumdrops popping at the player.
public sealed partial class CandyRegentBoss
{
    protected override void Attack()
    {
        if (arrived)
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
                yield return Every(6, .9f, n =>
                {
                    TaffyPull(n % 2 == 0 ? 1 : -1);
                    if (n % 2 == 1)
                        Canopy();
                });
                yield return Wait(.7f);
                yield return Every(6, .8f, n =>
                {
                    Sprinkles();
                    if (n % 3 == 2)
                        Gumdrops(3);
                    else if (n % 3 == 0)
                        Canopy();
                });
                yield return Wait(.7f);
            }
            else
            {
                yield return MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(64), 2, 66, 22, 3.4f, .07f);
                yield return Wait(.7f);
                yield return CandyWorms(56);
                yield return Wait(.7f);
                yield return Every(6, 1.0f, n => Gumdrops(3));
                yield return Wait(.7f);
            }
        }
    }

    bool Clear => player == null || Vector2.Distance(player.position, transform.position) > 2.5f;

    // One aim per hatch, a line of rounds from slow to fast that curls as it stretches.
    void TaffyPull(int sign)
    {
        if (!Clear)
            return;
        int beads = Dense(phaseTwo ? 16 : 13);
        for (int hatch = 0; hatch < 2; hatch++)
        {
            var port = Muzzle(hatch);
            var axis = CombatVisuals.Rotate(CombatVisuals.Aim(port), (hatch == 0 ? -1 : 1) * 14);
            int bend = (hatch == 0 ? 1 : -1) * sign;
            for (int i = 0; i < beads; i++)
            {
                var bead = Fire<SimpleRound>(port, axis, Mathf.Lerp(1.4f, 4.6f, i / (beads - 1f)), .2f);
                if (bead != null)
                    bead.Curl(bend * 26, 1.4f);
            }
        }
    }

    // Candy canopy: arrows thrown sideways and upward from both hatches that ricochet
    // between the walls and the ceiling, so the top of the arena is never a safe spot.
    void Canopy()
    {
        int count = Dense(7);
        for (int i = 0; i < count; i++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<NeedleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.right * mirror, mirror * (-15 + i * 9)), 3.4f, .19f).Bounce(3);
    }

    // Mirrored sprinkles thrown in a wide spread, ricocheting off the walls and ceiling.
    void Sprinkles()
    {
        if (!Clear)
            return;
        int count = Dense(phaseTwo ? 13 : 10);
        for (int i = 0; i < count; i++)
        {
            float angle = 12 + i * 88f / Mathf.Max(1, count - 1);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<SimpleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * angle), 2.8f + (i % 3) * .45f, .16f).Bounce(2);
        }
    }

    // Gumdrops brake hard, then pop into a narrow fast fan of arrows at the player.
    void Gumdrops(int drops)
    {
        if (!Clear)
            return;
        int shards = Dense(phaseTwo ? 9 : 7);
        var aim = Aim;
        for (int i = 0; i < drops; i++)
            Fire<SimpleRound>(Muzzle(i % 2), CombatVisuals.Rotate(aim, (i - (drops - 1) * .5f) * 34), 3.2f, .4f)
                .Ramp(.15f, .7f)
                .SplitAt(.95f, shards, 8, 4.6f, .19f, true);
    }

    // Wiggling streams in antiphase from both hatches, following the regent's path.
    IEnumerator CandyWorms(int length)
    {
        length = Dense(length);
        for (int i = 0; i < length; i++)
        {
            for (int hatch = 0; hatch < 2; hatch++)
                Fire<SimpleRound>(Muzzle(hatch), CombatVisuals.Rotate(Vector2.down, (hatch == 0 ? -1 : 1) * 18), 2.9f, .19f)
                    .Sway(hatch == 0 ? .45f : -.45f, .8f);
            yield return Wait(Rest(.075f));
        }
    }

    // Hard final phase, Sugar Crash: two layers only. A three-arm lace folds over the
    // candy worms; then gumdrop volleys alternate with the worms on a steady beat.
    IEnumerator FinalSpell()
    {
        Phrase(CandyWorms(60));
        yield return MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(84), 3, 70, 20, 3.5f, .07f);
        yield return Wait(.6f);
        yield return Every(6, 1.2f, n =>
        {
            if (n % 2 == 0)
                Gumdrops(3);
            else
                Phrase(CandyWorms(12));
        });
        yield return Wait(.6f);
    }
}
