// A repeatable filler: offered only once every other item has been taken or passed over.
[UnityEngine.Scripting.Preserve]
public sealed class TailFin : AItem
{
    void Awake() { SetData("Tail Fin", "Straighter flight", "+1.2 shot speed.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        GameRules.playerShootTravelSpeed += 1.2f;
        base.OnPickup();
    }
}
