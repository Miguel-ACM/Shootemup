using UnityEngine;

// Second Wind: every boss phase break refills energy and readies both actives at once.
[UnityEngine.Scripting.Preserve]
public sealed class SecondWind : AItem
{
    static readonly Color Gust = new Color(.75f, .95f, 1f);

    void Awake()
    {
        SetData("Second Wind", "Catch your breath", "Boss phase breaks refill energy and ready your actives.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.SecondWind);
        base.OnPickup();
    }

    public static void Breathe(Shipcontroller ship)
    {
        ship.RefillEnergy();
        var loadout = ship.GetComponent<ShipLoadout>();
        if (loadout != null)
            foreach (var active in loadout.Actives)
                if (active != null)
                    active.lastCD = active.cd;
        RunInventory.Ping(UpgradeKind.SecondWind);
        var p = ship.transform.position;
        PSXFireballs.Shockwave(p, 1.8f, .55f, Gust, 2, .7f);
        ItemProcVfx.Emit(ItemProcShape.Return, p, Gust, .6f);
        ItemSpark.Emit(p, Gust, 8, .1f);
        ItemSound.Cue("ready", volume: .14f, pitch: .9f);
    }
}
