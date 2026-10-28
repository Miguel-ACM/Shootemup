using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class AttackDrone : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 13;
        health = 14;
        hullScale = .38f;
        attackInterval = (1.55f);
        Mover<FreeReposition>();
    }
    // A crisp five-shot accent keeps City fast; delayed fragmentation belongs to Snow.
    // A headlight ray: one aim, four speeds, arriving as a dotted line.
    protected override void Attack()
    {
        int beads = Dense(4);
        var aim = Aim;
        for (int i = 0; i < beads; i++)
            Fire<SimpleRound>(Muzzle(i), aim, 5.0f - i * .7f, .17f);
    }
}
