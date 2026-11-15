using UnityEngine;

// Hector trails beside the ship with little hops. The first hostile round to reach him is
// stopped dead; he flips over as toast (dimmed, upside down) and rights himself after a rest.
public sealed class HectorPet : ACompanion
{
    const float Width = .46f, Reach = .27f, Rest = 5, Follow = 7, HopHeight = .07f;
    static readonly Color Stone = new Color(.8f, .8f, .84f);
    static readonly Color Rim = new Color(.63f, .22f, .86f);
    static readonly Color Toasted = new Color(.42f, .4f, .47f);
    Shipcontroller owner;
    Transform model;
    MeshRenderer body;
    MaterialPropertyBlock tint;
    Vector3 position, size;
    float toast, hop, flip, lean;
    int index;

    public bool Ready => toast <= 0;

    public static void Attach(Shipcontroller ship)
    {
        var go = new GameObject("Hector");
        go.transform.SetParent(ship.transform, false);
        var pet = go.AddComponent<HectorPet>();
        pet.owner = ship;
        pet.type = CompanionType.sides;
        pet.index = ship.GetComponentsInChildren<HectorPet>().Length - 1;
        pet.Build();
        pet.position = pet.Home();
        go.transform.position = pet.position;
    }

    void Build()
    {
        var visual = new GameObject("Hector model");
        visual.transform.SetParent(transform, false);
        var mesh = IndustrialMeshFactory.ExpansionItem(nameof(Hector));
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        body = visual.AddComponent<MeshRenderer>();
        body.sharedMaterial = EverydayItemMaterials.For(nameof(Hector));
        body.sortingLayerName = "Player";
        visual.transform.localScale = Vector3.one * (Width / (mesh.bounds.size.x * Mathf.Max(.01f, owner.transform.lossyScale.x)));
        model = visual.transform;
        size = model.localScale;
        tint = new MaterialPropertyBlock();
    }

    // First Hector sits off the left wing, the second off the right, later ones a step lower.
    Vector3 Home()
    {
        float side = index % 2 == 0 ? -1 : 1;
        float row = index / 2;
        return owner.transform.position + new Vector3(side * (.62f + row * .2f), -.42f - row * .34f, -.06f);
    }

    void LateUpdate()
    {
        if (owner == null || owner.health <= 0)
        {
            Destroy(gameObject);
            return;
        }
        float dt = GameClock.ActivePlay ? GameClock.PlayerDelta : 0;
        var home = Home();
        // While toasted he lags, so he visibly drops behind until he recovers.
        float pull = 1 - Mathf.Exp(-(Ready ? Follow : 2.5f) * dt);
        var previous = position;
        position = Vector3.Lerp(position, home, pull);
        position.x = Mathf.Clamp(position.x, Camera.LeftLimit, Camera.RightLimit);
        float speed = dt > 0 ? ((Vector2)(position - previous)).magnitude / dt : 0;
        if (Ready)
            hop += dt * Mathf.Lerp(4, 13, Mathf.Clamp01(speed / 4));
        lean = Mathf.Lerp(lean, Mathf.Clamp((position.x - previous.x) / Mathf.Max(dt, .001f) * -4, -14, 14), 1 - Mathf.Exp(-10 * dt));
        transform.position = position;
        Pose(dt);
        if (dt <= 0)
            return;
        if (!Ready)
        {
            toast -= dt;
            if (toast <= 0)
                Recover();
            return;
        }
        Guard();
    }

    void Pose(float dt)
    {
        float bounce = Mathf.Abs(Mathf.Sin(hop));
        flip = Mathf.MoveTowards(flip, Ready ? 0 : 180, 900 * dt);
        model.localPosition = new Vector3(0, Ready ? bounce * HopHeight : -.03f, 0);
        model.localRotation = Quaternion.Euler(0, 0, lean + flip);
        // A soft squash on landing keeps the hops weighty.
        float squash = Ready ? 1 - .08f * (1 - bounce) : 1;
        model.localScale = Vector3.Scale(size, new Vector3(2 - squash, squash, 1));
        tint.SetColor("_Tint", Ready ? Color.white : Toasted);
        body.SetPropertyBlock(tint);
    }

    void Guard()
    {
        Vector2 at = transform.position;
        float reach = Reach * Reach;
        CombatRound blocked = null;
        foreach (var round in CombatRound.Live)
            if (BulletReflection.CanReflect(round) && ((Vector2)round.transform.position - at).sqrMagnitude <= reach)
            {
                blocked = round;
                break;
            }
        if (blocked == null)
            return;
        CombatUpgrades.Current?.Block();
        blocked.Vanish();
        toast = Rest;
        RunInventory.Ping(this);
        ItemSpark.Emit(transform.position, Stone, 6, .1f);
        ItemProcVfx.Emit(ItemProcShape.Counter, transform.position, Rim, .4f);
        ItemSound.Cue("itemCounter", volume: .09f, pitch: .62f, cooldown: .2f);
    }

    void Recover()
    {
        toast = 0;
        hop = 0;
        ItemSpark.Emit(transform.position + Vector3.up * .12f, Rim, 4, .07f);
        ItemSound.Cue("ready", volume: .05f, pitch: 1.35f, cooldown: .3f);
    }
}
