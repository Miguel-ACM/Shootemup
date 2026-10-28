using System.Collections;
using UnityEngine;

// Tunnel saw: it ricochets around the whole arena and flies its attacks as cards.
// Phase one
//   Frogger Crossing: it brakes in the middle of the arena and spins out six widening
//     rings that turn in alternate directions; each ring has a gap and the gaps slide,
//     so crossing them is a timing puzzle.
//   Ricochet Storm: free bouncing; every wall it strikes is sprayed with fast arrows
//     and slow teeth along the same rays, and it answers with aimed arrow wavefronts.
// Phase two (no minions)
//   Rim Runners: each bounce also hurls teeth that cling to the walls and run round
//     the arena's rim, closing in the space.
//   Gearbox: it locks in the middle and turns out four interlocking spiral arms,
//     two winding each way, like meshing gears.
public sealed partial class TunnelSawBoss
{
    CombatHullVisual spinner;
    bool runners;
    protected override void Attack()
    {
        if (!miniature && entered)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        spinner = GetComponent<CombatHullVisual>();
        while (true)
        {
            if (!phaseTwo)
            {
                yield return FroggerCrossing();
                yield return RicochetStorm(8);
            }
            else
            {
                runners = true;
                yield return RicochetStorm(8);
                runners = false;
                yield return Gearbox();
            }
        }
    }

    void ImpactBurst(Vector2 inward)
    {
        using var sample = burstMarker.Auto();
        impacts++;
        int rays = Dense(phaseTwo ? 15 : 13);
        float step = 160f / (rays - 1);
        for (int i = 0; i < rays; i++)
        {
            var axis = CombatVisuals.Rotate(inward, (i - (rays - 1) * .5f) * step);
            var rim = transform.position + (Vector3)axis * Radius * .92f;
            Fire<NeedleRound>(rim, axis, 4.8f, .19f);
            if (i < rays - 1)
                Fire<SimpleRound>(rim, CombatVisuals.Rotate(axis, step * .5f), 1.5f, .3f);
        }
        if (!runners)
            return;
        int count = Dense(6);
        for (int i = 0; i < count; i++)
        {
            var axis = CombatVisuals.Rotate(inward, (i - (count - 1) * .5f) * 26);
            Fire<SimpleRound>(transform.position + (Vector3)axis * Radius, axis, 4.4f, .24f)
                .RunWalls(8, impacts % 2 == 0 ? 1 : -1);
        }
    }

    // Brake, travel to the centre and hold there while the card plays.
    IEnumerator Centre()
    {
        haltUntil = float.MaxValue;
        var centre = new Vector3(arena.center.x, arena.center.y + 1.2f, transform.position.z);
        while (Vector3.Distance(transform.position, centre) > .05f)
        {
            if (!Holding)
                transform.position = Vector3.MoveTowards(transform.position, centre, Time.deltaTime * LevelRun.EnemyTempoScale * 6);
            yield return null;
        }
        spinner.spin = 1600;
    }

    void ResumeBouncing()
    {
        haltUntil = 0;
        spinner.spin = 720;
        velocity = new Vector2(impacts % 2 == 0 ? 1 : -1, -.75f).normalized;
    }

    IEnumerator FroggerCrossing()
    {
        yield return Centre();
        yield return Charge(Hull, Vector3.zero, .5f);
        int teeth = Dense(30);
        for (int ring = 0; ring < 6; ring++)
        {
            int sign = ring % 2 == 0 ? 1 : -1;
            int gap = (ring * 9) % teeth;
            for (int i = 0; i < teeth; i++)
            {
                if ((i - gap + teeth) % teeth < 3)
                    continue;
                var tooth = Fire<SimpleRound>(transform.position, Vector2.down, 0, ring % 2 == 0 ? .2f : .26f);
                if (tooth != null)
                    tooth.Orbit(Radius * .9f, i * 360f / teeth, sign * 48, 10, 0, 0, 0, .05f).Expand(1.3f);
            }
            yield return Wait(Rest(.55f));
        }
        yield return Wait(.6f);
        ResumeBouncing();
    }

    IEnumerator RicochetStorm(float seconds)
    {
        yield return Every(seconds, 1.3f, n =>
            ArrowWavefront(transform.position, CombatVisuals.Aim(transform.position), Dense(13), 80, 2, 3.2f, .9f));
    }

    // Four spiral arms, two winding each way: the crossings mesh like gear teeth.
    IEnumerator Gearbox()
    {
        yield return Centre();
        yield return Charge(Hull, Vector3.zero, .5f);
        int beads = Dense(48);
        for (int b = 0; b < beads; b++)
        {
            for (int arm = 0; arm < 4; arm++)
            {
                int sign = arm % 2 == 0 ? 1 : -1;
                var tooth = Fire<SimpleRound>(transform.position, Vector2.down, 0, .2f);
                if (tooth != null)
                    tooth.Orbit(Radius * .8f, arm * 90 + sign * b * 9, sign * 40, 10, 0, 0, 0, .05f).Expand(1.5f);
            }
            yield return Wait(Rest(.085f));
        }
        yield return Wait(.8f);
        ResumeBouncing();
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
