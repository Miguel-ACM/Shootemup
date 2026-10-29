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
        public Vector2 exhaust = new Vector2(.2016f, -.245f);
        public float exhaustScale = 1;
    }

    static readonly Definition[] definitions =
    {
        new Definition { frame = ShipFrame.GR07, model = "ShipGR07", hits = 6 },
        // Long and slow: a continuous laser instead of the twin cannon, more hull and a barrel roll.
        new Definition { frame = ShipFrame.Lancer, model = "ShipLancer", hits = 5, moveSpeed = 6.8f, primary = "BeamEmitter", starters = new[] { "BarrelRoll" }, exhaust = new Vector2(.11f, -.40f), exhaustScale = .8f },
        // Glass cannon: three harder-hitting rounds that ignite, three hits.
        new Definition { frame = ShipFrame.Ember, model = "ShipEmber", hits = 3, moveSpeed = 9.2f, shootNumber = 3, shootDamage = 2.4f, starters = new[] { "Lighter" }, exhaust = new Vector2(.15f, -.30f), exhaustScale = .85f },
        // Unarmed carrier: two laser drones do the fighting.
        new Definition { frame = ShipFrame.Warden, model = "ShipWarden", hits = 6, primary = null, starters = new[] { "LaserDroneItem", "LaserDroneItem" }, exhaust = new Vector2(.17f, -.28f), exhaustScale = .9f },
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
    public static IEnumerator GrantStarters(Shipcontroller ship, Definition frame)
    {
        yield return null;
        yield return null;
        foreach (var kind in frame.starters)
        {
            var type = System.Type.GetType(kind);
            if (ship == null || type == null)
                yield break;
            var go = Object.Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(100, 100, 1), Quaternion.identity);
            var item = (AItem)go.AddComponent(type);
            item.SetRarity(ItemPool.RarityOf(kind));
            yield return null;
            if (ship != null && item != null)
                item.GrantTo(ship);
        }
    }

    /// <summary>A standalone copy of a frame's hull for display, posed like the player's hull.</summary>
    public static GameObject BuildDisplay(Shipcontroller template, Definition frame)
    {
        var visual = template.GetComponent<PSXShipVisual>();
        return visual != null ? visual.BuildDisplayHull(frame.model) : null;
    }
}
