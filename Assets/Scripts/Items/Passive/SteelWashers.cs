[UnityEngine.Scripting.Preserve]
public sealed class SteelWashers : AItem
{
    void Awake() { SetData("Steel Washers", "Extra weight", "+0.2 shot damage.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        GameRules.playerShootDamage += .2f * (s.GetComponent<OverdriveBoost>() != null ? 2 : 1);
        base.OnPickup();
    }
}
