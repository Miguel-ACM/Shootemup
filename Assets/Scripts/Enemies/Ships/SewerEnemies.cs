using System.Collections.Generic;
using UnityEngine;

// A flock has an authored entrance and a bounded pursuit, then drains away at the bottom.
public sealed class PlagueMote : DistrictEnemy
{
    static readonly HashSet<PlagueMote> flock = new HashSet<PlagueMote>();
    readonly Transform[] tail = new Transform[3];
    Vector2 velocity = Vector2.down * 2.5f;
    Shipcontroller pilot;
    bool eventSpawn;
    public override bool HoldsWave => !eventSpawn;
    public static int Population => flock.Count;

    protected override void Configure() { health = 5; hullScale = .24f; }
    protected override void Attack() { }

    public static void Spill(Vector2 entry, int count)
    {
        for (int i = 0; i < count && Population < 56; i++)
        {
            var offset = new Vector2((i % 4 - 1.5f) * .33f, i / 4 * .40f);
            var mote = (PlagueMote)Spawn(typeof(PlagueMote), entry + offset);
            mote.eventSpawn = true;
        }
    }

    protected override void OnEnable() { base.OnEnable(); flock.Add(this); }
    protected override void OnDisable() { flock.Remove(this); base.OnDisable(); }

    protected override void Start()
    {
        base.Start();
        pilot = player != null ? player.GetComponent<Shipcontroller>() : null;
        scoreOnKill = 12;
        manaDropChance = moneyDropChance = 0;
        var parent = Hull;
        for (int i = 0; i < tail.Length; i++)
        {
            var segment = new GameObject("Swimming tail").transform;
            segment.SetParent(parent, false);
            segment.localPosition = Vector3.up * (i == 0 ? .68f : .38f);
            segment.localScale = Vector3.one * (i == 0 ? 1 : .76f);
            segment.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.PlagueTail();
            var renderer = segment.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CombatMaterials.Fleet;
            renderer.sortingLayerName = "Enemy";
            tail[i] = parent = segment;
        }
    }

    protected override void Update()
    {
        if (eventSpawn && !DistrictHazards.Open)
        {
            Depart();
            return;
        }
        if (GameRules.playerDisabled || WorldTimeStop.Frozen || FrozenHull.Holds(gameObject))
            return;
        float dt = Time.deltaTime * LevelRun.EnemyTempoScale * ChillMovement;
        age += dt;
        if (movement is FormationFlight entrance && entrance.enabled)
        {
            if (age < .58f)
                return;
            if (entrance.Tangent.sqrMagnitude > .01f)
                velocity = entrance.Tangent * 2.8f;
            entrance.enabled = false;
        }
        Vector2 at = transform.position;
        bool hunting = age < 5.4f && player != null;
        Vector2 target = hunting ? CombatVisuals.PlayerHitPoint : new Vector2(at.x * .75f, Camera.DownLimit - 4);
        Vector2 seek = (target - at).normalized;
        Vector2 separate = Vector2.zero, centre = Vector2.zero, align = Vector2.zero;
        int neighbors = 0;
        if (hunting)
            foreach (var other in flock)
            {
                if (other == this || other.health <= 0)
                    continue;
                Vector2 offset = (Vector2)other.transform.position - at;
                float distance = offset.sqrMagnitude;
                if (distance > 2.25f)
                    continue;
                centre += offset;
                align += other.velocity;
                neighbors++;
                if (distance < .36f)
                    separate -= offset / Mathf.Max(.027f, distance);
            }
        if (neighbors != 0)
            seek += centre * (.29f / neighbors) + align * (.055f / neighbors) + Vector2.ClampMagnitude(separate * .51f, 1.8f);
        velocity = Vector2.MoveTowards(velocity, seek.normalized * (hunting ? 5.05f : 6.1f), dt * 11.2f);
        transform.position += (Vector3)(velocity * dt);
        Hull.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.down, velocity));
        for (int i = 0; i < tail.Length; i++)
            tail[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 18 - i * 1.25f) * 29);
        if (pilot != null && (CombatVisuals.PlayerHitPoint - (Vector2)transform.position).sqrMagnitude < .24f * .24f)
        {
            pilot.ReceiveHazardDamage(1);
            Depart();
        }
        else if (age > 13 || transform.position.y < Camera.DownLimit - 2)
            Depart();
    }
}

public sealed class SiltLeech : DistrictEnemy
{
    const float Reach = 1.55f;
    LineRenderer outline;
    Shipcontroller pilot;
    protected override void Configure() { health = 30; hullScale = .51f; }
    protected override void Attack() { }

    protected override void Start()
    {
        base.Start();
        pilot = player != null ? player.GetComponent<Shipcontroller>() : null;
        outline = CombatVisuals.Line(transform, new Color(.53f, .69f, .37f, .38f), .025f);
        outline.loop = true;
        outline.useWorldSpace = false;
        outline.positionCount = 32;
        for (int i = 0; i < outline.positionCount; i++)
        {
            float a = i * Mathf.PI * 2 / outline.positionCount;
            outline.SetPosition(i, new Vector3(Mathf.Cos(a) * Reach, Mathf.Sin(a) * Reach, .35f));
        }
    }

    protected override void Update()
    {
        base.Update();
        bool active = enteredFight && health > 0 && !GameRules.playerDisabled && !WorldTimeStop.Frozen &&
            !FrozenHull.Holds(gameObject) && transform.position.y < Camera.UpLimit - .45f;
        outline.enabled = active;
        if (active && pilot != null && (CombatVisuals.PlayerHitPoint - (Vector2)transform.position).sqrMagnitude < Reach * Reach)
            pilot.ApplySewerDrag(.25f);
    }
}

public sealed class SluiceBarge : DistrictEnemy
{
    protected override void Configure() { health = 116; hullScale = .81f; }
    protected override void Attack()
    {
        int count = Dense(5);
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<GiantOrb>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, lane * 19), 2.05f + .19f * Mathf.Abs(lane), .58f)
                .Ramp(.65f, 1.18f, 1.2f);
        }
    }
}

public sealed class OverflowBell : DistrictEnemy
{
    protected override void Configure() { health = 57; hullScale = .64f; }
    protected override void Attack()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 direction = new Vector2(side * .87f, .49f);
            Fire<GiantOrb>(Muzzle(side < 0 ? 0 : 1), direction, 2.75f, .52f).Fall(new Vector2(0, -2.4f));
            Fire<GiantOrb>(Muzzle(side < 0 ? 0 : 1), direction, 1.65f, .34f).Fall(new Vector2(0, -1.4f));
        }
    }
}

public sealed class PipeSkater : DistrictEnemy
{
    protected override void Configure() { health = 25; hullScale = .46f; }
    protected override void Attack()
    {
        int count = Dense(5);
        for (int i = 0; i < count; i++)
            Fire<NeedleRound>(Muzzle(i), Aim, 3.4f + i * .68f, .15f);
        Fire<GiantOrb>(Muzzle(), Aim, 2.15f, .33f);
    }
}

public sealed class GrateKeeper : DistrictMiniboss
{
    protected override void Configure() { health = 470; hullScale = .92f; attackInterval = 1.48f; nextAttack = 1.35f; }
    protected override Vector3 Position(float t) =>
        new Vector3(2.65f * Mathf.Sin(t * 1.3f), Camera.UpLimit - 3.4f + .9f * Mathf.Sin(t * 2.6f), 1);

    protected override void Attack()
    {
        int move = (volley - 1) % 3;
        int count = Dense(move == 1 ? 9 : 7);
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            if (move == 0)
                Fire<GiantOrb>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, lane * 20), 2.25f, .56f);
            else if (move == 1)
                Fire<GiantOrb>(Muzzle(i), CombatVisuals.Rotate(Vector2.up, lane * 13), 3.7f, .45f).Fall(new Vector2(0, -2.7f));
            else
                Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, lane * 28), 4.7f, .19f).Bounce(1, false);
        }
    }
}
