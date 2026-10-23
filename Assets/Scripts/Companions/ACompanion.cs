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
            AEnemyBullet bullet = other.GetComponent<AEnemyBullet>();
            if(bullet is PulseLaser)return;
            if(CombatUpgrades.Current!=null)CombatUpgrades.Current.Block();
            bullet.OnTrigger();
        }
    }

    public CompanionType GetCompanionType()
    {
        return type;
    }
}
