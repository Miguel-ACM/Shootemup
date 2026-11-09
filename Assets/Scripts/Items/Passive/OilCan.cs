// A repeatable filler: offered only once every other item has been taken or passed over.
[UnityEngine.Scripting.Preserve]
public sealed class OilCan : AItem
{
    void Awake() { SetData("Oil Can", "Smoother action", "+0.4 shots per second.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        GameRules.playerShootSpeed += .4f;
        base.OnPickup();
    }
}
