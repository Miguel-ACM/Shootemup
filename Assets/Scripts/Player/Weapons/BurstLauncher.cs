using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BurstLauncher : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Shell;
    protected override float Power => 3f;
    protected override float Velocity => 1f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .18f;
    }
}
