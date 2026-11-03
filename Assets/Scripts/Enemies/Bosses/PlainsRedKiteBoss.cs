using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class PlainsRedKiteBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.5f),new(-.42f,-1.28f),new(-1.52f,.26f),new(-1.28f,.59f),new(-1.30f,1.06f),new(-.53f,.9f),new(-.34f,1.04f),
        new(.34f,1.04f),new(.53f,.9f),new(1.30f,1.06f),new(1.28f,.59f),new(1.52f,.26f),new(.42f,-1.28f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 1050; hullScale = 1.02f; attackInterval = .8f; }

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
            for (int i = 0; i < Dense(phaseThree ? 7 : 5); i++)
            {
                Fire<NeedleRound>(Muzzle(), sight, 4.8f + .40f * i, .17f);
                if (phaseTwo)
                    Fire<NeedleRound>(Muzzle(i % 2 + 1), CombatVisuals.Rotate(sight, i % 2 == 0 ? -16 : 16), 3.7f, .16f);
                yield return Wait(Rest(.12f));
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
            yield return Wait(Rest(.75f));
        }
    }

    IEnumerator Crossing(int side, float depth)
    {
        yield return PrimePair(.32f);
        Vector3 start = transform.position, end = new Vector3(Lane(side), Camera.UpLimit - 2.4f, start.z);
        int beats = Mathf.Max(3, Dense(phaseTwo ? 11 : 8)), emitted = 0;
        float duration = phaseThree ? 1.18f : 1.6f;
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
                        float cant = (wing == 0 ? -1 : 1) * (18 + 32 * t);
                        Fire<NeedleRound>(Muzzle(1 + wing), CombatVisuals.Rotate(Vector2.down, cant), 2.7f, .17f).Ramp(1.45f, .7f, .4f);
                    }
                    if (phaseTwo && emitted % 3 == 0)
                        Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.up, side * 34), 2.4f, .23f).Bounce(1);
                    emitted++;
                }
            }
            yield return null;
        }
    }
}
