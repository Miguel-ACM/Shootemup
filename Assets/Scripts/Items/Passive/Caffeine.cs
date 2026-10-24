public class Caffeine : AItem
{
    private void Awake()
    {
        SetData("Caffeine", "Everything feels faster now!", "Faster movement, firing and bullets.", AItem.ItemType.passive);
    }

    public override void OnBegin()
    {

    }

    public override void OnPickup()
    {
        GameRules.playerShootSpeed += 5f;
        GameRules.playerShootTravelSpeed += 8f;
        GameRules.playerMoveSpeed += 4f;
        base.OnPickup();
    }
}
