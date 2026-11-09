using System.Collections;
using UnityEngine;

// Plains bomber, a working aircraft that flies its sorties as spell cards.
// Phase one
//   Wingtip Crossfire: both wingtips stream triple arrows at the player while the
//     bays drop mirrored bombs that travel the same distance, stop and explode 360.
//   Harvest Lace: a two-arm mirrored lace folds open and shut from the wings,
//     with slow orb wavefronts from the nose rolling under it.
// Phase two
//   Contrail Curtain: fences of rounds hang just above the visible top edge,
//     across its whole width, then fall in at alternating speeds; sweeping unaimed
//     arrow fans from both wings keep the player moving between them.
//   Bomb Run: rows of mirrored 360 bombs under a two-arm lace.
// Tones: the bombs are always dark. The dominant tone alternates card by card:
// bright crossfire, dark lace, bright fans over dark curtains, bright lace over dark bombs.
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

    // Bright aimed wingtip arrows; dark bombs and tracer.
    IEnumerator WingtipCrossfire()
    {
        yield return Charge(Hull, WeaponPort(0), .5f);
        // The triple streams run for 3.5 s (was 7), so the card does not drag.
        yield return Every(3.5f, .1f, n =>
        {
            UseTone(HostileTone.Bright);
            for (int side = 0; side < 2; side++)
            {
                var port = Muzzle(side);
                var axis = CombatVisuals.Aim(port);
                // All three streams are arrows, the middle one included (Miguel).
                foreach (float spread in new[] { -10f, 0f, 10f })
                    Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, spread), 4.6f, .19f);
            }
            if (n % 12 == 6)
                CarpetBombs(2);
        });
    }

    // Dark lace; bright orb arcs and nose tracer rolling under it.
    IEnumerator HarvestLace()
    {
        Phrase(Every(7, 1.4f, n => OrbWave(n)));
        // Two arms per wing; the third was one animated fan too many (Miguel).
        yield return Toned(HostileTone.Dark, MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(96), 2, 70, 24, 3.4f, .07f, 1));
    }

    // Two layers of slow orbs from the nose: an arc that rolls under the lace.
    void OrbWave(int n)
    {
        UseTone(HostileTone.Bright);
        Fire<SimpleRound>(Muzzle(4),CombatVisuals.Aim(Muzzle(4)),2.8f,.23f);
        int lanes = Dense(15);
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float step = 120f / (lanes - 1);
                Fire<PetalRound>(Muzzle(4), CombatVisuals.Rotate(Vector2.down, (i - (lanes - 1) * .5f) * step + (layer + n) % 2 * step * .5f), 1.7f + layer * .5f, .26f);
            }
    }

    // Mirrored bombs brake at matching distances, glint, then explode in a full ring.
    void CarpetBombs(int pairs)
    {
        UseTone(HostileTone.Dark);
        // A nose tracer punctuates each payload release, including above the wings.
        Fire<SimpleRound>(Muzzle(4),CombatVisuals.Aim(Muzzle(4)),2.9f,.22f);
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

    // Bright sweeping wing fans between dark falling curtains.
    IEnumerator Curtains()
    {
        for (int curtain = 0; curtain < 3; curtain++)
        {
            yield return Charge(Hull, WeaponPort(0), .4f);
            Phrase(ContrailCurtain());
            // Unaimed arrow fans sweep in and out from both wings, mirrored, instead
            // of single aimed arrows: many more needles, none of them tracking.
            int arrows = Dense(5);
            yield return Every(2.6f, .13f, n =>
            {
                if (n % 2 != 0)
                    return;
                UseTone(HostileTone.Bright);
                float sweep = Mathf.Sin(n * .42f + curtain) * 40;
                for (int side = 0; side < 2; side++)
                {
                    int mirror = side == 0 ? -1 : 1;
                    for (int i = 0; i < arrows; i++)
                        Fire<NeedleRound>(Muzzle(side), CombatVisuals.Rotate(Vector2.down,
                            mirror * (20 + sweep) + (i - (arrows - 1) * .5f) * 14), 4.2f, .19f);
                }
            });
        }
    }

    // Bright lace over dark bombs.
    IEnumerator BombRun()
    {
        Phrase(Toned(HostileTone.Bright, MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(80), 2, 55, 24, 3.2f, .085f, -1)));
        yield return Every(7, 1.1f, n => CarpetBombs(n % 2 == 0 ? 2 : 3));
    }

    // A line strung just above the visible top edge across the whole width. Each
    // round slides down out of sight, hangs there as a hidden fence, then drops in
    // with alternating speeds, so the gaps open as it falls.
    IEnumerator ContrailCurtain()
    {
        int lanes = Dense(24);
        float y = CombatRound.Border.yMax + 1.2f;
        for (int i = 0; i < lanes; i++)
        {
            UseTone(HostileTone.Dark);
            float x = Mathf.Lerp(Camera.LeftLimit + .25f, Camera.RightLimit - .25f, i / (lanes - 1f));
            // Braking over the last .35 s covers half that time's distance: about .5 units in.
            float arrive = .5f, launch = 1.6f;
            float fall = i % 2 == 0 ? 1.2f : 2.0f;
            Fire<SimpleRound>(new Vector3(x, y, transform.position.z), Vector2.down, WorldSpeed(launch), .24f)
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

    // Hard final phase, Firestorm Sortie: a curtain above the top, a three-arm wing
    // lace and rows of 360 bombs all at once, with a second curtain halfway through.
    // A bright lace weaves between the dark curtains and bombs.
    IEnumerator FinalSpell()
    {
        Phrase(ContrailCurtain());
        Phrase(Toned(HostileTone.Bright, MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(110), 3, 70, 20, 3.6f, .07f)));
        yield return Every(8, 1.0f, n =>
        {
            // One exploding bomb fewer per side than before (Miguel).
            CarpetBombs(2);
            if (n == 4)
                Phrase(ContrailCurtain());
        });
        yield return Wait(.6f);
    }

}
