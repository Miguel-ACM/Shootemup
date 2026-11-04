using UnityEngine;

// Ivory ceramic, warm brass and scorched recesses; shared by the fleet and ruined ramparts.
public static class HeavenMaterials
{
    static Material material;
    public static Material Ivory
    {
        get
        {
            if (material != null) return material;
            material = new Material(CombatMaterials.Fleet) { name = "Ivory and hammered celestial gold" };
            var colors = new[] { new Color(.86f,.87f,.80f), new Color(.97f,.95f,.84f), new Color(.78f,.61f,.24f), new Color(.98f,.77f,.25f),
                new Color(.25f,.28f,.33f), new Color(.41f,.35f,.23f), new Color(.37f,.32f,.22f), new Color(.68f,.53f,.20f),
                new Color(1,.87f,.42f), new Color(.70f,.82f,.87f), new Color(1,.66f,.13f), new Color(.09f,.11f,.16f),
                new Color(.81f,.83f,.78f), new Color(.55f,.47f,.30f), new Color(.64f,.66f,.63f), new Color(.96f,.96f,.91f) };
            var texture = new Texture2D(256,256,TextureFormat.RGBA32,false) { name="Celestial material trim", filterMode=FilterMode.Point };
            var pixels = new Color[256*256];
            for (int tile=0;tile<16;tile++) for (int y=0;y<64;y++) for (int x=0;x<64;x++)
            {
                float edge = x<5 || y<5 || x>58 || y>58 ? .67f : x<8 || y>55 ? 1.09f : 1;
                float grain = .96f + ((x*71+y*29+x*y)%31)*.0025f;
                Color c=colors[tile]*edge*grain; c.a=1;
                pixels[(tile/4*64+y)*256+tile%4*64+x]=c;
            }
            texture.SetPixels(pixels); texture.Apply(false,true); material.mainTexture=texture;
            return material;
        }
    }
}
