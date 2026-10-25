using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildTunnelSawAirframe(Builder b)
    {
        // A rotating industrial cutter: open slots expose its dark flywheel beneath.
        b.Drum(Vector3.zero, 1.13f, .25f, 11, 32, 6);
        Ring(b, new Vector3(0, 0, -.12f), .83f, 1.16f, .14f, 6, 32);
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI / 8;
            var p = V(Mathf.Cos(a), Mathf.Sin(a));
            var q = V(-p.y, p.x);
            Armor(b, new[] { p * 1.06f - q * .095f, p * 1.43f + q * .015f, p * 1.22f + q * .15f, p * 1.05f + q * .12f }, .17f, i % 2 == 0 ? 6 : 3, -.02f);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3;
            var p = V(Mathf.Cos(a), Mathf.Sin(a));
            var q = V(-p.y, p.x);
            BevelArmor(b, new[] { p * .34f - q * .12f, p * .86f - q * .19f, p * .95f + q * .04f, p * .45f + q * .2f }, .19f, 0, -.24f);
            b.Drum((Vector3)(p * .73f) + Vector3.back * .36f, .055f, .035f, 11, 8);
        }
        Ring(b, new Vector3(0, 0, -.3f), .23f, .42f, .17f, 6, 16);
        b.Drum(new Vector3(0, 0, -.41f), .22f, .08f, 11, 12);
        b.Drum(new Vector3(0, 0, -.47f), .12f, .05f, 3, 8);
    }
}
