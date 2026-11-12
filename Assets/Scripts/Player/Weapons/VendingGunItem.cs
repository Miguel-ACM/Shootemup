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
        for (int i = -2; i <= 2; i++)
        {
            var shot = PlayerRound.Spawn(transform.position + Vector3.up * .7f, CombatVisuals.Rotate(Vector2.up, i * 8),
                GameRules.playerShootDamage * 5f * charge, GameRules.playerShootTravelSpeed, PlayerDelivery.Needle);
            shot?.AwardVendingCoin();
            MagnifyingLens.Enlarge(ChargedShotCue.Apply(shot, charge));
        }
        GetComponentInParent<PSXShipEffects>()?.Shot();
        SoundManager.PlaySound("shotScatter", .08f);
    }
}
