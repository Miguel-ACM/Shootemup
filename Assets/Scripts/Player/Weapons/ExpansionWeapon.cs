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
    protected virtual float PelletSpacing => 13;
    protected virtual Vector2 FireDirection => Vector2.up;
    protected virtual Vector3 MuzzleOffset(Vector3 offset, Vector2 aim) => new Vector3(offset.x, offset.y * aim.y);
    protected virtual void ShotFeedback()
    {
        string cue = this is RailDriver ? "shotRail" : this is ScatterCannon ? "shotScatter" :
            Delivery == PlayerDelivery.Needle ? "shotNeedle" : Delivery == PlayerDelivery.Shell ? "shotShell" :
            Delivery == PlayerDelivery.Disc ? "shotDisc" : Delivery == PlayerDelivery.Missile ? "shotMissile" :
            Delivery == PlayerDelivery.Flame ? "shotFlame" : "shot1";
        SoundManager.PlaySound(cue, .07f);
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
        var aim = FireDirection;
        for (int muzzle = 0; muzzle < spawnPoints.Length; muzzle++)
            for (int p = 0; p < Pellets; p++)
                MagnifyingLens.Enlarge(ChargedShotCue.Apply(PlayerRound.Spawn(transform.position + MuzzleOffset(spawnPoints[muzzle], aim), CombatVisuals.Rotate(aim, (p - (Pellets - 1) * .5f) * PelletSpacing),
                    GameRules.playerShootDamage * Power * charge * DamageMultiplier, GameRules.playerShootTravelSpeed * Velocity, Delivery), charge));
    }
}
