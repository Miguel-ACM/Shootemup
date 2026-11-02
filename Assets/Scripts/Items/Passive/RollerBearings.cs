[UnityEngine.Scripting.Preserve]
public sealed class RollerBearings : AItem
{
    void Awake() { SetData("Roller Bearings", "Smooth sailing", "+0.48 movement speed.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup() { GameRules.playerMoveSpeed += .48f; base.OnPickup(); }
}
