using System.Collections.Generic;
using UnityEngine;

// Midas Touch: a kill sometimes turns the hostile rounds around it to gold. Up to eight of
// them become real coins where they were; the rest just vanish in a golden flash. The chance
// grows with luck and with extra copies.
[UnityEngine.Scripting.Preserve]
public sealed class MidasTouch : AItem
{
    public const float Chance = .3f, ExtraChance = .15f, Radius = 1.8f;
    const int MaxCoins = 8;
    static readonly Color Gold = new Color(1, .8f, .26f);
    static readonly List<CombatRound> touched = new List<CombatRound>();

    void Awake()
    {
        SetData("Midas Touch", "Everything turns to gold", "Kills can turn nearby enemy fire into coins.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Midas);
        base.OnPickup();
    }

    public static void Touch(AEnemy dead, int copies)
    {
        if (copies <= 0 || dead == null)
            return;
        float chance = Mathf.Clamp01(Chance + ExtraChance * (copies - 1) + GameRules.luck);
        if (Random.value >= chance)
            return;
        Vector2 at = dead.transform.position;
        touched.Clear();
        foreach (var round in CombatRound.Live)
            if (BulletReflection.CanReflect(round) && ((Vector2)round.transform.position - at).sqrMagnitude <= Radius * Radius)
                touched.Add(round);
        if (touched.Count == 0)
            return;
        var prefab = Resources.Load<GameObject>("Pickables/money");
        for (int i = 0; i < touched.Count; i++)
        {
            var p = touched[i].transform.position;
            ItemSpark.Emit(p, Gold, 2, .07f);
            if (i < MaxCoins && prefab != null)
            {
                var spot = new Vector3(Mathf.Clamp(p.x, Camera.LeftLimit + .25f, Camera.RightLimit - .25f),
                    Mathf.Clamp(p.y, Camera.DownLimit + .25f, Camera.UpLimit - .25f), 1);
                var coin = Object.Instantiate(prefab, spot, Quaternion.identity).GetComponent<Money>();
                coin.Launch(Random.insideUnitCircle * .8f, .25f);
            }
            touched[i].Vanish();
        }
        RunInventory.Ping(UpgradeKind.Midas);
        MidasStrike.Show(at);
        PSXFireballs.Shockwave(at, Radius * 1.3f, .45f, Gold, 2, .8f);
        ItemSound.Cue("itemSalvage", volume: .12f, pitch: 1.3f, cooldown: .15f);
    }
}

public sealed class MidasStrike : MonoBehaviour
{
    Vector3 target;
    float age, scale;

    public static void Show(Vector2 position)
    {
        var hand = new GameObject("Midas strike");
        var strike = hand.AddComponent<MidasStrike>();
        strike.target = new Vector3(position.x, position.y, -.65f);
        bool painted = GalleryShelf.TryGet(nameof(MidasTouch), out var exhibit);
        var mesh = painted ? exhibit.mesh : IndustrialMeshFactory.ExpansionItem(nameof(MidasTouch));
        hand.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = hand.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = painted ? exhibit.paint : EverydayItemMaterials.For(nameof(MidasTouch));
        renderer.sortingLayerName = "Objects";
        strike.scale = .88f / Mathf.Max(.01f, mesh.bounds.size.magnitude);
        strike.Pose(0);
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= .46f) Destroy(gameObject);
        else Pose(age);
    }

    void Pose(float t)
    {
        float strike = Mathf.SmoothStep(0, 1, t / .13f);
        float lift = Mathf.Clamp01((t - .18f) / .28f);
        transform.position = target + new Vector3(.48f, .65f) * (1 - strike + lift * .55f);
        transform.rotation = Quaternion.Euler(-16, 12, -42 + 38 * strike - 26 * lift);
        transform.localScale = Vector3.one * (scale * (1 - lift * lift));
    }
}
