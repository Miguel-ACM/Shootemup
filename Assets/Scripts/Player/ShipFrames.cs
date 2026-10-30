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
        public float moveSpeed = 8, shootDamage = 2;
        public int shootNumber = 2;
        public string primary = "BaseWeapon"; // null: no gun of its own
        public string[] starters = new string[0];
        public Vector2 exhaust = new Vector2(.1925f, -.18f);
        public float exhaustScale = .4f;
        public Color cockpit = new Color(1f, .82f, .3f);
        public int cockpitSides = 12;
    }

    static readonly Definition[] definitions =
    {
        new Definition { frame = ShipFrame.GR07, hits = 6, cockpitSides = 6 },
        // Long and slow: a continuous laser instead of the twin cannon, more hull and a barrel roll.
        new Definition { frame = ShipFrame.Lancer, model = "ShipLancer", hits = 5, moveSpeed = 6.8f, primary = "BeamEmitter", starters = new[] { "BarrelRoll" }, cockpit = new Color(.85f, .45f, 1f), exhaust = new Vector2(.11f, -.40f), exhaustScale = .32f },
        // Glass cannon: three harder-hitting rounds that ignite, three hits.
        new Definition { frame = ShipFrame.Ember, model = "ShipEmber", hits = 3, moveSpeed = 9.2f, shootNumber = 3, shootDamage = 2.4f, starters = new[] { "Lighter" }, cockpit = new Color(.4f, 1f, .45f), exhaust = new Vector2(.15f, -.30f), exhaustScale = .34f },
        // Unarmed carrier: two laser drones do the fighting.
        new Definition { frame = ShipFrame.Warden, model = "ShipWarden", hits = 6, primary = null, starters = new[] { "LaserDroneItem", "LaserDroneItem" }, cockpit = new Color(.35f, .7f, 1f), exhaust = new Vector2(.17f, -.28f), exhaustScale = .36f },
    };

    public static Definition Get(ShipFrame frame) => definitions[(int)frame];
    public static Definition Current => Get(MetaProgress.Ship);
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
        {
            visual.UseHull(frame.model);
            visual.UseCockpit(frame.cockpit, frame.cockpitSides);
        }
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
        kinds.AddRange(MetaProgress.TakeLoadout());
        yield return null;
        yield return null;
        foreach (var kind in kinds)
        {
            var type = System.Type.GetType(kind);
            if (ship == null || type == null)
                yield break;
            var go = Object.Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(100, 100, 1), Quaternion.identity);
            var item = (AItem)go.AddComponent(type);
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
        return visual != null ? visual.BuildDisplayHull(frame.model, frame.cockpit, frame.cockpitSides) : null;
    }
}
