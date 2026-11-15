using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class DiscThrower : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Disc;
    // Discs hold a few units ahead and keep cutting whatever they touch (PlayerRound.Grind).
    protected override WeaponAffinity Affinity => new WeaponAffinity(.9f, 1.3f, .9f);
    protected override float Power => 2.4f;
    protected override float Velocity => 0.65f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .24f;
    }
}
