using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class FrostAuger : GroundVehicle
{
    readonly Transform[] screws = new Transform[2];
    protected override bool Snow => true;
    protected override void Configure()
    {
        hull = 40;
        hullScale = .64f;
        health = 39;
        attackInterval = (1.9f);
        shellTravelDistance = 2.7f;
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (Weapon != null)
            Cue(Weapon.TransformPoint(new Vector3((volley % 2 == 0 ? 1 : -1) * .23f, -.57f, -.36f)), seconds);
    }

    protected override void Attack()
    {
        if (!Grounded || player == null || Vector2.Distance(player.position, transform.position) < 2.6f)
            return;
        int side = volley % 2 == 0 ? -1 : 1;
        var muzzle = Weapon.TransformPoint(new Vector3(side * .23f, -.57f, -.36f));
        Fire<BurstShell>(muzzle, CombatVisuals.Rotate(CombatVisuals.Aim(muzzle), side * 12), 3.5f, .34f);
    }

    protected override void BuildRunningGear()
    {
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Rotating helical ice screw " + i);
            go.transform.SetParent(Hull, false);
            go.transform.localPosition = new Vector3(i == 0 ? -.59f : .59f, 0, -.10f);
            go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.FrostScrew();
            go.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet;
            screws[i] = go.transform;
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        for (int i = 0; i < 2; i++)
            if (screws[i] != null)
                screws[i].Rotate(0, (i == 0 ? 1 : -1) * Time.deltaTime * 260, 0, Space.Self);
    }
}
public static partial class IndustrialMeshFactory
{
    static Mesh frostScrew;
    public static Mesh FrostScrew()
    {
        if (frostScrew != null)
            return frostScrew;
        var b = new Builder();
        for (int i = 0; i < 36; i++)
            for (int j = 0; j < 8; j++)
            {
                Vector3 P(int y, int angle)
                {
                    float a = angle * Mathf.PI / 4, r = .16f + (.5f + .5f * Mathf.Cos(a - y * Mathf.PI / 6)) * .07f;
                    return new Vector3(Mathf.Cos(a) * r, -.86f + y * 1.72f / 36, Mathf.Sin(a) * r);
                }
                b.Face(j % 2 == 0 ? 6 : 4, P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1));
            }
        b.Ball(new Vector3(0, -.83f, 0), new Vector3(.18f, .25f, .18f), 6);
        b.Ball(new Vector3(0, .83f, 0), new Vector3(.18f, .20f, .18f), 11);
        return frostScrew = b.Finish("Helical snow propulsion screw");
    }

    static void BuildFrostAuger(Builder b)
    {
        BevelArmor(b, new[] { V(-.35f, -.80f), V(.35f, -.80f), V(.45f, -.47f), V(.41f, .69f), V(-.41f, .69f), V(-.45f, -.47f) }, .28f, 1, -.08f);
        b.Prism(new[] { V(-.30f, -.69f), V(.30f, -.69f), V(.23f, -.35f), V(-.23f, -.35f) }, .09f, 4, new Vector3(0, 0, -.28f), 11);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .41f, .46f, -.1f), new Vector3(.27f, .13f, .19f), 6, 11);
            b.Prism(new[] { V(s * .15f, -.62f), V(s * .80f, -.92f), V(s * .82f, -.69f), V(s * .26f, -.43f) }, .11f, 1, Vector3.zero, 6);
        }
        b.Box(new Vector3(0, .49f, -.28f), new Vector3(.49f, .29f, .08f), 11, 4);
        for (int i = 0; i < 3; i++)
            b.Box(new Vector3(0, .4f + i * .09f, -.33f), new Vector3(.39f, .035f, .02f), 6, 11);
    }

    static void BuildFrostWeapon(Builder b)
    {
        b.Drum(Vector3.zero, .34f, .13f, 6, 10, 11);
        b.Box(new Vector3(0, .06f, -.14f), new Vector3(.67f, .48f, .19f), 1, 4);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .23f, -.25f, -.27f), new Vector3(.23f, .63f, .24f), 6, 11);
            b.Box(new Vector3(s * .23f, -.56f, -.29f), new Vector3(.28f, .13f, .29f), 11, 4);
            b.Box(new Vector3(s * .23f, -.47f, -.43f), new Vector3(.10f, .07f, .02f), 9, 11);
        }
        b.Box(new Vector3(0, .15f, -.27f), new Vector3(.16f, .22f, .06f), 4, 11);
    }
}
