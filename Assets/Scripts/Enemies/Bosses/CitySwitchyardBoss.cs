using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CitySwitchyardBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(-1,.86f),new(-.88f,1.2f),new(.84f,1.2f),new(1,.88f),new(1.55f,.68f),new(1.55f,.32f),
        new(1.26f,.27f),new(1.26f,-.46f),new(1,-.46f),new(1.05f,-1.2f),new(.66f,-1.30f),new(-.66f,-1.30f),new(-1.05f,-1.20f),
        new(-1,-.46f),new(-1.26f,-.46f),new(-1.26f,.29f),new(-1.32f,.33f),new(-1.32f,.66f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 2600; hullScale = 1.12f; attackInterval = .9f; }

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
            // Red Signal: traffic climbs toward the ceiling and parks. Green lights
            // turn both banks inward onto horizontal tracks; phase three switches
            // alternating banks through a vertical junction instead.
            yield return PrimePair(.52f);
            int cars = Mathf.Max(3, Dense(phaseThree ? 24 : phaseTwo ? 21 : 18));
            int tracks = Dense(phaseThree ? 7 : 5);
            for (int pair = 0; pair < cars; pair++)
            {
                float turnout = Mathf.Lerp(12, 66, pair / (cars - 1f));
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float hold = .42f + .10f * (cars - pair) + (phaseTwo && sign == side ? .36f : 0);
                    for (int track = 0; track < tracks; track++)
                    {
                        var car = Fire<NeedleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.up, sign * turnout),
                            2.8f + track * .7f, .18f);
                        if (phaseThree && pair % 2 == 0)
                            car.Pause(.62f, hold + track * .10f, CombatRound.Turn.Rotate, sign * (180 - turnout), 1.05f);
                        else
                            car.Pause(.62f, hold + track * .10f, CombatRound.Turn.Rotate, sign * (90 - turnout), .94f);
                    }
                }
                yield return Wait(Rest(.10f));
            }
            // Express Service: short trains follow a locked heading while the engine
            // changes platforms. Each train has a faster headlight and slower carriages.
            station.x = Lane(side);
            station.y = Camera.UpLimit - (side > 0 ? 2.2f : 3.55f);
            int trains = Dense(phaseThree ? 8 : phaseTwo ? 7 : 5);
            for (int train = 0; train < trains; train++)
            {
                bool express = train % 3 == 0;
                Vector2 track = express ? CombatVisuals.Aim(Muzzle()) :
                    CombatVisuals.Rotate(Vector2.right * side, (train % 2 == 0 ? 1 : -1) * (phaseTwo ? 35 : 22));
                PointWeapon(Muzzle(), track);
                yield return Charge(Hull, WeaponPort(), .28f);
                int rails = Dense(5);
                for (int rail = 0; rail < rails; rail++)
                {
                    var headlight = Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(track, (rail - (rails - 1) * .5f) * 17), 4.8f, .28f);
                    if (!express) headlight.Bounce(1);
                }
                for (int car = 0; car < Dense(10); car++)
                {
                    yield return Wait(Rest(.105f));
                    for (int rail = 0; rail < rails; rail++)
                    {
                        var carriage = Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(track, (rail - (rails - 1) * .5f) * 17), 3.65f - car * .12f, .17f);
                        if (!express && phaseThree && car % 3 == 0)
                            carriage.RunWalls(2.4f, side);
                        else if (!express) carriage.Bounce(1);
                    }
                }
                // Rebounding signal fans cross the vertical tracks, forcing a planned
                // junction crossing instead of letting the pilot park beside the engine.
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int signals = Dense(phaseThree ? 15 : phaseTwo ? 13 : 11);
                    for (int signal = 0; signal < signals; signal++)
                    {
                        float angle = Mathf.Lerp(48, 112, signal / Mathf.Max(1f, signals - 1));
                        Fire<SimpleRound>(Muzzle(sign < 0 ? 1 : 2), CombatVisuals.Rotate(Vector2.down, sign * angle), 2.6f, .20f).Bounce(1);
                    }
                }
            }
            side = -side;
            yield return Wait(Rest(.48f));
        }
    }
}
