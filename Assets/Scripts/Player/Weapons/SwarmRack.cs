using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SwarmRack : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Missile;
    protected override float Power => 1.4f;
    protected override float Velocity => 0.7f;
    protected override int Pellets => 2;
    protected override void Awake()
    {
        shootSpeedModifier = .28f;
    }
}
