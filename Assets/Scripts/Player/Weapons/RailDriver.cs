using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class RailDriver : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Needle;
    protected override float Power => 8f;
    protected override float Velocity => 2f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .12f;
    }
}
