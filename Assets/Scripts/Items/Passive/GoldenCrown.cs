using UnityEngine;

// Golden Crown: worn only while the hull is whole. At full health every hit lands twice as
// hard (three times with two crowns). A hit knocks the crown off; it returns at full health.
[UnityEngine.Scripting.Preserve]
public sealed class GoldenCrown : AItem
{
    void Awake()
    {
        SetData("Golden Crown", "Heavy is the head", "At full health, your hits deal double damage.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.GoldenCrown);
        if (s.GetComponent<CrownBearer>() == null)
            s.gameObject.AddComponent<CrownBearer>();
        base.OnPickup();
    }

    /// <summary>The damage multiplier the crown grants right now.</summary>
    public static float Scale(CombatUpgrades upgrades, Shipcontroller ship)
    {
        int crowns = upgrades.Count(UpgradeKind.GoldenCrown);
        return crowns > 0 && ship != null && ship.health >= ship.maxHealth ? 1 + crowns : 1;
    }
}

// The crown model floats over the cockpit while it is worn, tumbles away when a hit knocks it
// off, and drops back on with a glint once the hull is whole again.
public sealed class CrownBearer : MonoBehaviour
{
    const float Width = .42f, Height = .62f, FallTime = .7f;
    static readonly Color Gold = new Color(1, .8f, .26f);
    Shipcontroller ship;
    Transform crown;
    Vector3 size, fallFrom, fallVelocity;
    float fall = -1, spin, settle;
    bool worn;

    void Start()
    {
        ship = GetComponent<Shipcontroller>();
        var go = new GameObject("Golden crown");
        var mesh = IndustrialMeshFactory.ExpansionItem(nameof(GoldenCrown));
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(nameof(GoldenCrown));
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 2;
        crown = go.transform;
        size = Vector3.one * (Width / Mathf.Max(.01f, mesh.bounds.size.x));
        crown.localScale = size;
        worn = Worn;
        crown.gameObject.SetActive(worn);
    }

    void OnDestroy()
    {
        if (crown != null)
            Destroy(crown.gameObject);
    }

    bool Worn => CombatUpgrades.Current != null && GoldenCrown.Scale(CombatUpgrades.Current, ship) > 1 && ship.health > 0;

    void LateUpdate()
    {
        if (ship == null || crown == null)
            return;
        float dt = Time.deltaTime;
        bool now = Worn;
        if (worn && !now)
        {
            // Knocked off: it flies up and away, tumbling, then fades.
            fall = 0;
            fallFrom = crown.position;
            fallVelocity = new Vector3(Random.Range(-1.6f, 1.6f), 2.4f, 0);
            ItemSound.Cue("itemRicochet", volume: .08f, pitch: .8f);
        }
        else if (!worn && now)
        {
            fall = -1;
            settle = 0;
            crown.gameObject.SetActive(true);
            ItemSpark.Emit(ship.transform.position + Vector3.up * Height, Gold, 8, .1f);
            ItemSound.Cue("ready", volume: .1f, pitch: 1.3f);
            RunInventory.Ping(UpgradeKind.GoldenCrown);
        }
        worn = now;
        spin += dt * 70;
        if (fall >= 0)
        {
            fall += dt;
            fallVelocity += Vector3.down * 9 * dt;
            crown.position += fallVelocity * dt;
            crown.rotation = Quaternion.Euler(fall * 400, spin, fall * 260);
            crown.localScale = size * Mathf.Clamp01(1 - fall / FallTime);
            if (fall >= FallTime)
            {
                fall = -1;
                crown.gameObject.SetActive(false);
            }
            return;
        }
        if (!worn)
            return;
        // Dropping back on: a short fall onto its resting place, then a gentle bob.
        settle = Mathf.Min(1, settle + dt * 4);
        float drop = (1 - settle) * (1 - settle) * .6f;
        crown.position = ship.transform.position + new Vector3(0, Height + drop + .03f * Mathf.Sin(Time.time * 3), -.3f);
        crown.rotation = Quaternion.Euler(-15, spin, 0);
        crown.localScale = size;
    }
}
