using UnityEngine;

public sealed partial class SnowSiegeBoss
{

    void AttackHard()
    {
        int beat = (volley - 1) % 14;
        if (beat == 0 || beat == 5)
        {
            int sign = beat == 0 ? -1 : 1;
            var seed = Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, sign * 22), 1.85f, .72f);
            if (seed != null)
                seed.IceSeed(2.1f, phaseTwo ? 32 : 28, 4, 2.35f, sign * (phaseTwo ? 32 : 24), 1.1f, sign * 35);
        }
        else if (beat == 3 || beat == 8)
            for (int i = 0; i < (phaseTwo ? 4 : 3); i++)
            {
                var shell = Fire<BurstShell>(Muzzle(), CombatVisuals.Rotate(Vector2.down, (i - (phaseTwo ? 3 : 2) * .5f) * 34 + (beat == 3 ? -8 : 8)), 2.7f, .53f);
                if (shell != null)
                    shell.SetTravelDistance(beat == 3 ? 4.1f : 6.1f);
            }
        else if (beat == 10)
            DeployComb();
        else if (beat == 11 || beat == 12)
            for (int i = 0; i < combCount; i++)
            {
                var p = comb[i].TransformPoint(new Vector3(0, -.26f, -.55f));
                var shot = Fire<SimpleRound>(p, Vector2.down, beat == 11 ? 2.2f : 2.7f, .18f);
                if (shot != null)
                    shot.Curl((i < combCount / 2 ? -1 : 1) * (beat == 11 ? 12 : -12), 2.6f);
                releases[i] = .1f;
            }
        else if (beat == 13)
        {
            deployment = 0;
            returning = true;
            for (int i = 0; i < combCount; i++)
                launch[i] = comb[i].position;
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 14;
        if (beat == 0 || beat == 3 || beat == 5 || beat == 8)
            Cue(Muzzle(), seconds);
        else if (beat == 11 || beat == 12)
            for (int i = 0; i < combCount; i++)
                Cue(comb[i].TransformPoint(new Vector3(0, -.26f, -.55f)), seconds);
    }
}
