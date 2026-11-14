using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class VendingGunItem : AWeaponItem
{
    void Awake()
    {
        weaponName = nameof(VendingGun);
        SetData("Vending Gun", "Weapon", "One coin buys five rounds. Gun kills drop an extra coin.", ItemType.weapon);
    }
    public override void OnBegin() { }
}

public sealed class VendingGun : AWeaponAutomatic
{
    // Every volley costs a coin, so cadence helps least and damage most.
    protected override WeaponAffinity Affinity => new WeaponAffinity(.8f, 1.2f, .7f);
    void Awake() => shootSpeedModifier = .17f;

    public override void Shoot()
    {
        if (!KeepFiring()) return;
        if (!RunCoins.TrySpend(1))
        {
            ItemSound.Cue("powerdown", volume: .075f, cooldown: .5f);
            return;
        }
        float charge = CombatUpgrades.Current?.Volley() ?? 1;
        float damage = GameRules.playerShootDamage * 5f * charge * Affinity.DamageFactor(0);
        for (int i = -2; i <= 2; i++)
        {
            var shot = PlayerRound.Spawn(transform.position + Vector3.up * .7f, CombatVisuals.Rotate(Vector2.up, i * 8),
                damage, GameRules.playerShootTravelSpeed, PlayerDelivery.Needle);
            shot?.AwardVendingCoin();
            MagnifyingLens.Enlarge(ChargedShotCue.Apply(shot, charge));
        }
        GetComponentInParent<PSXShipEffects>()?.Shot();
        SoundManager.PlaySound("shotScatter", .08f);
    }
}
