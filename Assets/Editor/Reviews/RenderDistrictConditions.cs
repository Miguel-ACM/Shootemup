#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Edit-mode stills of the seeded district conditions and the rebuilt City, built from the
// runtime scenery code without starting a run. Batch: -executeMethod RenderDistrictConditions.Run
// Images omit the PSX composite filter, moving fog and animated shader time.
public static class RenderDistrictConditions
{
    const string Folder = "ArtSource/DistrictConditionsReview";
    const int Width = 300, Height = 520;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static Texture2D bulletTexture;

    // Batch: -executeMethod RenderDistrictConditions.Events
    // Stills of each district event's hazard over its scenery, the twelve tunnel layouts and
    // the phase break's implosion and rupture. Hazards are posed by hand (no play mode).
    public static void Events()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            var cells = new Texture2D[2, 4];
            cells[0, 0] = EventStill(LevelId.Thunder, DistrictCondition.Thunderstorm, () =>
            {
                LightningStrike.Begin(-1.6f, 11);
                LightningStrike.Begin(1.4f, 12);
                var strikes = UnityEngine.Object.FindObjectsByType<LightningStrike>(FindObjectsInactive.Exclude);
                // One flickering warning and one live bolt.
                PoseLines(strikes[0], new[] { new Color(1, .86f, .55f, .7f), Color.clear, Color.clear });
                PoseLines(strikes[1], new[] { Color.clear, new Color(1, .62f, .18f, .5f), Color.white });
            });
            cells[0, 1] = EventStill(LevelId.Heaven, DistrictCondition.SpearRain, () =>
            {
                for (int i = 0; i < 4; i++)
                    SpearStrike.Begin(-3 + i * 2);
                float t = 0;
                var gold = LevelProjectilePalette.Accent(LevelId.Heaven);
                foreach (var spear in UnityEngine.Object.FindObjectsByType<SpearStrike>(FindObjectsInactive.Exclude))
                {
                    t += .3f;
                    PoseLines(spear, new[] { new Color(gold.r, gold.g, gold.b, .12f), new Color(gold.r, gold.g, gold.b, .4f + t * .4f), Color.white });
                }
            });
            cells[0, 2] = EventStill(LevelId.Candyland, DistrictCondition.CandyRain, () =>
            {
                for (int i = 0; i < 4; i++)
                {
                    var candy = CandyDrop.Drop(-3 + i * 2, i);
                    candy.transform.position = new Vector3(-3 + i * 2, 4 - i * 2.2f, 1);
                }
            });
            cells[0, 3] = EventStill(LevelId.Underwater, DistrictCondition.Minefield, () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var mine = DepthMine.Rise(new Vector3(-2.5f + i * 2.5f, 1.5f - i, 1));
                    mine.Hull.localScale = Vector3.one * mine.hullScale * (.35f + i * .32f);
                }
                DistrictEnemy.Spawn(typeof(DepthMine), new Vector3(0, 5, 1));
            });
            cells[1, 0] = EventStill(LevelId.Hell, DistrictCondition.Eruption, () =>
            {
                foreach (var vent in UnityEngine.Object.FindObjectsByType<VolcanicVent>(FindObjectsInactive.Exclude))
                {
                    if (!vent.Eruption)
                        continue;
                    typeof(VolcanicVent).GetMethod("Start", Hidden).Invoke(vent, null);
                    typeof(VolcanicVent).GetMethod("ShowWarning", Hidden).Invoke(vent, new object[] { vent.transform.position, .75f });
                }
            });
            cells[1, 1] = EventStill(LevelId.City, DistrictCondition.RushHour, () => { });
            cells[1, 2] = PhaseBreakStill(.18f);
            cells[1, 3] = PhaseBreakStill(.45f);
            Save("Events.png", cells);
            Tunnels();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            code = 1;
        }
        DistrictConditions.Forced = null;
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    static void PoseLines(Component owner, Color[] colours)
    {
        var lines = owner.GetComponentsInChildren<LineRenderer>();
        for (int i = 0; i < lines.Length && i < colours.Length; i++)
            lines[i].startColor = lines[i].endColor = colours[i];
    }

    static Texture2D EventStill(LevelId level, DistrictCondition condition, Action stage)
    {
        DistrictConditions.Forced = condition;
        LevelRun.Reset(level, 5150);
        var root = Environment(level);
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>())
        {
            if (behaviour is LevelEnvironment || behaviour is DesertDustStorm || behaviour is VolcanicVent)
                continue;
            var start = behaviour.GetType().GetMethod("Start", Hidden);
            if (start != null && start.GetParameters().Length == 0)
            {
                var result = start.Invoke(behaviour, null);
                if (result is System.Collections.IEnumerator steps)
                    while (steps.MoveNext())
                    {
                    }
            }
        }
        stage();
        var camera = Camera(-.6f, 8.85f);
        var image = Capture(camera);
        foreach (var leftover in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (leftover is LightningStrike || leftover is SpearStrike || leftover is CandyDrop || leftover is DistrictEnemy)
                leftover.gameObject.SetActive(false);
        foreach (var line in UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude))
            line.gameObject.SetActive(false);
        root.SetActive(false);
        camera.gameObject.SetActive(false);
        return image;
    }

    static Texture2D PhaseBreakStill(float age)
    {
        LevelRun.Reset(LevelId.Snow, 5150);
        var root = Environment(LevelId.Snow);
        var boss = DistrictEnemy.Spawn(typeof(SnowSiegeBoss), new Vector3(0, 2.5f, 1));
        PhaseBreak.Play(boss.transform.position, boss.Hull);
        var effect = UnityEngine.Object.FindAnyObjectByType<PhaseBreak>();
        typeof(PhaseBreak).GetField("age", Hidden).SetValue(effect, age);
        typeof(PhaseBreak).GetMethod("PunchHull", Hidden).Invoke(effect, null);
        effect.Pose(age);
        var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Snow), -.6f);
        var camera = Camera(-.6f, 8.85f);
        var image = Capture(camera);
        effect.gameObject.SetActive(false);
        boss.gameObject.SetActive(false);
        combat.SetActive(false);
        root.SetActive(false);
        camera.gameObject.SetActive(false);
        return image;
    }

    // All twelve tunnel layouts side by side.
    static void Tunnels()
    {
        var cells = new Texture2D[2, 6];
        for (int layout = 1; layout <= 12; layout++)
        {
            var go = new GameObject("Layout " + layout);
            go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.TunnelPassageMesh(layout);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CombatMaterials.Fleet;
            renderer.sortingLayerName = "Enemy";
            float length = TunnelPassage.Length(layout);
            var camera = Camera(0, length * .5f + 1);
            camera.transform.position = new Vector3(0, global::Camera.UpLimit + 2 + length * .5f, -20);
            camera.backgroundColor = new Color(.12f, .1f, .09f);
            cells[(layout - 1) / 6, (layout - 1) % 6] = Capture(camera);
            go.SetActive(false);
            camera.gameObject.SetActive(false);
        }
        Save("TunnelLayouts.png", cells);
    }

    // Batch: -executeMethod RenderDistrictConditions.Elites
    public static void Elites()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            var types = new[] { typeof(ScoutInterceptor), typeof(PatrolGunship), typeof(CourierDiver), typeof(SkullRevenant) };
            var levels = new[] { LevelId.Fields, LevelId.Fields, LevelId.City, LevelId.Hell };
            var cells = new Texture2D[1, types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                LevelRun.Reset(levels[i], 17381);
                var camera = Camera(0, 2.2f);
                camera.backgroundColor = new Color(.18f, .22f, .2f);
                var plain = DistrictEnemy.Spawn(types[i], new Vector3(-.9f, 0, 1));
                var elite = DistrictEnemy.Spawn(types[i], new Vector3(.9f, 0, 1));
                elite.MakeElite();
                var mark = elite.GetComponent<EliteMark>();
                typeof(EliteMark).GetMethod("Start", Hidden).Invoke(mark, null);
                typeof(EliteMark).GetMethod("LateUpdate", Hidden).Invoke(mark, null);
                foreach (var enemy in new[] { plain, elite })
                    foreach (var behaviour in enemy.GetComponents<MonoBehaviour>())
                        behaviour.enabled = false;
                cells[0, i] = Capture(camera);
                plain.gameObject.SetActive(false);
                elite.gameObject.SetActive(false);
                camera.gameObject.SetActive(false);
            }
            Save("Elites.png", cells);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            code = 1;
        }
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    public static void Run()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // The runtime presentation sets the PS1 vertex grid; without it the valley collapses.
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            City();
            Plains();
            Weather(LevelId.Snow, DistrictCondition.Blizzard, "Snow");
            Weather(LevelId.Desert, DistrictCondition.Sandstorm, "Desert");
            File.WriteAllText(Folder + "/Readme.txt",
                "Edit-mode renders of the runtime scenery (no PSX filter, no motion).\n" +
                "City.png: rows are seeds 170, 421, 9826; columns are sections 0-3, with sample ships and gold rounds.\n" +
                "Plains.png: columns day, dusk, night; rows are the bridge stretch and the lookout stretch.\n" +
                "Snow.png / Desert.png: left ordinary, right blizzard or sandstorm, two seeds.\n");
            Debug.Log("RenderDistrictConditions: PASS");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            code = 1;
        }
        finally
        {
            DistrictConditions.Forced = null;
        }
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    static UnityEngine.Camera Camera(float y, float size)
    {
        var go = new GameObject("Review camera");
        var camera = go.AddComponent<UnityEngine.Camera>();
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.aspect = Width / (float)Height;
        camera.transform.position = new Vector3(0, y, -20);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 300;
        camera.tag = "MainCamera";
        return camera;
    }

    static Texture2D Capture(UnityEngine.Camera camera)
    {
        var target = new RenderTexture(Width, Height, 24);
        camera.targetTexture = target;
        // Grades follow the main camera at runtime; place them by hand for the still.
        foreach (var grade in UnityEngine.Object.FindObjectsByType<SceneryGrade>(FindObjectsInactive.Exclude))
        {
            float height = camera.orthographicSize * 2 + 2, width = height * camera.aspect + 2;
            grade.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 12);
            grade.transform.localScale = new Vector3(width, height, 1);
        }
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        return image;
    }

    static void Save(string name, Texture2D[,] cells)
    {
        int columns = cells.GetLength(1), rows = cells.GetLength(0);
        var sheet = new Texture2D(Width * columns + (columns - 1) * 4, Height * rows + (rows - 1) * 4, TextureFormat.RGB24, false);
        var fill = new Color32[sheet.width * sheet.height];
        for (int i = 0; i < fill.Length; i++)
            fill[i] = new Color32(16, 16, 16, 255);
        sheet.SetPixels32(fill);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (cells[r, c] != null)
                    sheet.SetPixels(c * (Width + 4), (rows - 1 - r) * (Height + 4), Width, Height, cells[r, c].GetPixels());
        sheet.Apply();
        File.WriteAllBytes(Folder + "/" + name, sheet.EncodeToPNG());
    }

    // A few hulls and rounds in the district's hue, so contrast is judged against real combat art.
    static GameObject Combatants(Color hue, float centreY)
    {
        var root = new GameObject("Sample combat");
        if (bulletTexture == null)
        {
            bulletTexture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float r = new Vector2(x - 7.5f, y - 7.5f).magnitude;
                    bulletTexture.SetPixel(x, y, r < 4 ? Color.white : r < 6 ? hue : r < 7 ? new Color(0, 0, 0, 1) : Color.clear);
                }
            bulletTexture.Apply();
        }
        var sprite = Sprite.Create(bulletTexture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 64);
        for (int i = 0; i < 14; i++)
        {
            var round = new GameObject("Round").AddComponent<SpriteRenderer>();
            round.transform.SetParent(root.transform, false);
            float a = i * Mathf.PI * 2 / 14;
            round.transform.position = new Vector3(Mathf.Cos(a) * 1.7f, centreY + 2.2f + Mathf.Sin(a) * 1.7f, -5);
            round.sprite = sprite;
            round.color = Color.Lerp(hue, Color.white, .1f);
            round.sortingLayerName = "Bullet";
        }
        foreach (var (x, y, kind, size) in new[] { (-2.4f, 4.6f, 12, .45f), (2.4f, 4.6f, 12, .45f), (0f, 2.2f, 14, .8f) })
        {
            var ship = new GameObject("Hull");
            ship.transform.SetParent(root.transform, false);
            ship.transform.position = new Vector3(x, centreY + y, -8);
            ship.transform.localScale = Vector3.one * size;
            ship.transform.rotation = Quaternion.Euler(0, 0, 180);
            ship.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(kind);
            var hull = ship.AddComponent<MeshRenderer>();
            hull.sharedMaterial = CombatMaterials.Fleet;
            hull.sortingLayerName = "Enemy";
        }
        return root;
    }

    static void City()
    {
        int[] seeds = { 170, 421, 9826 };
        var cells = new Texture2D[seeds.Length, 4];
        for (int row = 0; row < seeds.Length; row++)
        {
            LevelRun.Reset(LevelId.City, seeds[row]);
            var environment = Environment(LevelId.City);
            var camera = Camera(0, 8.85f);
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.City), -.6f);
            for (int section = 0; section < 4; section++)
            {
                // Section i sits at (i - 1) * 20 with no travel.
                float y = (section - 1) * 20;
                camera.transform.position = new Vector3(0, y, -20);
                combat.transform.position = new Vector3(0, y, 0);
                cells[row, section] = Capture(camera);
            }
            environment.SetActive(false);
            combat.SetActive(false);
            camera.gameObject.SetActive(false);
        }
        Save("City.png", cells);
    }

    static GameObject Environment(LevelId level)
    {
        var root = new GameObject(level + " review environment");
        var environment = root.AddComponent<LevelEnvironment>();
        typeof(LevelEnvironment).GetField("level", Hidden).SetValue(environment, level);
        typeof(LevelEnvironment).GetMethod("Build", Hidden).Invoke(environment, null);
        foreach (var storm in root.GetComponentsInChildren<DesertDustStorm>())
        {
            typeof(DesertDustStorm).GetMethod("Start", Hidden).Invoke(storm, null);
            // Let the storm reach a representative strength.
            typeof(DesertDustStorm).GetField("clock", Hidden).SetValue(storm, 9f);
            typeof(DesertDustStorm).GetMethod("Update", Hidden).Invoke(storm, null);
        }
        typeof(LevelEnvironment).GetField("clock", Hidden).SetValue(environment, 3.5f);
        typeof(LevelEnvironment).GetMethod("Update", Hidden).Invoke(environment, null);
        return root;
    }

    static void Weather(LevelId level, DistrictCondition storm, string name)
    {
        int[] seeds = { 2024, 77731 };
        var cells = new Texture2D[seeds.Length, 2];
        for (int row = 0; row < seeds.Length; row++)
            for (int column = 0; column < 2; column++)
            {
                DistrictConditions.Forced = column == 0 ? DistrictCondition.Ordinary : storm;
                LevelRun.Reset(level, seeds[row]);
                var environment = Environment(level);
                var camera = Camera(-.6f, 8.85f);
                var combat = Combatants(LevelProjectilePalette.Accent(level), -.6f);
                cells[row, column] = Capture(camera);
                environment.SetActive(false);
                combat.SetActive(false);
                camera.gameObject.SetActive(false);
            }
        DistrictConditions.Forced = null;
        Save(name + ".png", cells);
    }

    static void Plains()
    {
        const int Seed = 17381;
        var conditions = new[] { DistrictCondition.Ordinary, DistrictCondition.Dusk, DistrictCondition.Night };
        var cells = new Texture2D[2, conditions.Length];
        for (int column = 0; column < conditions.Length; column++)
        {
            DistrictConditions.Forced = conditions[column];
            LevelRun.Reset(LevelId.Fields, Seed);
            var layout = new PSXValleyLayout(Seed);
            var root = new GameObject("Valley review");
            var landscape = Resources.Load<Material>("PSXValley");
            var river = new Material(Resources.Load<Shader>("PSXValleyRiver"));
            river.SetFloat("_BridgeY", layout.BridgeY);
            float bridgeWidth = layout.HalfWidth(layout.BridgeY);
            river.SetFloat("_PierU", .5f - .37f * bridgeWidth / (bridgeWidth - .07f));
            var foliage = new Material(Resources.Load<Shader>("PSXValleyFoliage")) { mainTexture = PSXValleyFoliage.CreateAtlas() };
            var lightMaterial = SceneryLights.CreateMaterial();
            PlainsConditionScenery.Begin(root.transform, Seed);
            foreach (var birds in root.GetComponentsInChildren<DuskBirds>())
                typeof(DuskBirds).GetMethod("Start", Hidden).Invoke(birds, null);
            for (int i = 0; i < 4; i++)
            {
                float origin = (i - 1) * 24;
                var section = new GameObject("Valley section " + i).transform;
                section.SetParent(root.transform, false);
                section.localPosition = new Vector3(0, origin, 88);
                Layer(section, PSXValleyGeometry.Build(origin, 24, 1, layout, Seed), landscape);
                Layer(section, PSXValleyRiver.Build(origin, 24, layout), river);
                Layer(section, PSXValleyFoliage.Build(origin, 24, layout), foliage);
                var lights = PlainsConditionScenery.Lights(origin, 24, layout, Seed);
                if (lights != null)
                    PlainsConditionScenery.AddLights(section, lights, lightMaterial);
            }
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Fields), 0);
            var camera = Camera(0, 8.85f);
            float[] stops = { layout.BridgeY, layout.LookoutY };
            for (int row = 0; row < stops.Length; row++)
            {
                float y = PSXValleyLayout.Wrap(stops[row]);
                camera.transform.position = new Vector3(0, y, -20);
                combat.transform.position = new Vector3(0, y, 0);
                cells[row, column] = Capture(camera);
            }
            root.SetActive(false);
            combat.SetActive(false);
            camera.gameObject.SetActive(false);
        }
        DistrictConditions.Forced = null;
        Save("Plains.png", cells);
    }

    static void Layer(Transform parent, Mesh mesh, Material material)
    {
        var go = new GameObject(mesh.name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -40;
    }
}
#endif
