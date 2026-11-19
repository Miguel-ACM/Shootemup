// Every shot hits harder and wider, but the hull shatters at the first touch.
[UnityEngine.Scripting.Preserve]
public sealed class GlassHull : APactItem
{
    void Awake()
    {
        SetPact("Glass Hull", "+2 shots. +15% damage.", "One hit kills you.");
    }

    protected override void Seal()
    {
        GameRules.playerShootNumber += 2;
        GameRules.playerShootDamage *= 1.15f;
        s.updateBulletSpawnPoints();
        PactPool.BindGlassHull();
        s.SetMaxHits(1);
    }
}
