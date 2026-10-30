using UnityEngine;

// Blank Bullet's clearing shield, thrown: it spins up the screen erasing every round it
// touches and has no duration; it is gone only once it leaves the arena.
[UnityEngine.Scripting.Preserve]
public sealed class ThrowableShield : AActiveItem
{
    void Awake()
    {
        SetData("Throwable Shield", "Catch!", "Throw a shield that clears its path.", ItemType.active);
        cost = 25;
        cd = 1.5f;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        ThrownShield.Throw(s.transform.position + Vector3.up * .6f);
        ItemSound.Play("roll", .16f, .8f);
        lastCD = 0;
        return mana - cost;
    }
}

public sealed class ThrownShield : MonoBehaviour
{
    const float Speed = 7, Radius = .85f;
    static readonly Color Field = new Color(.35f, .95f, .85f);
    readonly System.Collections.Generic.List<CombatRound> touched = new System.Collections.Generic.List<CombatRound>();
    Transform model;
    LineRenderer ring;
    float spin;

    public static void Throw(Vector3 from)
    {
        var go = new GameObject("Thrown shield");
        go.transform.position = new Vector3(from.x, from.y, -1);
        go.AddComponent<ThrownShield>();
    }

    void Start()
    {
        var visual = new GameObject("Shield model");
        visual.transform.SetParent(transform, false);
        var mesh = IndustrialMeshFactory.ExpansionItem("ThrowableShield");
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For("ThrowableShield");
        renderer.sortingLayerName = "Bullet";
        renderer.sortingOrder = 17;
        visual.transform.localScale = Vector3.one * (Radius * 2 / Mathf.Max(.01f, mesh.bounds.size.x));
        model = visual.transform;
        ring = CombatVisuals.Line(transform, new Color(Field.r, Field.g, Field.b, .55f), .05f);
        PSXImpactEffects.Burst(transform.position, Field, true);
    }

    void Update()
    {
        if (Time.deltaTime <= 0)
            return;
        transform.position += Vector3.up * Speed * Time.deltaTime;
        spin += Time.deltaTime * 720;
        model.localRotation = Quaternion.AngleAxis(spin, Vector3.forward) * Quaternion.AngleAxis(25, Vector3.right);
        CombatVisuals.Ring(ring, transform.position, Radius * (1.05f + .05f * Mathf.Sin(spin * .05f)));
        touched.Clear();
        foreach (var round in CombatRound.Live)
            if (BulletReflection.CanReflect(round) && ((Vector2)(round.transform.position - transform.position)).sqrMagnitude <= Radius * Radius)
                touched.Add(round);
        foreach (var round in touched)
        {
            CombatUpgrades.Current?.Block();
            round.Vanish();
        }
        if (transform.position.y > Camera.UpLimit + Radius + .5f)
            Destroy(gameObject);
    }
}
