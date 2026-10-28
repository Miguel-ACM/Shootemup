using UnityEngine;
using System.Collections;

// Space citadel: the only district built on circles, and every circle moves.
// Phase one (shield generators up)
//   Orrery: each generator launches a breathing ring that sinks while turning
//     against its twin, so their gaps meet and part; turrets fling comets, tight
//     spinning rings that travel at the player as one body.
//   Eclipse: the charged centre laser holds a lane while nested counter-rotating
//     rings roll out of the core, their gaps lining up only now and then.
// Phase two (core exposed)
//   Galaxy: stars wound round the core on two arms slingshot off one by one along
//     spiral arms, alternating the winding each time.
//   Binary Comets: mirrored pairs of comets fired either side of the player, with
//     the laser cutting the middle.
public sealed partial class SpaceCitadelBoss
{
    Vector3 Core => parts[4] != null ? parts[4].transform.position : transform.position;
    Vector3 GeneratorPort(int i) => parts[i].transform.TransformPoint(new Vector3(0, -.3f, -.3f));
    Vector3 TurretPort(int i) => parts[i].transform.TransformPoint(new Vector3(i == 2 ? -.16f : .16f, -.695f, -.08f));
    protected override void Attack()
    {
        if (parts[0] != null)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (!phaseTwo)
            {
                yield return Every(8, 1.4f, n =>
                {
                    for (int i = 0; i < 2; i++)
                        if (parts[i].health > 0)
                            Orrery(GeneratorPort(i), (i == 0 ? 1 : -1) * (n % 2 == 0 ? 1 : -1), n);
                    if (n % 2 == 1)
                        for (int i = 2; i < 4; i++)
                            if (parts[i].health > 0)
                                Comet(TurretPort(i), i == 2 ? 1 : -1, 0);
                });
                yield return Wait(.8f);
                Laser();
                yield return Every(8, 1.8f, n => Eclipse(n));
                yield return Wait(.8f);
            }
            else
            {
                yield return Every(8, 1.6f, n => Galaxy(n % 2 == 0 ? 1 : -1));
                yield return Wait(.8f);
                Laser();
                yield return Every(7, .9f, n =>
                {
                    foreach (int mirror in new[] { -1, 1 })
                        Comet(Core, mirror, mirror * 24);
                });
                yield return Wait(.8f);
            }
        }
    }

    void Laser()
    {
        centralLaser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (centralLaser == null)
            return;
        centralLaser.Bind(Hull, WeaponPort());
        centralLaser.warning = 1.3f;
        centralLaser.activeTime = phaseTwo ? 2.2f : 1.8f;
        centralLaser.width = phaseTwo ? .42f : .34f;
    }

    // A breathing ring with a three-bead gap, turning one way while it sinks.
    void Orrery(Vector3 port, int sign, int n)
    {
        int beads = Dense(24);
        int gap = (n * 5) % beads;
        for (int i = 0; i < beads; i++)
        {
            if ((i - gap + beads) % beads < 3)
                continue;
            var bead = Fire<SimpleRound>(port, Vector2.down, 1.1f, .18f);
            if (bead != null)
                bead.Orbit(1.25f, i * 360f / beads, sign * 45, 11, 0, 0, 0, .9f).Breathe(.5f, .35f);
        }
    }

    // A tight fast-spinning ring that travels at the player as one body.
    void Comet(Vector3 port, int sign, float offset)
    {
        int beads = Dense(12);
        var aim = CombatVisuals.Rotate(CombatVisuals.Aim(port), offset);
        for (int i = 0; i < beads; i++)
        {
            var bead = Fire<SimpleRound>(port, aim, 2.1f, .2f);
            if (bead != null)
                bead.Orbit(.8f, i * 360f / beads, sign * 150, 10, 0, 0, 0, .4f);
        }
    }

    // Stars wound round the core on two arms, slung off on the tangent one after another.
    void Galaxy(int sign)
    {
        int stars = Dense(40);
        for (int i = 0; i < stars; i++)
        {
            var star = Fire<SimpleRound>(Core, Vector2.down, 0, .19f);
            if (star != null)
                star.Orbit(1.0f + (i % 2) * .5f, i * 720f / stars, sign * 85, 9, 0, 0, 0, .5f).Sling(.9f + i * .045f, .8f);
        }
    }

    // Nested rings turning against each other; their gaps line up only now and then.
    void Eclipse(int n)
    {
        int inner = Dense(18), outer = Dense(28);
        int innerGap = (n * 3) % inner, outerGap = (n * 7) % outer;
        for (int i = 0; i < inner; i++)
        {
            if ((i - innerGap + inner) % inner < 3)
                continue;
            var bead = Fire<SimpleRound>(Core, Vector2.down, .8f, .18f);
            if (bead != null)
                bead.Orbit(1.1f, i * 360f / inner, 38, 12, 0, 0, 0, .8f);
        }
        for (int i = 0; i < outer; i++)
        {
            if ((i - outerGap + outer) % outer < 4)
                continue;
            var bead = Fire<SimpleRound>(Core, Vector2.down, .8f, .2f);
            if (bead != null)
                bead.Orbit(2.0f, i * 360f / outer, -30, 12, 0, 0, 0, 1.1f).Breathe(.3f, .25f);
        }
    }
}
