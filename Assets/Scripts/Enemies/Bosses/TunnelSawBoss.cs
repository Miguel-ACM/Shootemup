using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed partial class TunnelSawBoss : DistrictBoss
{
    static readonly Unity.Profiling.ProfilerMarker burstMarker = new Unity.Profiling.ProfilerMarker("TunnelSaw.BounceBurst");
    public bool miniature;
    [EnemyTunable("Auxiliary saws / Base health", 1, 5000)] public float auxiliaryHealth = 360;
    public override bool UsesHealthBar => !miniature;
    Vector2 velocity = new Vector2(1, .72f).normalized;
    float haltUntil;
    Rect arena;
    float Radius => Hull.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * Hull.localScale.x;
    bool entered;
    int impacts;
    protected override void Configure()
    {
        hull = 20;
        hullScale = 1.25f;
        health = 1800;
        attackInterval = 1.6f;
    }

    protected override void Start()
    {
        base.Start();
        for (int i = 0; i < 8; i++)
            firingCue.Register(Hull, new Vector3(Mathf.Cos(i * Mathf.PI / 4) * .94f, Mathf.Sin(i * Mathf.PI / 4) * .94f, -.3f));
        GetComponent<CombatHullVisual>().spin = miniature ? 900 : 720;
        var camera = UnityEngine.Camera.main;
        float h = camera.orthographicSize;
        arena = new Rect(camera.transform.position.x - h * 405f / 720f, camera.transform.position.y - h, h * 810f / 720f, h * 2);
        if (miniature)
        {
            isBoss = false;
            bar = null;
            lifeDropChance = manaDropChance = moneyDropChance = 0;
            scoreOnKill = 100;
        }
    }

    protected override void MoveBoss(float dt)
    {
        if (!entered)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(0, Camera.UpLimit - 2, 1), dt * 3);
            if (transform.position.y < Camera.UpLimit - 1.9f)
            {
                entered = true;
                velocity = new Vector2(arena.xMax - Radius - transform.position.x, arena.yMin + Radius - transform.position.y).normalized;
            }
            return;
        }
        if (age < haltUntil)
            return;
        float speed = (2.2f + (1 - HealthFraction) * 1.8f) * (phaseTwo ? 1.12f : 1) * (miniature ? 1.08f : 1) * (1);
        float etaX = (velocity.x > 0 ? arena.xMax - Radius - transform.position.x : transform.position.x - arena.xMin - Radius) / Mathf.Max(.01f, Mathf.Abs(velocity.x) * speed);
        float etaY = (velocity.y > 0 ? arena.yMax - Radius - transform.position.y : transform.position.y - arena.yMin - Radius) / Mathf.Max(.01f, Mathf.Abs(velocity.y) * speed);
        firingCue.Warn(Mathf.Min(etaX, etaY));
        var p = transform.position + (Vector3)velocity * speed * dt;
        float r = Radius;
        Vector2 inward = Vector2.zero;
        if ((p.x < arena.xMin + r && velocity.x < 0) || (p.x > arena.xMax - r && velocity.x > 0))
        {
            velocity.x = -velocity.x;
            inward.x = Mathf.Sign(velocity.x);
        }
        if ((p.y < arena.yMin + r && velocity.y < 0) || (p.y > arena.yMax - r && velocity.y > 0))
        {
            velocity.y = -velocity.y;
            inward.y = Mathf.Sign(velocity.y);
        }
        p.x = Mathf.Clamp(p.x, arena.xMin + r, arena.xMax - r);
        p.y = Mathf.Clamp(p.y, arena.yMin + r, arena.yMax - r);
        transform.position = p;
        if (inward.sqrMagnitude > 0)
            ImpactBurst(inward.normalized);
        if (player != null && Vector2.Distance(player.position, p) < r)
            player.SendMessage("ReceiveHazardDamage", 14f, SendMessageOptions.DontRequireReceiver);
    }

    protected override bool PhaseTwoReady => !miniature && HealthFraction <= .5f;
}
