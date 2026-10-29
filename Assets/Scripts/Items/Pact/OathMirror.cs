using UnityEngine;

// Mirror Oath: a charged mirror reflects the first round that touches the hull, then recharges
// over active play only.
public sealed class OathMirror : MonoBehaviour
{
    public const int RechargeSeconds = 6;
    static readonly Color Silver = new Color(.86f, .9f, 1f);
    float charge = 1;
    public bool Ready => charge >= 1;

    /// <summary>Called from the ship's hit check; true when the round was reflected instead of hitting.</summary>
    public static bool Catches(GameObject ship, AEnemyBullet round)
    {
        return ship.TryGetComponent<OathMirror>(out var mirror) && mirror.Reflect(round);
    }

    bool Reflect(AEnemyBullet round)
    {
        if (!Ready || BulletReflection.Reflect(round, 1.5f) == null)
            return false;
        charge = 0;
        PSXFireballs.Shockwave(transform.position, 1.4f, .3f, Silver, 1, .5f);
        ItemSound.Play("shatter", .16f, 1.5f);
        return true;
    }

    void Update()
    {
        if (Ready || CombatDecision.Active || CloudTransition.Busy || GameRules.playerDisabled || GameOverState.Active)
            return;
        charge = Mathf.Min(1, charge + Time.deltaTime / RechargeSeconds);
        if (Ready)
        {
            PSXFireballs.Shockwave(transform.position, .9f, .25f, Silver, 1, .6f);
            ItemSound.Play("ready", .12f, 1.3f);
        }
    }
}
