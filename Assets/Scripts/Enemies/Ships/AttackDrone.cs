using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AttackDrone : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 13;
        health = 14;
        hullScale = .38f;
        attackInterval = 1.55f;
        Mover<FreeReposition>();
    }
    // A crisp five-shot accent keeps City fast; delayed fragmentation belongs to Snow.
    protected override void Attack()
    {
        Fan<SimpleRound>(5, 13, 5.1f, .17f);
    }
}
