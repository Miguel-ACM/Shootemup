public class Caffeine : AItem
{
    new void Start()
    {
        base.Start();
        //GoToPosition(new Vector3(0, 0, transform.position.z));
    }

    private void Awake()
    {
        SetData("Caffeine", "Everything feels faster now!", "Increases shoot speed, shoot travel speed and move speed.", AItem.ItemType.passive);
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
