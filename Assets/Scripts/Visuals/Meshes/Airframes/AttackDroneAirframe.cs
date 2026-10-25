using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildAttackDroneAirframe(Builder b)
    {

        Ring(b, new Vector3(0, 0, -.02f), .16f, .34f, .24f, 6, 12);
        b.Ball(new Vector3(0, 0, -.07f), new Vector3(.15f, .15f, .12f), 11);
        for (int i = 0; i < 3; i++)
        {
            float a = (30 + i * 120) * Mathf.Deg2Rad;
            var p = V(Mathf.Cos(a), Mathf.Sin(a));
            var q = V(-p.y, p.x);
            BevelArmor(b, new[] { p * .29f - q * .11f, p * .71f - q * .19f, p * 1.03f - q * .08f, p * .94f + q * .17f, p * .54f + q * .16f, p * .29f + q * .09f }, .23f, 12);
            Armor(b, new[] { p * .57f - q * .08f, p * .83f - q * .07f, p * .83f + q * .07f, p * .57f + q * .08f }, .025f, 11, -.18f);
            b.Drum((Vector3)(p * .84f) + Vector3.back * .2f, .06f, .035f, 10, 6);
        }
        b.Drum(new Vector3(0, 0, -.2f), .065f, .045f, 9, 6);
        Motor(b, 0, .42f, .2f, .42f, 11);
    }
}
