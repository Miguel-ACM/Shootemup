using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class RelayDrone : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 10;
        health = 32;
        hullScale = .55f;
        attackInterval = Timing(2.6f,1.9f);
        Mover<StrafeAndBrake>();
    }

    protected override void Attack()
    {
        int n=Shots(2,3);
        for(int i=0;i<n;i++) Fire<NeedleRound>(Muzzle(n==2?i*2:i),CombatVisuals.Rotate(Aim,(i-(n-1)*.5f)*15),4.8f,.18f);
    }
}
