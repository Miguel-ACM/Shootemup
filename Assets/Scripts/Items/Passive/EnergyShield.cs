using UnityEngine;

// Protects the next health cell to be lost, then recharges over a minute of combat.
[UnityEngine.Scripting.Preserve]
public sealed class EnergyShield : AItem
{
    void Awake()
    {
        SetData("Energy Shield", "First line", "Absorbs the next hit. Recharges in 1 minute.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        EnergyShieldCell.Install(s, UpgradeKind.EnergyShield);
        base.OnPickup();
    }
}

[UnityEngine.Scripting.Preserve]
public sealed class RechargingShield : AItem
{
    void Awake()
    {
        SetData("Recharging Shield", "Spare current", "Blocks one hit. Energy pickups at full energy recharge it.", ItemType.passive);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        EnergyShieldCell.Install(s, UpgradeKind.RechargingShield);
        base.OnPickup();
    }
}

public sealed class EnergyShieldCell : MonoBehaviour
{
    public const float RechargeSeconds = 60;
    const float ChargePerEnergy = .125f;
    static readonly Color Field = new Color(.45f, .85f, 1f);
    CombatUpgrades upgrades;

    public static void Install(Shipcontroller ship, UpgradeKind kind)
    {
        var upgrades = ship.GetComponent<CombatUpgrades>();
        upgrades.Add(kind);
        var cell = ship.GetComponent<EnergyShieldCell>() ?? ship.gameObject.AddComponent<EnergyShieldCell>();
        cell.upgrades = upgrades;
    }

    public float Charge
    {
        get; private set;
    } = 1;
    public bool Ready => Charge >= 1;
    // 1 the moment the shield breaks or comes back, fading quickly; the HUD flashes with it.
    public float Flash
    {
        get; private set;
    }

    public bool Absorb()
    {
        if (!isActiveAndEnabled || !Ready)
            return false;
        Charge = 0;
        Flash = 1;
        RunInventory.Ping(UpgradeKind.EnergyShield);
        RunInventory.Ping(UpgradeKind.RechargingShield);
        PSXFireballs.Shockwave(transform.position, 1.8f, .45f, Field, 2, .5f);
        PSXImpactEffects.Burst(transform.position, Field);
        ItemSound.Play("shatter", .22f);
        return true;
    }

    void Update()
    {
        Flash = Mathf.Max(0, Flash - Time.unscaledDeltaTime * 2.5f);
        // Only active play counts: shops, route choices, transitions and pauses do not recharge it.
        if (Ready || !GameClock.ActivePlay || upgrades == null || upgrades.Count(UpgradeKind.EnergyShield) == 0)
            return;
        Recharge(GameClock.ActivePlayDelta / RechargeSeconds, UpgradeKind.EnergyShield);
    }

    public float ConsumePickupOverflow(float overflow)
    {
        if (!isActiveAndEnabled || Ready || overflow <= 0 || upgrades == null || upgrades.Count(UpgradeKind.RechargingShield) == 0)
            return 0;
        float spent = Mathf.Min(overflow, (1 - Charge) / ChargePerEnergy);
        Recharge(spent * ChargePerEnergy, UpgradeKind.RechargingShield);
        return spent;
    }

    void Recharge(float fraction, UpgradeKind source)
    {
        if (fraction <= 0) return;
        Charge = Mathf.Min(1, Charge + fraction);
        if (Ready || source == UpgradeKind.RechargingShield)
            RunInventory.Ping(source);
        if (Ready)
        {
            Flash = 1;
            PSXFireballs.Shockwave(transform.position, 1.2f, .35f, Field, 1, .6f);
            ItemSound.Play("ready", .18f);
        }
    }
}
