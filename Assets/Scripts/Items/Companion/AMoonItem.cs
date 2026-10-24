public class AMoonItem : ACompanionItem
{
    private void Awake()
    {
        companionName = "AMoon";
        SetData("A Moon", "It orbits!", "Orbiting bullet shield.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }
}
