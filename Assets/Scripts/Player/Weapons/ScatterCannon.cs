using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScatterCannon : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Pulse;
    protected override float Power => .8f;
    protected override float Velocity => 1f;
    protected override int Pellets => 5;
    protected override void Awake()
    {
        shootSpeedModifier = .3f;
    }
}
