using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CanOpener : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Can Opener", "Tough lid", "A drill that bites and keeps grinding. 30 energy.", ItemType.active);
        cost = 30; cd = 6;
    }
    protected override OffensiveCast Launch() => CreateCast<BurrowingDrill>("Burrowing drill");
}

public sealed class BurrowingDrill : OffensiveCast
{
    static Mesh bit;
    Transform model;
    AEnemy lodged;
    Vector3 localBite;
    float nextTick, grinding;
    bool attached;
    protected override void Begin()
    {
        transform.position += new Vector3(0, .65f, -1);
        // Reuse the weapon's cached lance at its original dimensions, scaled locally.
        if (bit == null) bit = IndustrialMeshFactory.DrillLance(1.5f, .35f);
        var spinningBit = new GameObject("Can opener drill bit");
        spinningBit.transform.SetParent(transform, false);
        spinningBit.AddComponent<MeshFilter>().sharedMesh = bit;
        var renderer = spinningBit.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Bullet";
        renderer.sortingOrder = 15;
        model = spinningBit.transform;
        model.localScale = new Vector3(.24f / .35f, 1.15f / 1.5f, .24f / .35f);
        ItemSound.Cue("cannon", volume: .2f, pitch: .7f, cooldown: 0);
    }
    protected override void Step(float dt)
    {
        model.localRotation = Quaternion.Euler(0, Age * 1350, 0);
        if (!attached)
        {
            Vector3 from = transform.position + Vector3.up * 1.15f, to = from + Vector3.up * (7 * dt);
            SnapshotTargets();
            float nearest = float.PositiveInfinity;
            Vector2 bite = to;
            foreach (var enemy in Targets)
                if (Contact(enemy, from, to, .23f, out var point) && (point - (Vector2)from).sqrMagnitude < nearest)
                { lodged = enemy; bite = point; nearest = (point - (Vector2)from).sqrMagnitude; }
            transform.position = to - Vector3.up * 1.15f;
            if (lodged != null)
            {
                attached = true;
                localBite = lodged.transform.InverseTransformPoint(new Vector3(bite.x, bite.y, -1));
                ItemSound.Cue("itemChargeRelease", volume: .18f, pitch: .65f, cooldown: 0);
            }
            else if (Age >= 3 || to.y > Camera.UpLimit + 1) Finish();
            return;
        }
        // A phase break, destroyed weakpoint or diving submarine ends the bite.
        if (lodged == null || !lodged.CanReceiveDamage || !lodged.CanBeTargeted) { Finish(); return; }
        var contactPoint = lodged.transform.TransformPoint(localBite);
        transform.position = contactPoint - Vector3.up * 1.05f;
        grinding += dt;
        if (grinding >= nextTick)
        {
            nextTick = grinding + .2f;
            CombatHit.Apply(lodged, GameRules.playerShootDamage * 1.1f, contactPoint, Vector2.up, generation: 1);
            ItemSpark.Emit(contactPoint, new Color(.85f, .63f, .3f), 2, .08f);
            ItemSound.Cue("fan", volume: .075f, pitch: 1.6f, cooldown: .18f);
        }
        if (grinding >= 2.4f) Finish();
    }
}
