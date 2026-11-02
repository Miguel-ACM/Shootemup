using System;
using System.Collections.Generic;
using UnityEngine;

// Owns individual pickups, including duplicates. Trading reverses that copy's actual
// contribution, preserving later additive items, pact multipliers and shared effects.
public sealed class RunInventory : MonoBehaviour
{
    public sealed class Entry
    {
        public string key, name, rarity;
        public Transform display;
        public bool tradeable, multiply;
        internal float[] changes;
        internal readonly List<UpgradeKind> upgrades = new List<UpgradeKind>();
        internal readonly List<MonoBehaviour> effects = new List<MonoBehaviour>();
        internal readonly List<AWeapon> guns = new List<AWeapon>();
    }

    public sealed class Pickup
    {
        internal float[] stats;
        internal int[] upgrades;
        internal MonoBehaviour[] effects;
        internal AWeapon[] guns;
    }

    readonly List<Entry> entries = new List<Entry>();
    static readonly UpgradeKind[] upgradeKinds = (UpgradeKind[])Enum.GetValues(typeof(UpgradeKind));
    Shipcontroller ship;
    public IReadOnlyList<Entry> Entries => entries;
    public static RunInventory For(Shipcontroller ship)
    {
        var inventory = ship.GetComponent<RunInventory>();
        if (inventory == null)
            inventory = ship.gameObject.AddComponent<RunInventory>();
        inventory.ship = ship;
        return inventory;
    }

    float DamageScale => ship.GetComponent<OverdriveBoost>() != null ? 2 : 1;
    float[] ReadStats() => new[] { GameRules.playerShootDamage / DamageScale, GameRules.playerShootSpeed,
        GameRules.playerShootTravelSpeed, GameRules.playerShootSize, GameRules.playerShootNumber,
        GameRules.playerMoveSpeed, GameRules.playerShootFollowness, GameRules.pickDistance,
        GameRules.luck, GameRules.manaDropMultiplier, ship.slowMultiplier };
    void WriteStats(float[] values)
    {
        GameRules.playerShootDamage = values[0] * DamageScale;
        GameRules.playerShootSpeed = values[1];
        GameRules.playerShootTravelSpeed = values[2];
        GameRules.playerShootSize = values[3];
        GameRules.playerShootNumber = Mathf.RoundToInt(values[4]);
        GameRules.playerMoveSpeed = values[5];
        GameRules.playerShootFollowness = values[6];
        GameRules.pickDistance = values[7];
        GameRules.luck = values[8];
        GameRules.manaDropMultiplier = values[9];
        ship.slowMultiplier = values[10];
        ship.updateBulletSpawnPoints();
    }

    public Pickup Capture()
    {
        var upgrades = ship.GetComponent<CombatUpgrades>();
        var counts = new int[upgradeKinds.Length];
        for (int i = 0; i < counts.Length; i++)
            counts[i] = upgrades.Count(upgradeKinds[i]);
        return new Pickup { stats = ReadStats(), upgrades = counts,
            effects = ship.GetComponents<MonoBehaviour>(), guns = ship.GetComponentsInChildren<AWeapon>() };
    }

    public void Record(AItem item, Pickup before, Transform display)
    {
        if (before == null || !ItemCatalog.TryGet(item.GetType().Name, out _))
            return;
        var data = item.GetData();
        var entry = new Entry { key = item.GetType().Name, name = data[0], rarity = item.rarity, display = display,
            multiply = item is APactItem || item is EarlyRise || item is FocusTime,
            tradeable = (item.Kind == AItem.ItemType.passive || item.Kind == AItem.ItemType.passiveWeapon) &&
                !(item is APactItem) && !(item is ExplodingShield) && !(item is LargeHealthKit) &&
                !(item is SparePlating) && !(item is DoubleHull) && !(item is ShrinkerJuice) &&
                !(item is WarpCore) && !(item is RecoveryCoil) };
        var after = ReadStats();
        entry.changes = new float[after.Length];
        for (int i = 0; i < after.Length; i++)
            entry.changes[i] = entry.multiply ? (Mathf.Abs(before.stats[i]) > .000001f ? after[i] / before.stats[i] : 1) : after[i] - before.stats[i];
        var upgrades = ship.GetComponent<CombatUpgrades>();
        for (int i = 0; i < upgradeKinds.Length; i++)
            for (int n = before.upgrades[i]; n < upgrades.Count(upgradeKinds[i]); n++)
                entry.upgrades.Add(upgradeKinds[i]);
        foreach (var effect in ship.GetComponents<MonoBehaviour>())
            if (Array.IndexOf(before.effects, effect) < 0 && !(effect is HealthRecovery))
                entry.effects.Add(effect);
        foreach (var gun in ship.GetComponentsInChildren<AWeapon>())
            if (Array.IndexOf(before.guns, gun) < 0)
                entry.guns.Add(gun);
        entries.Add(entry);
    }

    public List<Entry> Tradeable()
    {
        return entries.FindAll(entry => entry.tradeable);
    }
    public bool Contains(Entry entry) => entry != null && entries.Contains(entry);

    public bool Remove(Entry entry)
    {
        int index = entries.IndexOf(entry);
        if (index < 0 || !entry.tradeable)
            return false;
        var successor = entries.Find(other => other != entry && other.key == entry.key);
        // Focus Brake applies its handling change once, even with several copies.
        bool transferStats = entry.key == nameof(FocusTime) && successor != null;
        var stats = ReadStats();
        for (int s = 0; s < stats.Length; s++)
        {
            if (transferStats)
            {
                successor.changes[s] *= entry.changes[s];
                continue;
            }
            float factor = 1, tail = 0;
            for (int n = index + 1; n < entries.Count; n++)
            {
                var later = entries[n];
                if (later.multiply)
                {
                    factor *= later.changes[s];
                    tail *= later.changes[s];
                }
                else
                    tail += later.changes[s];
            }
            stats[s] = entry.multiply ? (stats[s] - tail) / Mathf.Max(.000001f, entry.changes[s]) + tail : stats[s] - entry.changes[s] * factor;
        }
        WriteStats(stats);
        var upgrades = ship.GetComponent<CombatUpgrades>();
        foreach (var kind in entry.upgrades)
            upgrades.Remove(kind);
        foreach (var effect in entry.effects)
        {
            if (effect == null)
                continue;
            Entry keeper = successor;
            if (effect is GlassesFieldCue)
                keeper = entries.Find(other => other != entry && (other.key == nameof(BadGlasses) || other.key == nameof(QualityGlasses)));
            if (keeper != null && !(effect is SupportDrone))
                keeper.effects.Add(effect);
            else
            {
                effect.enabled = false;
                Destroy(effect);
            }
        }
        foreach (var gun in entry.guns)
            if (gun != null)
            {
                gun.StopFiring();
                gun.enabled = false;
                Destroy(gun.gameObject);
            }
        PSXInventoryDisplay.Remove(entry.display);
        entries.RemoveAt(index);
        return true;
    }
}
