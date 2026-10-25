using UnityEngine;
using System;
using System.Collections.Generic;

// Cool painted ordnance, with small highlights instead of large white cores.
public static partial class PlayerProjectileArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly Color Ink = new Color(.035f, .13f, .18f), Blue = new Color(.15f, .48f, .61f), Teal = new Color(.27f, .67f, .72f), Highlight = new Color(.65f, .83f, .84f);
    public static Sprite For(PlayerDelivery delivery)
    {
        if (delivery == PlayerDelivery.Shell)
            return Shell();
        if (delivery == PlayerDelivery.Disc)
            return Disc();
        if (delivery == PlayerDelivery.Flame)
            return Flame();
        if (delivery == PlayerDelivery.Needle)
            return Needle();
        if (delivery == PlayerDelivery.Missile)
            return Missile();
        return CombatVisuals.Projectile(true);
    }

    static Sprite Make(string name, int width, int height, Func<int, int, Color> pixel)
    {
        if (cache.TryGetValue(name, out var sprite))
            return sprite;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = pixel(x, y);
        texture.SetPixels(pixels);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), Mathf.Max(width, height));
        sprite.name = name;
        return cache[name] = sprite;
    }

    static Sprite Shell() => Make("Friendly fragmentation shell", 24, 24, (x, y) =>
         {
             var p = new Vector2(x - 11.5f, y - 11.5f);
             float r = p.magnitude;
             if (r > 11)
                 return Color.clear;
             if (r > 9.2f)
                 return Ink;
             if (y == 9 || y == 10)
                 return new Color(.12f, .32f, .43f);
             if (p.x < 0 && p.y > 2 && r > 5 && r < 8)
                 return Highlight;
             return Color.Lerp(Blue, Teal, Mathf.Clamp01((y - 4) / 16f));
         });
}
