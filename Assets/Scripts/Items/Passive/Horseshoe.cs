// A repeatable filler: offered only once every other item has been taken or passed over.
[UnityEngine.Scripting.Preserve]
public sealed class Horseshoe : AItem
{
    void Awake() { SetData("Horseshoe", "Hung the right way up", "+0.03 luck.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        GameRules.luck += .03f;
        base.OnPickup();
    }
}
