using UnityEngine;

// Matchbox: the first hit on each enemy strikes it alight, every burn runs hotter, and a
// hull that dies burning passes its fire to the enemies around it.
[UnityEngine.Scripting.Preserve]
public sealed class Matchbox : AItem
{
    void Awake()
    {
        SetData("Matchbox", "Strike anywhere", "Lights every enemy once. Fire burns hotter and spreads when they die.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Matchbox);
        base.OnPickup();
    }
}

public static class MatchStrike
{
    public const float BurnBonus = .3f;
    const float SpreadRadius = 1.8f, SpreadShare = .6f;

    static int Copies => CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(UpgradeKind.Matchbox) : 0;

    /// <summary>Burn damage multiplier: every copy makes fire hotter.</summary>
    public static float BurnScale => 1 + BurnBonus * Copies;

    /// <summary>True the first time a Matchbox owner hits this enemy.</summary>
    public static bool Strike(AEnemy enemy)
    {
        if (Copies == 0 || enemy.GetComponent<MatchStruck>() != null)
            return false;
        enemy.gameObject.AddComponent<MatchStruck>();
        RunInventory.Ping(UpgradeKind.Matchbox);
        return true;
    }

    /// <summary>A burning hull that dies hands most of its heat to every enemy nearby.</summary>
    public static void Spread(AEnemy dead)
    {
        if (Copies == 0 || !dead.TryGetComponent<BurningHull>(out var burn) || burn.Heat < .5f)
            return;
        Vector2 at = dead.transform.position;
        // Ignite adds 35% of its amount as heat, so this hands over SpreadShare of the heat itself.
        float amount = burn.Heat * SpreadShare / .35f;
        int caught = 0;
        foreach (var target in AEnemy.Live)
        {
            if (target == null || target == dead || !target.CanBeTargeted)
                continue;
            target.ClosestDamagePoint(at, out Vector2 closest);
            if ((closest - at).sqrMagnitude > SpreadRadius * SpreadRadius)
                continue;
            (target.GetComponent<BurningHull>() ?? target.gameObject.AddComponent<BurningHull>()).Ignite(amount, false);
            ItemSpark.Emit(closest, PlayerShotPalette.Fire, 3, .09f);
            caught++;
        }
        if (caught == 0)
            return;
        RunInventory.Ping(UpgradeKind.Matchbox);
        ItemProcVfx.Emit(ItemProcShape.Ignite, at, PlayerShotPalette.Fire, Mathf.Clamp(SpreadRadius * .45f, .5f, .9f));
        ItemSound.Cue("itemBurn", volume: .09f, pitch: .8f, cooldown: .2f);
    }
}

// Marks an enemy the Matchbox has already lit, so it strikes each one only once.
public sealed class MatchStruck : MonoBehaviour
{
}
