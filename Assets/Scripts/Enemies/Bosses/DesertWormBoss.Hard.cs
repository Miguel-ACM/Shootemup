using UnityEngine;

public sealed partial class DesertWormBoss
{

    int WindingCore(int wanted)
    {
        for (int n = 0; n < cores.Length; n++)
        {
            int i = (wanted + n) % cores.Length;
            if (CanShoot(cores[i]))
                return i;
        }
        return -1;
    }
    void AttackHard()
    {
        int beat = (volley - 1) % 16;
        if (beat == 0 || beat == 2 || beat == 4)
        {
            int i = WindingCore(1 + (beat / 2 + (volley - 1) / 16) % 7);
            if (i < 0)
                return;
            Patterns.Spiral(cores[i].transform, Vector3.zero, 2, 18, (i % 2 == 0 ? 1 : -1) * 20, .085f, 2.6f, .16f, i * 31, cores[i]);
        }
        else if (beat == 6)
        {
            int i = WindingCore(0);
            if (i < 0)
                return;
            Vector3 p = i == 0 ? Muzzle() : cores[i].transform.position;
            Vector2 axis = i == 0 ? (Vector2)Hull.TransformDirection(Vector3.down) : Vector2.down;
            HeavyTrail(p, axis, .7f, 1.8f);
        }
        else if (beat == 9 || beat == 10)
        {
            for (int i = 1; i < cores.Length; i++)
                if (CanShoot(cores[i]))
                    for (int j = -1; j <= 1; j++)
                    {
                        var shot = Fire<SimpleRound>(cores[i].transform.position, CombatVisuals.Rotate(Vector2.down, j * 35 + (i % 2 == 0 ? 12 : -12)), 2.4f, .2f);
                        if (shot != null)
                            shot.Curl((i % 2 == 0 ? 1 : -1) * 18, 2.2f);
                    }
        }
        else if (phaseTwo && beat == 13)
        {
            int i = WindingCore((volley / 16) % 8);
            if (i >= 0)
                Patterns.Ribbon(cores[i].transform, Vector3.zero, CombatVisuals.Aim(cores[i].transform.position), 3, 16, 32, 3.2f, .09f, cores[i]);
        }
    }
    protected override void PrepareHardFire(float seconds)
    {
        int beat = volley % 16;
        if (beat == 0 || beat == 2 || beat == 4)
        {
            int i = WindingCore(1 + (beat / 2 + volley / 16) % 7);
            if (i >= 0)
                Cue(cores[i].transform.position, seconds);
        }
        else if (beat == 6)
        {
            int i = WindingCore(0);
            if (i >= 0)
                Cue(i == 0 ? Muzzle() : cores[i].transform.position, seconds);
        }
        else if (beat == 9 || beat == 10)
        {
            for (int i = 1; i < cores.Length; i++)
                if (CanShoot(cores[i]))
                    Cue(cores[i].transform.position, seconds);
        }
        else if (phaseTwo && beat == 13)
        {
            int i = WindingCore(((volley + 1) / 16) % 8);
            if (i >= 0)
                Cue(cores[i].transform.position, seconds);
        }
    }
}
