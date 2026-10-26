using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class Backdraft : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Flame;
    protected override Vector2 FireDirection => Vector2.down;
    protected override float Power => .5f;
    protected override float Velocity => .7f;
    protected override int Pellets => 3;
    protected override void Awake() { shootSpeedModifier = .85f; }
}
