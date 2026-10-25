using UnityEngine;

// Separate confection trim sheet: existing fleet and projectile artwork stay on their own materials.
public static class CandyMaterials
{
    static Material material;
    public static Material Glaze
    {
        get
        {
            if (material != null)
                return material;
            material = new Material(CombatMaterials.Fleet) { name = "Candy enamel and chocolate" };
            var palette = new[]{new Color(.78f,.19f,.40f),new Color(.97f,.85f,.71f),new Color(.30f,.78f,.66f),new Color(1,.59f,.25f),
                new Color(.39f,.17f,.12f),new Color(.62f,.28f,.18f),new Color(.45f,.25f,.34f),new Color(.57f,.38f,.79f),
                new Color(.90f,.44f,.65f),new Color(.27f,.91f,.83f),new Color(1,.82f,.39f),new Color(.13f,.055f,.13f),
                new Color(.92f,.31f,.49f),new Color(.83f,.66f,.39f),new Color(.51f,.26f,.58f),new Color(.98f,.96f,.88f)};
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false) { name = "Confection trim atlas", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[256 * 256];
            for (int tile = 0; tile < 16; tile++)
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        var color = palette[tile];
                        float shade = x < 5 || y < 5 || x > 58 || y > 58 ? .73f : x < 9 || y > 54 ? 1.12f : 1;
                        if (tile == 0 || tile == 2 || tile == 8)
                        {
                            if ((x + y / 2) % 32 < 9)
                                color = Color.Lerp(color, palette[15], .7f);
                        }
                        if (tile == 13 && (x % 12 < 2 || y % 12 < 2))
                            shade *= .65f;
                        if (tile == 4 && (x < 11 || y < 11))
                            shade *= .72f;
                        color *= shade;
                        color.a = 1;
                        pixels[(tile / 4 * 64 + y) * 256 + tile % 4 * 64 + x] = color;
                    }
            texture.SetPixels(pixels);
            texture.Apply();
            material.mainTexture = texture;
            return material;
        }
    }
}
