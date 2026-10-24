public class BabyYouItem : ACompanionItem
{
    private void Awake()
    {
        companionName = "BabyYou";
        SetData("Baby you", "She can't walk yet", "A small copy of your ship.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }
}
