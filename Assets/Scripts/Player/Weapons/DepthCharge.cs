using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class DepthCharge : ExpansionWeapon
{
    protected override PlayerDelivery Delivery => PlayerDelivery.Shell;
    protected override Vector2 FireDirection => Vector2.down;
    protected override float Power => 3.2f;
    protected override float Velocity => .55f;
    protected override void Awake()
    {
        shootSpeedModifier = .18f;
    }

    protected override void ShotFeedback()
    {
        ItemProcVfx.Emit(ItemProcShape.Counter, transform.position + Vector3.down * .7f, new Color(.65f, .7f, .6f), .25f, Vector2.down);
        if (ItemSound.Clip("itemDepthLaunch") != null)
            ItemSound.Cue("itemDepthLaunch", volume: .1f);
        else
            base.ShotFeedback();
    }
}
