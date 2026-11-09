using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // Three glass marbles of different sizes and colours, each with a white swirl,
    // resting in front of a small tied cloth pouch.
    static void GlassMarblesModel(Builder b)
    {
        b.Pearl(new Vector3(.08f, .1f, .12f), new Vector3(.24f, .26f, .18f), 7, 14, 10);
        b.Halo(new Vector3(.08f, .3f, .12f), .09f, .022f, new Vector3(1, .45f, 1), Quaternion.Euler(80, 0, 0), 13, 16);
        b.Prism(new[] { new Vector2(.02f, .34f), new Vector2(.14f, .34f), new Vector2(.2f, .44f), new Vector2(-.04f, .44f) }, .12f, 7, new Vector3(.08f, 0, .12f), 7, 7);
        var marbles = new[]
        {
            (centre: new Vector3(-.16f, -.2f, -.1f), radius: .17f, tile: 9),
            (centre: new Vector3(.17f, -.24f, -.12f), radius: .14f, tile: 3),
            (centre: new Vector3(-.02f, .0f, -.2f), radius: .12f, tile: 10),
        };
        for (int i = 0; i < marbles.Length; i++)
        {
            var m = marbles[i];
            b.Pearl(m.centre, Vector3.one * m.radius, m.tile, 16, 10);
            b.Halo(m.centre, m.radius * .98f, m.radius * .09f, new Vector3(1, .55f, 1), Quaternion.Euler(20 + i * 35, 30 - i * 40, i * 50), 15, 20);
        }
    }
}
