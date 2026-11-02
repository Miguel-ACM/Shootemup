[UnityEngine.Scripting.Preserve]
public sealed class LuckyPenny : AItem
{
    void Awake() { SetData("Lucky Penny", "Heads up", "+0.05 luck.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup() { GameRules.luck += .05f; base.OnPickup(); }
}
