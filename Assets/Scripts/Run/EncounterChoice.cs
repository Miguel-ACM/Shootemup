using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterKind
{
    Shop, MultShop, Item, Health, Challenge
}
public static class EncounterChoice
{
    static readonly Sprite[] icons = new Sprite[5];
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

    public static EncounterKind[] Draw(int seed, bool challengeAvailable, float health, float maxHealth)
    {
        // Shops are no longer offered mid-run; the hangar shop between runs replaces them.
        var bag = new List<EncounterKind> { EncounterKind.Item };
        // Integer factors keep the exact 70/100 boundary inclusive on Unity's runtime.
        if (maxHealth > 0 && health * 10f <= maxHealth * 7f)
            bag.Add(EncounterKind.Health);
        if (challengeAvailable)
            bag.Add(EncounterKind.Challenge);
        var rng = new System.Random(seed);
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var swap = bag[i];
            bag[i] = bag[j];
            bag[j] = swap;
        }
        // Item and Challenge never appear together; without a compatible second option a single zone is offered.
        var picked = new List<EncounterKind> { bag[0] };
        foreach (var kind in bag)
            if (picked.Count < 2 && !picked.Contains(kind) && !Incompatible(picked[0], kind))
                picked.Add(kind);
        return picked.ToArray();
    }

    static bool Incompatible(EncounterKind a, EncounterKind b)
    {
        return a == EncounterKind.Item && b == EncounterKind.Challenge || a == EncounterKind.Challenge && b == EncounterKind.Item;
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
            default:
                return new Color(.76f, .48f, .95f);
        }
    }
}
