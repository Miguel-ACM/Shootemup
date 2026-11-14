using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ScatterCannon : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Pulse;
    // A count gun: every extra shot adds a whole fan.
    protected override WeaponAffinity Affinity => new WeaponAffinity(.9f, 1, 1.3f);
    protected override float Power => .8f;
    protected override float Velocity => 1f;
    protected override int Pellets => 5;
    protected override float PelletSpacing => 10;
    protected override void Awake()
    {
        shootSpeedModifier = .4f;
    }
}
