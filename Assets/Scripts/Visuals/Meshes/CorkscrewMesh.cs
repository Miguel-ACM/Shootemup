using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A waiter's corkscrew: a wine-red T handle, a brass collar and a real helical worm
    // twisting down into a cork.
    static void CorkscrewModel(Builder b)
    {
        b.Prism(new[] { new Vector2(-.30f, -.05f), new Vector2(-.08f, -.07f), new Vector2(.08f, -.07f), new Vector2(.30f, -.05f),
            new Vector2(.30f, .05f), new Vector2(.08f, .07f), new Vector2(-.08f, .07f), new Vector2(-.30f, .05f) }, .13f, 6, new Vector3(0, .31f, 0), 6, 11);
        for (int side = -1; side <= 1; side += 2)
        {
            b.Drum(new Vector3(side * .33f, .31f, 0), .075f, .15f, 9, 8, 6);
            b.Drum(new Vector3(side * .33f, .31f, -.08f), .035f, .02f, 13, 8);
        }
        EquipmentRod(b, new Vector3(0, .25f, 0), new Vector3(0, .13f, 0), .05f, 13);
        EquipmentRod(b, new Vector3(0, .14f, 0), new Vector3(0, .10f, 0), .07f, 13);
        EquipmentRod(b, new Vector3(0, .10f, 0), new Vector3(0, .03f, 0), .025f, 15);
        const int Steps = 30;
        const float Turns = 3.5f, Radius = .075f, Top = .03f, Bottom = -.36f;
        Vector3 Coil(int i)
        {
            float t = i / (float)Steps, a = t * Turns * Mathf.PI * 2;
            float r = Radius * Mathf.Lerp(1, .55f, t * t);
            return new Vector3(Mathf.Cos(a) * r, Mathf.Lerp(Top, Bottom, t), Mathf.Sin(a) * r);
        }
        for (int i = 0; i < Steps; i++)
            EquipmentRod(b, Coil(i), Coil(i + 1), .02f, 15);
        EquipmentRod(b, new Vector3(0, -.30f, 0), new Vector3(0, -.45f, 0), .11f, 13);
        EquipmentRod(b, new Vector3(0, -.45f, 0), new Vector3(0, -.47f, 0), .095f, 5);
    }
}
