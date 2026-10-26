using UnityEngine;
public static class EverydayItemMaterials
{
    static Material gum;
    public static Material For(string kind)
    {
        if (System.Array.IndexOf(IndustrialMeshFactory.ItemKinds, kind) >= 0)
        {
            var prefab = Resources.Load<GameObject>("Items/Item");
            var visual = prefab != null ? prefab.GetComponent<PSXObjectVisual>() : null;
            if (visual != null && visual.modelRenderer != null)
                return visual.modelRenderer.sharedMaterial;
        }
        if (kind != "ChewingGum")
            return CombatMaterials.Fleet;
        if (gum != null)
            return gum;
        gum = new Material(CombatMaterials.Fleet) { name = "Pink gum wrapper" };
        var atlas = Object.Instantiate((Texture2D)CombatMaterials.Fleet.mainTexture);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float light = x < 5 || x > 58 || y < 5 || y > 58 ? .6f : y > 53 ? 1.1f : 1;
                atlas.SetPixel(192 + x, 64 + y, new Color(.9f, .32f, .57f) * light);
            }
        atlas.Apply();
        gum.mainTexture = atlas;
        return gum;
    }
}
