using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CitySwitchyardBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(-1,.86f),new(-.88f,1.2f),new(.84f,1.2f),new(1,.88f),new(1.55f,.68f),new(1.55f,.32f),
        new(1.26f,.27f),new(1.26f,-.46f),new(1,-.46f),new(1.05f,-1.2f),new(.66f,-1.30f),new(-.66f,-1.30f),new(-1.05f,-1.20f),
        new(-1,-.46f),new(-1.26f,-.46f),new(-1.26f,.29f),new(-1.32f,.33f),new(-1.32f,.66f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 1930; hullScale = 1.12f; attackInterval = .9f; }

    protected override void MoveBoss(float dt)
    {
        // Brake into each signal box, then wait there for the timetable.
        float distance = Vector3.Distance(transform.position, station);
        transform.position = Vector3.MoveTowards(transform.position, station, dt * Mathf.Clamp(distance * 2.6f, .8f, 6.6f));
    }

    protected override IEnumerator Cards()
    {
        int side = -1;
        while (true)
        {
            // Red Signal: pairs fan out, park, then switch onto vertical tracks.
            // Later phrases stagger the green lights and throw the points across the junction.
            yield return PrimePair(.52f);
            int cars = Mathf.Max(3, Dense(phaseThree ? 11 : 8));
            for (int pair = 0; pair < cars; pair++)
            {
                float turnout = Mathf.Lerp(12, 66, pair / (cars - 1f));
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float hold = .42f + .10f * (cars - pair) + (phaseTwo && sign == side ? .36f : 0);
                    var car = Fire<NeedleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, sign * turnout), 3.3f, .20f);
                    if (phaseThree && pair % 2 == 0)
                        car.Pause(.62f, hold, CombatRound.Turn.Rotate, -sign * (turnout + 24), 1.24f);
                    else
                        car.Pause(.62f, hold, CombatRound.Turn.Down, speed: 1.08f);
                }
                yield return Wait(Rest(.10f));
            }
            // Express Service: short trains follow a locked heading while the engine
            // changes platforms. Each train has a faster headlight and slower carriages.
            station.x = Lane(side);
            station.y = Camera.UpLimit - (side > 0 ? 2.2f : 3.55f);
            int trains = phaseThree ? 4 : phaseTwo ? 3 : 2;
            for (int train = 0; train < trains; train++)
            {
                yield return Charge(Hull, WeaponPort(), .28f);
                Vector2 track = CombatVisuals.Aim(Muzzle());
                Fire<SimpleRound>(Muzzle(), track, 4.8f, .28f);
                for (int car = 0; car < Dense(5); car++)
                {
                    yield return Wait(Rest(.105f));
                    Fire<NeedleRound>(Muzzle(), track, 3.65f, .17f);
                }
                if (phaseTwo)
                {
                    int signal = train % 2 + 1;
                    Fire<SimpleRound>(Muzzle(signal), CombatVisuals.Rotate(Vector2.up, signal == 1 ? -28 : 28), 3, .22f).Bounce(1);
                }
            }
            side = -side;
            yield return Wait(Rest(.85f));
        }
    }
}
