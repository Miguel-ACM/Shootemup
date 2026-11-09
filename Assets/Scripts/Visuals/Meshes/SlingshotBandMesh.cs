using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A forked wooden slingshot with a wrapped grip; the green band is drawn back
    // toward the viewer around a leather pouch and its pebble.
    static void SlingshotBandModel(Builder b)
    {
        b.Prism(new[] { new Vector2(-.06f, -.45f), new Vector2(.06f, -.45f), new Vector2(.065f, -.08f), new Vector2(.31f, .29f),
            new Vector2(.22f, .35f), new Vector2(0, .04f), new Vector2(-.22f, .35f), new Vector2(-.31f, .29f), new Vector2(-.065f, -.08f) }, .12f, 13, Vector3.zero, 5, 5);
        for (int i = 0; i < 4; i++)
            b.Box(new Vector3(0, -.38f + i * .075f, 0), new Vector3(.15f, .045f, .15f), 11, 11);
        var pouch = new Vector3(0, .02f, -.30f);
        for (int side = -1; side <= 1; side += 2)
        {
            var tip = new Vector3(side * .265f, .32f, -.02f);
            b.Box(tip, new Vector3(.12f, .06f, .15f), 6, 11);
            EquipmentRod(b, tip, pouch + new Vector3(side * .07f, 0, 0), .024f, 6);
        }
        b.Prism(new[] { new Vector2(-.09f, -.04f), new Vector2(.09f, -.04f), new Vector2(.11f, .03f), new Vector2(-.11f, .03f) }, .07f, 5, pouch, 5, 5);
        b.Ball(pouch + new Vector3(0, .02f, -.05f), new Vector3(.065f, .06f, .06f), 14);
    }
}
