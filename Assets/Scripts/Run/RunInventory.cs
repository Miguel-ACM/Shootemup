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
        public AItem.ItemType kind;
        public int slot;
        internal AItem item;
        internal float[] changes;
        internal readonly List<ACompanion> companions = new List<ACompanion>();
        internal readonly List<ImpCompanion> imps = new List<ImpCompanion>();
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
        internal ACompanion[] companions;
        internal ImpCompanion[] imps;
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
        current = inventory;
        return inventory;
    }

    // Item pings (Miguel, 14 November 2026): whatever activates an item lights that item's icon in
    // the collected column. Each entry already knows the upgrades and ship effects its pickup added.
    static RunInventory current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCurrent() => current = null;

    public static void Ping(AItem item)
    {
        if (current == null || item == null)
            return;
        foreach (var entry in current.entries)
            if (entry.item == item)
                Light(entry);
    }

    public static void Ping(UpgradeKind kind)
    {
        if (current == null)
            return;
        foreach (var entry in current.entries)
            if (entry.upgrades.Contains(kind))
                Light(entry);
    }

    public static void Ping(MonoBehaviour effect)
    {
        if (current == null || effect == null)
            return;
        foreach (var entry in current.entries)
            if (entry.effects.Contains(effect))
                Light(entry);
    }

    static void Light(Entry entry) => PSXInventoryDisplay.Ping(entry.display, AItem.GetRarityColor(entry.rarity));

    float DamageScale => ship.GetComponent<OverdriveBoost>() != null ? 2 : 1;
    float[] ReadStats() => new[] { GameRules.playerShootDamage / DamageScale, GameRules.playerShootSpeed,
        GameRules.playerShootTravelSpeed, GameRules.playerShootSize, GameRules.playerShootNumber,
        GameRules.playerMoveSpeed, GameRules.playerShootFollowness, GameRules.pickDistance,
        GameRules.luck, GameRules.manaDropMultiplier, ship.slowMultiplier,
        GameRules.activeEnergyCost, ship.maxMana, ship.maxHealth };
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
        GameRules.activeEnergyCost = values[11];
        ship.SetMaxEnergy(values[12]);
        ship.SetMaxHits(Mathf.RoundToInt(values[13]));
        ship.updateBulletSpawnPoints();
    }

    public Pickup Capture()
    {
        var upgrades = ship.GetComponent<CombatUpgrades>();
        var counts = new int[upgradeKinds.Length];
        for (int i = 0; i < counts.Length; i++)
            counts[i] = upgrades.Count(upgradeKinds[i]);
        return new Pickup { stats = ReadStats(), upgrades = counts,
            effects = ship.GetComponents<MonoBehaviour>(), guns = ship.GetComponentsInChildren<AWeapon>(),
            companions = OwnedCompanions(), imps = ship.GetComponentsInChildren<ImpCompanion>() };
    }

    ACompanion[] OwnedCompanions()
    {
        var result = new List<ACompanion>(ship.Companions);
        foreach (var companion in ship.GetComponentsInChildren<ACompanion>())
            if (!result.Contains(companion)) result.Add(companion);
        return result.ToArray();
    }

    public void Record(AItem item, Pickup before, Transform display)
    {
        if (before == null || !ItemCatalog.TryGet(item.GetType().Name, out _))
            return;
        var data = item.GetData();
        var entry = new Entry { key = item.GetType().Name, item = item, name = data[0], rarity = item.rarity, display = display,
            kind = item.Kind, slot = item.ReplacementSlot,
            multiply = item is APactItem || item is EarlyRise || item is FocusTime || item is HeatSink,
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
        foreach (var companion in OwnedCompanions())
            if (Array.IndexOf(before.companions, companion) < 0) entry.companions.Add(companion);
        foreach (var imp in ship.GetComponentsInChildren<ImpCompanion>())
            if (Array.IndexOf(before.imps, imp) < 0) entry.imps.Add(imp);
        entries.Add(entry);
    }

    public List<Entry> Tradeable()
    {
        return entries.FindAll(entry => entry.tradeable);
    }

    // Keep permanent hex bonuses in the stat history so trading an earlier multiplier
    // cannot divide away a bonus earned later. These are never inventory/trade items.
    public void RecordStatBonus(Pickup before)
    {
        var after = ReadStats();
        var entry = new Entry { changes = new float[after.Length] };
        bool changed = false;
        for (int i = 0; i < after.Length; i++)
        {
            entry.changes[i] = after[i] - before.stats[i];
            changed |= Mathf.Abs(entry.changes[i]) > .000001f;
        }
        if (changed)
            entries.Add(entry);
    }
    public bool Contains(Entry entry) => entry != null && entries.Contains(entry);

    // Equipment has already been detached by the loadout; discard only its pickup/icon.
    public void ForgetEquipment(AItem.ItemType kind, int slot)
    {
        for (int i = entries.Count - 1; i >= 0; i--)
            if (entries[i].kind == kind && entries[i].slot == slot)
            {
                PSXInventoryDisplay.Remove(entries[i].display);
                entries.RemoveAt(i);
            }
    }

    public bool DebugRemove(string key, int preferredSlot)
    {
        var entry = entries.FindLast(e => e.key == key && e.slot == preferredSlot) ?? entries.FindLast(e => e.key == key);
        if (entry == null) return false;
        var loadout = ship.GetComponent<ShipLoadout>();
        if (entry.kind == AItem.ItemType.weapon)
        {
            loadout.ClearWeapon(entry.slot);
            return true;
        }
        if (entry.kind == AItem.ItemType.active)
        {
            loadout.EquipActive(null, entry.slot);
            return true;
        }
        return Remove(entry, true);
    }

    public bool Remove(Entry entry) => Remove(entry, false);

    bool Remove(Entry entry, bool debug)
    {
        int index = entries.IndexOf(entry);
        if (index < 0 || (!debug && !entry.tradeable))
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
                if (gun.gameObject == ship.gameObject) Destroy(gun);
                else Destroy(gun.gameObject);
            }
        foreach (var companion in entry.companions)
            if (companion != null) ship.RemoveCompanion(companion);
        foreach (var imp in entry.imps)
            if (imp != null) { imp.gameObject.SetActive(false); Destroy(imp.gameObject); }
        if (entry.key == nameof(CursedCoin)) PactPool.UnbindCursedCoin(successor != null);
        if (entry.key == nameof(GlassHull) && successor == null) PactPool.UnbindGlassHull();
        PSXInventoryDisplay.Remove(entry.display);
        entries.RemoveAt(index);
        return true;
    }
}
