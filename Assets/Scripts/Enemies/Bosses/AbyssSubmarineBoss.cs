using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class AbyssSubmarineBoss : DistrictBoss
{
    float cycleClock, nextWake;
    int lastCycle = -1;
    Vector3 emerge, descent;
    bool eruptionA, eruptionB, eruptionC;
    LineRenderer wake;
    Collider2D hit;
    MeshRenderer model;
    SpriteRenderer sprite;
    MaterialPropertyBlock ink;
    void Awake()
    {
        ink = new MaterialPropertyBlock();
    }

    [EnemyTunable("Dive / Cycle seconds", 15, 22)] public float cycleSeconds = 16;
    [EnemyTunable("Dive / Eruption crown", 12, 36)] public int crownCount = 20;
    int CrownSize => (crownCount + (phaseTwo ? 4 : 0));
    public bool Submerged
    {
        get; private set;
    }

    public int EmergenceBursts
    {
        get; private set;
    }

    public int ChargeTrails
    {
        get; private set;
    }

    public override bool CanBeTargeted => !Submerged && health > 0;
    protected override void Configure()
    {
        hull = 53;
        hullScale = .98f;
        health = 2600;
        attackInterval = (.68f);
        nextAttack = 1.7f;
    }

    protected override void Start()
    {
        base.Start();
        var old = GetComponent<CircleCollider2D>();
        old.enabled = false;
        Destroy(old);
        var capsule = gameObject.AddComponent<CapsuleCollider2D>();
        capsule.isTrigger = true;
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = new Vector2(2.02f, 4.12f) * hullScale;
        hit = capsule;
        GetComponent<CombatHullVisual>().enabled = false;
        model = Hull.GetComponent<MeshRenderer>();
        sprite = GetComponent<SpriteRenderer>();
        wake = CombatVisuals.Line(transform, LevelProjectilePalette.Accent(LevelId.Underwater), .055f);
        wake.enabled = false;
        foreach (int side in new[] { -1, 1 })
            foreach (int lane in new[] { -1, 1 })
                firingCue.Register(Hull, new Vector3(side * 1.02f, .3f + lane * .35f, -.3f));
    }

    protected override void MoveBoss(float dt)
    {
        if (hit == null)
            return;
        cycleClock += dt * (phaseTwo ? 1.10f : 1);
        float t = Mathf.Repeat(cycleClock, cycleSeconds);
        int cycle = Mathf.FloorToInt(cycleClock / cycleSeconds);
        if (cycle != lastCycle)
        {
            lastCycle = cycle;
            eruptionA = eruptionB = eruptionC = false;
        }
        Submerged = t >= 3.5f && t < 6.4f;
        hit.enabled = !Submerged;
        float opacity = 1;
        if (t < 3.5f)
        {
            station = new Vector3(Mathf.Sin(motionAge * .65f) * 2.1f, Camera.UpLimit - 2.65f, 1);
            transform.position = Vector3.MoveTowards(transform.position, station, dt * 5);
            emerge = new Vector3(Mathf.Clamp(player != null ? player.position.x : 0, -2.8f, 2.8f), Mathf.Clamp((player != null ? player.position.y : -2) + 2, -3.5f, 2.3f), 1);
            descent = transform.position;
        }
        else if (t < 4.4f)
        {
            float p = (t - 3.5f) / .9f;
            transform.position = descent + new Vector3(0, -p * 1.6f, p * 28);
            opacity = 1 - p;
        }
        else if (t < 5.5f)
        {
            transform.position = emerge + Vector3.forward * 28;
            opacity = 0;
        }
        else if (t < 6.4f)
        {
            float p = (t - 5.5f) / .9f;
            transform.position = emerge + Vector3.forward * (28 * (1 - p));
            opacity = p;
        }
        else if (t < 8.5f)
        {
            transform.position = emerge;
            if (!eruptionA)
            {
                eruptionA = true;
                Crown(0, 2.2f);
            }
            if (t >= 7.05f && !eruptionB)
            {
                eruptionB = true;
                Crown(180f / CrownSize, 2.6f);
            }
            if (t >= 7.7f && !eruptionC)
            {
                eruptionC = true;
                Crown(90f / CrownSize, 3.0f);
            }
        }
        else if (t < 10.3f)
        {
            var target = new Vector3(emerge.x, Camera.UpLimit - 2.3f, 1);
            transform.position = Vector3.MoveTowards(transform.position, target, dt * 12);
            descent = transform.position;
            nextWake = 0;
        }
        else if (t < 11.35f)
        {
            descent = transform.position;
        }
        else if (t < 13.1f)
        {
            transform.position = descent + Vector3.down * ((t - 11.35f) * 14);
            if (age >= nextWake)
            {
                nextWake = age + (phaseTwo ? .15f : .19f);
                ChargeTrails++;
                for (int side = -1; side <= 1; side += 2)
                    for (int lane = -1; lane <= 1; lane++)
                    {
                        var port = Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .35f, -.3f));
                        var direction = CombatVisuals.Rotate(Vector2.right * side, lane * 23);
                        if (Difficulty.Hard)
                            HardWake(port, direction, 2.3f + lane * .16f, side);
                        else
                            Fire<SuspendedRound>(Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .35f, -.3f)), CombatVisuals.Rotate(Vector2.right * side, lane * 23), 1.8f + lane * .12f, .21f);
                    }
            }
            if (player != null && Mathf.Abs(player.position.x - transform.position.x) < .65f && Mathf.Abs(player.position.y - transform.position.y) < 1.5f)
                player.SendMessage("ReceiveHazardDamage", 16f, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            // Retreat completely below the viewport, then enter again from above.
            if (transform.position.y < Camera.DownLimit - 2.5f)
                transform.position = new Vector3(-emerge.x, Camera.UpLimit + 3.5f, 1);
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(-emerge.x, Camera.UpLimit - 2.65f, 1), dt * 5.5f);
        }
        wake.enabled = t >= 4.4f && t < 6.4f || t >= 10.3f && t < 11.35f;
        if (t >= 4.4f && t < 6.4f)
            CombatVisuals.Ring(wake, new Vector3(emerge.x, emerge.y, -.3f), Mathf.Lerp(1.45f, .65f, Mathf.InverseLerp(4.4f, 6.4f, t)));
        else if (wake.enabled)
        {
            wake.positionCount = 2;
            wake.SetPosition(0, descent + Vector3.back * .3f);
            wake.SetPosition(1, new Vector3(descent.x, Camera.DownLimit, -.3f));
        }
        Hull.localScale = Vector3.one * hullScale * Mathf.Lerp(.60f, 1, opacity);
        ink.SetColor("_Tint", new Color(sprite.color.r, sprite.color.g, sprite.color.b, opacity * sprite.color.a));
        model.SetPropertyBlock(ink);
    }

    void Crown(float offset, float speed)
    {
        if (Difficulty.Hard)
        {
            HardCrown(offset, speed);
            return;
        }
        EmergenceBursts++;
        PSXImpactEffects.Burst(transform.position, new Color(.42f, .83f, .94f), true);
        for (int i = 0; i < CrownSize; i++)
        {
            int side = i % 2 == 0 ? -1 : 1, lane = (i / 2) % 3 - 1;
            var port = Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .35f, -.3f));
            Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, offset + i * 360f / CrownSize), speed, .26f);
        }
    }

    protected override void Attack()
    {
        if (Difficulty.Hard)
        {
            AttackHard();
            return;
        }
        float t = Mathf.Repeat(cycleClock, cycleSeconds);
        if (Submerged || t >= 8.5f)
            return;
        if (t >= 3.5f)
            return;
        if (volley % 4 != 3)
            PetalFan<NeedleRound>(Hull.TransformPoint(new Vector3(0, -1.65f, -.5f)), CombatVisuals.Rotate(Aim, (volley % 3 - 1) * 9), (phaseTwo ? 7 : 5), 19, 3.6f, .21f);
        else
            for (int side = -1; side <= 1; side += 2)
                PetalFan<SimpleRound>(Hull.TransformPoint(new Vector3(side * .65f, -.6f, -.4f)), CombatVisuals.Rotate(Vector2.down, side * 42), (5), 24, 2.3f, .28f);
    }

    protected override void OnPhaseTwo()
    {
        cycleClock = 0;
        lastCycle = -1;
        volley = 0;
        nextAttack = age + 1.5f;
    }
    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        float t = Mathf.Repeat(cycleClock, cycleSeconds);
        if (t < 3.5f)
        {
            if ((volley + 1) % 4 != 3)
                Cue(Muzzle(), seconds);
            else
            {
                Cue(Muzzle(1), seconds);
                Cue(Muzzle(2), seconds);
            }
        }
        else if (t >= 5.9f && t < 7.7f || t >= 10.85f && t < 13.1f)
        {
            float nextCrown = t < 6.4f ? 6.4f : t < 7.05f ? 7.05f : 7.7f;
            float warning = t < 7.7f ? (nextCrown - t) / (phaseTwo ? 1.1f : 1) : (11.35f - t) / (phaseTwo ? 1.1f : 1);
            if (t >= 11.35f)
                warning = Mathf.Max(0, (nextWake - age) / LevelRun.EnemyTempoScale);
            for (int side = -1; side <= 1; side += 2)
                for (int lane = -1; lane <= 1; lane++)
                    Cue(Hull.TransformPoint(new Vector3(side * 1.02f, .3f + lane * .35f, -.3f)), warning);
        }
    }

    public override void damage(float amount)
    {
        if (!Submerged)
            base.damage(amount);
    }
}
