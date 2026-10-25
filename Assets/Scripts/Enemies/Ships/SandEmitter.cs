using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SandEmitter : DistrictEnemy
{
    Transform ground;
    Vector3 groundPoint;
    SandEmergenceEffects sand;
    MeshRenderer hullRenderer;
    MaterialPropertyBlock tint;
    float anchoredAt, rise;
    bool exposed;
    public bool Grounded => ground != null;
    public Transform GroundSection => ground;
    public Vector3 GroundPoint => groundPoint;
    public override bool CanBeTargeted => health > 0 && Grounded && exposed && rise > .65f;
    protected override void Configure()
    {
        hull = 30;
        hullScale = .63f;
        health = 40;
        attackInterval = 1.3f;
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled = false;
        Hull.localRotation = Quaternion.identity;
        hullRenderer = Hull.GetComponent<MeshRenderer>();
        hullRenderer.sortingLayerName = "Background";
        hullRenderer.sortingOrder = -35;
        hullRenderer.enabled = false;
        tint = new MaterialPropertyBlock();
        sand = gameObject.AddComponent<SandEmergenceEffects>();
        GetComponent<Collider2D>().enabled = false;
    }

    protected override void Update()
    {
        var flight = movement as FormationFlight;
        float progress = flight != null ? flight.Progress : age / 6;
        if (!Grounded && progress > .13f && progress < .65f)
        {
            bool Clear(Vector3 p)
            {
                foreach (var enemy in Live)
                    if (enemy != this && enemy is SandEmitter emitter && emitter.Grounded && Vector2.Distance(p, enemy.transform.position) < 1.8f)
                        return false;
                return true;
            }
            if (LevelEnvironment.TryDesertGround(transform.position + Vector3.up * .65f, .90f, Clear, out ground, out groundPoint))
            {
                anchoredAt = age;
                flight?.AnchorToGround(ground, groundPoint);
                sand.Initialize(ground.TransformPoint(groundPoint));
                sand.Burst(12, .45f);
            }
        }
        bool close = player != null && Vector2.Distance(player.position, transform.position) < 2.4f;
        bool next = Grounded && age - anchoredAt > .55f && progress > .24f && progress < .80f && !close && !GameRules.playerDisabled;
        if (next != exposed)
            sand.Burst(next ? 38 : 24, next ? 1 : .7f);
        exposed = next;
        rise = Mathf.MoveTowards(rise, exposed ? 1 : 0, Time.deltaTime * 3.8f);
        GetComponent<Collider2D>().enabled = CanBeTargeted;
        base.Update();
        if (flight == null && age > 6)
            Depart();
    }

    void LateUpdate()
    {
        if (!Grounded)
            return;
        var surface = ground.TransformPoint(groundPoint);
        transform.position = new Vector3(surface.x, surface.y, 1);
        // Keep its footprint fixed. The terrain depth buffer reveals the rising hull from below the sand.
        Hull.position = surface + new Vector3(0, rise * .07f, .45f * (1 - rise) - .10f * rise);
        Hull.rotation = Quaternion.identity;
        hullRenderer.enabled = true;
        tint.SetColor("_Tint", GetComponent<SpriteRenderer>().color);
        hullRenderer.SetPropertyBlock(tint);
        sand.Place(surface);
    }

    protected override void Attack()
    {
        if (!CanBeTargeted || CombatRound.Live.Count + 14 > CombatRound.Limit)
            return;
        for (int i = 0; i < 14; i++)
        {
            Vector2 direction = CombatVisuals.Rotate(Vector2.down, i * 360f / 14 + volley * 12.857f);
            Fire<SimpleRound>(transform.position + (Vector3)direction * .52f, direction, 3.7f, .14f);
        }
    }

    public override void damage(float amount)
    {
        if (CanBeTargeted)
            base.damage(amount);
    }
}

public static partial class IndustrialMeshFactory
{
    static void BuildSandEmitter(Builder b)
    {
        b.Drum(Vector3.zero, .70f, .18f, 11, 12, 6);
        b.Drum(new Vector3(0, 0, -.13f), .58f, .13f, 13, 12, 11);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var n = new Vector2(-d.y, d.x);
            b.Prism(new[] { d * .42f + n * .13f, d * .73f + n * .11f, d * .82f, d * .73f - n * .11f, d * .42f - n * .13f }, .13f, 6, new Vector3(0, 0, -.14f), 11);
            b.Drum((Vector3)(d * .52f) + Vector3.back * .25f, .085f, .10f, 11, 6);
        }
        b.Drum(new Vector3(0, 0, -.25f), .32f, .15f, 1, 8, 6);
        b.Drum(new Vector3(0, 0, -.35f), .17f, .06f, 3, 8, 11);
    }
}
