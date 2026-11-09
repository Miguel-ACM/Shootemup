using UnityEngine;
using System.Collections.Generic;

// Ball of Yarn: a slow orbiter trailing a short thread. Hostile rounds crossing the thread
// are erased; the ball itself carries the pilot's on-hit coatings into enemies it touches.
public sealed class YarnBall : ACompanion
{
    const int Points = 16, Remembered = 6;
    const float Radius = 1.35f, Turn = 1.1f, Sample = .035f, ThreadReach = .1f, BallReach = .3f, Rehit = .4f;
    static readonly List<YarnBall> live = new List<YarnBall>();
    static readonly List<CombatRound> caught = new List<CombatRound>(32);
    Shipcontroller owner;
    Transform model;
    LineRenderer thread;
    readonly Vector3[] trail = new Vector3[Points];
    readonly AEnemy[] recent = new AEnemy[Remembered];
    readonly float[] recentUntil = new float[Remembered];
    Vector3 worldPosition;
    float angle, phase, sampleClock;
    int newest;

    public static YarnBall Attach(Shipcontroller ship)
    {
        var go = new GameObject("Ball of yarn");
        go.transform.SetParent(ship.transform, false);
        var yarn = go.AddComponent<YarnBall>();
        yarn.owner = ship;
        yarn.type = CompanionType.orbital;
        yarn.angle = Time.time * Turn;
        var body = new GameObject("Yarn model");
        yarn.model = body.transform;
        yarn.model.SetParent(go.transform, false);
        yarn.model.localScale = Vector3.one * (.55f / Mathf.Max(.01f, ship.transform.lossyScale.x));
        body.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ExpansionItem(nameof(BallOfYarn));
        var renderer = body.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(nameof(BallOfYarn));
        renderer.sortingLayerName = "Player";
        yarn.thread = CombatVisuals.Line(go.transform, new Color(.95f, .45f, .6f, .9f), .035f);
        yarn.thread.positionCount = Points + 1;
        yarn.thread.endWidth = .012f;
        yarn.thread.sortingLayerName = "Player";
        yarn.thread.sortingOrder = -3;
        yarn.worldPosition = ship.transform.position;
        yarn.Place();
        for (int i = 0; i < Points; i++)
            yarn.trail[i] = yarn.worldPosition;
        return yarn;
    }

    void OnEnable()
    {
        live.Add(this);
    }

    void OnDisable()
    {
        live.Remove(this);
    }

    void Place()
    {
        int index = Mathf.Max(0, live.IndexOf(this));
        float spread = index * Mathf.PI * 2 / Mathf.Max(1, live.Count);
        phase = Mathf.LerpAngle(phase * Mathf.Rad2Deg, spread * Mathf.Rad2Deg, .05f) * Mathf.Deg2Rad;
        var ship = owner.transform.position;
        worldPosition = new Vector3(ship.x + Mathf.Cos(angle + phase) * Radius, ship.y + Mathf.Sin(angle + phase) * Radius, ship.z - .1f);
    }

    void LateUpdate()
    {
        if (owner == null)
            return;
        float dt = Time.deltaTime;
        if (WorldTimeStop.Frozen || dt <= 0)
        {
            transform.position = worldPosition;
            return;
        }
        angle += Turn * dt;
        Place();
        transform.position = worldPosition;
        model.Rotate(0, 0, -140 * dt, Space.Self);
        sampleClock += dt;
        if (sampleClock >= Sample)
        {
            sampleClock = 0;
            newest = (newest + 1) % Points;
            trail[newest] = worldPosition;
        }
        thread.SetPosition(0, worldPosition);
        for (int i = 0; i < Points; i++)
            thread.SetPosition(i + 1, trail[(newest - i + Points) % Points]);
        if (!GameClock.ActivePlay || owner.health <= 0)
            return;
        EraseCrossingRounds();
        TouchEnemies();
    }

    void EraseCrossingRounds()
    {
        Vector2 lo = worldPosition, hi = worldPosition;
        for (int i = 0; i < Points; i++)
        {
            lo = Vector2.Min(lo, trail[i]);
            hi = Vector2.Max(hi, trail[i]);
        }
        lo -= Vector2.one * .4f;
        hi += Vector2.one * .4f;
        caught.Clear();
        foreach (var round in CombatRound.Live)
        {
            if (round == null || round is PulseLaser)
                continue;
            Vector2 p = round.transform.position;
            if (p.x < lo.x || p.y < lo.y || p.x > hi.x || p.y > hi.y)
                continue;
            float reach = ThreadReach + Mathf.Min(.3f, round.BorderReach * .6f);
            if (NearThread(p, reach * reach))
                caught.Add(round);
        }
        foreach (var round in caught)
        {
            if (round == null || !round.isActiveAndEnabled)
                continue;
            CombatUpgrades.Current?.Block();
            round.OnTrigger();
        }
    }

    bool NearThread(Vector2 p, float reach)
    {
        Vector2 a = worldPosition;
        for (int i = 0; i < Points; i++)
        {
            Vector2 b = trail[(newest - i + Points) % Points];
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
            if ((a + ab * t - p).sqrMagnitude < reach)
                return true;
            a = b;
        }
        return false;
    }

    void TouchEnemies()
    {
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanReceiveDamage || RecentlyHit(enemy))
                continue;
            enemy.ClosestDamagePoint(worldPosition, out Vector2 point);
            if ((point - (Vector2)worldPosition).sqrMagnitude > BallReach * BallReach)
                continue;
            Remember(enemy);
            owner.PlayHitSound("hit1");
            CombatHit.Apply(enemy, GameRules.playerShootDamage * .6f, point, (point - (Vector2)owner.transform.position).normalized,
                generation: 1, companion: true);
            return; // One contact per frame keeps the enemy set unchanged while it is enumerated.
        }
    }

    bool RecentlyHit(AEnemy enemy)
    {
        for (int i = 0; i < Remembered; i++)
            if (recent[i] == enemy && Time.time < recentUntil[i])
                return true;
        return false;
    }

    void Remember(AEnemy enemy)
    {
        int slot = 0;
        for (int i = 1; i < Remembered; i++)
            if (recentUntil[i] < recentUntil[slot])
                slot = i;
        recent[slot] = enemy;
        recentUntil[slot] = Time.time + Rehit;
    }
}
