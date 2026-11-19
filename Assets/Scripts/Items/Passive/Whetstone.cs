// A repeatable filler: offered only once every other item has been taken or passed over.
[UnityEngine.Scripting.Preserve]
public sealed class Whetstone : AItem
{
    void Awake() { SetData("Whetstone", "A little sharper", "+0.1 shot damage.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        GameRules.playerShootDamage += .1f * (s.GetComponent<OverdriveBoost>() != null ? 2 : 1);
        base.OnPickup();
    }
}
