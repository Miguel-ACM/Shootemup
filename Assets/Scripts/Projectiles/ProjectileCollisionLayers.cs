using UnityEngine;

// Hostile ordnance never needs contacts against its own fleet or other hostile rounds.
public static class ProjectileCollisionLayers
{
    public const int Hostile = 9, Destructible = 10;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Configure()
    {
        for (int layer = 0; layer < 32; layer++)
        {
            Physics2D.IgnoreLayerCollision(Hostile, layer, layer != 0 && layer != 7);
            Physics2D.IgnoreLayerCollision(Destructible, layer, layer != 0 && layer != 3 && layer != 7);
        }
    }
}
