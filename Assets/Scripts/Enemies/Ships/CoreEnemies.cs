using System.Collections.Generic;
using UnityEngine;

// These blocks constrain the pilot independently of the projectile wall system.
// A shot can still hit a destructible face or pass a fixed block's lane.
public abstract class CoreCube : DistrictEnemy
{
    static readonly HashSet<CoreCube> active = new HashSet<CoreCube>();
    static readonly List<CombatWall> solid = new List<CombatWall>(24);
    protected virtual bool Breakable => true;
    public override bool CanBeTargeted => Breakable && base.CanBeTargeted;
    float HalfWidth => hullScale * .81f;
    LineRenderer routeTell;

    protected override void OnEnable() { base.OnEnable(); active.Add(this); }
    protected override void OnDisable() { active.Remove(this); base.OnDisable(); }

    protected override void Start()
    {
        base.Start();
        GetComponent<CircleCollider2D>().enabled = false;
        var box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = Vector2.one * (HalfWidth * 2);
        if (!Breakable)
            box.enabled = false;
    }

    public override void FlyPhrase(WaveUnit unit)
    {
        base.FlyPhrase(unit);
        if (!(movement is FormationFlight flight))
            return;
        routeTell = CombatVisuals.Line(transform, new Color(.94f, .37f, .17f, .40f), .036f);
        routeTell.sortingLayerName = "Enemy";
        routeTell.sortingOrder = -2;
        routeTell.positionCount = 12;
        for (int i = 0; i < routeTell.positionCount; i++)
        {
            var p = flight.PositionAt(i / 11f * .78f);
            routeTell.SetPosition(i, new Vector3(p.x * Camera.RightLimit,
                Mathf.LerpUnclamped(Camera.DownLimit, Camera.UpLimit, p.y), 4));
        }
    }

    protected override void Update()
    {
        base.Update();
        if (routeTell != null && age > .85f)
            routeTell.enabled = false;
    }

    public override void damage(float amount)
    {
        if (Breakable)
            base.damage(amount);
    }

    static void Collect()
    {
        solid.Clear();
        foreach (var cube in active)
        {
            Vector2 center = cube.transform.position;
            float extent = cube.HalfWidth;
            if (cube.health <= 0 || center.y - extent > Camera.UpLimit + 1 || center.y + extent < Camera.DownLimit - 1 ||
                center.x - extent > Camera.RightLimit + 1 || center.x + extent < Camera.LeftLimit - 1)
                continue;
            solid.Add(Rect.MinMaxRect(center.x - extent, center.y - extent, center.x + extent, center.y + extent));
        }
    }

    public static Vector3 Constrain(Vector3 from, Vector3 desired)
    {
        if (active.Count == 0)
            return desired;
        Collect();
        return CombatWalls.Constrain(from, desired, solid, out _);
    }

    public static bool Occupies(Vector2 point)
    {
        if (active.Count == 0)
            return false;
        Collect();
        return CombatWalls.Overlaps(solid, point, CombatWalls.HullRadius);
    }
}

public sealed class TransitCube : CoreCube
{
    protected override bool Breakable => false;
    public override bool HoldsWave => false;
    protected override void Configure() { health = 1; hullScale = .59f; }
    protected override void Attack() { }
}

public sealed class FuseCube : CoreCube
{
    protected override void Configure() { health = 37; hullScale = .59f; }
    protected override void Attack() { }
}

public sealed class SentryCube : CoreCube
{
    protected override void Configure() { health = 55; hullScale = .58f; }
    protected override void Attack()
    {
        int count = Dense(9);
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, lane * 13), 4.0f, .17f)
                .Ramp(1.5f, .9f, .35f);
        }
    }
}

public sealed class RamCube : CoreCube
{
    protected override void Configure() { health = 120; hullScale = .76f; }
    protected override void Attack()
    {
        int count = Dense(5);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < count; i++)
                Fire<SimpleRound>(Muzzle(side < 0 ? 0 : 1),
                    CombatVisuals.Rotate(new Vector2(side, -.18f), side * (i * 12 - 12)), 3.15f, .21f).Bounce(1, false);
    }
}

public sealed class RelayCarrier : DistrictEnemy
{
    protected override void Configure() { health = 96; hullScale = .78f; }
    protected override void Attack()
    {
        int count = Dense(11), gap = count / 2 - 1 + volley % 3;
        for (int i = 0; i < count; i++)
        {
            if (Mathf.Abs(i - gap) < 2)
                continue;
            Vector2 direction = CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * 15);
            Fire<NeedleRound>(Muzzle(i), direction, 2.85f, .17f)
                .Pause(.75f, .36f, CombatRound.Turn.Down, 0, 4.85f);
        }
    }
}

public sealed class RivetCutter : DistrictEnemy
{
    protected override void Configure() { health = 34; hullScale = .51f; }
    protected override void Attack()
    {
        for (int i = 0; i < Dense(4); i++)
        {
            var direction = CombatVisuals.Rotate(Aim, volley % 2 == 0 ? -13 : 13);
            Fire<NeedleRound>(Muzzle(i), direction, 2.8f + i * .62f, .17f);
            if (i % 2 == 0)
                Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(direction, volley % 2 == 0 ? 25 : -25),
                    3.25f + i * .38f, .16f, false);
        }
    }
}

public sealed class PressWarden : DistrictMiniboss
{
    protected override void Configure() { health = 635; hullScale = .98f; attackInterval = 1.3f; nextAttack = 1.2f; }
    protected override Vector3 Position(float t)
    {
        float step = Mathf.Sin(t * 1.17f);
        return new Vector3(2.7f * step, Camera.UpLimit - 2.7f - .95f * Mathf.Sin(t * 2.34f), 1);
    }

    protected override void Attack()
    {
        int move = (volley - 1) % 3;
        if (move == 0)
        {
            MirrorFan(Muzzle(0), new Vector2(1, -.8f), Dense(7), 12, 4.8f);
            MirrorFan(Muzzle(1), new Vector2(-1, -.8f), Dense(7), 12, 4.8f);
        }
        else if (move == 1)
        {
            int count = Dense(9);
            for (int i = 0; i < count; i++)
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.up, 26 * (i - (count - 1) * .5f)), 4.8f, .22f).Bounce(2);
        }
        else
        {
            Stream(5, .13f, i =>
            {
                Fire<NeedleRound>(Muzzle(0), CombatVisuals.Rotate(Vector2.down, -45 + i * 18), 5.7f, .21f);
                Fire<NeedleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.down, 45 - i * 18), 5.7f, .21f);
            });
        }
    }
}
