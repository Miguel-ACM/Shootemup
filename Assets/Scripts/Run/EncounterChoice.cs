using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterKind
{
    Shop, MultShop, Item, Health, Challenge, Pact, Salvage, HealthTrade, Reforge, Cargo,
    AttackSpeed, AttackDamage, MaxHealth, ExtraShot
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

    public static int OptionCount(float luck, float roll)
    {
        luck = Mathf.Max(0, luck);
        float four = Mathf.Min(.20f, .05f + luck * .10f);
        float three = Mathf.Min(.45f, .20f + luck * .25f);
        return roll < four ? 4 : roll < four + three ? 3 : 2;
    }
    public static int SalvageCoins(int tier, float luck) => 12 + Mathf.Max(0, tier - 1) * 4 + Mathf.FloorToInt(Mathf.Clamp(luck, 0, 2) * 12);

    const double ExtraShotChance = .05;
    public const int MaxOptions = 5;

    // A regular hex has four tickets for every offensive upgrade's one.
    static int Tickets(EncounterKind kind) => kind == EncounterKind.AttackSpeed ||
        kind == EncounterKind.AttackDamage || kind == EncounterKind.ExtraShot ? 1 : 4;

    static EncounterKind DrawWeighted(List<EncounterKind> bag, System.Random rng)
    {
        int total = 0;
        foreach (var kind in bag)
            total += Tickets(kind);
        int ticket = rng.Next(total);
        int index = 0;
        while (ticket >= Tickets(bag[index]))
        {
            ticket -= Tickets(bag[index]);
            index++;
        }
        return bag[index];
    }

    public static EncounterKind[] Draw(int seed, bool challengeAvailable, float health, float maxHealth, bool pactAvailable = false,
        float luck = 0, bool canTrade = false, bool canReforge = false, bool cargoAvailable = true, bool canGrowHull = true, bool opening = false)
    {
        // Shops are no longer offered mid-run; the hangar shop between runs replaces them.
        var bag = new List<EncounterKind> { EncounterKind.Item, EncounterKind.Salvage,
            EncounterKind.AttackSpeed, EncounterKind.AttackDamage };
        // Even before the weighted draw, only one in twenty offers can contain SHOTS +1.
        if (new System.Random(seed ^ 0x5bd1e995).NextDouble() < ExtraShotChance)
            bag.Add(EncounterKind.ExtraShot);
        if (canGrowHull)
            bag.Add(EncounterKind.MaxHealth);
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
        int count = OptionCount(luck, (float)rng.NextDouble());
        int slotsBeforePact = pactAvailable ? count - 1 : count;
        var picked = new List<EncounterKind>(count);
        while (picked.Count < slotsBeforePact && bag.Count > 0)
        {
            var kind = opening && picked.Count == 0 ? EncounterKind.Item : DrawWeighted(bag, rng);
            picked.Add(kind);
            bag.RemoveAll(other => other == kind || Incompatible(kind, other));
        }
        if (pactAvailable)
            picked.Add(EncounterKind.Pact);
        return picked.ToArray();
    }

    static bool Incompatible(EncounterKind a, EncounterKind b)
    {
        return a == EncounterKind.Item && b == EncounterKind.Challenge || a == EncounterKind.Challenge && b == EncounterKind.Item ||
            a == EncounterKind.Health && b == EncounterKind.HealthTrade || a == EncounterKind.HealthTrade && b == EncounterKind.Health;
    }

    public static bool IsStatReward(EncounterKind kind) => kind == EncounterKind.AttackSpeed ||
        kind == EncounterKind.AttackDamage || kind == EncounterKind.MaxHealth || kind == EncounterKind.ExtraShot;

    // Slotless run bonuses, independent of the item catalog and its availability pool.
    public static void ApplyStatReward(EncounterKind kind, Shipcontroller ship)
    {
        if (!IsStatReward(kind))
            throw new ArgumentException("Not a stat reward", nameof(kind));
        var inventory = RunInventory.For(ship);
        var before = inventory.Capture();
        switch (kind)
        {
            case EncounterKind.AttackSpeed:
                GameRules.playerShootSpeed += .8f;
                break;
            case EncounterKind.AttackDamage:
                GameRules.playerShootDamage += .25f * (ship.GetComponent<OverdriveBoost>() != null ? 2 : 1);
                break;
            case EncounterKind.MaxHealth:
                ship.AddMaxHits(1);
                break;
            case EncounterKind.ExtraShot:
                GameRules.playerShootNumber++;
                ship.updateBulletSpawnPoints();
                break;
        }
        inventory.RecordStatBonus(before);
    }

    // ELITE HUNT's promised reward rarity while it is on offer or underway; its colour is the offer's.
    public static string ChallengeRarity;

    public static string Label(EncounterKind kind)
    {
        switch (kind)
        {
            case EncounterKind.Challenge: return "ELITE HUNT";
            case EncounterKind.MultShop: return "MULT SHOP";
            case EncounterKind.HealthTrade: return "HEALTH TRADE";
            case EncounterKind.AttackSpeed: return "FIRE RATE +0.8";
            case EncounterKind.AttackDamage: return "DAMAGE +0.25";
            case EncounterKind.MaxHealth: return "MAX HEALTH +1";
            case EncounterKind.ExtraShot: return "SHOTS +1";
            default: return kind.ToString().ToUpperInvariant();
        }
    }
    public static Color Accent(EncounterKind kind)
    {
        if (kind == EncounterKind.Challenge && ChallengeRarity != null)
            return AItem.GetRarityColor(ChallengeRarity);
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
            case EncounterKind.AttackSpeed:
                return new Color(.36f, .86f, .95f);
            case EncounterKind.AttackDamage:
                return new Color(1, .62f, .26f);
            case EncounterKind.MaxHealth:
                return new Color(.96f, .46f, .58f);
            case EncounterKind.ExtraShot:
                return new Color(.76f, .63f, 1);
            default:
                return new Color(.76f, .48f, .95f);
        }
    }
}
