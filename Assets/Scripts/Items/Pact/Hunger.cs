// Faster and stronger, as long as the killing keeps up.
[UnityEngine.Scripting.Preserve]
public sealed class Hunger : APactItem
{
    void Awake()
    {
        SetPact("Hunger", "+30% speed. +50% damage.", "Every " + HungerPang.Waves + " waves, lose a hit unless you destroyed 2/3 of their enemies.");
    }

    protected override void Seal()
    {
        GameRules.playerMoveSpeed *= 1.3f;
        GameRules.playerShootDamage *= 1.5f;
        if (s.GetComponent<HungerPang>() == null)
            s.gameObject.AddComponent<HungerPang>();
    }
}
