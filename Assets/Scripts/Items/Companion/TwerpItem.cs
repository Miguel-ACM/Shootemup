public class TwerpItem : ACompanionItem
{
    private void Awake()
    {
        companionName = "Twerp";
        SetData("Twerp", "Annoying, but useful?", "Small shooting companion.", AItem.ItemType.partner);
    }

    public override void OnBegin()
    {
    }
}
