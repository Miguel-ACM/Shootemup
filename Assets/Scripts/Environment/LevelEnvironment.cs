using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Composed scrolling districts with the same ship-driven lateral parallax as Fields.</summary>
public sealed class LevelEnvironment : MonoBehaviour
{
    [Serializable]
    sealed class Kit
    {
        public MeshData[] meshes;
    }

    [Serializable]
    sealed class MeshData
    {
        public string name; public float[] vertices, colors; public int[] triangles;
    }

    // Reads LevelKit.json without any Unity API, so the 17 MB parse can stay off the
    // main thread. JsonUtility on a thread-pool worker allocated from the player's
    // native mimalloc heap alongside the main thread and corrupted it: the 9 November
    // build crashed inside mi_page_malloc_zero on the next Mesh.SetVertices.
    static class KitReader
    {
        public static Kit Parse(string json)
        {
            int i = 0;
            var kit = new Kit { meshes = Array.Empty<MeshData>() };
            Expect(json, ref i, '{');
            while (NextMember(json, ref i, out string key))
            {
                if (key == "meshes")
                    kit.meshes = ReadMeshes(json, ref i);
                else
                    Skip(json, ref i);
            }
            return kit;
        }

        static MeshData[] ReadMeshes(string json, ref int i)
        {
            var meshes = new List<MeshData>();
            Expect(json, ref i, '[');
            while (NextElement(json, ref i))
            {
                var mesh = new MeshData();
                Expect(json, ref i, '{');
                while (NextMember(json, ref i, out string key))
                {
                    switch (key)
                    {
                        case "name":
                            mesh.name = ReadString(json, ref i);
                            break;
                        case "vertices":
                            mesh.vertices = ReadFloats(json, ref i);
                            break;
                        case "colors":
                            mesh.colors = ReadFloats(json, ref i);
                            break;
                        case "triangles":
                            mesh.triangles = ReadInts(json, ref i);
                            break;
                        default:
                            Skip(json, ref i);
                            break;
                    }
                }
                meshes.Add(mesh);
            }
            return meshes.ToArray();
        }

        static float[] ReadFloats(string json, ref int i)
        {
            var values = new List<float>(1024);
            Expect(json, ref i, '[');
            while (NextElement(json, ref i))
                values.Add((float)ReadNumber(json, ref i));
            return values.ToArray();
        }

        static int[] ReadInts(string json, ref int i)
        {
            var values = new List<int>(1024);
            Expect(json, ref i, '[');
            while (NextElement(json, ref i))
                values.Add((int)Math.Round(ReadNumber(json, ref i)));
            return values.ToArray();
        }

        // Inside an object: true with the next key read (and its colon consumed), false at '}'.
        static bool NextMember(string json, ref int i, out string key)
        {
            key = null;
            SkipSpace(json, ref i);
            if (json[i] == ',')
            {
                i++;
                SkipSpace(json, ref i);
            }
            if (json[i] == '}')
            {
                i++;
                return false;
            }
            key = ReadString(json, ref i);
            Expect(json, ref i, ':');
            return true;
        }

        // Inside an array: true when another element follows, false at ']'.
        static bool NextElement(string json, ref int i)
        {
            SkipSpace(json, ref i);
            if (json[i] == ',')
            {
                i++;
                SkipSpace(json, ref i);
            }
            if (json[i] == ']')
            {
                i++;
                return false;
            }
            return true;
        }

        static void Expect(string json, ref int i, char c)
        {
            SkipSpace(json, ref i);
            if (json[i] != c)
                throw new FormatException("LevelKit: expected '" + c + "' at " + i);
            i++;
        }

        static void SkipSpace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
        }

        static string ReadString(string json, ref int i)
        {
            Expect(json, ref i, '"');
            var text = new System.Text.StringBuilder();
            while (json[i] != '"')
            {
                char c = json[i++];
                if (c == '\\')
                {
                    char e = json[i++];
                    switch (e)
                    {
                        case 'n': text.Append('\n'); break;
                        case 't': text.Append('\t'); break;
                        case 'r': text.Append('\r'); break;
                        case 'b': text.Append('\b'); break;
                        case 'f': text.Append('\f'); break;
                        case 'u':
                            text.Append((char)Convert.ToInt32(json.Substring(i, 4), 16));
                            i += 4;
                            break;
                        default: text.Append(e); break;
                    }
                }
                else
                    text.Append(c);
            }
            i++;
            return text.ToString();
        }

        // Decimal mantissa and exponent, no substring allocation per number.
        static double ReadNumber(string json, ref int i)
        {
            SkipSpace(json, ref i);
            bool negative = json[i] == '-';
            if (negative || json[i] == '+')
                i++;
            long mantissa = 0;
            int exponent = 0, digits = 0;
            for (; i < json.Length && json[i] >= '0' && json[i] <= '9'; i++)
                Digit(json[i], ref mantissa, ref exponent, ref digits, false);
            if (i < json.Length && json[i] == '.')
                for (i++; i < json.Length && json[i] >= '0' && json[i] <= '9'; i++)
                    Digit(json[i], ref mantissa, ref exponent, ref digits, true);
            if (i < json.Length && (json[i] == 'e' || json[i] == 'E'))
            {
                i++;
                bool down = json[i] == '-';
                if (down || json[i] == '+')
                    i++;
                int power = 0;
                for (; i < json.Length && json[i] >= '0' && json[i] <= '9'; i++)
                    power = power * 10 + (json[i] - '0');
                exponent += down ? -power : power;
            }
            double value = exponent == 0 ? mantissa : mantissa * Math.Pow(10, exponent);
            return negative ? -value : value;
        }

        // Keeps the first 18 significant digits; later ones only move the decimal point.
        static void Digit(char c, ref long mantissa, ref int exponent, ref int digits, bool fraction)
        {
            if (digits < 18)
            {
                mantissa = mantissa * 10 + (c - '0');
                if (mantissa != 0)
                    digits++;
                if (fraction)
                    exponent--;
            }
            else if (!fraction)
                exponent++;
        }

        // Skips any JSON value of a member this reader does not use.
        static void Skip(string json, ref int i)
        {
            SkipSpace(json, ref i);
            char c = json[i];
            if (c == '"')
            {
                ReadString(json, ref i);
                return;
            }
            if (c == '{' || c == '[')
            {
                int depth = 0;
                bool quoted = false;
                for (; i < json.Length; i++)
                {
                    char k = json[i];
                    if (quoted)
                    {
                        if (k == '\\')
                            i++;
                        else if (k == '"')
                            quoted = false;
                    }
                    else if (k == '"')
                        quoted = true;
                    else if (k == '{' || k == '[')
                        depth++;
                    else if ((k == '}' || k == ']') && --depth == 0)
                    {
                        i++;
                        return;
                    }
                }
                return;
            }
            while (i < json.Length && json[i] != ',' && json[i] != '}' && json[i] != ']')
                i++;
        }
    }

    sealed class Atmosphere
    {
        public Transform transform; public Material material; public float parallax, speed;
        // +1 drifts right; ordinary layers keep their original rightward travel.
        public float downwind = 1;
        // Blizzard whiteout banks breathe with the gusts.
        public float gustAlpha;
    }

    struct Flake
    {
        public Vector2 position; public float speed, size, phase, depth;
    }

    const float SectionLength = 20, LoopLength = 80;
    static Dictionary<string, Mesh> kit;
    static bool preparing, sourceReady;
    public static bool SourceReady => sourceReady || kit != null;
    static LevelEnvironment active;
    static readonly Dictionary<LevelId, LevelEnvironment> preparedRoutes = new Dictionary<LevelId, LevelEnvironment>();
    [Range(0, .5f)] public float sideParallax = .12f;
    public float HorizontalOffset
    {
        get; private set;
    }

    readonly Vector3[][] snowSurface = new Vector3[4][];
    public static bool TrySnowGround(Vector3 requested, out Transform tile, out Vector3 localPoint)
    {
        tile = null;
        localPoint = default;
        if (active == null || active.level != LevelId.Snow)
            return false;
        requested.x += active.HorizontalOffset;
        for (int i = 0; i < active.sections.Count; i++)
        {
            var section = active.sections[i];
            var p = section.InverseTransformPoint(requested);
            // Projected drift edges extend beyond the nominal tile boundary; try both neighbours at a seam.
            if (p.y < -10.15f || p.y > 10.15f)
                continue;
            var points = active.snowSurface[i];
            if (points == null)
                continue;
            float z = 1;
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
                float d = v0.x * v1.y - v1.x * v0.y;
                if (Mathf.Abs(d) < .000001f)
                    return;
                float u = (v2.x * v1.y - v1.x * v2.y) / d, v = (v0.x * v2.y - v2.x * v0.y) / d;
                if (u >= -.0001f && v >= -.0001f && u + v <= 1.0001f)
                    z = Mathf.Min(z, a.z + u * (b.z - a.z) + v * (c.z - a.z));
            }
            int row = Mathf.Clamp(Mathf.FloorToInt((p.y + 10) / .5f), 0, 39), col = Mathf.Clamp(Mathf.FloorToInt((p.x + 11) / (22f / 16)), 0, 15);
            void Quad(int k)
            {
                Triangle(points[k], points[k + 1], points[k + 2]);
                Triangle(points[k], points[k + 2], points[k + 3]);
            }
            for (int y = Mathf.Max(0, row - 1); y <= Mathf.Min(39, row + 1); y++)
                for (int x = Mathf.Max(0, col - 1); x <= Mathf.Min(15, col + 1); x++)
                    Quad((y * 16 + x) * 4);
            for (int k = 2560; k + 3 < points.Length; k += 4)
                Quad(k);
            if (z > 0)
                continue;
            tile = section;
            localPoint = new Vector3(p.x, p.y, z);
            return true;
        }
        return false;
    }

    public static bool TryDesertGround(Vector3 requested, float radius, Predicate<Vector3> available, out Transform section, out Vector3 localPoint)
    {
        section = null;
        localPoint = default;
        if (active == null || active.level != LevelId.Desert)
            return false;
        float half = UnityEngine.Camera.main.orthographicSize * 405f / 720f;
        // Search nearby clear sand before showing any disturbance. Terrain coordinates stay fixed for life.
        for (int ring = 0; ring < 4; ring++)
            for (int spoke = 0; spoke < (ring == 0 ? 1 : 12); spoke++)
            {
                float angle = spoke * Mathf.PI / 6;
                var candidate = requested + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (ring * .70f);
                if (Mathf.Abs(candidate.x) > Mathf.Min(Camera.RightLimit, half) - radius || candidate.y > Camera.UpLimit - 1 || candidate.y < Camera.DownLimit + 3)
                    continue;
                for (int i = 0; i < active.sections.Count; i++)
                {
                    var tile = active.sections[i];
                    var p = tile.InverseTransformPoint(candidate);
                    if (Mathf.Abs(p.y) > 10 - radius || Mathf.Abs(p.x) > 13 - radius)
                        continue;
                    if (!DesertGeometry.ClearSand(i, LevelRun.Seed, p, radius / .8f + .12f))
                        continue;
                    var local = DesertGeometry.SurfacePoint(i, LevelRun.Seed, p);
                    var world = tile.TransformPoint(local);
                    if (available != null && !available(world))
                        continue;
                    section = tile;
                    localPoint = local;
                    return true;
                }
            }
        return false;
    }

    Material material;
    Material snowFoliageMaterial, snowTerrainMaterial, cityShadowMaterial;
    Texture2D snowFoliageAtlas;
    SnowLandscape snowLandscape;
    Transform player, weatherTransform, lightningRoot;
    readonly List<Transform> sections = new List<Transform>();
    readonly List<Transform> foreground = new List<Transform>();
    readonly List<Atmosphere> atmosphere = new List<Atmosphere>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    readonly List<AudioClip> ownedAudio = new List<AudioClip>();
    Mesh weather;
    Vector3[] weatherVertices;
    Flake[] flakes;
    LevelId level;
    // The district's seeded condition and the side its wind blows from (-1 left, +1 right).
    DistrictCondition condition;
    int windSide;
    bool Blizzard => condition == DistrictCondition.Blizzard;
    float Downwind => -windSide;
    float travel, clock, stormAt, flash;
    AudioSource thunder;
    AudioSource rainAudio;
    // Blizzard and sandstorm wind, swelling with the gusts.
    AudioSource windAudio;
    DesertDustStorm dustStorm;
    Transform stormAudioRoot;
    bool stormAudioReleased;
    bool titleBackdrop;
    // The title's tunnel can be brought to a stop, so the hangar can take over from the very same view.
    static LevelEnvironment title;
    float travelRate = 1;
    public static bool TitleReady => title != null && title.sections.Count == 4;
    static LevelEnvironment carried;
    public static bool CarriesTitle => carried != null;

    /// <summary>Keeps the halted title tunnel alive into the hangar, so the hangar shows exactly the same view.</summary>
    public static void CarryTitle()
    {
        if (title == null)
            return;
        DontDestroyOnLoad(title.gameObject);
        carried = title;
    }

    public static void ReleaseTitle()
    {
        if (carried != null)
            Destroy(carried.gameObject);
        carried = null;
    }

    public const float TunnelSpeed = 10.2f;
    // How fast a district's ground scrolls past (world units per second at the normal rate).
    public static float ScrollSpeed(LevelId level) => level == LevelId.Tunnel ? TunnelSpeed : level == LevelId.City ? 1.8f : 4.4f;

    // Pin the live tile phase only when the dock camera is ready to move.
    public static float MoorTitle(float approach)
    {
        if (carried == null)
            return 0;
        carried.travelRate = 0;
        carried.transform.position = Vector3.down * approach;
        return carried.travel;
    }
    const float StormFadeSeconds = 3.5f;
    readonly LineRenderer[] bolts = new LineRenderer[3];
    System.Random random;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntime()
    {
        kit = null;
        active = null;
        preparing = sourceReady = false;
        preparedRoutes.Clear();
    }

    public static IEnumerator CreateTitleBackdrop()
    {
        yield return Prepare();
        var root = new GameObject("Title tunnel backdrop");
        var environment = root.AddComponent<LevelEnvironment>();
        environment.level = LevelId.Tunnel;
        environment.titleBackdrop = true;
        title = environment;
        var build = environment.BuildSteps(true);
        while (build.MoveNext())
            yield return build.Current;
    }

    /// <summary>Build an offered route while the player is still flying in the current level.</summary>
    public static IEnumerator PrepareRoute(LevelId next)
    {
        if (next == LevelId.Fields)
            yield break; // The valley is already present in the gameplay scene.
        if (preparedRoutes.TryGetValue(next, out var ready) && ready != null)
            yield break;
        yield return Prepare();
        var root = new GameObject(next + " prepared environment");
        root.SetActive(false);
        var environment = root.AddComponent<LevelEnvironment>();
        environment.level = next;
        bool complete = false;
        try
        {
            var steps = environment.BuildSteps(true);
            while (steps.MoveNext())
                yield return steps.Current;
            // Only fully built routes may be activated. Cancellation discards partial builds.
            preparedRoutes[next] = environment;
            complete = true;
        }
        finally { if (!complete && root != null) Destroy(root); }
    }

    public static void Apply()
    {
        FadeOutStorm();
        if (active != null)
        {
            active.gameObject.SetActive(false);
            Destroy(active.gameObject);
        }
        float parallax = .12f;
        foreach (var field in FindObjectsByType<PSXGrassField>(FindObjectsInactive.Include))
        {
            parallax = field.sideParallax;
            field.gameObject.SetActive(LevelRun.Current == LevelId.Fields);
        }
        // Retain the existing camera/compositor setup without its old disconnected scenery.
        foreach (var industrial in FindObjectsByType<PSXIndustrialField>())
            industrial.steelMaterial = null;
        if (LevelRun.Current != LevelId.Fields)
        {
            if (preparedRoutes.TryGetValue(LevelRun.Current, out var ready) && ready != null)
                active = ready;
            else
            {
                active = new GameObject(LevelRun.CurrentName + " environment").AddComponent<LevelEnvironment>();
                active.level = LevelRun.Current;
                active.Build();
            }
            active.sideParallax = parallax;
            active.gameObject.name = LevelRun.CurrentName + " environment";
            active.gameObject.SetActive(true);
            active.ApplyParallax();
            foreach (var sound in active.GetComponentsInChildren<AudioSource>())
                if (sound.loop && !sound.isPlaying)
                    sound.Play();
        }
        foreach (var route in preparedRoutes.Values)
            if (route != null && route != active)
                Destroy(route.gameObject);
        preparedRoutes.Clear();
        WavePreparation.Begin();
    }

    // A THUNDERSTORM strike lights the storm clouds behind the arena.
    public static void StrikeFlash()
    {
        if (active != null && active.level == LevelId.Thunder)
            active.flash = Mathf.Max(active.flash, .62f);
    }

    public static void FadeOutStorm()
    {
        if (active == null || active.stormAudioRoot == null || active.stormAudioReleased)
            return;
        active.stormAudioReleased = true;
        // Let the existing rain and thunder trail off after their scenery has gone away.
        var audioRoot = active.stormAudioRoot.gameObject;
        audioRoot.transform.SetParent(null, true);
        DontDestroyOnLoad(audioRoot);
        CloudTransition.FadeOutAudio(audioRoot, active.ownedAudio.ToArray(), StormFadeSeconds);
        active.ownedAudio.Clear(); // The fading audio now owns these clips.
    }

    /// <summary>A shared scenery mesh from the level kit; the hangar reuses a Tunnel section.</summary>
    public static Mesh KitMesh(string name)
    {
        LoadKit();
        return kit.TryGetValue(name, out var mesh) ? mesh : null;
    }

    static void LoadKit()
    {
        if (kit != null)
            return;
        var data = KitReader.Parse(Resources.Load<TextAsset>("LevelKit").text);
        var loaded = new Dictionary<string, Mesh>();
        foreach (var item in data.meshes)
            loaded.Add(item.name, CreateMesh(item));
        kit = loaded;
    }

    /// <summary>Prepare shared scenery in the background before scene activation needs the meshes.</summary>
    public static IEnumerator Prepare()
    {
        if (kit != null)
            yield break;
        if (preparing)
        {
            while (preparing && kit == null)
                yield return null;
            yield break;
        }
        preparing = true;
        var loaded = new Dictionary<string, Mesh>();
        try
        {
            var request = Resources.LoadAsync<TextAsset>("LevelKit");
            yield return request;
            string json = ((TextAsset)request.asset).text;
            // Finish the resource request before parking a scene load at allowSceneActivation=false.
            sourceReady = true;
            // Pure managed parse on a worker. No Unity API may run there: JsonUtility shared
            // the native heap with the main thread and corrupted it. Mesh APIs stay on main.
            var parsing = Task.Run(() => KitReader.Parse(json));
            while (!parsing.IsCompleted)
                yield return null;
            var budget = Stopwatch.StartNew();
            foreach (var item in parsing.GetAwaiter().GetResult().meshes)
            {
                // A direct scene entry can have populated the synchronous fallback in the meantime.
                if (kit != null)
                    yield break;
                loaded.Add(item.name, CreateMesh(item));
                if (budget.ElapsedMilliseconds >= 3)
                {
                    yield return null;
                    budget.Restart();
                }
            }
            kit = loaded;
        }
        finally
        {
            if (kit != loaded)
                foreach (var mesh in loaded.Values)
                    Destroy(mesh);
            preparing = false;
        }
    }

    static Mesh CreateMesh(MeshData item)
    {
        int count = item.vertices.Length / 3;
        var vertices = new Vector3[count];
        var colors = new Color[count];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = new Vector3(item.vertices[i * 3], item.vertices[i * 3 + 1], item.vertices[i * 3 + 2]);
            colors[i] = new Color(item.colors[i * 4], item.colors[i * 4 + 1], item.colors[i * 4 + 2], item.colors[i * 4 + 3]);
        }
        var mesh = new Mesh { name = item.name, indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = item.triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    void Build()
    {
        var steps = BuildSteps(false);
        while (steps.MoveNext())
        {
        }
    }

    IEnumerator BuildSteps(bool background)
    {
        LoadKit();
        random = new System.Random(LevelRun.Seed + 810 + (int)level);
        // Routes are prepared while the previous district is still flying: ask for this one's.
        condition = DistrictConditions.For(level, LevelRun.Seed);
        windSide = DistrictConditions.Side(level, LevelRun.Seed);
        material = new Material(Resources.Load<Shader>("LevelScenery"));
        if (level == LevelId.Snow)
        {
            snowLandscape = new SnowLandscape(LevelRun.Seed);
            snowTerrainMaterial = new Material(Resources.Load<Shader>("SnowSurface"));
            snowFoliageAtlas = PSXValleyFoliage.CreateAtlas(true);
            snowFoliageMaterial = new Material(Resources.Load<Shader>("PSXValleyFoliage")) { mainTexture = snowFoliageAtlas };
        }
        if (level == LevelId.City)
            material.SetFloat("_Haze", .08f);
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
            player = go.transform;
        yield return null;
        if (level == LevelId.Void)
        {
            var abyss = new GameObject("Void / black liquid falls");
            abyss.transform.SetParent(transform, false);
            abyss.AddComponent<VoidBackdrop>();
        }
        else if (level == LevelId.Heaven)
        {
            var heaven = new GameObject("Heaven / the shattered firmament");
            heaven.transform.SetParent(transform, false);
            heaven.AddComponent<HeavenBackdrop>();
        }
        else if (level == LevelId.Hell)
        {
            var caldera = new GameObject("Hell basalt caldera");
            caldera.transform.SetParent(transform, false);
            caldera.AddComponent<InfernalCaldera>();
        }
        else if (level == LevelId.Underwater)
        {
            var depth = new GameObject(level + " depth and atmosphere");
            depth.transform.SetParent(transform, false);
            depth.AddComponent<DepthBackdrop>().infernal = level == LevelId.Hell;
        }
        else if (level == LevelId.Space)
        {
            var orbit = new GameObject("Distant planets and starfield");
            orbit.transform.SetParent(transform, false);
            orbit.AddComponent<SpaceBackdrop>();
        }
        else if (level == LevelId.Candyland)
        {
            var candy = new GameObject("Candyland sky and falling sweets");
            candy.transform.SetParent(transform, false);
            candy.AddComponent<CandylandBackdrop>();
        }
        else if (level == LevelId.Thunder)
        {
            var storm = BuildStormSteps(background);
            while (storm.MoveNext())
                yield return storm.Current;
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                // All four authored sections form one district. Their boundaries match after a full wrap.
                var section = new GameObject(level + " district " + i).transform;
                section.SetParent(transform, false);
                section.localPosition = new Vector3(0, (i - 1) * SectionLength, 90);
                section.localScale = new Vector3(.8f, 1, 1);
                if (level == LevelId.Desert)
                {
                    var sand = DesertGeometry.BuildSection(i, LevelRun.Seed, background, (ground, detail) =>
                    {
                        ownedMeshes.Add(ground);
                        ownedMeshes.Add(detail);
                        Add(ground, section, Vector3.zero, -50);
                        Add(detail, section, Vector3.zero, -40);
                    });
                    while (sand.MoveNext()) yield return sand.Current;
                }
                else if (level == LevelId.Snow)
                {
                    var ground = SnowLandscapeGeometry.Ground(i, snowLandscape);
                    ownedMeshes.Add(ground);
                    snowSurface[i] = ground.vertices;
                    Add(ground, section, Vector3.zero, -50).GetComponent<MeshRenderer>().sharedMaterial = snowTerrainMaterial;
                    var detail = SnowLandscapeGeometry.Detail(i, snowLandscape);
                    ownedMeshes.Add(detail);
                    Add(detail, section, Vector3.zero, -40);
                    var leaves = PSXValleyFoliage.Build(i * 20 - 10, 20, snowLandscape.Plants, snowLandscape.Height);
                    ownedMeshes.Add(leaves);
                    // Leaf builder uses a section-start origin; the terrain uses its centre.
                    Add(leaves, section, new Vector3(0, -10, 0), -39).GetComponent<MeshRenderer>().sharedMaterial = snowFoliageMaterial;
                }
                else if (level == LevelId.City)
                {
                    // A destroyed city, rebuilt from the seed (Miguel, 15 November 2026): no street grid.
                    var ruinedGround = CityRuinsGeometry.Ground(LevelRun.Seed, i);
                    ownedMeshes.Add(ruinedGround);
                    Add(ruinedGround, section, Vector3.zero, -50);
                }
                else
                    Add(kit[level + "Ground" + i], section, Vector3.zero, -50);
                if (level == LevelId.City)
                {
                    var buildings = CityRuinsGeometry.Build(LevelRun.Seed, i, out var shadows);
                    ownedMeshes.Add(buildings);
                    ownedMeshes.Add(shadows);
                    Add(buildings, section, Vector3.zero, -40);
                    // Drawn after the ruins without writing depth: shadows land on the ground and
                    // rubble but never on the broken walls above them.
                    if (cityShadowMaterial == null)
                    {
                        cityShadowMaterial = new Material(material) { name = "City sun shadows" };
                        cityShadowMaterial.SetFloat("_ZWrite", 0);
                    }
                    Add(shadows, section, Vector3.zero, -39).GetComponent<MeshRenderer>().sharedMaterial = cityShadowMaterial;
                }
                else if (level != LevelId.Desert && level != LevelId.Snow)
                    Add(kit[level + "Detail" + i], section, Vector3.zero, -40);
                sections.Add(section);
                var nearMesh = level == LevelId.Desert ? DesertGeometry.Near(i, LevelRun.Seed) : level == LevelId.Snow ? SnowLandscapeGeometry.Detail(i, snowLandscape, true) :
                    level == LevelId.City ? CityRuinsGeometry.Near(LevelRun.Seed, i) : kit[level + "Near" + i];
                if (level == LevelId.Desert || level == LevelId.Snow || level == LevelId.City)
                    ownedMeshes.Add(nearMesh);
                var near = Add(nearMesh, transform, new Vector3(0, (i - 1) * SectionLength, 76), -25);
                near.localScale = new Vector3(.8f, 1, 1);
                foreground.Add(near);
                if (level == LevelId.Snow)
                {
                    var leaves = PSXValleyFoliage.Build(i * 20 - 10, 20, snowLandscape.NearPlants, snowLandscape.Height);
                    ownedMeshes.Add(leaves);
                    Add(leaves, near, new Vector3(0, -10, 0), -24).GetComponent<MeshRenderer>().sharedMaterial = snowFoliageMaterial;
                }
                yield return null;
            }
            Color tint = level == LevelId.Snow ? new Color(.74f, .85f, .89f, .22f) : level == LevelId.City ? new Color(.52f, .60f, .66f, .08f) : new Color(.10f, .16f, .19f, .10f);
            AddAtmosphere("Low drifting haze", 0, 44, tint, 1.9f, level == LevelId.Snow ? .55f : .20f);
            if (level == LevelId.Snow)
            {
                AddAtmosphere("Snowdust over the drifts", 3, 42, new Color(.83f, .90f, .92f, .38f), 1.5f, 1.1f);
                AddAtmosphere("Near windblown powder", 3, 35, new Color(.89f, .94f, .95f, .21f), 2.5f, 1.8f);
                if (Blizzard)
                {
                    BuildBlizzard();
                    BuildWind("BlizzardWind");
                }
                yield return null;
            }
            if (level == LevelId.Desert)
            {
                var dust = new GameObject("Desert dust storms");
                dust.transform.SetParent(transform, false);
                var storm = dust.AddComponent<DesertDustStorm>();
                storm.Sandstorm = condition == DistrictCondition.Sandstorm;
                storm.Downwind = Downwind;
                dustStorm = storm;
                if (storm.Sandstorm)
                    BuildWind("SandstormWind");
            }
            if (level == LevelId.City)
            {
                // Smoke drifts over the ruins; no traffic runs through them.
                AddAtmosphere("Smoke over the ruins", 0, 35, new Color(.30f, .28f, .27f, .16f), 2.6f, .32f);
            }
        }
        if (level == LevelId.Snow || level == LevelId.Thunder)
        {
            var precipitation = BuildWeatherSteps();
            while (precipitation.MoveNext())
                yield return precipitation.Current;
        }
        ApplyParallax();
    }

    Transform Add(Mesh mesh, Transform parent, Vector3 position, int order)
    {
        var go = new GameObject(mesh.name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }

    Mesh Quad(string name, float width, float height)
    {
        var mesh = new Mesh { name = name };
        mesh.vertices = new[] { new Vector3(-width / 2, -height / 2), new Vector3(width / 2, -height / 2), new Vector3(width / 2, height / 2), new Vector3(-width / 2, height / 2) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        ownedMeshes.Add(mesh);
        return mesh;
    }

    void AddAtmosphere(string name, int kind, float z, Color tint, float parallax, float speed)
    {
        var plane = Add(Quad(name, 26, 38), transform, new Vector3(0, 0, z), kind == 1 ? -60 : -9);
        var mat = new Material(Resources.Load<Shader>("LevelAtmosphere"));
        mat.SetFloat("_Kind", kind);
        mat.SetColor("_Tint", tint);
        plane.GetComponent<MeshRenderer>().sharedMaterial = mat;
        atmosphere.Add(new Atmosphere { transform = plane, material = mat, parallax = parallax, speed = speed });
    }

    // A whiteout: the ground pales under cold light, banks of driven snow race downwind and
    // gust over the drifts, and the falling snow turns into a slanting storm.
    void BuildBlizzard()
    {
        foreach (var layer in atmosphere)
        {
            layer.downwind = Downwind;
            layer.speed *= 2.2f;
        }
        AddAtmosphere("Whiteout banks", 3, 38, new Color(.93f, .96f, .98f, .55f), 2.0f, 3.2f);
        var whiteout = atmosphere[atmosphere.Count - 1];
        whiteout.downwind = Downwind;
        whiteout.gustAlpha = .55f;
        AddAtmosphere("Driven spindrift", 3, 33, new Color(.96f, .98f, 1, .34f), 2.8f, 4.4f);
        atmosphere[atmosphere.Count - 1].downwind = Downwind;
        // Above the near drifts (-24), below the storm's own atmosphere layers (-9).
        SceneryGrade.Create(transform, "Blizzard light", -22)
            .Multiply(new Color(.74f, .82f, .94f), new Color(.82f, .88f, .96f), windSide, 1, .10f)
            .Glow(new Color(.15f, .16f, .18f), new Color(.08f, .09f, .10f), windSide, .7f);
    }

    // A looping storm bed from the generated wind recordings (ArtSource/AudioReview/Storms).
    void BuildWind(string clip)
    {
        if (stormAudioRoot == null)
        {
            stormAudioRoot = new GameObject("Storm ambience").transform;
            stormAudioRoot.SetParent(transform, false);
        }
        var wind = new GameObject(clip);
        wind.transform.SetParent(stormAudioRoot, false);
        windAudio = wind.AddComponent<AudioSource>();
        windAudio.clip = Resources.Load<AudioClip>("Sounds/Ambience/" + clip);
        windAudio.playOnAwake = false;
        windAudio.loop = true;
        windAudio.spatialBlend = 0;
        windAudio.priority = 40;
        windAudio.volume = 0;
        var manager = FindAnyObjectByType<SoundManager>();
        if (manager != null)
            windAudio.outputAudioMixerGroup = manager.GetComponent<AudioSource>().outputAudioMixerGroup;
    }

    void UpdateWind()
    {
        if (windAudio == null || stormAudioReleased)
            return;
        float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(clock / StormFadeSeconds));
        float swell = Blizzard ? .55f + .45f * Gust : .55f + .45f * (dustStorm != null ? dustStorm.Strength : 1);
        // The blizzard's howl sits well under the combat mix; the sandstorm's hiss a little louder.
        float level = Blizzard ? .13f : .2f;
        windAudio.volume = Mathf.MoveTowards(windAudio.volume, level * fade * swell, Time.deltaTime * .4f);
    }

    IEnumerator BuildStormSteps(bool background)
    {
        AddAtmosphere("Deep rolling thunderheads", 1, 102, new Color(.19f, .24f, .29f, 1), .65f, .34f);
        AddAtmosphere("Low torn storm clouds", 2, 63, new Color(.14f, .19f, .24f, .88f), 1.65f, .67f);
        AddAtmosphere("Windblown rain mist", 0, 44, new Color(.36f, .47f, .58f, .14f), 2.1f, 1.0f);
        lightningRoot = new GameObject("Lightning in the cloud banks").transform;
        lightningRoot.SetParent(transform, false);
        for (int i = 0; i < bolts.Length; i++)
        {
            var go = new GameObject(i == 0 ? "Lightning main stroke" : "Lightning fork " + i);
            go.transform.SetParent(lightningRoot, false);
            var bolt = go.AddComponent<LineRenderer>();
            bolt.useWorldSpace = false;
            bolt.sharedMaterial = material;
            bolt.startColor = bolt.endColor = new Color(.73f, .85f, 1);
            bolt.startWidth = i == 0 ? .065f : .035f;
            bolt.endWidth = .008f;
            bolt.sortingLayerName = "Background";
            bolt.sortingOrder = -11;
            bolt.enabled = false;
            bolts[i] = bolt;
            yield return null;
        }
        stormAudioRoot = new GameObject("Storm ambience").transform;
        stormAudioRoot.SetParent(transform, false);
        thunder = stormAudioRoot.gameObject.AddComponent<AudioSource>();
        thunder.playOnAwake = false;
        thunder.volume = 0;
        stormAt = 1.7f;
        if (background)
        {
            var noise = CreateNoiseAsync(false, clip => thunder.clip = clip);
            while (noise.MoveNext())
                yield return noise.Current;
        }
        else
            thunder.clip = CreateNoise(false);
        var rain = new GameObject("Continuous rain");
        rain.transform.SetParent(stormAudioRoot, false);
        var sound = rain.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.loop = true;
        sound.volume = 0;
        rainAudio = sound;
        if (background)
        {
            var noise = CreateNoiseAsync(true, clip => sound.clip = clip);
            while (noise.MoveNext())
                yield return noise.Current;
        }
        else
            sound.clip = CreateNoise(true);
    }

    IEnumerator BuildWeatherSteps()
    {
        bool snow = level == LevelId.Snow;
        flakes = new Flake[snow ? Blizzard ? 2600 : 1600 : 1150];
        weatherVertices = new Vector3[flakes.Length * 4];
        var colors = new Color[weatherVertices.Length];
        var triangles = new int[flakes.Length * 6];
        for (int i = 0; i < flakes.Length; i++)
        {
            float depth = Range(.45f, 1.5f);
            flakes[i] = new Flake { position = new Vector2(Range(-12, 12), Range(-16, 18)), speed = Range(.65f, 1.4f), size = Range(.012f, .047f) * depth, phase = Range(0, 6.28f), depth = depth };
            int k = i * 4, t = i * 6;
            triangles[t] = k;
            triangles[t + 1] = k + 2;
            triangles[t + 2] = k + 1;
            triangles[t + 3] = k;
            triangles[t + 4] = k + 3;
            triangles[t + 5] = k + 2;
            var color = snow ? new Color(.85f, .93f, .96f, Range(.38f, .85f)) : new Color(.52f, .66f, .81f, Range(.13f, .40f));
            for (int j = 0; j < 4; j++)
                colors[k + j] = color;
            if ((i + 1) % 128 == 0)
                yield return null;
        }
        yield return null;
        weather = new Mesh { name = level + " precipitation" };
        weather.MarkDynamic();
        weather.vertices = weatherVertices;
        weather.colors = colors;
        weather.triangles = triangles;
        weather.bounds = new Bounds(Vector3.zero, new Vector3(32, 42, 2));
        ownedMeshes.Add(weather);
        weatherTransform = Add(weather, transform, new Vector3(0, 0, 32), -5);
        UpdateWeather(0);
    }

    void Update()
    {
        float dt = titleBackdrop ? Time.unscaledDeltaTime : Time.deltaTime;
        clock += dt;
        travel = Mathf.Repeat(travel + dt * travelRate * ScrollSpeed(level), LoopLength);
        for (int i = 0; i < sections.Count; i++)
        {
            float y = SectionY(i, travel);
            sections[i].localPosition = new Vector3(HorizontalOffset, y, 90);
            foreground[i].localPosition = new Vector3(HorizontalOffset * 1.65f, y, 76);
        }
        if (flakes != null)
            UpdateWeather(dt);
        if (snowFoliageMaterial != null)
            snowFoliageMaterial.SetFloat("_FlowTime", clock);
        if (level == LevelId.Thunder)
            UpdateStorm(dt);
        UpdateWind();
        float gust = Blizzard ? Gust : 0;
        foreach (var layer in atmosphere)
        {
            layer.material.SetVector("_Drift", new Vector4(-clock * layer.speed * .11f * layer.downwind, clock * layer.speed * .18f, 0, 0));
            if (layer.gustAlpha > 0)
            {
                var tint = layer.material.GetColor("_Tint");
                tint.a = layer.gustAlpha * (.45f + .55f * gust);
                layer.material.SetColor("_Tint", tint);
            }
            layer.material.SetFloat("_Flash", flash > .65f ? flash * .62f : flash > .4f ? .02f : flash * .7f);
        }
    }

    public static float SectionY(int index, float distance) => Mathf.Repeat((index - 1) * SectionLength - distance + 30, LoopLength) - 30;

    void LateUpdate()
    {
        ApplyParallax();
    }

    void ApplyParallax()
    {
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.transform;
        }
        // The title's tunnel never follows a ship, including when it is carried into the hangar.
        HorizontalOffset = player != null && !titleBackdrop ? -player.position.x * sideParallax : 0;
        foreach (var section in sections)
        {
            var p = section.localPosition;
            p.x = HorizontalOffset;
            section.localPosition = p;
        }
        foreach (var near in foreground)
        {
            var p = near.localPosition;
            p.x = HorizontalOffset * 1.65f;
            near.localPosition = p;
        }
        foreach (var layer in atmosphere)
            layer.transform.localPosition = new Vector3(HorizontalOffset * layer.parallax, 0, layer.transform.localPosition.z);
        if (weatherTransform != null)
            weatherTransform.localPosition = new Vector3(HorizontalOffset * 2.2f, 0, 32);
        if (lightningRoot != null)
            lightningRoot.localPosition = new Vector3(HorizontalOffset * 1.65f, 0, 0);
    }

    void UpdateStorm(float dt)
    {
        if (!stormAudioReleased)
        {
            float volume = Mathf.SmoothStep(0, 1, Mathf.Clamp01(clock / StormFadeSeconds));
            thunder.volume = .24f * volume;
            rainAudio.volume = .035f * volume;
        }
        if (clock >= stormAt)
        {
            stormAt = clock + Range(3.4f, 6.7f);
            flash = 1;
            if (!stormAudioReleased)
            {
                thunder.pitch = Range(.8f, 1.1f);
                thunder.PlayDelayed(.2f);
            }
            float x = Range(-3.8f, 3.8f);
            var main = new Vector3[10];
            for (int i = 0; i < main.Length; i++)
            {
                x += Range(-.6f, .6f);
                main[i] = new Vector3(x, 13 - i * 2.7f, 47);
            }
            bolts[0].positionCount = main.Length;
            bolts[0].SetPositions(main);
            for (int b = 1; b < bolts.Length; b++)
            {
                var start = main[b * 2 + 1];
                var points = new Vector3[5];
                for (int i = 0; i < points.Length; i++)
                    points[i] = start + new Vector3((b == 1 ? -1 : 1) * i * .52f + Range(-.2f, .2f), -i * 1.1f, 0);
                points[0] = start;
                bolts[b].positionCount = points.Length;
                bolts[b].SetPositions(points);
            }
        }
        flash = Mathf.MoveTowards(flash, 0, dt * 2.8f);
        bool visible = flash > .68f || (flash < .39f && flash > .27f);
        foreach (var bolt in bolts)
            bolt.enabled = visible;
        material.SetFloat("_Flash", flash > .65f ? flash * .45f : flash > .4f ? .02f : flash * .5f);
    }

    // 0 calm to 1 at a blizzard gust's peak: a slow swell with a few sharp gusts.
    float Gust => Mathf.Clamp01(.45f + .30f * Mathf.Sin(clock * .41f) + .25f * Mathf.Sin(clock * 1.07f + 2) * Mathf.Sin(clock * .23f));

    void UpdateWeather(float dt)
    {
        bool snow = level == LevelId.Snow;
        float gust = Mathf.Sin(clock * .53f) * .5f + Mathf.Sin(clock * .21f + 1) * .5f;
        float wind = snow ? -2.6f + gust * 2.6f : -4.0f + gust * 1.8f;
        if (Blizzard)
            wind = Downwind * (4.2f + 3.6f * Gust);
        for (int i = 0; i < flakes.Length; i++)
        {
            var f = flakes[i];
            f.position.y -= dt * f.speed * (snow ? Blizzard ? 7.5f : 5.5f : 24);
            f.position.x += dt * (wind + (snow ? Mathf.Sin(clock * 1.5f + f.phase) * f.depth : 0)) * f.depth;
            f.position.y = Mathf.Repeat(f.position.y + 16, 34) - 16;
            f.position.x = Mathf.Repeat(f.position.x + 12, 24) - 12;
            flakes[i] = f;
            float w = snow ? f.size : .009f * f.depth;
            float h = snow ? f.size * .72f : .4f * f.speed * f.depth;
            float slant = snow ? Mathf.Sin(clock + f.phase) * w * .6f : -wind / 24 * h;
            if (Blizzard)
            {
                // Driven snow streaks along the wind instead of tumbling.
                w *= 1 + Mathf.Abs(wind) * .32f;
                h *= 1.3f;
                slant = 0;
            }
            int k = i * 4;
            var p = (Vector3)f.position;
            weatherVertices[k] = p + new Vector3(-w, -h);
            weatherVertices[k + 1] = p + new Vector3(w, -h);
            weatherVertices[k + 2] = p + new Vector3(w + slant, h);
            weatherVertices[k + 3] = p + new Vector3(-w + slant, h);
        }
        weather.vertices = weatherVertices;
    }

    AudioClip CreateNoise(bool rain)
    {
        return CreateNoiseClip(rain, GenerateNoise(rain, random.Next()));
    }

    IEnumerator CreateNoiseAsync(bool rain, Action<AudioClip> ready)
    {
        int seed = random.Next();
        var task = Task.Run(() => GenerateNoise(rain, seed));
        while (!task.IsCompleted)
            yield return null;
        ready(CreateNoiseClip(rain, task.GetAwaiter().GetResult()));
    }

    // Pure managed work: no Unity objects or native audio calls on the worker thread.
    static float[] GenerateNoise(bool rain, int seed)
    {
        const int rate = 22050;
        var samples = new float[rate * 4];
        float low = 0;
        var noiseRandom = new System.Random(seed);
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float noise = (float)noiseRandom.NextDouble() * 2 - 1;
            low += (noise - low) * (rain ? .12f : .035f);
            float seam = Math.Min(1, Math.Min(t, 4 - t) * 20);
            samples[i] = rain ? (noise * .14f + low * .35f) * seam :
                (low * .85f + (float)Math.Sin(t * 62) * .05f) * (float)Math.Exp(-t * 1.2f) * Math.Min(1, t * 35);
        }
        return samples;
    }

    AudioClip CreateNoiseClip(bool rain, float[] samples)
    {
        var clip = AudioClip.Create(rain ? "Rain wash" : "Storm rumble", samples.Length, 1, 22050, false);
        clip.SetData(samples, 0);
        ownedAudio.Add(clip);
        return clip;
    }

    float Range(float lo, float hi) => Mathf.Lerp(lo, hi, (float)random.NextDouble());
    void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (snowFoliageMaterial != null)
            Destroy(snowFoliageMaterial);
        if (snowTerrainMaterial != null)
            Destroy(snowTerrainMaterial);
        if (cityShadowMaterial != null)
            Destroy(cityShadowMaterial);
        if (snowFoliageAtlas != null)
            Destroy(snowFoliageAtlas);
        foreach (var layer in atmosphere)
            if (layer.material != null)
                Destroy(layer.material);
        foreach (var clip in ownedAudio)
            if (clip != null)
                Destroy(clip);
        foreach (var mesh in ownedMeshes)
            if (mesh != null)
                Destroy(mesh);
        if (active == this)
            active = null;
    }
}
