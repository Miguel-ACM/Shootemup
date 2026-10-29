using System.Collections.Generic;
using UnityEngine;

// Pacts are legendary bargains: a strong boon bound to a permanent curse.
// Each pact is offered at most once per run; declining still spends the offer.
public static class PactPool
{
    public const string Rarity = "pact";
    static readonly string[] all = { "GlassHull", "BloodPrice", "DevilImp", "MirrorOath", "Hunger", "CursedCoin" };
    static List<string> remaining = new List<string>(all);

    // Run-wide curse state, applied outside the ship so it survives district changes.
    public static bool GlassHull
    {
        get; private set;
    }

    public static bool RarityUpgrade
    {
        get; private set;
    }

    public static float EnemyHealth
    {
        get; private set;
    } = 1;

    /// <summary>The stage whose reward break already showed a pact hexagon.</summary>
    public static int HexStage
    {
        get; set;
    } = -1;

    public static void ResetRun()
    {
        remaining = new List<string>(all);
        HexStage = -1;
        GlassHull = false;
        RarityUpgrade = false;
        EnemyHealth = 1;
    }

    public static void BindGlassHull() => GlassHull = true;
    public static void BindCursedCoin()
    {
        RarityUpgrade = true;
        EnemyHealth *= 1.25f;
    }

    /// <summary>Every second district (stages 2, 4, 6...) offers one pact hexagon.</summary>
    public static bool Due => LevelRun.Stage % 2 == 0;

    static bool Eligible(string pact, Shipcontroller ship)
    {
        switch (pact)
        {
            case "DevilImp":
                return ship.health > 2;
            case "MirrorOath":
                return ship.maxHealth > 1 && !GlassHull;
            default:
                return true;
        }
    }

    public static bool HasEligible(Shipcontroller ship) => ship != null && remaining.Exists(pact => Eligible(pact, ship));

    /// <summary>Draws up to count distinct pacts and removes them from the run.</summary>
    public static List<string> Draw(Shipcontroller ship, int count)
    {
        var eligible = remaining.FindAll(pact => Eligible(pact, ship));
        var drawn = new List<string>();
        while (drawn.Count < count && eligible.Count > 0)
        {
            int i = Random.Range(0, eligible.Count);
            drawn.Add(eligible[i]);
            remaining.Remove(eligible[i]);
            eligible.RemoveAt(i);
        }
        return drawn;
    }
}
