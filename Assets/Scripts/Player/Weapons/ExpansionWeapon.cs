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
        // Rocket salvos fire often: their launch sits lower in the mix (Miguel, 15 November 2026).
        SoundManager.PlaySound(cue, Delivery == PlayerDelivery.Missile ? .04f : .07f);
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
        float damage = GameRules.playerShootDamage * Power * charge * DamageMultiplier * Affinity.DamageFactor();
        for (int muzzle = 0; muzzle < spawnPoints.Length; muzzle++)
            for (int p = 0; p < Pellets; p++)
            {
                var round = PlayerRound.Spawn(transform.position + MuzzleOffset(spawnPoints[muzzle], aim), CombatVisuals.Rotate(aim, (p - (Pellets - 1) * .5f) * PelletSpacing),
                    damage, GameRules.playerShootTravelSpeed * Velocity, Delivery);
                Launched(round, muzzle, p);
                MagnifyingLens.Enlarge(ChargedShotCue.Apply(round, charge));
            }
    }

    // Lets a gun dress its own rounds (rockets ignite and splash) before the shared cues apply.
    protected virtual void Launched(PlayerRound round, int muzzle, int pellet) { }
}
