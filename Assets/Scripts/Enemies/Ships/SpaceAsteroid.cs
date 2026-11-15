using System.Collections.Generic;
using UnityEngine;

// A tumbling Space asteroid. It never fires: it drifts through in formation and hurts the pilot on
// contact. Destroyed, it breaks into two smaller rocks that fly apart on diverging headings,
// faster the smaller they are, and bounce off the arena sides until they leave below. Every split
// takes a generation: an ordinary asteroid splits three times, a great boulder five times (Miguel,
// 15 November 2026). Fragments keep the wave open but are not counted as separate formation kills.
[UnityEngine.Scripting.Preserve]
public class SpaceAsteroid : DistrictEnemy
{
    public const int MaxGeneration = 5;
    static int pendingGeneration = -1;
    int generation;
    bool drifting;
    Vector2 velocity, drift;
    Vector3 last, tumbleAxis;
    float tumbleRate;
    bool hasLast;

    public int Generation => generation;
    public bool Fragment => drifting;
    public Vector2 Velocity => velocity;
    // Contact uses the cockpit centre against the rock's body, a little inside its lumps.
    public float ContactRadius => hullScale * .78f;
    protected virtual int WaveGeneration => 3;

    // From a 0.34 pebble up to a 1.19 boulder.
    public static float ScaleFor(int generation) => .34f + .17f * generation;

    protected override void Configure()
    {
        generation = pendingGeneration >= 0 ? pendingGeneration : WaveGeneration;
        pendingGeneration = -1;
        hullScale = ScaleFor(generation);
        health = 5 + 5 * generation;
        attackInterval = float.PositiveInfinity;
        nextAttack = float.PositiveInfinity;
    }

    // A piece of a broken rock: one generation smaller, flying free on its own heading.
    public static SpaceAsteroid Shard(int generation, Vector3 position, Vector2 velocity)
    {
        pendingGeneration = Mathf.Clamp(generation, 0, MaxGeneration);
        var rock = (SpaceAsteroid)Spawn(typeof(SpaceAsteroid), position);
        pendingGeneration = -1;
        rock.drifting = true;
        rock.drift = velocity;
        rock.velocity = velocity;
        return rock;
    }

    protected override void Start()
    {
        base.Start();
        scoreOnKill = 12 + 8 * generation;
        // Sixty-three rocks can come from one boulder: coins and energy stay modest per piece.
        moneyDropChance = generation >= 3 ? .3f : .1f;
        manaDropChance = .04f;
        Hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.AsteroidRock(Random.Range(0, IndustrialMeshFactory.AsteroidVariants));
        Hull.localRotation = Random.rotation;
        tumbleAxis = Random.onUnitSphere;
        tumbleRate = Random.Range(40f, 95f) / (.5f + hullScale);
        var visual = GetComponent<CombatHullVisual>();
        if (visual != null)
            visual.spin = (Random.value < .5f ? -1 : 1) * Random.Range(18f, 50f) / (.5f + hullScale);
    }

    protected override void Update()
    {
        bool held = FrozenHull.Holds(gameObject) || WorldTimeStop.Frozen;
        float dt = Time.deltaTime;
        if (!held && dt > 0)
        {
            if (drifting)
            {
                var p = transform.position + (Vector3)(drift * dt * LevelRun.EnemyTempoScale);
                // Fragments glance off the arena sides, so they stay in play until they fall out.
                float r = hullScale * .8f;
                if (p.x < Camera.LeftLimit + r && drift.x < 0 || p.x > Camera.RightLimit - r && drift.x > 0)
                    drift.x = -drift.x;
                transform.position = p;
                velocity = drift;
                if (p.y < Camera.DownLimit - hullScale - .5f)
                {
                    Depart();
                    return;
                }
            }
            else if (hasLast)
                velocity = Vector2.Lerp(velocity, (transform.position - last) / dt, .25f);
            last = transform.position;
            hasLast = true;
            Hull.Rotate(tumbleAxis, tumbleRate * dt, Space.Self);
            TouchPilot();
        }
        base.Update();
    }

    void TouchPilot()
    {
        if (health <= 0)
            return;
        var pilot = CombatVisuals.Player;
        if (pilot != null && Vector2.Distance(pilot.position, transform.position) < ContactRadius)
            pilot.SendMessage("ReceiveHazardDamage", 1f, SendMessageOptions.DontRequireReceiver);
    }

    protected override void Die()
    {
        Crumble();
        if (generation > 0)
            Split();
        base.Die();
    }

    // Two smaller rocks fly apart, both still heading down the arena.
    void Split()
    {
        var heading = velocity.sqrMagnitude > .01f ? velocity.normalized : Vector2.down;
        if (heading.y > -.35f)
            heading = (heading + Vector2.down * .8f).normalized;
        float speed = SpeedFor(generation - 1) * Random.Range(.92f, 1.08f);
        float spread = Random.Range(26f, 40f);
        var across = new Vector2(-heading.y, heading.x);
        foreach (int side in new[] { -1, 1 })
        {
            var direction = CombatVisuals.Rotate(heading, side * spread);
            if (direction.y > -.3f)
                direction = new Vector2(direction.x, -.3f).normalized;
            Shard(generation - 1, transform.position + (Vector3)(across * side * hullScale * .45f), direction * speed);
        }
    }

    // Smaller pieces fly faster.
    public static float SpeedFor(int generation) => Mathf.Lerp(3.1f, 1.6f, generation / (float)MaxGeneration);

    void Crumble()
    {
        var dust = new Color(.55f, .4f, .27f);
        for (int i = 0; i < 2 + generation; i++)
            PSXImpactEffects.Burst(transform.position + (Vector3)(Random.insideUnitCircle * hullScale * .6f) + Vector3.back, dust, i % 2 == 0, Random.insideUnitCircle);
    }

    protected override void Attack()
    {
    }
}

// The great boulder: the same rock at full size, splitting five times.
[UnityEngine.Scripting.Preserve]
public sealed class SpaceBoulder : SpaceAsteroid
{
    protected override int WaveGeneration => MaxGeneration;
}

// Brown asteroid stone: the PSX Asteroid shader with a procedural tileable rock texture.
public static class AsteroidMaterials
{
    static Material rock;
    static Texture2D texture;

    public static Material Rock
    {
        get
        {
            if (rock != null)
                return rock;
            texture = RockTexture();
            rock = new Material(Resources.Load<Shader>("PSXAsteroid")) { name = "Asteroid stone", mainTexture = texture };
            return rock;
        }
    }

    // 128 px of tileable brown stone: layered lumps, darker strata, fine cracks and grit.
    static Texture2D RockTexture()
    {
        const int Size = 128;
        var result = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "Asteroid stone", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[Size * Size];
        var umber = new Color(.20f, .13f, .08f);
        var brown = new Color(.38f, .26f, .16f);
        var tan = new Color(.55f, .40f, .26f);
        var dust = new Color(.68f, .54f, .38f);
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = x / (float)Size, v = y / (float)Size;
                float lumps = Tiled(u, v, 4, 11) * .5f + Tiled(u, v, 8, 23) * .27f + Tiled(u, v, 16, 37) * .15f + Tiled(u, v, 32, 41) * .08f;
                float strata = Tiled(u * .5f + v, v, 3, 53);
                float crack = 1 - Mathf.Abs(Tiled(u, v, 12, 67) * 2 - 1);
                float grit = Hash(x, y, 71);
                float tone = Mathf.Clamp01(lumps * 1.25f - .12f + (strata - .5f) * .18f);
                var colour = tone < .4f ? Color.Lerp(umber, brown, tone / .4f) : tone < .75f ? Color.Lerp(brown, tan, (tone - .4f) / .35f) : Color.Lerp(tan, dust, (tone - .75f) / .25f);
                colour *= Mathf.Lerp(1, .5f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.9f, .985f, crack)));
                colour *= .9f + .2f * grit;
                if (grit > .985f)
                    colour = Color.Lerp(colour, dust, .6f);
                colour.a = 1;
                pixels[y * Size + x] = colour;
            }
        result.SetPixels32(pixels);
        result.Apply(true, true);
        return result;
    }

    // Value noise on a lattice that repeats every period cells, so the texture tiles.
    static float Tiled(float u, float v, int period, int seed)
    {
        float x = u * period, y = v * period;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        float Corner(int cx, int cy) => Hash(((cx % period) + period) % period, ((cy % period) + period) % period, seed);
        return Mathf.Lerp(Mathf.Lerp(Corner(x0, y0), Corner(x0 + 1, y0), fx), Mathf.Lerp(Corner(x0, y0 + 1), Corner(x0 + 1, y0 + 1), fx), fy);
    }

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xffffff) / (float)0xffffff;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    public const int AsteroidVariants = 4;
    static readonly Mesh[] asteroidRocks = new Mesh[AsteroidVariants];

    // An irregular rock of unit radius with 1280 facets: a three-times subdivided icosahedron
    // pushed out by layered 3D noise into lumps and hollows, cleaved flat on a few sides, pitted
    // with real craters and stretched unevenly. Vertex colours shade the relief (dark hollows and
    // crater floors, light ridges) and tint each variant: red-brown, dark chocolate, sandy and
    // grey-brown stone.
    public static Mesh AsteroidRock(int variant)
    {
        variant = Mathf.Abs(variant) % AsteroidVariants;
        if (asteroidRocks[variant] != null)
            return asteroidRocks[variant];
        var random = new System.Random(5021 + variant * 733);
        float R(float a, float c) => Mathf.Lerp(a, c, (float)random.NextDouble());
        Vector3 Direction()
        {
            var d = new Vector3(R(-1, 1), R(-1, 1), R(-1, 1));
            return d.sqrMagnitude < .0001f ? Vector3.up : d.normalized;
        }
        var points = IcosphereDirections(3, out var triangles);
        var offset = new Vector3(R(-50, 50), R(-50, 50), R(-50, 50));
        var craters = new List<(Vector3 centre, float size, float depth)>();
        for (int i = 0; i < 6; i++)
        {
            var centre = Direction();
            // Most craters face the camera, where they can be seen.
            if (i < 4 && centre.z > 0)
                centre.z = -centre.z;
            craters.Add((centre, R(.16f, .32f), R(.05f, .1f)));
        }
        var cleaves = new List<(Vector3 normal, float distance)>();
        for (int i = 0; i < 3; i++)
            cleaves.Add((Direction(), R(.66f, .8f)));
        var stretch = new Vector3(1, R(.68f, .84f), R(.74f, .9f));
        var crater = new float[points.Count];
        float largest = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var direction = points[i];
            float radius = 1 + (RockNoise(direction * 1.6f + offset) - .5f) * .55f + (RockNoise(direction * 4.2f + offset * 2) - .5f) * .14f;
            foreach (var (centre, size, depth) in craters)
            {
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction, centre), -1, 1));
                float t = angle / size;
                if (t < 1)
                {
                    radius -= depth * (1 - t * t);
                    crater[i] = Mathf.Max(crater[i], 1 - t);
                }
                else if (t < 1.4f)
                    radius += depth * .45f * (1 - (t - 1) / .4f);
            }
            var point = Vector3.Scale(direction * radius, stretch);
            foreach (var (normal, distance) in cleaves)
            {
                float reach = Vector3.Dot(point, normal);
                if (reach > distance)
                    point -= normal * (reach - distance);
            }
            points[i] = point;
            largest = Mathf.Max(largest, point.magnitude);
        }
        for (int i = 0; i < points.Count; i++)
            points[i] /= largest;
        float lowest = float.MaxValue, highest = 0;
        foreach (var point in points)
        {
            lowest = Mathf.Min(lowest, point.magnitude);
            highest = Mathf.Max(highest, point.magnitude);
        }
        var tint = variant switch
        {
            0 => new Color(1.0f, .86f, .76f),
            1 => new Color(.78f, .66f, .58f),
            2 => new Color(1.0f, .93f, .8f),
            _ => new Color(.86f, .82f, .78f)
        };
        // Flat facets: every triangle has its own three vertices and one colour.
        var vertices = new List<Vector3>(triangles.Count);
        var colours = new List<Color>(triangles.Count);
        var indices = new List<int>(triangles.Count);
        for (int i = 0; i < triangles.Count; i += 3)
        {
            var a = points[triangles[i]];
            var b = points[triangles[i + 1]];
            var c = points[triangles[i + 2]];
            var centre = (a + b + c) / 3;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), centre) < 0)
                (b, c) = (c, b);
            float relief = Mathf.InverseLerp(lowest, highest, centre.magnitude);
            float pit = (crater[triangles[i]] + crater[triangles[i + 1]] + crater[triangles[i + 2]]) / 3;
            float shade = Mathf.Lerp(.5f, 1, Mathf.SmoothStep(0, 1, relief)) * Mathf.Lerp(1, .55f, pit) * (.9f + .1f * RockNoise(centre * 9 + offset));
            var colour = tint * Mathf.Clamp01(shade);
            colour.a = 1;
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            for (int k = 0; k < 3; k++)
            {
                colours.Add(colour);
                indices.Add(start + k);
            }
        }
        var mesh = new Mesh { name = "Space asteroid " + variant };
        mesh.SetVertices(vertices);
        mesh.SetColors(colours);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return asteroidRocks[variant] = mesh;
    }

    // Unit directions of an icosahedron subdivided the given number of times, and its triangles.
    static List<Vector3> IcosphereDirections(int subdivisions, out List<int> triangles)
    {
        float t = (1 + Mathf.Sqrt(5)) / 2;
        var points = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        for (int i = 0; i < points.Count; i++)
            points[i] = points[i].normalized;
        triangles = new List<int>
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        for (int level = 0; level < subdivisions; level++)
        {
            var middle = new Dictionary<long, int>();
            int Middle(int a, int c)
            {
                long key = a < c ? (long)a << 32 | (uint)c : (long)c << 32 | (uint)a;
                if (middle.TryGetValue(key, out int index))
                    return index;
                points.Add(((points[a] + points[c]) * .5f).normalized);
                return middle[key] = points.Count - 1;
            }
            var finer = new List<int>(triangles.Count * 4);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i], c = triangles[i + 1], d = triangles[i + 2];
                int ac = Middle(a, c), cd = Middle(c, d), da = Middle(d, a);
                finer.AddRange(new[] { a, ac, da, c, cd, ac, d, da, cd, ac, cd, da });
            }
            triangles = finer;
        }
        return points;
    }

    // Smooth 3D value noise in [0, 1], layered over three octaves.
    static float RockNoise(Vector3 p)
    {
        float sum = 0, weight = .5f, total = 0;
        for (int octave = 0; octave < 3; octave++)
        {
            sum += ValueNoise(p) * weight;
            total += weight;
            p = p * 2.03f + new Vector3(17.1f, 3.7f, 9.2f);
            weight *= .5f;
        }
        return sum / total;
    }

    static float ValueNoise(Vector3 p)
    {
        int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z);
        float fx = p.x - x, fy = p.y - y, fz = p.z - z;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        fz = fz * fz * (3 - 2 * fz);
        float H(int i, int j, int k)
        {
            unchecked
            {
                uint h = (uint)(i * 73856093 ^ j * 19349663 ^ k * 83492791);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }
        float x00 = Mathf.Lerp(H(x, y, z), H(x + 1, y, z), fx), x10 = Mathf.Lerp(H(x, y + 1, z), H(x + 1, y + 1, z), fx);
        float x01 = Mathf.Lerp(H(x, y, z + 1), H(x + 1, y, z + 1), fx), x11 = Mathf.Lerp(H(x, y + 1, z + 1), H(x + 1, y + 1, z + 1), fx);
        return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
    }
}
