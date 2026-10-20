public class TwerpItem : ACompanionItem
{
    private void Awake()
    {
        companionName = "Twerp";
        SetData("Twerp", "Annoying, but useful?", "Your new tiny companion. It's weak af.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }
}
