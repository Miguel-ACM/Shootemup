using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh cargoCrate, cargoLid, cargoSeal;
    public static Mesh CargoCrate()
    {
        if (cargoCrate != null)
            return cargoCrate;
        var b = new Builder();
        // Hollow walls beneath a separate hinged top, so the item emerges from inside.
        b.Box(new Vector3(0, -.25f, 0), new Vector3(.62f, .06f, .42f), 13);
        foreach (float x in new[] { -.28f, .28f })
            b.Box(new Vector3(x, 0, 0), new Vector3(.06f, .5f, .42f), 13);
        b.Box(new Vector3(0, 0, .18f), new Vector3(.56f, .5f, .06f), 13);
        b.Box(new Vector3(0, 0, -.23f), new Vector3(.53f, .45f, .04f), 2);
        foreach (float x in new[] { -.23f, .23f })
        {
            foreach (float z in new[] { -.26f, .21f })
                b.Box(new Vector3(x, -.02f, z), new Vector3(.075f, .52f, .04f), 6);
            foreach (float y in new[] { -.2f, .2f })
                b.Box(new Vector3(x, y, -.29f), new Vector3(.08f, .075f, .04f), 11);
        }
        b.Box(new Vector3(0, -.05f, -.28f), new Vector3(.18f, .15f, .06f), 11);
        b.Box(new Vector3(0, -.05f, -.32f), new Vector3(.11f, .07f, .03f), 10);
        return cargoCrate = b.Finish("Strapped cargo crate");
    }
    public static Mesh CargoLid()
    {
        if (cargoLid != null) return cargoLid;
        var b = new Builder();
        b.Box(new Vector3(0, 0, -.21f), new Vector3(.66f, .06f, .5f), 13);
        foreach (float x in new[] { -.23f, .23f })
            b.Box(new Vector3(x, .01f, -.21f), new Vector3(.075f, .075f, .51f), 6);
        return cargoLid = b.Finish("Hinged cargo lid");
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
