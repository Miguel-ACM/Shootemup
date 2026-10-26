using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class HeavyBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 2;
        health = 80;
        hullScale = .9f;
        attackInterval = Timing(2.6f, 1.8f);
        shellTravelDistance = 5.2f;
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (Difficulty.Easy && (volley + 1) % 2 == 0)
            Cue(Muzzle(), seconds);
        else
            firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
            Fan<BurstShell>(Shots(1, 2), 32, 3.0f, .44f);
        else
            Fan<NeedleRound>(Shots(3, 5), 18, 3.8f, .19f);
    }
}
