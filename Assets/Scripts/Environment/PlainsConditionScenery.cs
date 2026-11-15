using UnityEngine;
using UnityEngine.Rendering;

// Dusk and night over the river valley. The ground takes a low warm sun or deep blue moonlight
// while ships and bullets keep their own light. Night lights the road lamps, bridge lanterns,
// the lookout's fire, fireflies over the groves and moonlight on the water; dusk catches the
// low sun on the river, lights a few early lamps and sends birds home across the valley.
public static class PlainsConditionScenery
{
    // Between the valley (-40) and ground vehicles, tread marks and buried hulls (-35..-31).
    public const int GradeOrder = -37, LightOrder = -36;

    public static void Begin(Transform field, int seed)
    {
        var condition = DistrictConditions.For(LevelId.Fields, seed);
        int side = DistrictConditions.Side(LevelId.Fields, seed);
        if (condition == DistrictCondition.Dusk)
        {
            SceneryGrade.Create(field, "Dusk light", GradeOrder)
                .Multiply(new Color(1, .80f, .60f), new Color(.52f, .48f, .68f), side, 1, .16f)
                .Glow(new Color(.30f, .12f, .03f), new Color(.02f, 0, .02f), side, .55f);
            var birds = new GameObject("Birds flying home");
            birds.transform.SetParent(field, false);
            birds.AddComponent<DuskBirds>().Side = side;
        }
        else if (condition == DistrictCondition.Night)
        {
            SceneryGrade.Create(field, "Moonlight", GradeOrder)
                .Multiply(new Color(.36f, .42f, .64f), new Color(.29f, .34f, .54f), side, 1, .38f)
                .Glow(new Color(.004f, .010f, .028f), new Color(0, .004f, .014f), side);
        }
    }

    // The high clouds pass over everything; they take the condition's colour themselves.
    public static Color CloudTint(int seed, Color day)
    {
        switch (DistrictConditions.For(LevelId.Fields, seed))
        {
            case DistrictCondition.Dusk:
                return new Color(.86f, .58f, .50f, day.a * 1.15f);
            case DistrictCondition.Night:
                return new Color(.10f, .12f, .19f, day.a * 1.3f);
            default:
                return day;
        }
    }

    // One light mesh per valley section, attached to it so every light scrolls with the ground.
    public static Mesh Lights(float origin, float length, PSXValleyLayout layout, int seed)
    {
        var condition = DistrictConditions.For(LevelId.Fields, seed);
        if (condition != DistrictCondition.Dusk && condition != DistrictCondition.Night)
            return null;
        bool night = condition == DistrictCondition.Night;
        var rng = new System.Random(seed ^ (int)(origin * 131) ^ 0x2b61);
        float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
        var lights = new SceneryLights();
        // In front of the bridge deck, banks and canopies; light spills over all of them.
        const float Lift = -2f;

        // Road lamps stand on the far verge, every few metres; at dusk only the first few are on.
        int lamp = 0;
        for (float y = Mathf.Ceil(origin / 2.4f) * 2.4f; y < origin + length; y += 2.4f, lamp++)
        {
            if (!night && (Mathf.RoundToInt(y / 2.4f) % 3 != 0))
                continue;
            if (PSXValleyLayout.DistanceY(y, layout.BridgeY) < 1.2f)
                continue;
            float road = layout.Centre(y) - layout.HalfWidth(y) - 1.15f;
            var post = new Vector3(road - .46f, y - origin, Lift);
            float strength = night ? 1 : .42f;
            lights.Add(post + new Vector3(.24f, 0), 1.05f, .92f, new Color(1, .64f, .30f, .62f * strength), R(0, 6.28f), SceneryLights.Kind.Lamp);
            lights.Add(post, .14f, .14f, new Color(1, .90f, .66f, 1f * strength), R(0, 6.28f), SceneryLights.Kind.Lamp);
        }

        if (night)
        {
            // Lanterns on the bridge's four corners.
            float bridge = layout.BridgeY;
            if (bridge >= origin && bridge < origin + length)
            {
                float centre = layout.Centre(bridge), half = layout.HalfWidth(bridge) + .18f;
                foreach (int across in new[] { -1, 1 })
                    foreach (int along in new[] { -1, 1 })
                    {
                        var lantern = new Vector3(centre + across * half, bridge - origin + along * .42f, Lift);
                        lights.Add(lantern, .80f, .80f, new Color(1, .58f, .26f, .62f), R(0, 6.28f), SceneryLights.Kind.Fire);
                        lights.Add(lantern, .12f, .12f, new Color(1, .86f, .55f, 1), R(0, 6.28f), SceneryLights.Kind.Fire);
                    }
            }
            // A watch fire burns at the concrete lookout.
            float lookout = layout.LookoutY;
            if (lookout >= origin && lookout < origin + length)
            {
                var fire = new Vector3(layout.Centre(lookout) + layout.HalfWidth(lookout) + 1.6f, lookout - origin - .55f, Lift);
                lights.Add(fire, 1.5f, 1.4f, new Color(1, .44f, .14f, .70f), R(0, 6.28f), SceneryLights.Kind.Fire);
                lights.Add(fire, .22f, .22f, new Color(1, .80f, .40f, .95f), R(0, 6.28f), SceneryLights.Kind.Fire);
            }
            // Fireflies drift around the edges of the groves.
            foreach (var plant in layout.Plants)
            {
                if (plant.kind == 2 || plant.y < origin || plant.y >= origin + length || rng.NextDouble() > .55)
                    continue;
                int swarm = rng.Next(1, 4);
                for (int i = 0; i < swarm; i++)
                {
                    float a = R(0, Mathf.PI * 2), r = plant.radius * R(.9f, 1.6f);
                    var at = new Vector3(plant.x + Mathf.Cos(a) * r, plant.y - origin + Mathf.Sin(a) * r, Lift - .3f);
                    lights.Add(at, .16f, .16f, new Color(.78f, 1, .42f, .62f), R(0, 1), SceneryLights.Kind.Firefly);
                    lights.Add(at, .045f, .045f, new Color(.95f, 1, .7f, .9f), R(0, 1), SceneryLights.Kind.Firefly);
                }
            }
        }

        // Moonlight glints on the water at night only; Miguel did not want the river sparkling at dusk.
        int glints = night ? 46 : 0;
        for (int i = 0; i < glints; i++)
        {
            float y = R(origin, origin + length);
            float half = layout.HalfWidth(y) * .82f;
            float across = night ? R(-1, 1) : Mathf.Lerp(R(-1, 1), DistrictConditions.Side(LevelId.Fields, seed), R(0, .45f));
            var at = new Vector3(layout.Centre(y) + across * half, y - origin, Lift);
            var color = night ? new Color(.62f, .74f, 1, R(.6f, .9f)) : new Color(1, .72f, .36f, R(.8f, 1));
            lights.Add(at, R(.14f, .30f), R(.03f, .055f), color, R(0, 1), SceneryLights.Kind.Glint);
        }
        return lights.Count > 0 ? lights.Finish("Valley " + condition.ToString().ToLowerInvariant() + " lights") : null;
    }

    public static void AddLights(Transform section, Mesh mesh, Material material)
    {
        var go = new GameObject(mesh.name);
        go.transform.SetParent(section, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = LightOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Night headlights for a road vehicle: a warm pool thrown ahead of its hull.
    static Mesh headlightBeam;
    public static MeshRenderer Headlights(Transform hull, Material material)
    {
        if (headlightBeam == null)
        {
            // Shared by every tank for the whole session, like the hull meshes.
            var lights = new SceneryLights();
            lights.Add(new Vector3(0, -1.05f, -.05f), .42f, .95f, new Color(1, .86f, .58f, .42f), 0, SceneryLights.Kind.Lamp);
            foreach (int side in new[] { -1, 1 })
                lights.Add(new Vector3(side * .19f, -.58f, -.05f), .07f, .07f, new Color(1, .95f, .80f, .9f), side, SceneryLights.Kind.Lamp);
            headlightBeam = lights.Finish("Tank headlights");
        }
        var go = new GameObject("Night headlights");
        go.transform.SetParent(hull, false);
        go.AddComponent<MeshFilter>().sharedMesh = headlightBeam;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -33;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return renderer;
    }
}

// Small dark flocks crossing away from the low sun. Purely scenery: no colliders or physics.
public sealed class DuskBirds : MonoBehaviour
{
    const int Flocks = 2, PerFlock = 7, Birds = Flocks * PerFlock;
    static readonly Color Silhouette = new Color(.07f, .05f, .07f, .82f);
    public int Side { get; set; } = 1;
    Mesh mesh;
    Material material;
    Transform drawn;
    readonly Vector3[] vertices = new Vector3[Birds * 4];
    readonly float[] started = new float[Flocks], speeds = new float[Flocks], heights = new float[Flocks];
    readonly float[] phases = new float[Birds];
    readonly Vector2[] offsets = new Vector2[Birds];
    System.Random rng;

    void Start()
    {
        rng = new System.Random(LevelRun.Seed ^ 0x6b17);
        var colors = new Color[vertices.Length];
        var triangles = new int[Birds * 6];
        for (int i = 0; i < Birds; i++)
        {
            // Four vertices a bird: body, near tip, far tip and a wing root just behind the body.
            int k = i * 4, t = i * 6;
            for (int j = 0; j < 4; j++)
                colors[k + j] = Silhouette;
            triangles[t] = k;
            triangles[t + 1] = k + 1;
            triangles[t + 2] = k + 3;
            triangles[t + 3] = k;
            triangles[t + 4] = k + 3;
            triangles[t + 5] = k + 2;
            phases[i] = (float)rng.NextDouble() * 6.28f;
            // A loose V: the leader in front, the rest trailing on alternate sides.
            int rank = i % PerFlock;
            int wing = rank == 0 ? 0 : rank % 2 == 0 ? 1 : -1;
            int depth = (rank + 1) / 2;
            offsets[i] = new Vector2(-depth * .30f, wing * depth * .24f) + new Vector2((float)rng.NextDouble() * .08f, (float)rng.NextDouble() * .08f);
        }
        mesh = new Mesh { name = "Dusk birds" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(80, 80, 100));
        material = new Material(Resources.Load<Shader>("LevelScenery")) { name = "Dusk birds" };
        var go = new GameObject("Bird silhouettes");
        go.transform.SetParent(transform, false);
        drawn = go.transform;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = PlainsConditionScenery.LightOrder + 1;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        for (int f = 0; f < Flocks; f++)
            Launch(f, 3 + f * 9);
    }

    void Launch(int flock, float delay)
    {
        started[flock] = Time.time + delay;
        speeds[flock] = .9f + (float)rng.NextDouble() * .5f;
        heights[flock] = (float)rng.NextDouble() * 6 - 1;
    }

    void LateUpdate()
    {
        var view = UnityEngine.Camera.main;
        if (view == null || mesh == null)
            return;
        // Vertices are written in world space.
        drawn.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        drawn.localScale = Vector3.one;
        float halfWidth = view.orthographicSize * view.aspect + 2;
        var centre = (Vector2)view.transform.position;
        // Away from the sun, drifting slowly down the screen with the scroll.
        var heading = new Vector2(-Side, -.22f).normalized;
        var across = new Vector2(-heading.y, heading.x);
        for (int f = 0; f < Flocks; f++)
        {
            float travel = (Time.time - started[f]) * speeds[f];
            if (travel > halfWidth * 2 + 4)
            {
                Launch(f, 4 + (float)rng.NextDouble() * 8);
                travel = -1;
            }
            var lead = centre + new Vector2(Side * halfWidth, heights[f]) + heading * travel;
            for (int b = 0; b < PerFlock; b++)
            {
                int i = f * PerFlock + b, k = i * 4;
                if (travel < 0)
                {
                    vertices[k] = vertices[k + 1] = vertices[k + 2] = vertices[k + 3] = Vector3.zero;
                    continue;
                }
                var body = lead + heading * offsets[i].x + across * offsets[i].y;
                float sweep = -.05f + Mathf.Sin(Time.time * 7 + phases[i]) * .03f;
                var near = body + across * .13f + heading * sweep;
                var far = body - across * .13f + heading * sweep;
                var root = body - heading * .05f;
                const float Z = 40;
                vertices[k] = new Vector3(body.x, body.y, Z);
                vertices[k + 1] = new Vector3(near.x, near.y, Z);
                vertices[k + 2] = new Vector3(far.x, far.y, Z);
                vertices[k + 3] = new Vector3(root.x, root.y, Z);
            }
        }
        mesh.vertices = vertices;
    }

    void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
        if (material != null)
            Destroy(material);
    }
}
