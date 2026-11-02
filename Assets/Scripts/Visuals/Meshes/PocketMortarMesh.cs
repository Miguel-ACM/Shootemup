using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void PocketMortarModel(Builder b)
    {
        b.Box(new Vector3(0, -.28f, 0), new Vector3(.58f, .10f, .38f), 11, 6);
        EquipmentRod(b, new Vector3(-.18f, -.23f, -.05f), new Vector3(.04f, .05f, -.05f), .035f, 9);
        EquipmentRod(b, new Vector3(.18f, -.23f, -.05f), new Vector3(.04f, .05f, -.05f), .035f, 9);
        Vector3 basePoint = new Vector3(-.08f, -.17f, .03f), muzzle = new Vector3(.14f, .27f, -.12f);
        Vector3 axis = (muzzle - basePoint).normalized;
        Vector3 across = Vector3.Cross(axis, Vector3.forward).normalized;
        Vector3 other = Vector3.Cross(axis, across).normalized;
        const int sides = 10;
        Vector3 Rim(Vector3 center, float radius, int i)
        {
            float a = i * Mathf.PI * 2 / sides;
            return center + (across * Mathf.Cos(a) + other * Mathf.Sin(a)) * radius;
        }
        Vector3 collar = muzzle - axis * .08f;
        Vector3 inside = muzzle - axis * .08f;
        var bore = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            int j = (i + 1) % sides;
            b.Face(6, Rim(basePoint, .105f, i), Rim(basePoint, .105f, j), Rim(collar, .105f, j), Rim(collar, .105f, i));
            b.Face(11, Rim(collar, .125f, i), Rim(collar, .125f, j), Rim(muzzle, .125f, j), Rim(muzzle, .125f, i));
            b.Face(9, Rim(muzzle, .125f, i), Rim(muzzle, .125f, j), Rim(muzzle, .075f, j), Rim(muzzle, .075f, i));
            b.Face(11, Rim(muzzle, .075f, i), Rim(muzzle, .075f, j), Rim(inside, .075f, j), Rim(inside, .075f, i));
            bore[i] = Rim(inside, .075f, i);
        }
        b.Face(11, bore);
        b.Ball(basePoint, Vector3.one * .12f, 11);
    }
}
