using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MantaRay : OceanEnemy
{
    protected override void Configure()
    {
        hull = 51;
        hullScale = .57f;
        health = 58;
        attackInterval = Timing(2.9f,2.2f);
        Mover<Slalom>();
    }

    protected override void Attack()
    {
        int n=Shots(2,3);
        for(int i=0;i<n;i++) Fire<SimpleRound>(Muzzle(n==2?i*2:i),CombatVisuals.Rotate(Aim,(i-(n-1)*.5f)*29),2.6f,.27f);
    }
}
