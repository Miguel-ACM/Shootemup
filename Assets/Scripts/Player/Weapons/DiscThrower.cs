using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class DiscThrower : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Disc;
    protected override float Power => 2f;
    protected override float Velocity => 0.65f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .24f;
    }
}
