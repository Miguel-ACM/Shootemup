public class Magnet : AItem
{
    private void Awake()
    {
        SetData("Magnet", "How does it work, right? HAHA", "Longer pickup range.", AItem.ItemType.passive);
    }

    public override void OnBegin()
    {

    }

    public override void OnPickup()
    {
        GameRules.pickDistance += 2f;
        base.OnPickup();
    }
}
