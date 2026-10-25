using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class NeedleCannon : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Needle;
    protected override float Power => 1.3f;
    protected override float Velocity => 1.3f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .65f;
    }
}
