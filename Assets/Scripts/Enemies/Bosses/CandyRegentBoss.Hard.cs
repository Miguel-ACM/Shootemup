using UnityEngine;

public sealed partial class CandyRegentBoss
{

    void AttackHard()
    {
        if (!arrived || player != null && Vector2.Distance(player.position, transform.position) < 2.5f)
            return;
        int beat = (volley - 1) % 16;
        if (beat == 0 || beat == 4)
            Patterns.Ribbon(Hull, WeaponPort(beat == 0 ? 0 : 1), Vector2.down, phaseTwo ? 3 : 2, 18, beat == 0 ? 38 : -38, 2.8f, .075f);
        else if (beat == 7 || phaseTwo && beat == 8)
            HeavyTrail(Muzzle(beat % 2), CombatVisuals.Rotate(Vector2.down, beat == 7 ? -16 : 16), .82f, 1.8f);
        else if (beat == 11)
            Patterns.Spiral(Hull, WeaponPort(1), 2, 24, 15, .07f, 2.5f, .15f);
        else if (beat == 14)
            OrbitalCrown(Muzzle(0), phaseTwo ? 26 : 22, 4, 1.75f, phaseTwo ? -46 : 38, 1.45f, volley * 7);
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 16;
        if (beat == 0 || beat == 4)
            Cue(Muzzle(beat == 0 ? 0 : 1), seconds);
        else if (beat == 7 || phaseTwo && beat == 8)
            Cue(Muzzle(beat % 2), seconds);
        else if (beat == 11)
            Cue(Muzzle(1), seconds);
        else if (beat == 14)
            Cue(Muzzle(0), seconds);
    }
}
