using UnityEngine;

// Hive: every kill lets a bee out (one per copy). It flies at the nearest enemy and stings it;
// a sting that kills lets out another bee, so swarms chain through a formation. At most
// sixteen bees fly at once and each gives up after a few seconds.
[UnityEngine.Scripting.Preserve]
public sealed class Hive : AItem
{
    void Awake()
    {
        SetData("Hive", "Busy busy", "Every kill releases a bee that stings the next enemy.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Hive);
        base.OnPickup();
    }
}

public sealed class HiveBee : MonoBehaviour
{
    const int Cap = 16;
    const float Speed = 7.5f, Accel = 34, Life = 3.5f, Reach = .22f, Sting = 4, Size = .26f, Seek = 7;
    static readonly Color Pollen = new Color(1, .82f, .2f);
    static int live;
    Transform body;
    Vector3 size;
    Vector2 velocity;
    AEnemy target, home;
    float age;

    public static void Release(AEnemy dead, int copies)
    {
        if (dead == null)
            return;
        for (int i = 0; i < copies && live < Cap; i++)
            Spawn(dead.transform.position, dead);
    }

    static void Spawn(Vector3 at, AEnemy dead)
    {
        var mesh = IndustrialMeshFactory.TerraModel(nameof(HiveBee));
        if (mesh == null)
            return;
        var go = new GameObject("Hive bee");
        go.transform.position = new Vector3(at.x, at.y, -.6f);
        var bee = go.AddComponent<HiveBee>();
        bee.home = dead;
        var visual = new GameObject("Bee model");
        visual.transform.SetParent(go.transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = IndustrialMeshFactory.TerraMaterial(nameof(HiveBee)) ?? CombatMaterials.Fleet;
        renderer.sortingLayerName = "Player";
        bee.body = visual.transform;
        bee.size = Vector3.one * (Size / Mathf.Max(.01f, mesh.bounds.size.y));
        bee.body.localScale = bee.size;
        bee.velocity = Random.insideUnitCircle.normalized * 3.5f;
        ItemSound.Cue("itemGum", "popcorn", .03f, 1.8f, .12f);
    }

    void OnEnable()
    {
        live++;
    }

    void OnDisable()
    {
        live--;
    }

    void Update()
    {
        if (GameRules.playerDisabled || GameOverState.Active)
        {
            Destroy(gameObject);
            return;
        }
        float dt = GameClock.ActivePlay ? GameClock.PlayerDelta : 0;
        if (dt <= 0)
            return;
        age += dt;
        if (age >= Life)
        {
            Fizzle();
            return;
        }
        if (target == null || !target.CanBeTargeted || target == home)
            target = CombatTargets.Nearest(transform.position, null, Seek);
        if (target == home)
            target = null;
        Vector2 p = transform.position;
        Vector2 wanted;
        if (target != null)
        {
            target.ClosestDamagePoint(p, out Vector2 aim);
            if ((aim - p).sqrMagnitude <= Reach * Reach)
            {
                Strike(target, aim);
                return;
            }
            wanted = (aim - p).normalized * Speed;
        }
        else
            // Nothing to sting: drift in a lazy loop until something turns up.
            wanted = CombatVisuals.Rotate(velocity.sqrMagnitude > .01f ? velocity.normalized : Vector2.up, 90 * dt * 4) * Speed * .5f;
        velocity = Vector2.MoveTowards(velocity, wanted, Accel * dt);
        transform.position += (Vector3)(velocity * dt);
        // Nose along the flight, wings blurring.
        float heading = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90;
        body.rotation = Quaternion.Euler(0, 0, heading);
        body.localScale = Vector3.Scale(size, new Vector3(1 + .12f * Mathf.Sin(age * 70), 1, 1));
    }

    void Strike(AEnemy enemy, Vector2 point)
    {
        RunInventory.Ping(UpgradeKind.Hive);
        ItemSpark.Emit(point, Pollen, 3, .07f);
        CombatHit.Apply(enemy, GameRules.playerShootDamage * Sting, point, velocity.normalized, generation: 1, companion: true);
        ItemSound.Cue("itemRicochet", volume: .03f, pitch: 1.9f, cooldown: .08f);
        Destroy(gameObject);
    }

    void Fizzle()
    {
        ItemSpark.Emit(transform.position, Pollen, 2, .05f);
        Destroy(gameObject);
    }
}
