public class AMoonItem : ACompanionItem
{

    public AMoonItem()
    {
    }

    private void Awake()
    {
        companionName = "AMoon";
        SetData("A Moon", "It orbits!", "A moon orbits around you blocking enemy projectiles.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        base.OnPickup();
    }
}
