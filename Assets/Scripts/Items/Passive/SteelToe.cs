using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SteelToe : AItem
{
    void Awake() => SetData("Steel Toe", "Shin first", "Damage enemies you bump into.", ItemType.passive);

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.SteelToe);
        base.OnPickup();
    }
}

public sealed class SteelToeContact : MonoBehaviour
{
    readonly List<AEnemy> pressed = new List<AEnemy>();
    HashSet<AEnemy> previous = new HashSet<AEnemy>(), touching = new HashSet<AEnemy>();
    Collider2D core;
    CombatUpgrades upgrades;

    void Awake()
    {
        core = GetComponent<Collider2D>();
        upgrades = GetComponent<CombatUpgrades>();
    }

    void FixedUpdate()
    {
        if (!GameClock.ActivePlay || core == null || !core.enabled || upgrades == null)
            return;
        int copies = upgrades.Count(UpgradeKind.SteelToe);
        if (copies == 0) return;
        var bounds = core.bounds;
        Vector2 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y);
        touching.Clear();
        pressed.Clear();
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanReceiveDamage ||
                !enemy.ClosestDamagePoint(center, out var point) || (point - center).sqrMagnitude > radius * radius)
                continue;
            touching.Add(enemy);
            if (!previous.Contains(enemy))
                pressed.Add(enemy);
        }
        // Applying damage can kill a ship and remove it from the live registry.
        foreach (var enemy in pressed)
        {
            if (enemy == null || !enemy.CanReceiveDamage) continue;
            Vector2 axis = (Vector2)enemy.transform.position - center;
            if (CombatHit.Apply(enemy, GameRules.playerShootDamage * 2.5f * copies, center, axis.normalized, generation: 1) > 0)
                RunInventory.Ping(UpgradeKind.SteelToe);
        }
        (previous, touching) = (touching, previous);
    }

    void OnDisable()
    {
        previous.Clear();
        touching.Clear();
        pressed.Clear();
    }
}
