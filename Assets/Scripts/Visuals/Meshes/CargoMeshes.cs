using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh cargoCrate, cargoSeal;
    public static Mesh CargoCrate()
    {
        if (cargoCrate != null)
            return cargoCrate;
        var b = new Builder();
        b.Box(Vector3.zero, new Vector3(.62f, .56f, .42f), 13);
        b.Box(new Vector3(0, 0, -.23f), new Vector3(.53f, .45f, .04f), 2);
        foreach (float x in new[] { -.23f, .23f })
        {
            b.Box(new Vector3(x, 0, -.02f), new Vector3(.075f, .62f, .5f), 6);
            foreach (float y in new[] { -.2f, .2f })
                b.Box(new Vector3(x, y, -.29f), new Vector3(.08f, .075f, .04f), 11);
        }
        b.Box(new Vector3(0, -.05f, -.28f), new Vector3(.18f, .15f, .06f), 11);
        b.Box(new Vector3(0, -.05f, -.32f), new Vector3(.11f, .07f, .03f), 10);
        return cargoCrate = b.Finish("Strapped cargo crate");
    }
    public static Mesh CargoSeal()
    {
        if (cargoSeal != null)
            return cargoSeal;
        var b = new Builder();
        b.Box(Vector3.zero, Vector3.one, 10);
        return cargoSeal = b.Finish("Cargo integrity seal");
    }
}
