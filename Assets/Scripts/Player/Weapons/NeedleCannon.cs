using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class NeedleCannon : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Needle;
    // A rate gun: fire-speed upgrades pay off most (Miguel, 14 November 2026).
    protected override WeaponAffinity Affinity => new WeaponAffinity(1.35f, .85f, 1);
    protected override float Power => 1.7f;
    protected override float Velocity => 1.3f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .65f;
    }
}
