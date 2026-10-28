using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CourierDiver : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 15;
        health = 9;
        hullScale = .34f;
        attackInterval = (0.95f);
        Mover<CommittedDive>();
    }

    // A needle and a slow round on the same aim: dodge the first, then the second.
    protected override void Attack()
    {
        var aim = Aim;
        Fire<NeedleRound>(Muzzle(), aim, 4.8f, .18f);
        Fire<SimpleRound>(Muzzle(), aim, 2.2f, .2f);
    }
}
