// Heavier rounds, paid for with a slower trigger.
[UnityEngine.Scripting.Preserve]
public sealed class BloodPrice : APactItem
{
    void Awake()
    {
        SetPact("Blood Price", "+60% damage.", "-30% fire rate.");
    }

    protected override void Seal()
    {
        GameRules.playerShootDamage *= 1.6f;
        GameRules.playerShootSpeed *= .7f;
    }
}
