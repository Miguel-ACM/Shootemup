using UnityEngine;

public sealed partial class ThunderLanceBoss
{

    void AttackHard()
    {
        int beat = (volley - 1) % 16;
        if (beat == 0)
        {
            Beam(0);
            if (phaseTwo)
                Beam(1);
        }
        else if (beat == 3 && !phaseTwo)
            Beam(1);
        else if (beat == 7)
            Beam(2);
        else if (beat == 2)
            Patterns.Web(Hull, WeaponPort(2), phaseTwo ? 9 : 8, 3, 2.35f, 25);
        else if (beat == 5 || beat == 11)
            for (int i = 0; i < 2; i++)
                Patterns.Ribbon(Hull, WeaponPort(i), Vector2.down, 2, phaseTwo ? 18 : 14, (i == 0 ? 1 : -1) * 30, 3.1f, .08f);
        else if (beat == 13)
        {
            var pulse = Fire<SimpleRound>(Muzzle(2), Vector2.down, 1.6f, .82f);
            if (pulse != null)
                pulse.IceSeed(1.8f, phaseTwo ? 32 : 28, 5, 2.7f, phaseTwo ? -30 : 22, 1.15f, 20);
        }
    }
    protected override void PrepareHardFire(float seconds)
    {
        int beat = volley % 16;
        if (beat == 0 || beat == 5 || beat == 11)
        {
            Cue(Muzzle(0), seconds);
            if (phaseTwo || beat != 0)
                Cue(Muzzle(1), seconds);
        }
        else if (beat == 3 && !phaseTwo)
            Cue(Muzzle(1), seconds);
        else if (beat == 2 || beat == 7 || beat == 13)
            Cue(Muzzle(2), seconds);
    }
}
