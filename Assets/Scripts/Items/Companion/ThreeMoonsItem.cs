public class ThreeMoonsItem : ACompanionItem
{
    private void Awake()
    {
        companionName = "AMoon";
        SetData("Three Moons", "Feeling a bit Saturn", "Three orbiting bullet shields.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.AddCompanion(companionName, numItems: 2);
        base.OnPickup();
    }
}
