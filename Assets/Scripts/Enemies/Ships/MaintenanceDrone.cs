using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class MaintenanceDrone : DistrictEnemy
{
    protected override void Configure()
    {
        health = 25;
        hullScale = .5f;
        attackInterval = (1.9f);
        Mover<FreeReposition>();
    }

    // Fast and slow along one aim, Tunnel's grammar: a quick rice grain from one muzzle and a slow
    // round shot from the other, the flanks swapping each volley. Miguel removed the S-shaped
    // helix pairs from Tunnel (15 November 2026).
    protected override void Attack()
    {
        int flank = volley % 2 == 0 ? 1 : -1;
        Fire<RiceRound>(Muzzle(flank > 0 ? 0 : 1), CombatVisuals.Rotate(Aim, -flank * 9), 3.6f, .17f);
        Fire<SimpleRound>(Muzzle(flank > 0 ? 1 : 0), CombatVisuals.Rotate(Aim, flank * 11), 1.8f, .18f);
    }
}
