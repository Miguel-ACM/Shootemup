public class ThreeMoonsItem : ACompanionItem
{

    public ThreeMoonsItem()
    {
    }
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        companionName = "AMoon";
        SetData("Three Moons", "Feeling a bit Saturn", "Three moons orbit around you blocking enemy projectiles.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.AddCompanion(companionName, numItems: 2);
        base.OnPickup();
    }
}
