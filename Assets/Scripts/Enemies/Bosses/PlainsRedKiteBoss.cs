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
            int bursts = Dense(phaseThree ? 12 : phaseTwo ? 11 : 10);
            int feathers = Dense(6);
            for (int i = 0; i < bursts; i++)
            {
                // Two gun ribbons bracket the committed sightline. The pilot must leave
                // that line rather than sidestep one isolated needle and stop moving.
                for (int wing = 1; wing <= 2; wing++)
                    for (int feather = 0; feather < feathers; feather++)
                    {
                        float spread = (feather - (feathers - 1) * .5f) * 8;
                        Fire<NeedleRound>(Muzzle(wing), CombatVisuals.Rotate(sight, spread), 4.1f + .18f * (i % 4), .17f);
                    }
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
        int beats = Mathf.Max(3, Dense(phaseThree ? 24 : phaseTwo ? 22 : 20)), emitted = 0;
        int feathers = Dense(6);
        float duration = phaseThree ? 1.55f : 2.05f;
        for (float t = 0; t < 1;)
        {
            if (!Holding)
            {
                t = Mathf.Min(1, t + Time.deltaTime * LevelRun.EnemyTempoScale * MovementScale / duration);
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t)) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * depth);
                while (emitted < beats && t >= (emitted + .5f) / beats)
                {
                    for (int wing = 0; wing < 2; wing++)
                    {
                        float cant = (wing == 0 ? -1 : 1) * (12 + 32 * t);
                        for (int feather = 0; feather < feathers; feather++)
                            Fire<NeedleRound>(Muzzle(1 + wing), CombatVisuals.Rotate(Vector2.down, cant +
                                (feather - (feathers - 1) * .5f) * 7), 2.7f, .17f).Ramp(1.35f, .9f, .5f);
                    }
                    if (emitted % (phaseThree ? 4 : 6) == 0)
                        // Nose payloads open flat across the wake, never into circles.
                        Fire<SimpleRound>(Muzzle(), Vector2.down, 2.6f, .32f)
                            .Pause(.48f, .28f).SplitAt(.76f, Dense(13), 12, 2.7f, .16f);
                    emitted++;
                }
            }
            yield return null;
        }
    }
}
