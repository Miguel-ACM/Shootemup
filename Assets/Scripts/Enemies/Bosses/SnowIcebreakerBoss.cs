using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SnowIcebreakerBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.53f),new(-.78f,-1.05f),new(-1.14f,-.55f),new(-1.02f,.65f),new(-.79f,1.08f),
        new(.79f,1.08f),new(1.02f,.65f),new(1.14f,-.55f),new(.78f,-1.05f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 1900; hullScale = 1.14f; attackInterval = 1.05f; }

    protected override void MoveBoss(float dt) => transform.position = Vector3.MoveTowards(transform.position, station, dt * 4.1f);

    protected override IEnumerator Cards()
    {
        int side = 1;
        while (true)
        {
            // Sounding Line: plant a diagonal seam of ice. Each heavy floe stops
            // in place and fractures in a forward cone; successive cracks chase the bow.
            station = new Vector3(Lane(-side), Camera.UpLimit - 2.3f, 1);
            while (Vector2.Distance(transform.position, station) > .1f) yield return null;
            yield return Charge(Hull, WeaponPort(), .52f);
            int floes = Mathf.Max(2, Dense(phaseThree ? 7 : 5));
            for (int i = 0; i < floes; i++)
            {
                float heading = Mathf.Lerp(12, 63, i / (floes - 1f));
                float stop = .38f + .13f * i;
                Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, side * heading), 3.7f, .37f)
                    .Pause(stop, .72f).SplitAt(stop + .60f, Dense(phaseThree ? 8 : 6), 14, 2.7f, .16f);
                yield return Wait(Rest(.20f));
            }
            // Cut Channel: the prow commits to one shallow diagonal run. Its
            // shoulder vents eject paired sheets outward, leaving water open behind it.
            yield return PrimePair(.50f);
            station = new Vector3(Lane(side), Camera.UpLimit - (phaseThree ? 6.2f : 4.9f), 1);
            int wakes = Mathf.Max(3, Dense(phaseTwo ? 9 : 6));
            for (int i = 0; i < wakes; i++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float cant = 50 + 16 * Mathf.Sin(i * .8f);
                    var shard = Fire<NeedleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, sign * cant), 2.9f, .2f);
                    shard.Ramp(.38f, .38f).Fall(new Vector2(0,-.95f));
                    if (phaseTwo && i % 2 == 0)
                        Fire<SimpleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.up, sign * 40), 2.6f, .22f)
                            .Pause(.55f, .38f, CombatRound.Turn.Down, speed: .78f);
                }
                yield return Wait(Rest(.22f));
            }
            if (phaseThree)
            {
                // Calving Front: a second fracture races inward along the cut the pilot just crossed.
                yield return Charge(Hull, WeaponPort(), .42f);
                for (int i = 0; i < floes; i++)
                    Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, -side * (12 + i * 10)), 2.9f, .27f)
                        .SplitAt(.55f + .15f * i, Dense(5), 18, 2.6f, .16f);
            }
            yield return Wait(Rest(.80f));
            side = -side;
        }
    }
}
