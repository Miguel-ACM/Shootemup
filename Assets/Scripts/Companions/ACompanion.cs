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

    // Start is called before the first frame update
    void Start()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (blocksBullets && other.tag == "EnemyBullet")
        {
            AEnemyBullet bullet = other.GetComponent<AEnemyBullet>();
            bullet.OnTrigger();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public CompanionType GetCompanionType()
    {
        return type;
    }

}
