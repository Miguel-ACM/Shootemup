public class BlockerOrbital : ACompanion
{
    void OnEnable()
    {
        ProjectileCollisionWindow.Register(GetComponent<UnityEngine.Collider2D>());
    }

    void OnDisable()
    {
        ProjectileCollisionWindow.Unregister(GetComponent<UnityEngine.Collider2D>());
    }

    public BlockerOrbital()
    {
        type = ACompanion.CompanionType.orbital;
        blocksBullets = true;
    }
}
