[UnityEngine.Scripting.Preserve]
public sealed class ClockworkSpring : AItem
{
    void Awake() { SetData("Clockwork Spring", "Another turn", "+0.8 shots per second.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup() { GameRules.playerShootSpeed += .8f; base.OnPickup(); }
}
