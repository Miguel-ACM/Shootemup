using System.Collections;
using UnityEngine;

public enum ShipFrame
{
    GR07, Lancer, Ember, Warden
}

/// <summary>The selectable player ships: hull, hits, handling, primary gun and starting equipment.</summary>
public static class ShipFrames
{
    public sealed class Definition
    {
        public ShipFrame frame;
        public string model;            // Terra model name; null keeps the GR-07 hull
        public int hits;
        // Base handling is about 10% slower than the original 8 (Miguel, 9 November 2026).
        public float moveSpeed = 7.2f, shootDamage = 2;
        public int shootNumber = 2;
        public string primary = "BaseWeapon"; // null: no gun of its own
        public string[] starters = new string[0];
        public Vector2 exhaust = new Vector2(.1925f, -.18f);
        public float exhaustScale = .4f;
    }

    static readonly Definition[] definitions =
    {
        new Definition { frame = ShipFrame.GR07, hits = 5 },
        // A slower laser hull with a stronger beam and a barrel roll.
        new Definition { frame = ShipFrame.Lancer, model = "ShipLancer", hits = 4, moveSpeed = 6.1f, shootDamage = 4, shootNumber = 1, primary = "BeamEmitter", starters = new[] { "BarrelRoll" }, exhaust = new Vector2(.11f, -.40f), exhaustScale = .32f },
        // Glass cannon: three harder-hitting rounds that ignite, three hits.
        new Definition { frame = ShipFrame.Ember, model = "ShipEmber", hits = 3, moveSpeed = 8.3f, shootNumber = 3, shootDamage = 2.4f, starters = new[] { "Lighter" }, exhaust = new Vector2(.15f, -.30f), exhaustScale = .34f },
        // Unarmed carrier: two laser drones do the fighting.
        new Definition { frame = ShipFrame.Warden, model = "ShipWarden", hits = 6, primary = null, starters = new[] { "LaserDroneItem", "LaserDroneItem" }, exhaust = new Vector2(.17f, -.28f), exhaustScale = .36f },
    };

    public static Definition Get(ShipFrame frame) => definitions[(int)frame];
    public static Definition Current => Get(FlightTrials.Active?.frame ?? MetaProgress.Ship);
    public static int Count => definitions.Length;

    /// <summary>Handling and gun stats, applied over the run's defaults before any item.</summary>
    public static void ApplyStats(Definition ship)
    {
        GameRules.playerMoveSpeed = ship.moveSpeed;
        GameRules.playerShootNumber = ship.shootNumber;
        GameRules.playerShootDamage = ship.shootDamage;
    }

    /// <summary>Puts the frame's hull, hits and exhaust on the player; the hangar calls this again on every switch.</summary>
    public static void ApplyHull(Shipcontroller ship, Definition frame)
    {
        var visual = ship.GetComponent<PSXShipVisual>();
        if (visual != null)
            visual.UseHull(frame.model);
        var effects = ship.GetComponent<PSXShipEffects>();
        if (effects != null)
        {
            effects.exhaustOffset = frame.exhaust;
            effects.exhaustScale = frame.exhaustScale;
        }
    }

    public static void ApplyWeapons(ShipLoadout loadout, Definition frame)
    {
        loadout.SetPrimary(frame.primary);
        foreach (var weapon in loadout.GetComponentsInChildren<AWeapon>())
            weapon.updateSpawnPoints();
    }

    /// <summary>Starting equipment arrives through the normal pickup feedback, like the Exploding Shield.</summary>
    // Items bought in the hangar since the last run follow the frame's own.
    public static IEnumerator GrantStarters(Shipcontroller ship, Definition frame)
    {
        var kinds = new System.Collections.Generic.List<string>(frame.starters);
        if (FlightTrials.Active != null) kinds.AddRange(FlightTrials.Active.kit);
        else kinds.AddRange(MetaProgress.TakeLoadout());
        return GrantItems(ship, kinds);
    }

    internal static bool HasLoanedGun(string[] kit) =>
        System.Array.Exists(kit, key => ItemCatalog.TryGet(key, out var item) && typeof(AWeaponItem).IsAssignableFrom(item.ComponentType));

    internal static void SwapSimulatorKit(Shipcontroller ship, string[] previous, string[] next)
    {
        if (next == null) return;
        var inventory = RunInventory.For(ship);
        foreach (var key in previous)
        {
            var entry = System.Linq.Enumerable.LastOrDefault(inventory.Entries, item => item.key == key);
            if (entry == null || entry.kind == AItem.ItemType.weapon || entry.kind == AItem.ItemType.active) continue;
            inventory.Remove(entry);
        }
        var loadout = ship.GetComponent<ShipLoadout>();
        for (int slot = 0; slot < 2; slot++)
        {
            loadout.ClearWeapon(slot);
            loadout.EquipActive(null, slot);
        }
        if (!HasLoanedGun(next))
            loadout.SetPrimary(Current.primary);
        ship.StartCoroutine(GrantItems(ship, next));
    }

    static IEnumerator GrantItems(Shipcontroller ship, System.Collections.Generic.IEnumerable<string> kinds)
    {
        yield return null;
        yield return null;
        foreach (var kind in kinds)
        {
            ItemCatalog.TryGet(kind, out var definition);
            if (ship == null || definition == null)
                yield break;
            var go = Object.Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(100, 100, 1), Quaternion.identity);
            var item = definition.Attach(go);
            item.SetRarity(ItemPool.RarityOf(kind));
            yield return null;
            if (ship == null || item == null)
                continue;
            // Bought weapons and actives take a free slot, keeping the frame's own gun.
            int slot = ship.GetComponent<ShipLoadout>().SlotFor(item);
            item.GrantTo(ship, slot < 0 ? 1 : slot);
        }
    }

    /// <summary>A standalone copy of a frame's hull for display, posed like the player's hull.</summary>
    public static GameObject BuildDisplay(Shipcontroller template, Definition frame)
    {
        var visual = template.GetComponent<PSXShipVisual>();
        return visual != null ? visual.BuildDisplayHull(frame.model) : null;
    }
}
