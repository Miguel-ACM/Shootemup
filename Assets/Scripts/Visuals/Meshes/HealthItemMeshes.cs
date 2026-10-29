using UnityEngine;
public static partial class IndustrialMeshFactory
{
    // Hull and shield equipment from the hit-segment health redesign.
    static bool HealthItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "ExplodingShield":
                // A heater shield with a charged starburst on its face and a lit fuse on top.
                var shield = new[] { new Vector2(-.36f, .34f), new Vector2(.36f, .34f), new Vector2(.39f, .04f), new Vector2(.25f, -.23f), new Vector2(0, -.43f), new Vector2(-.25f, -.23f), new Vector2(-.39f, .04f) };
                b.Prism(Scaled(shield, 1.12f), .1f, 11, new Vector3(0, 0, .05f), 11);
                b.Prism(shield, .14f, 6, Vector3.zero, 6);
                b.Prism(Star(8, .25f, .1f), .06f, 10, new Vector3(0, .02f, -.1f), 13);
                b.Ball(new Vector3(0, .02f, -.14f), new Vector3(.08f, .08f, .06f), 3);
                b.Drum(new Vector3(0, .41f, 0), .05f, .1f, 11, 6);
                b.Ball(new Vector3(.03f, .5f, 0), new Vector3(.06f, .06f, .06f), 10);
                return true;
            case "SparePlating":
                ArmorPlate(b, Vector3.zero, 0);
                b.Box(new Vector3(0, 0, -.08f), new Vector3(.24f, .07f, .04f), 15);
                b.Box(new Vector3(0, 0, -.08f), new Vector3(.07f, .24f, .04f), 15);
                return true;
            case "DoubleHull":
                // Two overlapping plates, the front one marked with a double chevron.
                ArmorPlate(b, new Vector3(.11f, .12f, .09f), -12);
                ArmorPlate(b, new Vector3(-.07f, -.06f, 0), 8);
                foreach (float y in new[] { -.12f, .02f })
                    b.Prism(new[] { new Vector2(-.16f, y), new Vector2(0, y + .1f), new Vector2(.16f, y), new Vector2(.16f, y + .06f), new Vector2(0, y + .16f), new Vector2(-.16f, y + .06f) }, .04f, 15, new Vector3(-.07f, -.06f, -.08f), 15);
                return true;
            default:
                return false;
        }
    }

    // A clipped-corner hull plate with four rivets.
    static void ArmorPlate(Builder b, Vector3 center, float degrees)
    {
        var rotation = Quaternion.Euler(0, 0, degrees);
        var outline = new[] { new Vector2(-.24f, .27f), new Vector2(.24f, .27f), new Vector2(.32f, .19f), new Vector2(.32f, -.19f), new Vector2(.24f, -.27f), new Vector2(-.24f, -.27f), new Vector2(-.32f, -.19f), new Vector2(-.32f, .19f) };
        for (int i = 0; i < outline.Length; i++)
            outline[i] = rotation * outline[i];
        b.Prism(outline, .09f, 6, center, 6);
        foreach (var corner in new[] { new Vector2(-.22f, .17f), new Vector2(.22f, .17f), new Vector2(.22f, -.17f), new Vector2(-.22f, -.17f) })
            b.Ball(center + (Vector3)(Vector2)(rotation * corner) + Vector3.back * .055f, new Vector3(.035f, .035f, .025f), 1);
    }

    static Vector2[] Scaled(Vector2[] outline, float scale)
    {
        var result = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++)
            result[i] = outline[i] * scale;
        return result;
    }

    static Vector2[] Star(int points, float outer, float inner)
    {
        var result = new Vector2[points * 2];
        for (int i = 0; i < result.Length; i++)
        {
            float a = Mathf.PI * .5f - i * Mathf.PI / points;
            result[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? outer : inner);
        }
        return result;
    }
}
