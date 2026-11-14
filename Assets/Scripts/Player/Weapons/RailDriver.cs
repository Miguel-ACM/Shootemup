using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class RailDriver : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Needle;
    // A damage gun: heavy slugs grow most from damage, least from cadence and extra barrels.
    protected override WeaponAffinity Affinity => new WeaponAffinity(.75f, 1.45f, .7f);
    protected override float Power => 11f;
    protected override float Velocity => 2f;
    protected override int Pellets => 1;
    protected override void Awake()
    {
        shootSpeedModifier = .12f;
    }
}
