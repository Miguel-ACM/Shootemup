using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FlameProjector : ExpansionWeapon
{
    FlameWeaponFeedback feedback;
    protected override PlayerDelivery Delivery => PlayerDelivery.Flame;
    // More flames per second also stoke the burn.
    protected override WeaponAffinity Affinity => new WeaponAffinity(1.35f, .85f, 1);
    protected override float Power => .45f;
    protected override float Velocity => .65f;
    protected override int Pellets => 2;
    protected override void Awake()
    {
        shootSpeedModifier = 1.2f;
        feedback = gameObject.AddComponent<FlameWeaponFeedback>();
        feedback.Build(this, Vector2.up);
    }

    protected override void ShotFeedback()
    {
        feedback.Fire();
        if (!feedback.HasApprovedLoop)
            base.ShotFeedback();
    }
}
