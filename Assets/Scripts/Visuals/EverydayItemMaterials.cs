using UnityEngine;
using System.Collections.Generic;
public static class EverydayItemMaterials
{
    static Material gum;
    static readonly Dictionary<string, Material> identities = new Dictionary<string, Material>();

    // Original items keep their authored materials. New equipment gets a deliberate
    // colour identity while retaining the existing pixel-art trim and lighting.
    static bool IdentityColor(string kind, out Color color)
    {
        switch (kind)
        {
            case "SplitterCore":
                color = new Color(1f, .72f, .16f);
                return true;
            case "ReturnModule":
                color = new Color(1f, .4f, .15f);
                return true;
            case "ReboundCoil":
                color = new Color(.7f, .36f, .95f);
                return true;
            case "RecoveryCoil":
                color = new Color(.18f, .56f, 1f);
                return true;
            case "ReserveCell":
                color = new Color(.25f, .88f, .32f);
                return true;
            case "ProximityFuse":
                color = new Color(1f, .2f, .24f);
                return true;
            case "BeamEmitterItem":
                color = new Color(.16f, .85f, .93f);
                return true;
            case "NeedleCannonItem":
                color = new Color(.75f, .45f, .96f);
                return true;
            case "ScatterCannonItem":
                color = new Color(.95f, .75f, .18f);
                return true;
            case "SwarmRackItem":
                color = new Color(.51f, .85f, .23f);
                return true;
            case "BurstLauncherItem":
                color = new Color(1f, .46f, .18f);
                return true;
            case "RailDriverItem":
                color = new Color(.48f, .43f, .94f);
                return true;
            case "DiscThrowerItem":
                color = new Color(.3f, .8f, .77f);
                return true;
            case "FlameProjectorItem":
                color = new Color(1f, .34f, .12f);
                return true;
            case "Capacitor":
                color = new Color(.37f, .65f, 1f);
                return true;
            case "SpareBattery":
                color = new Color(.75f, .8f, .2f);
                return true;
            case "Overdrive":
                color = new Color(1f, .25f, .2f);
                return true;
            case "FocusingLens":
                color = new Color(.35f, .86f, .91f);
                return true;
            case "SalvageConverter":
                color = new Color(1f, .73f, .18f);
                return true;
            case "ChainReactor":
                color = new Color(1f, .34f, .12f);
                return true;
            case "MirrorBurst":
                color = new Color(.62f, .77f, 1f);
                return true;
            case "GravityWell":
                color = new Color(.24f, .78f, 1f);
                return true;
            case "BackdraftItem":
                color = new Color(1f, .29f, .12f);
                return true;
            case "DepthChargeItem":
                color = new Color(.22f, .52f, .92f);
                return true;
            case "WindUpCannonItem":
                color = new Color(.8f, .38f, .88f);
                return true;
            case "BulletTime":
                color = new Color(.54f, .43f, .95f);
                return true;
            case "DoubleTrouble":
                color = new Color(1f, .66f, .18f);
                return true;
            case "EmpPulse":
                color = new Color(.26f, .86f, .82f);
                return true;
            case "DroneLink":
                color = new Color(.85f, .47f, .83f);
                return true;
            case "BallBearing":
                color = new Color(.6f, .73f, .92f);
                return true;
            case "SparePlating":
                color = new Color(.4f, .88f, .46f);
                return true;
            case "DoubleHull":
                color = new Color(.95f, .36f, .42f);
                return true;
            case "EnergyShield":
                color = new Color(.42f, .8f, 1f);
                return true;
            case "ExplodingShield":
                color = new Color(1f, .5f, .16f);
                return true;
            case "BadGlasses":
                color = new Color(.75f, .42f, .86f);
                return true;
            default:
                color = default;
                return false;
        }
    }

    static Material Identity(string kind, Color tint)
    {
        if (identities.TryGetValue(kind, out var material) && material != null)
            return material;
        material = new Material(CombatMaterials.Fleet) { name = kind + " coloured equipment" };
        var texture = Object.Instantiate((Texture2D)CombatMaterials.Fleet.mainTexture);
        texture.name = kind + " equipment palette";
        var pixels = texture.GetPixels();
        foreach (int tile in new[] { 6, 9, 11 })
        {
            Color target = tile == 11 ? tint * .32f : tile == 9 ? Color.Lerp(tint, Color.white, .25f) : tint;
            float reference = tile == 11 ? .085f : tile == 9 ? .64f : .59f;
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    int index = (tile / 4 * 64 + y) * 256 + tile % 4 * 64 + x;
                    Color original = pixels[index];
                    float light = Mathf.Clamp((original.r * .2126f + original.g * .7152f + original.b * .0722f) / reference, 0, 1.35f);
                    pixels[index] = new Color(target.r * light, target.g * light, target.b * light, original.a);
                }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        material.mainTexture = texture;
        identities[kind] = material;
        return material;
    }

    public static Material For(string kind)
    {
        if (System.Array.IndexOf(IndustrialMeshFactory.ItemKinds, kind) >= 0)
        {
            var prefab = Resources.Load<GameObject>("Items/Item");
            var visual = prefab != null ? prefab.GetComponent<PSXObjectVisual>() : null;
            if (visual != null && visual.modelRenderer != null)
                return visual.modelRenderer.sharedMaterial;
        }
        if (IdentityColor(kind, out var identity))
            return Identity(kind, identity);
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
