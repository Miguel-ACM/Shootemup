using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BurstLauncher : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Shell;
    // Shells and shrapnel grow with damage.
    protected override WeaponAffinity Affinity => new WeaponAffinity(.85f, 1.3f, 1.1f);
    protected override float Power => 3f;
    protected override float Velocity => 1f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .18f;
    }
}
