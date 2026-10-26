using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class DepthCharge : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Shell;
    protected override Vector2 FireDirection => Vector2.down;
    protected override float Power => 3.2f;
    protected override float Velocity => .55f;
    protected override void Awake() { shootSpeedModifier = .18f; }
}
