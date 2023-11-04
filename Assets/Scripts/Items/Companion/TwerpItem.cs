public class TwerpItem : ACompanionItem
{

    public TwerpItem()
    {
    }
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        companionName = "Twerp";
        SetData("Twerp", "Annoying, but useful?", "Your new tiny companion. It's weak af.", AItem.ItemType.partner);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        base.OnPickup();
    }
}
