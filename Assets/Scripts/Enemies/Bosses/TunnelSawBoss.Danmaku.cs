using System.Collections;
using UnityEngine;

// Tunnel saw: it ricochets around the whole arena. Every bounce sprays the wall
// it struck with paired fast needles and slow heavy teeth along the same rays.
// Regularly it brakes and spins in place, throwing concentric rings that keep
// widening while turning in alternate directions: a frogger crossing whose gaps
// slide sideways. In phase two it hurls teeth that cling to the walls and run
// round the arena's rim, closing the space instead of deploying smaller saws.
public sealed partial class TunnelSawBoss
{
    CombatHullVisual spinner;
    protected override void Attack()
    {
        if (miniature || !entered || age < haltUntil)
            return;
        if ((volley - 1) % 3 == 2)
            Phrase(SpinRings());
    }

    void ImpactBurst(Vector2 inward)
    {
        using var sample = burstMarker.Auto();
        impacts++;
        int rays = Dense(phaseTwo ? 9 : 7);
        float jitter = impacts % 2 == 0 ? 8 : -8;
        for (int i = 0; i < rays; i++)
        {
            float lane = i - (rays - 1) * .5f;
            var axis = CombatVisuals.Rotate(inward, lane * 150f / (rays - 1) + jitter);
            var rim = transform.position + (Vector3)axis * Radius * .92f;
            Fire<NeedleRound>(rim, axis, 4.6f, .18f);
            Fire<SimpleRound>(rim, CombatVisuals.Rotate(axis, 150f / (rays - 1) * .5f), 1.45f, .3f);
        }
        if (!phaseTwo)
            return;
        int runners = Dense(4);
        for (int i = 0; i < runners; i++)
        {
            var axis = CombatVisuals.Rotate(inward, (i - (runners - 1) * .5f) * 34);
            Fire<SimpleRound>(transform.position + (Vector3)axis * Radius, axis, 4.2f, .24f)
                .RunWalls(7.5f, impacts % 2 == 0 ? 1 : -1);
        }
    }

    // Brake, spin up, and release widening rings that rotate in alternate directions.
    // Each ring keeps a three-tooth gap; the gaps drift apart, so the path weaves.
    IEnumerator SpinRings()
    {
        if (spinner == null)
            spinner = GetComponent<CombatHullVisual>();
        int rings = phaseTwo ? 4 : 3, teeth = Dense(phaseTwo ? 24 : 20);
        float hold = .45f + rings * .38f;
        haltUntil = age + hold + .2f;
        spinner.spin = 1500;
        yield return Charge(Hull, Vector3.zero, .45f);
        for (int ring = 0; ring < rings; ring++)
        {
            int sign = ring % 2 == 0 ? 1 : -1;
            int gap = (ring * 7 + volley * 5) % teeth;
            var center = transform.position;
            for (int i = 0; i < teeth; i++)
            {
                if ((i - gap + teeth) % teeth < 3)
                    continue;
                var tooth = Fire<SimpleRound>(center, Vector2.down, 0, ring % 2 == 0 ? .2f : .26f);
                if (tooth != null)
                    tooth.Orbit(Radius * .9f, i * 360f / teeth, sign * (phaseTwo ? 62 : 50), 9, 0, 0, 0, .05f).Expand(phaseTwo ? 1.55f : 1.35f);
            }
            yield return Wait(.38f);
        }
        spinner.spin = 720;
    }

    protected override void OnPhaseTwo()
    {
        if (miniature)
            return;
        // No minions: the saw instead starts throwing teeth that run along the walls.
        haltUntil = age + 1.8f;
        GetComponent<CombatHullVisual>().spin = 720;
        PSXFireballs.Shockwave(transform.position, 2, 1.8f, Color.yellow, 2, .1f);
    }
}
