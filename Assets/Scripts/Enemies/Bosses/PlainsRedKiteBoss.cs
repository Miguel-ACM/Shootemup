using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class PlainsRedKiteBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.5f),new(-.42f,-1.28f),new(-1.52f,.26f),new(-1.28f,.59f),new(-1.30f,1.06f),new(-.53f,.9f),new(-.34f,1.04f),
        new(.34f,1.04f),new(.53f,.9f),new(1.30f,1.06f),new(1.28f,.59f),new(1.52f,.26f),new(.42f,-1.28f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 1200; hullScale = 1.02f; attackInterval = .8f; }

    protected override void MoveBoss(float dt)
    {
        if (!openingComplete) base.MoveBoss(dt * 2);
    }

    protected override IEnumerator Cards()
    {
        int side = transform.position.x > 0 ? -1 : 1;
        while (true)
        {
            // Sightline: commit to an aim, fire a tight walking burst, then break away.
            yield return Charge(Hull, WeaponPort(), .50f);
            Vector2 sight = CombatVisuals.Aim(Muzzle());
            int bursts = Dense(phaseThree ? 14 : phaseTwo ? 12 : 10);
            int feathers = Dense(7);
            for (int i = 0; i < bursts; i++)
            {
                // Two gun ribbons bracket the committed sightline. The pilot must leave
                // that line rather than sidestep one isolated needle and stop moving.
                for (int wing = 1; wing <= 2; wing++)
                    for (int feather = 0; feather < feathers; feather++)
                    {
                        float spread = (feather - (feathers - 1) * .5f) * 8;
                        ScoredShot(i%3==0,Muzzle(wing), CombatVisuals.Rotate(sight, spread), 4.1f + .18f * (i % 4), .17f);
                    }
                if(i%3==0)Fire<SimpleRound>(Muzzle(),CombatVisuals.Aim(Muzzle()),3.4f,.21f);
                if (phaseTwo && i % 2 == 0)
                    Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.up, side * (24 + i * 3)), 2.6f, .25f).Bounce(1);
                yield return Wait(Rest(.15f));
            }
            yield return Wait(Rest(.22f));
            // Scissor pass: the two wing guns leave a diverging wake along a low, banked cut.
            yield return Crossing(side, phaseThree ? 4.1f : phaseTwo ? 3.35f : 2.4f);
            side = -side;
            if (phaseThree)
            {
                // Reversal: double back high through the wake before lining up the next sightline.
                yield return Wait(Rest(.24f));
                yield return Crossing(side, 1.1f);
                side = -side;
            }
            yield return Wait(Rest(.48f));
        }
    }

    IEnumerator Crossing(int side, float depth)
    {
        yield return PrimePair(.32f);
        Vector3 start = transform.position, end = new Vector3(Lane(side), Camera.UpLimit - 2.4f, start.z);
        int beats = Mathf.Max(3, Dense(phaseThree ? 30 : phaseTwo ? 26 : 22)), emitted = 0;
        int feathers = Dense(7);
        float duration = phaseThree ? 1.55f : 2.05f;
        for (float t = 0; t < 1;)
        {
            if (!Holding)
            {
                t = Mathf.Min(1, t + Time.deltaTime * LevelRun.EnemyTempoScale * MovementScale / duration);
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t)) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * depth);
                while (emitted < beats && t >= (emitted + .5f) / beats)
                {
                    // Follow the bank's tangent, not the screen's down axis. The
                    // contrails peel above and beside the aircraft, then hook across
                    // its old flight lane after a visible braking interval.
                    Vector2 tangent = new Vector2((end.x - start.x) * 6 * t * (1 - t),
                        -Mathf.Cos(t * Mathf.PI) * Mathf.PI * depth).normalized;
                    for (int wing = 0; wing < 2; wing++)
                    {
                        int mirror = wing == 0 ? -1 : 1;
                        float cant = mirror * (64 + 24 * t);
                        for (int feather = 0; feather < feathers; feather++)
                            ScoredShot(emitted%3==0,Muzzle(1 + wing), CombatVisuals.Rotate(tangent, cant +
                                (feather - (feathers - 1) * .5f) * 7), 2.7f, .17f)
                                .Pause(.70f, .26f + feather * .035f, CombatRound.Turn.Rotate,
                                    -mirror * side * (phaseThree ? 110 : 75), .85f)
                                .Ramp(1.35f, .9f, 1.1f);
                    }
                    if (emitted % (phaseThree ? 4 : 6) == 0)
                        // Nose payloads open flat across the wake, never into circles.
                        Fire<SimpleRound>(Muzzle(), -tangent, 2.6f, .32f)
                            .Pause(.48f, .28f).SplitAt(.76f, Dense(13), 12, 2.7f, .16f);
                    emitted++;
                }
            }
            yield return null;
        }
    }
}
