public class BabyYouItem : ACompanionItem
{

    public BabyYouItem()
    {
    }
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        companionName = "BabyYou";
        SetData("Baby you", "She can't walk yet", "A mini version of yourself follows you. It inherits most of your stats.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        base.OnPickup();
    }
}
