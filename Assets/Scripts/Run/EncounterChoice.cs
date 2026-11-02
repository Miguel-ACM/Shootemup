using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterKind
{
    Shop, MultShop, Item, Health, Challenge, Pact, Salvage, HealthTrade, Reforge, Cargo
}
public static class EncounterChoice
{
    static readonly Sprite[] icons = new Sprite[Enum.GetValues(typeof(EncounterKind)).Length];
    public static Sprite Icon(EncounterKind kind)
    {
        int index = (int)kind;
        if (icons[index] != null)
            return icons[index];
        var texture = Resources.Load<Texture2D>("EncounterIcons/" + kind);
        if (texture == null)
            throw new InvalidOperationException("Missing encounter icon: " + kind);
        return icons[index] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
    }

    public static int SalvageCoins(int tier, float luck) => 12 + Mathf.Max(0, tier - 1) * 4 + Mathf.FloorToInt(Mathf.Clamp(luck, 0, 2) * 12);

    public static EncounterKind[] Draw(int seed, bool challengeAvailable, float health, float maxHealth, bool pactAvailable = false,
        float luck = 0, bool canTrade = false, bool canReforge = false, bool cargoAvailable = true)
    {
        // Shops are no longer offered mid-run; the hangar shop between runs replaces them.
        var bag = new List<EncounterKind> { EncounterKind.Item, EncounterKind.Salvage };
        // Integer factors keep the exact 70/100 boundary inclusive on Unity's runtime.
        if (maxHealth > 0 && health * 10f <= maxHealth * 7f)
            bag.Add(EncounterKind.Health);
        if (maxHealth > 0 && health < maxHealth && canTrade)
            bag.Add(EncounterKind.HealthTrade);
        if (canReforge)
            bag.Add(EncounterKind.Reforge);
        if (cargoAvailable)
            bag.Add(EncounterKind.Cargo);
        if (challengeAvailable)
            bag.Add(EncounterKind.Challenge);
        var rng = new System.Random(seed);
        int count = 2;
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var swap = bag[i];
            bag[i] = bag[j];
            bag[j] = swap;
        }
        // Salvage provides a useful alternative even at full health and without spare items.
        var picked = new List<EncounterKind> { bag[0] };
        foreach (var kind in bag)
            if (picked.Count < count && !picked.Contains(kind) && picked.TrueForAll(other => !Incompatible(other, kind)))
                picked.Add(kind);
        // A due pact replaces the last zone, so it is never crowded out.
        if (pactAvailable)
        {
            if (picked.Count == count)
                picked[picked.Count - 1] = EncounterKind.Pact;
            else
                picked.Add(EncounterKind.Pact);
        }
        return picked.ToArray();
    }

    static bool Incompatible(EncounterKind a, EncounterKind b)
    {
        return a == EncounterKind.Item && b == EncounterKind.Challenge || a == EncounterKind.Challenge && b == EncounterKind.Item ||
            a == EncounterKind.Health && b == EncounterKind.HealthTrade || a == EncounterKind.HealthTrade && b == EncounterKind.Health;
    }

    public static string Label(EncounterKind kind) => kind == EncounterKind.MultShop ? "MULT SHOP" : kind.ToString().ToUpperInvariant();
    public static Color Accent(EncounterKind kind)
    {
        switch (kind)
        {
            case EncounterKind.Shop:
                return new Color(.95f, .73f, .32f);
            case EncounterKind.MultShop:
                return new Color(.46f, .79f, .94f);
            case EncounterKind.Item:
                return new Color(.61f, .87f, .49f);
            case EncounterKind.Health:
                return new Color(.94f, .42f, .44f);
            case EncounterKind.Pact:
                return new Color(.88f, .1f, .2f);
            case EncounterKind.Salvage:
                return new Color(.95f, .73f, .32f);
            case EncounterKind.HealthTrade:
                return new Color(.95f, .56f, .65f);
            case EncounterKind.Reforge:
                return new Color(.5f, .8f, .95f);
            case EncounterKind.Cargo:
                return new Color(.96f, .63f, .33f);
            default:
                return new Color(.76f, .48f, .95f);
        }
    }
}
