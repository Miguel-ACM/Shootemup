using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CourierDiver : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 15;
        health = 9;
        hullScale = .34f;
        attackInterval = Timing(1.3f,0.95f);
        Mover<CommittedDive>();
    }

    protected override void Attack()
    {
        Fan<NeedleRound>(1,0,4.8f,.18f);
    }
}
