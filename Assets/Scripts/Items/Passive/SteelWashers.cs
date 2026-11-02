[UnityEngine.Scripting.Preserve]
public sealed class SteelWashers : AItem
{
    void Awake() { SetData("Steel Washers", "Extra weight", "+0.25 shot damage.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        GameRules.playerShootDamage += .25f * (s.GetComponent<OverdriveBoost>() != null ? 2 : 1);
        base.OnPickup();
    }
}
