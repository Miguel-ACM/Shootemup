using System.Collections;
using UnityEngine;

// Plains bomber, a working aircraft that flies its sorties as spell cards.
// Phase one
//   Wingtip Crossfire: both wingtips stream triple arrows at the player while the
//     bays drop mirrored bombs that stop mid-air and burst into flat arrow fans.
//   Harvest Lace: a three-arm mirrored lace folds open and shut from the wings,
//     with slow orb wavefronts from the nose rolling under it.
// Phase two
//   Contrail Curtain: three fences of rounds strung across the arena, each with a
//     gap, hang and then fall at once; wing streams keep the player moving between.
//   Scatter Squadron: orange carriers released sparsely and irregularly, rows of
//     mirrored bombs, and a two-arm lace.
public sealed partial class FieldsBomberBoss
{
    protected override void Attack()
    {
        OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (!phaseTwo)
            {
                yield return WingtipCrossfire();
                yield return Wait(1.1f);
                yield return HarvestLace();
                yield return Wait(1.1f);
            }
            else
            {
                yield return Curtains();
                yield return Wait(1f);
                yield return ScatterSquadron();
                yield return Wait(1f);
            }
        }
    }

    IEnumerator WingtipCrossfire()
    {
        yield return Charge(Hull, WeaponPort(0), .5f);
        yield return Every(7, .1f, n =>
        {
            for (int side = 0; side < 2; side++)
            {
                var port = Muzzle(side);
                var axis = CombatVisuals.Aim(port);
                foreach (float spread in new[] { -10f, 0f, 10f })
                    Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, spread), 4.6f, .19f);
            }
            if (n % 12 == 6)
                CarpetBombs(2);
        });
    }

    IEnumerator HarvestLace()
    {
        Phrase(Every(7, 1.4f, n => OrbWave(n)));
        yield return MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(96), 3, 70, 20, 3.4f, .07f, 1);
    }

    // Two layers of slow orbs from the nose: an arc that rolls under the lace.
    void OrbWave(int n)
    {
        int lanes = Dense(15);
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float step = 120f / (lanes - 1);
                Fire<SimpleRound>(Muzzle(4), CombatVisuals.Rotate(Vector2.down, (i - (lanes - 1) * .5f) * step + (layer + n) % 2 * step * .5f), 1.7f + layer * .5f, .26f);
            }
    }

    // Mirrored bombs brake at matching heights, glint, then burst into flat arrow fans.
    void CarpetBombs(int pairs)
    {
        int shards = Dense(13);
        for (int i = 0; i < Dense(pairs); i++)
            for (int mirror = 0; mirror < 2; mirror++)
            {
                var port = Hull.TransformPoint(bays[mirror == 0 ? i : bays.Length - 1 - i]);
                float stop = .8f + i * .4f;
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, (mirror == 0 ? -1 : 1) * (6 + i * 10)), 2.5f, .42f)
                    .Pause(stop, .45f)
                    .SplitAt(stop + .45f, shards, 170f / (shards - 1), 2.6f, .19f);
            }
    }

    IEnumerator Curtains()
    {
        for (int curtain = 0; curtain < 3; curtain++)
        {
            yield return Charge(Hull, WeaponPort(0), .4f);
            Phrase(ContrailCurtain());
            yield return Every(2.6f, .13f, n =>
            {
                if (n % 2 == 0)
                    for (int side = 0; side < 2; side++)
                        Fire<NeedleRound>(Muzzle(side), CombatVisuals.Aim(Muzzle(side)), 4.8f, .19f);
            });
        }
    }

    IEnumerator ScatterSquadron()
    {
        Phrase(MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(80), 2, 55, 24, 3.2f, .085f, -1));
        yield return Every(7, .9f, n =>
        {
            if (n % 3 == 0)
                Fire<ScatterCarrier>(Muzzle(4), CombatVisuals.Rotate(Vector2.down, Random.Range(-20f, 20f)), 2.2f, .33f);
            else
                CarpetBombs(n % 3 == 1 ? 2 : 3);
        });
    }

    // A line strung from wingtip to wingtip across the arena, with a gap where the
    // player was. It hangs as a fence, then drops with alternating speeds.
    IEnumerator ContrailCurtain()
    {
        int lanes = Dense(22);
        float y = Camera.UpLimit - 4.2f;
        float gap = player != null ? player.position.x : 0;
        for (int i = 0; i < lanes; i++)
        {
            float x = Mathf.Lerp(Camera.LeftLimit + .3f, Camera.RightLimit - .3f, i / (lanes - 1f));
            if (Mathf.Abs(x - gap) < .75f)
                continue;
            var port = Muzzle(x < transform.position.x ? 0 : 1);
            Vector2 travel = new Vector2(x, y) - (Vector2)port;
            // Braking over the last .35 s covers half that time's distance.
            float arrive = .75f, launch = travel.magnitude / (arrive - .175f);
            float fall = i % 2 == 0 ? 1.2f : 2.0f;
            Fire<SimpleRound>(port, travel, WorldSpeed(launch), .24f)
                .Pause(arrive, 1.1f - i * .02f, CombatRound.Turn.Down, 0, fall / launch)
                .Ramp(1.8f, 1.2f, arrive + 1.1f);
            yield return Wait(.02f);
        }
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1;
    }
}
