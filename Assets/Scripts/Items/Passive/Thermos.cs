[UnityEngine.Scripting.Preserve]
public sealed class Thermos : AItem
{
    void Awake() { SetData("Thermos", "Keep it warm", "+20 maximum energy. Fills the added capacity.", ItemType.passive); }
    public override void OnBegin() { }
    public override void OnPickup()
    {
        s.SetMaxEnergy(s.maxMana + 20);
        s.HealManaFlat(20 / UnityEngine.Mathf.Max(.001f, GameRules.manaHealMultiplier));
        base.OnPickup();
    }
}
