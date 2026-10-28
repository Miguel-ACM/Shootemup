using UnityEngine;

public sealed partial class FieldsBomberBoss
{

    void AttackHard()
    {
        int beat = (volley - 1) % 12;
        if (beat == 0 || beat == 3)
        {
            int side = beat == 0 ? 0 : 1;
            Patterns.Spiral(Hull, WeaponPort(side), phaseTwo ? 4 : 3, 24, side == 0 ? 15 : -15, .085f, 2.8f, .17f, side == 0 ? -35 : 35);
        }
        else if (beat == 6)
            for (int side = -1; side <= 1; side += 2)
                HeavyTrail(Hull.TransformPoint(bays[side < 0 ? 1 : 4]), CombatVisuals.Rotate(Vector2.down, side * 12), .78f);
        else if (beat == 9)
            Fire<ScatterCarrier>(Muzzle(4), Vector2.down, 2.6f, .33f);
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 12;
        if (beat == 0 || beat == 3)
            Cue(Muzzle(beat == 0 ? 0 : 1), seconds);
        else if (beat == 6)
        {
            Cue(Hull.TransformPoint(bays[1]), seconds);
            Cue(Hull.TransformPoint(bays[4]), seconds);
        }
        else if (beat == 9)
            Cue(Muzzle(4), seconds);
    }
}
