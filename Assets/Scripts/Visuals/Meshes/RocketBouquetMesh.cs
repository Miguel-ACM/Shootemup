using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static void RocketBouquetModel(Builder b)
    {
        int[] colors = { 3, 9, 5, 10, 7 };
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * .13f, top = .26f - Mathf.Abs(i - 2) * .035f;
            Vector3 a = new Vector3(x * .72f, -.28f, i % 2 * .07f), c = new Vector3(x, top, i % 2 * .07f);
            EquipmentRod(b, a, c, .065f, colors[i]);
            b.Prism(new[] { new Vector2(x - .07f, top), new Vector2(x + .07f, top), new Vector2(x, top + .15f) }, .13f, colors[i], new Vector3(0, 0, c.z), 6);
            b.Prism(new[] { new Vector2(a.x - .06f, -.24f), new Vector2(a.x - .12f, -.35f), new Vector2(a.x, -.31f) }, .08f, 11, new Vector3(0, 0, a.z));
        }
        b.Box(new Vector3(0, -.14f, -.12f), new Vector3(.58f, .09f, .08f), 10, 11);
        b.Box(new Vector3(0, -.14f, -.17f), new Vector3(.12f, .13f, .035f), 1, 11);
    }
}
