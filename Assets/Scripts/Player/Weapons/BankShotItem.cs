using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class BankShotItem : AWeaponItem
{
    void Awake()
    {
        weaponName = nameof(BankShot);
        SetData("Bank Shot", "Weapon", "Diagonal rounds arm on their first wall bounce.", ItemType.weapon);
    }
    public override void OnBegin() { }
}

public sealed class BankShot : AWeaponAutomatic
{
    void Awake() => shootSpeedModifier = .34f;

    public override void Shoot()
    {
        if (!KeepFiring()) return;
        float charge = CombatUpgrades.Current?.Volley() ?? 1;
        var start = transform.position + Vector3.up * .65f;
        Fire(start, -1, charge);
        Fire(start, 1, charge);
        SoundManager.PlaySound("shotNeedle", .07f);
        GetComponentInParent<PSXShipEffects>()?.Shot();
    }

    void Fire(Vector3 start, int side, float charge)
    {
        var shot = PlayerRound.Spawn(start, new Vector2(side * .82f, .57f), GameRules.playerShootDamage * 6f * charge,
            GameRules.playerShootTravelSpeed * 1.15f, PlayerDelivery.Needle);
        shot?.RequireBank();
        MagnifyingLens.Enlarge(ChargedShotCue.Apply(shot, charge));
    }
}
