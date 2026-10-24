public class Telekinesis : AItem
{
    private void Awake()
    {
        SetData("Telekinesis", "The spoon is acting wonky", "Homing shots.", AItem.ItemType.passive);
    }

    public override void OnBegin()
    {

    }

    public override void OnPickup()
    {
        GameRules.playerShootFollowness += 1f;
        base.OnPickup();
    }
}
