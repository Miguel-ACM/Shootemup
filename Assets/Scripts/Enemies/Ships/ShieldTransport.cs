using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ShieldTransport : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 8;
        health = 100;
        hullScale = .95f;
        attackInterval = Timing(2.6f,1.9f);
        Mover<DriftDownPattern>();
    }

    protected override void Attack()
    {
        if(volley%2==0) Fan<PlasmaOrb>(Shots(2,3),37,2.2f,.48f);
        else Fan<NeedleRound>(Shots(3,5),20,3.7f,.2f);
    }
}
