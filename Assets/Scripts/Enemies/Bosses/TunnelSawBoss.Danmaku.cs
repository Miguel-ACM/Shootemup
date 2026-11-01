using System.Collections;
using UnityEngine;

// Tunnel saw. Every round leaves from the centre of the spinning blade.
// Phase one
//   Frogger Crossing, over and over: it glides to a new station, brakes, spins up and
//     throws six widening rings that turn in alternate directions; each ring has a
//     gap and the gaps slide, so crossing them is a timing puzzle.
// Phase two (no minions)
//   Ricochet Storm: it ricochets round the whole arena and fires only when it hits a
//     wall. Every impact throws a full 360-degree burst of fast arrows and slow teeth,
//     some of which bounce off the walls, an aimed shotgun of arrows, and teeth that
//     cling to the walls and run round the rim. Between impacts it is silent.
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
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
                yield return FroggerCrossing();
            else
            {
                runners = true;
                yield return RicochetStorm(10);
            }
        }
    }

    void ImpactBurst(Vector2 inward)
    {
        using var sample = burstMarker.Auto();
        impacts++;
        int rays = Dense(phaseTwo ? 28 : 20);
        float step = 360f / rays;
        for (int i = 0; i < rays; i++)
        {
            var axis = CombatVisuals.Rotate(inward, i * step + impacts * 7);
            Fire<NeedleRound>(transform.position, axis, 4.8f, .19f);
            var tooth = Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(axis, step * .5f), 1.6f, .3f);
            if (i % 2 == 0)
                tooth.Bounce(2);
        }
        if (phaseTwo)
        {
            // An aimed shotgun out of every impact, so a bounce near the player is a threat.
            var aim = CombatVisuals.Aim(transform.position);
            for (int i = 0; i < Dense(5); i++)
                Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(aim, (i - 2) * 9), 5.6f, .19f);
        }
        if (!runners)
            return;
        int count = Dense(6);
        for (int i = 0; i < count; i++)
        {
            var axis = CombatVisuals.Rotate(inward, (i - (count - 1) * .5f) * 26);
            Fire<SimpleRound>(transform.position, axis, 4.4f, .24f)
                .RunWalls(8, impacts % 2 == 0 ? 1 : -1);
        }
    }

    static readonly Vector2[] Stations = { new Vector2(0, .25f), new Vector2(-.55f, .5f), new Vector2(.55f, .05f), new Vector2(-.5f, -.1f), new Vector2(.5f, .55f) };
    int nextStation;

    // Brake, glide to a station (the centre by default) and hold there while the card plays.
    IEnumerator Centre(Vector2? spot = null)
    {
        haltUntil = float.MaxValue;
        var at = spot ?? new Vector2(0, .25f);
        var centre = new Vector3(arena.center.x + at.x * (arena.width * .5f - Radius), arena.center.y + at.y * (arena.height * .5f - Radius), transform.position.z);
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
        spinner.spin = 720;
        yield return Centre(Stations[nextStation++ % Stations.Length]);
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
    }

    // Silent while it travels: all of its fire comes from the impacts.
    IEnumerator RicochetStorm(float seconds)
    {
        if (haltUntil > age)
            ResumeBouncing();
        yield return Wait(seconds);
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

    // Hard final phase, Overdrive: it ricochets round the arena throwing wall runners
    // while turning out a four-arm arrow spiral from its centre the whole time.
    IEnumerator FinalSpell()
    {
        runners = true;
        if (haltUntil > age)
            ResumeBouncing();
        yield return Every(9, .08f, n =>
        {
            for (int arm = 0; arm < 4; arm++)
                Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, arm * 90 + n * 13), 3.4f, .19f);
        });
        runners = false;
    }
}
