using System.Collections;
using UnityEngine;

// City duo: rush hour around a hub, flown as spell cards by both ships together.
// Phase one (both ships)
//   Ring Road: the light interceptor circles the heavy hauler and fires arrows
//     straight out from the hub, winding spiral arms all round it; the hauler covers
//     its back with mirrored fans fired up and out that ricochet off the ceiling.
//   Rush Hour: the hauler lays a two-family street grid in front and a second grid
//     behind it that rebounds off the ceiling; the interceptor fires headlight rays.
// Phase two (the survivor, enraged)
//   Hauler: Cargo Storm, mirrored crates lobbed high that explode all round, with
//     rear-guard fans and headlights.
//   Interceptor: Junction, a four-arm arrow spiral turning round itself in every
//     direction, with headlights between.
public sealed partial class CityDuoBoss
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
            else if (!enraged)
            {
                if (heavy)
                {
                    yield return Every(8, 1.1f, n => RearGuard(n));
                    yield return Wait(.6f);
                    yield return Every(8, .45f, n => Gridlock(n));
                    yield return Wait(.6f);
                }
                else
                {
                    yield return SpiralArm(8);
                    yield return Wait(.6f);
                    yield return Every(8, .9f, n => Headlights(Muzzle()));
                    yield return Wait(.6f);
                }
            }
            else if (heavy)
            {
                yield return Every(7, 1.2f, n =>
                {
                    CargoLob();
                    RearGuard(n);
                });
                yield return Wait(.5f);
                yield return Every(6, .7f, n => Headlights(Muzzle(1)));
                yield return Wait(.5f);
            }
            else
            {
                yield return Junction(7);
                yield return Wait(.5f);
                yield return Every(5, .7f, n => Headlights(Muzzle()));
                yield return Wait(.5f);
            }
        }
    }

    // Arrows fired straight out from the hub while circling it: winding spiral arms.
    IEnumerator SpiralArm(float seconds)
    {
        int shots = Mathf.RoundToInt(seconds / .055f);
        for (int i = 0; i < shots; i++)
        {
            Vector2 hub = partner != null ? partner.transform.position : transform.position + Vector3.up;
            Vector2 outward = ((Vector2)transform.position - hub).normalized;
            if (outward.sqrMagnitude < .01f)
                outward = Vector2.down;
            foreach (float spread in new[] { -9f, 9f })
                Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(outward, spread), 3.6f, .19f);
            yield return Wait(Rest(.055f));
        }
    }

    // Four arrow arms turning round the ship in every direction.
    IEnumerator Junction(float seconds)
    {
        int steps = Mathf.RoundToInt(seconds / .07f);
        for (int i = 0; i < steps; i++)
        {
            for (int arm = 0; arm < 4; arm++)
                Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, arm * 90 + i * 11), 3.4f, .19f);
            yield return Wait(Rest(.07f));
        }
    }

    // The hauler covers its back: mirrored fans fired up and out that rebound off the ceiling.
    void RearGuard(int n)
    {
        int lanes = Dense(7);
        foreach (int mirror in new[] { -1, 1 })
            for (int i = 0; i < lanes; i++)
                Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.up, mirror * (20 + i * 11 + (n % 2) * 5)), 3.0f, .2f).Bounce(2);
    }

    // Three mirrored rays, each a line of arrows at falling speeds.
    void Headlights(Vector3 port)
    {
        int beads = Dense(enraged ? 9 : 7);
        var aim = CombatVisuals.Aim(port);
        foreach (float ray in new[] { -22f, 0f, 22f })
            for (int b = 0; b < beads; b++)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, ray), 5.4f - b * .45f, .18f);
    }

    // Two mirrored diagonal families in front, and the same grid behind that
    // rebounds off the ceiling: the diamonds between them are the streets.
    void Gridlock(int row)
    {
        int lanes = Dense(enraged ? 11 : 9);
        foreach (int family in new[] { -1, 1 })
        {
            var front = CombatVisuals.Rotate(Vector2.down, family * 36);
            var back = CombatVisuals.Rotate(Vector2.up, family * 36);
            for (int i = 0; i < lanes; i++)
            {
                float offset = (i - (lanes - 1) * .5f) * 6.5f + (row % 2) * 3.25f;
                Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(front, offset), 2.2f, .2f);
                if (i % 2 == row % 2)
                    Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(back, offset), 2.2f, .2f).Bounce(1);
            }
        }
    }

    // Mirrored crates lobbed high, falling under gravity, exploding all round.
    void CargoLob()
    {
        int shards = Dense(14);
        foreach (float lob in new[] { -60f, -30f, 30f, 60f })
            Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.up, lob), 2.7f, .42f)
                .Fall(new Vector2(0, -2.7f))
                .SplitAt(1.8f, shards, 360f / shards, 3.0f, .19f);
    }

    // Hard final phase for the survivor. Hauler: Gridlock Storm, cargo, rear guard and
    // street grid together. Interceptor: Six-Way Junction with headlights.
    IEnumerator FinalSpell()
    {
        if (heavy)
            yield return Every(8, .8f, n =>
            {
                Gridlock(n);
                if (n % 2 == 0)
                    RearGuard(n);
                if (n % 3 == 1)
                    CargoLob();
            });
        else
            yield return Every(8, .07f, n =>
            {
                for (int arm = 0; arm < 6; arm++)
                    Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, arm * 60 + n * 9), 3.5f, .19f);
                if (n % 17 == 8)
                    Headlights(Muzzle());
            });
        yield return Wait(.5f);
    }
}
