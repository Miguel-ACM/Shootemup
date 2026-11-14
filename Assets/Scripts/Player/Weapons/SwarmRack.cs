using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SwarmRack : ExpansionWeapon
{
    // Rockets (Miguel, 14 November 2026): a quicker rack of bigger rockets that ignite after launch,
    // seek, and burst on impact. A count gun: every extra shot adds another pair to the salvo.
    protected override WeaponAffinity Affinity => new WeaponAffinity(1.15f, .9f, 1.4f);
    protected override PlayerDelivery Delivery => PlayerDelivery.Missile;
    protected override float Power => 1.45f;
    protected override float Velocity => .8f;
    protected override int Pellets => 2;
    protected override float PelletSpacing => 24;
    protected override void Awake()
    {
        shootSpeedModifier = .42f;
    }

    protected override void ShotFeedback()
    {
        base.ShotFeedback();
        foreach (var muzzle in spawnPoints)
            ItemSpark.Emit(transform.position + muzzle, new Color(1f, .7f, .32f), 2, .08f, true);
    }
}
