using System.Collections;
using UnityEngine;

// Plains bomber, a working aircraft that flies its sorties as spell cards.
// Phase one
//   Wingtip Crossfire: both wingtips stream triple arrows at the player while the
//     bays drop mirrored bombs that travel the same distance, stop and explode 360.
//   Harvest Lace: a three-arm mirrored lace folds open and shut from the wings,
//     with slow orb wavefronts from the nose rolling under it.
// Phase two
//   Contrail Curtain: fences of rounds strung along the very top of the screen,
//     across its whole width, hang and then fall at alternating speeds; wing streams
//     keep the player moving between them.
//   Bomb Run: rows of mirrored 360 bombs under a two-arm lace.
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
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
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
                yield return BombRun();
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

    // Mirrored bombs brake at matching distances, glint, then explode in a full ring.
    void CarpetBombs(int pairs)
    {
        int shards = Dense(16);
        for (int i = 0; i < Dense(pairs); i++)
            for (int mirror = 0; mirror < 2; mirror++)
            {
                var port = Hull.TransformPoint(bays[mirror == 0 ? i : bays.Length - 1 - i]);
                // Every bomb travels the same distance before it stops and explodes all round.
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, (mirror == 0 ? -1 : 1) * (8 + i * 14)), 2.5f, .42f)
                    .Pause(1.0f, .45f)
                    .SplitAt(1.45f, shards, 360f / shards, 2.6f, .19f);
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

    IEnumerator BombRun()
    {
        Phrase(MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(80), 2, 55, 24, 3.2f, .085f, -1));
        yield return Every(7, 1.1f, n => CarpetBombs(n % 2 == 0 ? 2 : 3));
    }

    // A line strung along the very top of the screen across its whole width. It hangs
    // as a fence, then drops with alternating speeds, so the gaps open as it falls.
    IEnumerator ContrailCurtain()
    {
        int lanes = Dense(24);
        float y = Camera.UpLimit - .45f;
        for (int i = 0; i < lanes; i++)
        {
            float x = Mathf.Lerp(Camera.LeftLimit + .25f, Camera.RightLimit - .25f, i / (lanes - 1f));
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

    // Hard final phase, Firestorm Sortie: a curtain along the top, a three-arm wing
    // lace and rows of 360 bombs all at once, with a second curtain halfway through.
    IEnumerator FinalSpell()
    {
        Phrase(ContrailCurtain());
        Phrase(MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(110), 3, 70, 20, 3.6f, .07f));
        yield return Every(8, 1.0f, n =>
        {
            CarpetBombs(3);
            if (n == 4)
                Phrase(ContrailCurtain());
        });
        yield return Wait(.6f);
    }
}
