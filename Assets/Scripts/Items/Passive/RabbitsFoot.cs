public class RabbitsFoot : AItem
{
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        SetData("Rabbit's Foot", "Why would you take this?", "Increase your luck.", AItem.ItemType.passive);
    }

    public override void OnBegin()
    {

    }

    public override void OnPickup()
    {
        GameRules.luck += 0.2f;
        base.OnPickup();
    }


}
