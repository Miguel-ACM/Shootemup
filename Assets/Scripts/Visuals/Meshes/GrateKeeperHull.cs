using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Vector3 dredgeSpindle = new Vector3(0, -.64f, -.32f);
    static Mesh dredgeChassis, dredgeCutter;

    public static Vector3 DredgeMuzzle(int side) => new Vector3(side * 1.01f, -1.24f, -.17f);

    static Mesh BuildGrateKeeper()
    {
        var body = new Builder();
        // The bridge sits across two independently buoyant pump housings.
        BevelArmor(body, new[] { V(-1.13f,.12f), V(-.52f,-.21f), V(.52f,-.21f),
            V(1.13f,.12f), V(1.19f,.65f), V(.42f,.93f), V(-.42f,.93f), V(-1.19f,.65f) }, .44f, 12);
        KeelSkin(body, -.15f, 8, Frame(-.66f,.15f,.13f), Frame(-.22f,.33f,.25f),
            Frame(.16f,.54f,.36f), Frame(.67f,.49f,.40f), Frame(1.10f,.31f,.25f), Frame(1.22f,.19f,.12f));
        Glass(body, 0, .30f, .96f, .65f, -.51f, 10);
        BevelArmor(body, new[] { V(-.37f,.54f), V(0,.45f), V(.37f,.54f), V(.28f,.98f),
            V(0,1.07f), V(-.28f,.98f) }, .14f, 8, -.54f);
        Inlet(body, 0, .79f, .40f, .29f, -.63f, 6);
        for (int side = -1; side <= 1; side += 2)
        {
            KeelSkin(body, 0, 8, Frame(-.99f,.16f,.15f,side*1.01f), Frame(-.69f,.34f,.30f,side*.98f),
                Frame(-.13f,.44f,.37f,side*1.06f), Frame(.41f,.39f,.33f,side*1.12f),
                Frame(.92f,.25f,.20f,side*1.22f), Frame(1.16f,.07f,.09f,side*1.30f));
            FieldBarrel(body, DredgeMuzzle(side), .235f, .82f);
            BevelArmor(body, new[] { V(side*.89f,.24f), V(side*1.03f,.67f), V(side*1.30f,.96f),
                V(side*1.48f,.17f), V(side*1.37f,-.34f), V(side*1.09f,-.15f) }, .17f, 12, -.33f);
            FoldedFin(body, new Vector3(side*1.03f,.14f,-.42f), new Vector3(side*1.15f,.49f,-.42f),
                new Vector3(side*1.29f,.79f,-.42f), new Vector3(side*1.33f,-.16f,-.42f),
                new Vector3(side*1.20f,.23f,-.52f), 3, 13);
            // The brass feed pipe bends over the crossbeam into the cannon breech.
            body.Sweep(new Vector3(side*.38f,.61f,-.25f), new Vector3(side*.88f,.61f,-.52f),
                new Vector3(side*.65f,-.41f,-.49f), new Vector3(side*.97f,-.70f,-.27f),
                .082f,.074f,13,false,6,5);
            // Two gantry arms suspend the cutter ahead of the hull.
            FoldedFin(body, new Vector3(side*.88f,-.16f,-.07f), new Vector3(side*.80f,.18f,-.12f),
                new Vector3(side*.09f,-.51f,-.16f), new Vector3(side*.12f,-.69f,-.14f),
                new Vector3(side*.48f,-.28f,-.30f), 6, 14);
        }
        dredgeChassis = body.Finish("Grate Keeper dredger");

        var rotor = new Builder();
        CutBand(rotor, .42f, .066f, .068f, 15, 12);
        for (int tooth = 0; tooth < 5; tooth++)
        {
            var turn = Quaternion.AngleAxis(tooth * 72, Vector3.forward);
            Vector3 P(float x, float y, float z) => turn * new Vector3(x,y,z-.082f);
            FoldedFin(rotor, P(-.09f,.11f,0), P(-.16f,.42f,-.01f), P(.22f,.65f,-.045f),
                P(.12f,.31f,-.02f), P(.006f,.39f,-.14f), 14, 6);
            FoldedFin(rotor, P(-.13f,.43f,-.045f), P(-.074f,.53f,-.055f), P(.22f,.65f,-.056f),
                P(.087f,.48f,-.076f), P(.005f,.51f,-.14f), 1, 13);
        }
        CutStone(rotor, new Vector3(0,0,-.06f), new Vector3(.22f,.22f,.12f), 13, 8);
        CutStone(rotor, new Vector3(0,0,-.17f), new Vector3(.104f,.104f,.065f), 6, 6);
        dredgeCutter = rotor.Finish("Grate Keeper cutter");

        // Single-mesh callers such as the catalogue display the complete silhouette.
        var complete = new Mesh { name = "Grate Keeper" };
        complete.CombineMeshes(new[] {
            new CombineInstance { mesh = dredgeChassis, transform = Matrix4x4.identity },
            new CombineInstance { mesh = dredgeCutter, transform = Matrix4x4.Translate(dredgeSpindle) }
        });
        return complete;
    }

    public static Transform FitGrateKeeperCutter(Transform hull)
    {
        var shell = hull.GetComponent<MeshRenderer>();
        hull.GetComponent<MeshFilter>().sharedMesh = dredgeChassis;
        var cutter = new GameObject("Dredging cutter");
        cutter.layer = hull.gameObject.layer;
        cutter.transform.SetParent(hull, false);
        cutter.transform.localPosition = dredgeSpindle;
        cutter.AddComponent<MeshFilter>().sharedMesh = dredgeCutter;
        var renderer = cutter.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = shell.sharedMaterial;
        renderer.sortingLayerID = shell.sortingLayerID;
        renderer.sortingOrder = shell.sortingOrder;
        return cutter.transform;
    }
}
