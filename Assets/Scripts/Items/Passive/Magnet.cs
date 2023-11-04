public class Magnet : AItem
{
    new void Start()
    {
        base.Start();
        //GoToPosition(new Vector3(0, 0, transform.position.z));
    }

    private void Awake()
    {
        SetData("Magnet", "How does it work, right? HAHA", "Dramatically increases pick up range.", AItem.ItemType.passive);
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
