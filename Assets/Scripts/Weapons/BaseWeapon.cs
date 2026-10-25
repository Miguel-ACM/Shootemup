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
        if (!KeepFiring())
            return;

        SoundManager.PlaySound("shot1", 0.1f);
        var effects = GetComponent<PSXShipEffects>();
        if (effects != null) effects.Shot();
        float charge=CombatUpgrades.Current!=null ? CombatUpgrades.Current.Volley() : 1;
        for (int i = 0; i < GameRules.playerShootNumber; i++)
        {
            Vector3 spawn = effects != null && GameRules.playerShootNumber == 2
                ? effects.MuzzlePosition(i) : spawnPoints[i] + transform.position;
            var b=PlayerRound.Spawn(spawn,Vector2.up,GameRules.playerShootDamage*charge,GameRules.playerShootTravelSpeed);
            MagnifyingLens.Enlarge(b);
            ChargedShotCue.Apply(b,charge);

        }
    }


}
