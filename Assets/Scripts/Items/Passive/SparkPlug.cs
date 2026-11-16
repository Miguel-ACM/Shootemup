using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SparkPlug : AItem
{
    void Awake()
    {
        SetData("Spark Plug", "Jump start", "Electricity jumps once more and pops stuck gum.", ItemType.passive);
    }

    public override void OnBegin() { }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.SparkPlug);
        base.OnPickup();
    }

    static bool Equipped => CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.SparkPlug) > 0;

    public static void ArcContact(AEnemy enemy)
    {
        if (Equipped && StickyGum.TryPop(enemy))
            RunInventory.Ping(UpgradeKind.SparkPlug);
    }

    public static void ExtraHop(Vector3 from, ICollection<AEnemy> visited, float damage, bool score)
    {
        if (!Equipped || damage <= 0) return;
        const float reach = 3;
        AEnemy target = null;
        float nearest = reach * reach;
        bool foundGum = false;
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanBeTargeted || !enemy.CanReceiveDamage || visited.Contains(enemy))
                continue;
            var at = enemy.transform.position;
            float distance = ((Vector2)(at - from)).sqrMagnitude;
            if (distance > reach * reach) continue;
            bool hasGum = enemy.TryGetComponent<StickyGum>(out var gum) && gum.Count > 0;
            if ((foundGum && !hasGum) || (hasGum == foundGum && distance >= nearest) ||
                CombatWalls.Sweep(from, at, .04f, out _, out _))
                continue;
            target = enemy;
            nearest = distance;
            foundGum = hasGum;
        }
        if (target == null) return;
        var point = target.transform.position;
        RunInventory.Ping(UpgradeKind.SparkPlug);
        StaticArc.Draw(from, point, .19f);
        ArcContact(target);
        CombatHit.Apply(target, damage * .45f, point, ((Vector2)(point - from)).normalized, generation: 1, score: score);
    }
}
