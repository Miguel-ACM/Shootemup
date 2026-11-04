using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SnowIcebreakerBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.53f),new(-.78f,-1.05f),new(-1.14f,-.55f),new(-1.02f,.65f),new(-.79f,1.08f),
        new(.79f,1.08f),new(1.02f,.65f),new(1.14f,-.55f),new(.78f,-1.05f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 2100; hullScale = 1.14f; attackInterval = 1.05f; }

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
            int floes = Mathf.Max(2, Dense(phaseThree ? 11 : phaseTwo ? 9 : 8));
            for (int i = 0; i < floes; i++)
            {
                float heading = Mathf.Lerp(8, 52, i / (floes - 1f));
                float stop = .38f + .10f * i;
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<SimpleRound>(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, mirror * heading), 3.7f, .37f)
                        .Pause(stop, .64f).SplitAt(stop + .60f, Dense(phaseThree ? 15 : 13), 11, 2.7f, .16f);
                yield return Wait(Rest(.18f));
            }
            // Cut Channel: the prow commits to one shallow diagonal run. Its
            // shoulder vents eject paired sheets outward, leaving water open behind it.
            yield return PrimePair(.50f);
            station = new Vector3(Lane(side), Camera.UpLimit - (phaseThree ? 6.2f : 4.9f), 1);
            int wakes = Mathf.Max(3, Dense(phaseThree ? 24 : phaseTwo ? 21 : 18));
            int shards = Dense(7);
            for (int i = 0; i < wakes; i++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float cant = 24 + 20 * Mathf.Sin(i * .38f);
                    for (int shard = 0; shard < shards; shard++)
                        Fire<NeedleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down,
                            sign * (cant + (shard - (shards - 1) * .5f) * 9)), 2.9f, .18f)
                            .Ramp(.38f, .38f).Fall(new Vector2(0,-.95f));
                    if (phaseTwo && i % 3 == 0)
                        for (int shard = 0; shard < Dense(7); shard++)
                            Fire<SimpleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.up, sign * (24 + shard * 13)), 2.6f, .22f)
                                .Pause(.55f, .48f + shard * .05f, CombatRound.Turn.Down, speed: .60f)
                                .Ramp(2.1f, 1.3f, 1.2f);
                }
                yield return Wait(Rest(.18f));
            }
            if (phaseThree)
            {
                // Calving Front: a second fracture races inward along the cut the pilot just crossed.
                yield return Charge(Hull, WeaponPort(), .42f);
                for (int i = 0; i < floes; i++)
                {
                    for (int mirror = -1; mirror <= 1; mirror += 2)
                        Fire<SimpleRound>(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, mirror * (8 + i * 8)), 2.9f, .27f)
                            .SplitAt(.55f + .10f * i, Dense(11), 12, 2.6f, .16f);
                    yield return Wait(Rest(.12f));
                }
            }
            yield return Wait(Rest(.48f));
            side = -side;
        }
    }
}
