using System.Collections.Generic;
using UnityEngine;

public abstract class CoreCube : DistrictEnemy
{
    static readonly HashSet<CoreCube> active = new HashSet<CoreCube>();
    static readonly List<CombatWall> solid = new List<CombatWall>(64);
    static readonly List<CoreCube> owners = new List<CoreCube>(64);
    static int collectedFrame = -1;
    protected virtual bool Breakable => true;
    public override bool CanBeTargeted => Breakable && base.CanBeTargeted;
    protected virtual float HalfWidth => Hull.localScale.x * .81f;
    float HalfHeight => Hull.localScale.y * .81f;

    protected override void OnEnable() { base.OnEnable(); active.Add(this); collectedFrame = -1; }
    protected override void OnDisable() { active.Remove(this); collectedFrame = -1; base.OnDisable(); }

    protected override void Start()
    {
        base.Start();
        GetComponent<CircleCollider2D>().enabled = false;
        if (!Breakable) return;
        var box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(HalfWidth, HalfHeight) * 2;
    }

    public override void damage(float amount)
    {
        if (Breakable)
        {
            base.damage(amount);
            if (health <= 0) collectedFrame = -1;
        }
    }

    static void Collect()
    {
        collectedFrame = Time.frameCount;
        solid.Clear();
        owners.Clear();
        foreach (var cube in active)
        {
            Vector2 center = cube.transform.position;
            float w = cube.HalfWidth, h = cube.HalfHeight;
            if (cube.health <= 0 || center.y - h > Camera.UpLimit + 1 || center.y + h < Camera.DownLimit - 1 ||
                center.x - w > Camera.RightLimit + 1 || center.x + w < Camera.LeftLimit - 1)
                continue;
            solid.Add(Rect.MinMaxRect(center.x - w, center.y - h, center.x + w, center.y + h));
            owners.Add(cube);
        }
    }

    internal bool ContainsShot(Vector2 point, float radius)
    {
        Vector2 offset = point - (Vector2)transform.position;
        return Mathf.Abs(offset.x) <= HalfWidth + radius && Mathf.Abs(offset.y) <= HalfHeight + radius;
    }

    internal static bool SweepShot(Vector2 from, Vector2 to, float radius, CoreCube emitter,
        out Vector2 point, out Vector2 normal, out CoreCube block)
    {
        point = to;
        normal = Vector2.zero;
        block = null;
        if (active.Count == 0) return false;
        if (collectedFrame != Time.frameCount) Collect();
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < solid.Count; i++)
        {
            if (ReferenceEquals(owners[i], emitter) || !solid[i].Sweep(from, to, radius, out float t, out var face) || t >= nearest)
                continue;
            nearest = t;
            normal = face;
            block = owners[i];
        }
        if (block == null) return false;
        point = Vector2.LerpUnclamped(from, to, nearest);
        return true;
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
    int bankTiles;
    public override bool HoldsWave => bankTiles > 0;
    protected override float HalfWidth => base.HalfWidth + Mathf.Max(0, bankTiles - 1) * IndustrialMeshFactory.CoreTilePitch * Hull.localScale.x * .5f;
    public override void FlyPhrase(WaveUnit unit)
    {
        base.FlyPhrase(unit);
        bankTiles = unit.coreBankTiles;
        if (bankTiles == 0) return;
        var scale = Hull.localScale;
        scale.x = CoreMazeWaves.CellWidth / IndustrialMeshFactory.CoreTilePitch;
        Hull.localScale = scale;
        Hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CoreBank(bankTiles);
    }
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
