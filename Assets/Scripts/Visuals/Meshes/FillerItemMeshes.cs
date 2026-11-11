using UnityEngine;

// Small, plain models for the repeatable filler items.
public static partial class IndustrialMeshFactory
{
    static bool TryFillerItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "Whetstone": WhetstoneModel(b); break;
            case "OilCan": OilCanModel(b); break;
            case "TailFin": TailFinModel(b); break;
            case "Horseshoe": HorseshoeModel(b); break;
            default: return false;
        }
        return true;
    }

    // A long bevelled sharpening stone with a lighter honing face.
    static void WhetstoneModel(Builder b)
    {
        b.Prism(new[] { new Vector2(-.40f, -.09f), new Vector2(.36f, -.13f), new Vector2(.42f, -.04f), new Vector2(.40f, .09f),
            new Vector2(-.34f, .13f), new Vector2(-.42f, .04f) }, .16f, 6, Vector3.zero, 11);
        b.Prism(new[] { new Vector2(-.34f, -.05f), new Vector2(.33f, -.08f), new Vector2(.35f, .05f), new Vector2(-.31f, .08f) }, .02f, 9, new Vector3(0, 0, -.09f));
        for (int i = 0; i < 4; i++)
            b.Box(new Vector3(-.22f + i * .15f, 0, -.105f), new Vector3(.018f, .11f, .012f), 11);
    }

    // A squat oil can with a long tapering spout and a looped handle.
    static void OilCanModel(Builder b)
    {
        b.Drum(new Vector3(0, -.10f, 0), .24f, .20f, 6, 12, 11);
        b.Drum(new Vector3(0, .05f, 0), .15f, .16f, 6, 12, 11);
        EquipmentRod(b, new Vector3(.08f, .10f, 0), new Vector3(.30f, .30f, 0), .045f, 11);
        EquipmentRod(b, new Vector3(.30f, .30f, 0), new Vector3(.42f, .40f, 0), .025f, 9);
        EquipmentRod(b, new Vector3(-.20f, -.02f, 0), new Vector3(-.34f, .04f, 0), .03f, 6);
        EquipmentRod(b, new Vector3(-.34f, .04f, 0), new Vector3(-.34f, -.22f, 0), .03f, 6);
        EquipmentRod(b, new Vector3(-.34f, -.22f, 0), new Vector3(-.20f, -.20f, 0), .03f, 6);
        Ring(b, new Vector3(0, -.10f, -.11f), .14f, .2f, .02f, 9, 12);
        Ring(b, new Vector3(0, -.10f, .113f), .14f, .2f, .02f, 9, 12);
    }

    // Three swept fletching fins around a short shaft.
    static void TailFinModel(Builder b)
    {
        EquipmentRod(b, new Vector3(0, -.38f, 0), new Vector3(0, .36f, 0), .05f, 11);
        b.Drum(new Vector3(0, .38f, 0), .07f, .06f, 9, 8);
        for (int s = -1; s <= 1; s += 2)
            b.Prism(new[] { new Vector2(0, -.34f), new Vector2(s * .30f, -.40f), new Vector2(s * .22f, -.10f), new Vector2(0, .16f) }, .03f, 6, Vector3.zero, 11);
        b.Prism(new[] { new Vector2(-.03f, -.34f), new Vector2(.03f, -.34f), new Vector2(.03f, .16f), new Vector2(-.03f, .16f) }, .30f, 6, Vector3.zero, 11);
    }

    // A horseshoe built from short bevelled segments, with nail holes on its face.
    static void HorseshoeModel(Builder b)
    {
        const int Segments = 9;
        for (int i = 0; i < Segments; i++)
        {
            float a0 = Mathf.Lerp(-30, 210, i / (float)Segments) * Mathf.Deg2Rad;
            float a1 = Mathf.Lerp(-30, 210, (i + 1) / (float)Segments) * Mathf.Deg2Rad;
            Vector2 o0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), o1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            b.Prism(new[] { o0 * .22f, o1 * .22f, o1 * .36f, o0 * .36f }, .09f, 6, Vector3.zero, 11);
            if (i % 2 == 1)
                b.Drum((Vector3)((o0 + o1) * .145f) + new Vector3(0, 0, -.05f), .025f, .02f, 9, 6);
        }
    }
}
