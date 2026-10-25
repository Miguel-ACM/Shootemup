using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh volcano, fishAnimal, rayAnimal;
    public static Mesh Volcano()
    {
        if (volcano != null)
            return volcano;
        var b = new Builder();
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI / 5, c = (i + 1) * Mathf.PI / 5;
            var u = new Vector3(Mathf.Cos(a) * 1.45f, Mathf.Sin(a) * 1.15f, 0);
            var v = new Vector3(Mathf.Cos(c) * 1.45f, Mathf.Sin(c) * 1.15f, 0);
            var p = new Vector3(Mathf.Cos(a) * .46f, Mathf.Sin(a) * .38f, -.85f);
            var q = new Vector3(Mathf.Cos(c) * .46f, Mathf.Sin(c) * .38f, -.85f);
            b.Face(i % 3 == 0 ? 5 : 11, u, v, q, p);
            b.Face(5, p, q, new Vector3(q.x * .76f, q.y * .76f, -.62f), new Vector3(p.x * .76f, p.y * .76f, -.62f));
        }
        b.Drum(new Vector3(0, 0, -.64f), .36f, .03f, 12, 10);
        return volcano = b.Finish("Cracked obsidian volcano crater");
    }

    public static Mesh OceanAnimal(bool ray)
    {
        if (ray && rayAnimal != null)
            return rayAnimal;
        if (!ray && fishAnimal != null)
            return fishAnimal;
        var b = new Builder();
        if (ray)
        {
            b.Prism(new[] { new Vector2(0, -.80f), new Vector2(-1.4f, .22f), new Vector2(-1.05f, .55f), new Vector2(-.25f, .30f), new Vector2(0, .58f), new Vector2(.25f, .30f), new Vector2(1.05f, .55f), new Vector2(1.4f, .22f) }, .09f, 8, Vector3.zero, 6);
            b.Box(new Vector3(0, 1.03f, 0), new Vector3(.05f, 1.28f, .05f), 8);
            return rayAnimal = b.Finish("Ocean ray silhouette");
        }
        b.Ball(Vector3.zero, new Vector3(.36f, .73f, .13f), 14);
        b.Prism(new[] { new Vector2(0, .42f), new Vector2(-.40f, .99f), new Vector2(.40f, .99f) }, .035f, 13, Vector3.zero);
        b.Prism(new[] { new Vector2(-.18f, .10f), new Vector2(-.55f, .38f), new Vector2(-.32f, -.22f) }, .03f, 13, Vector3.zero);
        return fishAnimal = b.Finish("Schooling reef fish");
    }
}
