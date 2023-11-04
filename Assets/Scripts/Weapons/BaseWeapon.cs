using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BaseWeapon : AWeaponAutomatic
{

    // Start is called before the first frame update
    [SerializeField] GameObject bullet;
    
    // Update is called once per frame


    public override void Shoot()
    {
        base.Shoot();

        SoundManager.PlaySound("shot1", 0.1f);
        for (int i = 0; i < GameRules.playerShootNumber; i++)
        {
            ABullet b = Instantiate(bullet, spawnPoints[i] + transform.position, Quaternion.identity).GetComponent<ABullet>();
            b.setProperties(new Dictionary<string, float> { { "speed", GameRules.playerShootTravelSpeed }, { "damage", GameRules.playerShootDamage }, { "size", GameRules.playerShootSize }, { "followness", GameRules.playerShootFollowness } });
        }
    }


}
