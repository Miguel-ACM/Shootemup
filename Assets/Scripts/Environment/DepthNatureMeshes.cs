using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh volcano, fishAnimal, rayAnimal;
    public static Mesh Volcano()
    {
        if (volcano != null)
            return volcano;
        return volcano = BasaltCraterGeometry.Build();
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
        b.Prism(new[] { new Vector2(-.18f, .10f), new Vector2(-.55f, .38f), new Vector2(-.32f, -.22f) }, .03f, 13, Vector3.zero);
        return fishAnimal = b.Finish("Schooling reef fish");
    }
}
