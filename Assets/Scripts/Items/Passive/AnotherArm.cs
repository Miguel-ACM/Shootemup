public class AnotherArm : AItem
{
    new void Start()
    {
        base.Start();
    }

    private void Awake()
    {
        SetData("Another arm", "It might be useful?", "One more bullet per shot.", AItem.ItemType.passive);
    }

    public override void OnBegin()
    {

    }

    public override void OnPickup()
    {
        GameRules.playerShootNumber += 1;
        s.updateBulletSpawnPoints();
        base.OnPickup();
    }


}