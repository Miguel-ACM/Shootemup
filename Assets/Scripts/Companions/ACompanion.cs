using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ACompanion : MonoBehaviour
{
    public enum CompanionType
    {
        follower,
        sides,
        orbital
    }

    protected CompanionType type;
    protected bool blocksBullets = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (blocksBullets && other.tag == "EnemyBullet")
        {
            var round = other.GetComponent<CombatRound>();
            if (round != null && !round.PixelOverlaps(GetComponent<Collider2D>()))
                return;
            AEnemyBullet bullet = other.GetComponent<AEnemyBullet>();
            if (bullet is PulseLaser)
                return;
            if (CombatUpgrades.Current != null)
                CombatUpgrades.Current.Block();
            bullet.OnTrigger();
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!blocksBullets)
            return;
        var round = other.GetComponent<CombatRound>();
        if (round != null && round.UsesPixelContact)
            OnTriggerEnter2D(other);
    }

    public CompanionType GetCompanionType()
    {
        return type;
    }
}
