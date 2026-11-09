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

    // Tones: Sounding Line's floes take the lead tone with re-aimed mouth rounds in
    // the other; Cut Channel's bow wave the other tone with lead-tone shoulder drops;
    // Hard's Calving Front the lead tone again. Phase two swaps them.
    HostileTone Lead => CurrentPhase == 2 ? HostileTone.Dark : HostileTone.Bright;
    static HostileTone Opposite(HostileTone value) => value == HostileTone.Bright ? HostileTone.Dark : HostileTone.Bright;

    protected override IEnumerator Cards()
    {
        int side = 1;
        while (true)
        {
            // Sounding Line: tusks of ice rise inward from the shoulders, freeze,
            // then crack sideways across each other. The upper arena belongs to
            // this seam too; it is not another downward shell fan.
            station = new Vector3(Lane(-side), Camera.UpLimit - 2.3f, 1);
            while (Vector2.Distance(transform.position, station) > .1f) yield return null;
            yield return Charge(Hull, WeaponPort(), .52f);
            int floes = Mathf.Max(2, Dense(phaseThree ? 11 : phaseTwo ? 9 : 8));
            for (int i = 0; i < floes; i++)
            {
                UseTone(Opposite(Lead));
                if(i%3==0)
                    Fire<SimpleRound>(Muzzle(),CombatVisuals.Aim(Muzzle()),1.2f,.24f)
                        .Pause(.4f,.45f,CombatRound.Turn.Aim,0,2.4f);
                float heading = Mathf.Lerp(8, 52, i / (floes - 1f));
                float stop = .38f + .10f * i;
                UseTone(Lead);
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<SimpleRound>(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.up, mirror * heading), 3.7f, .37f)
                        .Pause(stop, .64f, CombatRound.Turn.Rotate, mirror * (90 - heading), .72f)
                        .SplitAt(stop + .70f, Dense(phaseThree ? 15 : 13), 11, 2.7f, .16f);
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
                    UseTone(Opposite(Lead));
                    float cant = 24 + 20 * Mathf.Sin(i * .38f);
                    for (int shard = 0; shard < shards; shard++)
                        // Outward upthrust describes a breaking bow wave. Gravity
                        // curls it back over the pilot while the prow changes sides.
                        ScoredShot(i%3==0,Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.right * sign,
                            sign * (cant + (shard - (shards - 1) * .5f) * 9)), 2.9f, .18f)
                            .Ramp(.38f, .38f).Fall(new Vector2(0,-.95f));
                    UseTone(Lead);
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
                    UseTone(Lead);
                    for (int mirror = -1; mirror <= 1; mirror += 2)
                        Fire<SimpleRound>(Muzzle(mirror < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.up, mirror * (8 + i * 8)), 2.9f, .27f)
                            .Pause(.35f, .30f, CombatRound.Turn.Rotate, -mirror * 120, .85f)
                            .SplitAt(.72f + .10f * i, Dense(11), 12, 2.6f, .16f);
                    yield return Wait(Rest(.12f));
                }
            }
            yield return Wait(Rest(.48f));
            side = -side;
        }
    }
}
