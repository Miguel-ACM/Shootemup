using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Electricity : AWeaponAutomatic
{

    // Start is called before the first frame update
    [SerializeField] GameObject bullet;
    // Every arc chains through the whole column, so extra arcs and cadence matter most.
    protected override WeaponAffinity Affinity => new WeaponAffinity(1.2f, .9f, 1.2f);

    public override void Shoot()
    {
        if (!KeepFiring())
            return;

        SoundManager.PlaySound("shotElectric", 0.1f);
        float damage = GameRules.playerShootDamage * .75f * Affinity.DamageFactor();
        for (int i = 0; i < GameRules.playerShootNumber; i++)
        {
            ABullet b = Instantiate(bullet, spawnPoints[i] + transform.position, Quaternion.identity).GetComponent<ABullet>();
            b.setProperties(new Dictionary<string, float> { { "speed", GameRules.playerShootTravelSpeed }, { "damage", damage }, { "size", GameRules.playerShootSize }, { "followness", GameRules.playerShootFollowness } });
        }
    }
}
