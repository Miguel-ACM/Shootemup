using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AK45 : AWeaponAutomatic
{
    [SerializeField] GameObject bullet;
    
    public override void Shoot()
    {
        base.Shoot();

        SoundManager.PlaySound("shot1", 0.1f);
        for (int i = 0; i < GameRules.playerShootNumber; i++)
        {
            ABullet b = Instantiate(bullet, transform.position, Quaternion.identity).GetComponent<ABullet>();
            b.setProperties(new Dictionary<string, float> { 
                { "speed", GameRules.playerShootTravelSpeed },
                { "damage", GameRules.playerShootDamage },
                { "size", GameRules.playerShootSize },
                { "followness", GameRules.playerShootFollowness },
                { "directionX", Mathf.Cos(45 * Mathf.Deg2Rad) },
                { "directionY", Mathf.Sin(45 * Mathf.Deg2Rad) }
            });

            ABullet b2 = Instantiate(bullet, transform.position, Quaternion.identity).GetComponent<ABullet>();
            b2.setProperties(new Dictionary<string, float> {
                { "speed", GameRules.playerShootTravelSpeed },
                { "damage", GameRules.playerShootDamage },
                { "size", GameRules.playerShootSize },
                { "followness", GameRules.playerShootFollowness },
                { "directionX", -Mathf.Cos(45 * Mathf.Deg2Rad) },
                { "directionY", Mathf.Sin(45 * Mathf.Deg2Rad) }
            });
        }
    }


}
