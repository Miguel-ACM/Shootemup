public class Telekinesis : AItem
{
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        SetData("Telekinesis", "The spoon is acting wonky", "Your shots chases your enemies (to an extent).", AItem.ItemType.passive);
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
