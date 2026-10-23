using UnityEngine;
public abstract class ExpansionWeapon : AWeaponAutomatic
{
    protected abstract PlayerDelivery Delivery {get;}
    protected virtual float Power=>1;
    protected virtual float Velocity=>1;
    protected virtual int Pellets=>1;
    protected virtual void Awake(){shootSpeedModifier=.35f;}
    public override void Shoot()
    {
        if(!KeepFiring())return;
        float charge=CombatUpgrades.Current!=null ? CombatUpgrades.Current.Volley() : 1;
        var effects=GetComponentInParent<PSXShipEffects>();if(effects!=null)effects.Shot();
        SoundManager.PlaySound("shot1",.07f);
        for(int muzzle=0;muzzle<spawnPoints.Length;muzzle++)for(int p=0;p<Pellets;p++)
            PlayerRound.Spawn(transform.position+spawnPoints[muzzle],CombatVisuals.Rotate(Vector2.up,(p-(Pellets-1)*.5f)*13),
                GameRules.playerShootDamage*Power*charge,GameRules.playerShootTravelSpeed*Velocity,Delivery);
    }
}
