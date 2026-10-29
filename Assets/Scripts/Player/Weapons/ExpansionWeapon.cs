using UnityEngine;
public abstract class ExpansionWeapon : AWeaponAutomatic
{
    protected abstract PlayerDelivery Delivery
    {
        get;
    }

    protected virtual float Power => 1;
    protected virtual float Velocity => 1;
    protected virtual int Pellets => 1;
    protected virtual Vector2 FireDirection => Vector2.up;
    protected virtual void ShotFeedback()
    {
        SoundManager.PlaySound("shot1", .07f);
    }

    protected virtual void Awake()
    {
        shootSpeedModifier = .35f;
    }

    public override void Shoot()
    {
        if (!KeepFiring())
            return;
        float charge = CombatUpgrades.Current != null ? CombatUpgrades.Current.Volley() : 1;
        var effects = GetComponentInParent<PSXShipEffects>();
        if (effects != null)
            effects.Shot();
        ShotFeedback();
        for (int muzzle = 0; muzzle < spawnPoints.Length; muzzle++)
            for (int p = 0; p < Pellets; p++)
                MagnifyingLens.Enlarge(ChargedShotCue.Apply(PlayerRound.Spawn(transform.position + new Vector3(spawnPoints[muzzle].x, spawnPoints[muzzle].y * FireDirection.y), CombatVisuals.Rotate(FireDirection, (p - (Pellets - 1) * .5f) * 13),
                    GameRules.playerShootDamage * Power * charge, GameRules.playerShootTravelSpeed * Velocity, Delivery), charge));
    }
}
