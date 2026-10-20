public class AngryMoonItem : ACompanionItem
{

    public AngryMoonItem()
    {
    }

    private void Awake()
    {
        companionName = "AngryMoon";
        SetData("Angry Moon", "This time it's personal", "A moon orbits around you blocking enemy projectiles. It also shoots at them. Angrily.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        base.OnPickup();
    }
}
