[UnityEngine.Scripting.Preserve]
public sealed class HeatSink : AItem
{
    void Awake() { SetData("Heat Sink", "Cool running", "Active items use 10% less energy.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup() { GameRules.activeEnergyCost *= .9f; base.OnPickup(); }
}
