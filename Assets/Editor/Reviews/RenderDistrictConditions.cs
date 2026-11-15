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

    // Batch: -executeMethod RenderDistrictConditions.CoinLauncher
    // The rebuilt Coin Launcher pickup from three angles.
    public static void CoinLauncher()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mesh = IndustrialMeshFactory.ExpansionItem("SalvageConverter");
            var cells = new Texture2D[1, 3];
            var angles = new[] { new Vector3(-18, 30, -12), new Vector3(-10, 0, 0), new Vector3(-25, -40, 8) };
            for (int i = 0; i < angles.Length; i++)
            {
                var go = new GameObject("Coin launcher");
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = EverydayItemMaterials.For("SalvageConverter");
                renderer.sortingLayerName = "Enemy";
                go.transform.rotation = Quaternion.Euler(angles[i]);
                go.transform.position = new Vector3(-mesh.bounds.center.x, -mesh.bounds.center.y, 0);
                var camera = Camera(0, 1.05f);
                camera.backgroundColor = new Color(.86f, .87f, .88f);
                cells[0, i] = Capture(camera);
                go.SetActive(false);
                camera.gameObject.SetActive(false);
            }
            Save("CoinLauncher.png", cells);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            code = 1;
        }
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    // Batch: -executeMethod RenderDistrictConditions.ForestAndFlare
    // Two stretches of the FOREST valley with tanks ploughing through the canopy, the Thicket
    // Lurker close up, and a SOLAR FLARE in its warning, its eruption and its full burn.
    public static void ForestAndFlare()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            const int Seed = 424242;
            var cells = new Texture2D[1, 6];
            DistrictConditions.Forced = DistrictCondition.Forest;
            LevelRun.Reset(LevelId.Fields, Seed);
            var layout = new PSXValleyLayout(Seed, true);
            var root = new GameObject("Forest review");
            var landscape = Resources.Load<Material>("PSXValley");
            var foliage = new Material(Resources.Load<Shader>("PSXValleyFoliage")) { mainTexture = PSXValleyFoliage.CreateAtlas() };
            for (int i = 0; i < 4; i++)
            {
                float origin = (i - 1) * 24;
                var section = new GameObject("Forest section " + i).transform;
                section.SetParent(root.transform, false);
                section.localPosition = new Vector3(0, origin, 88);
                Layer(section, PSXValleyGeometry.BuildForest(origin, 24, 1, layout, Seed), landscape);
                Layer(section, PSXValleyFoliage.Build(origin, 24, layout), foliage);
            }
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Fields), 0);
            var camera = Camera(0, 8.85f);
            var tankMesh = IndustrialMeshFactory.CombatHull((int)CombatHullId.MeadowTank);
            var tanks = new GameObject("Forest tanks");
            var spots = new[] { new Vector2(-2.1f, 2.6f), new Vector2(1.7f, -1.2f), new Vector2(.4f, 5.4f) };
            foreach (var spot in spots)
            {
                var tank = new GameObject("Tank");
                tank.transform.SetParent(tanks.transform, false);
                tank.transform.localPosition = new Vector3(spot.x, spot.y, 87.8f);
                tank.transform.localRotation = Quaternion.Euler(0, 0, spot.x * 9);
                tank.transform.localScale = Vector3.one * .55f;
                tank.AddComponent<MeshFilter>().sharedMesh = tankMesh;
                var part = tank.AddComponent<MeshRenderer>();
                part.sharedMaterial = CombatMaterials.Fleet;
                part.sortingLayerName = "Background";
                part.sortingOrder = -32;
            }
            foreach (var (y, column) in new[] { (6f, 0), (40f, 1) })
            {
                camera.transform.position = new Vector3(0, y, -20);
                combat.transform.position = new Vector3(0, y, 0);
                tanks.transform.position = new Vector3(0, y, 0);
                var push = new Vector4[4];
                for (int i = 0; i < spots.Length; i++)
                    push[i] = new Vector4(spots[i].x, y + spots[i].y, .95f, 0);
                Shader.SetGlobalVectorArray("_CanopyPush", push);
                cells[0, column] = Capture(camera);
            }
            Shader.SetGlobalVectorArray("_CanopyPush", new Vector4[4]);
            root.SetActive(false);
            combat.SetActive(false);
            tanks.SetActive(false);

            var lurker = new GameObject("Thicket lurker");
            lurker.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ThicketLurkerMesh();
            var hull = lurker.AddComponent<MeshRenderer>();
            hull.sharedMaterial = CombatMaterials.Fleet;
            hull.sortingLayerName = "Enemy";
            lurker.transform.rotation = Quaternion.Euler(-20, 0, 0);
            camera.transform.position = new Vector3(0, 0, -20);
            camera.orthographicSize = 1.75f;
            camera.backgroundColor = new Color(.25f, .33f, .19f);
            cells[0, 2] = Capture(camera);
            lurker.SetActive(false);
            camera.gameObject.SetActive(false);

            DistrictConditions.Forced = DistrictCondition.SolarFlare;
            LevelRun.Reset(LevelId.Space, Seed);
            var space = Environment(LevelId.Space);
            foreach (var behaviour in space.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is LevelEnvironment)
                    continue;
                var start = behaviour.GetType().GetMethod("Start", Hidden);
                if (start != null && start.GetParameters().Length == 0)
                    start.Invoke(behaviour, null);
            }
            var spaceCombat = Combatants(LevelProjectilePalette.Accent(LevelId.Space), -.6f);
            var spaceCamera = Camera(-.6f, 8.85f);
            foreach (var (age, column) in new[] { (1.2f, 3), (SolarFlare.Warning + .2f, 4), (SolarFlare.Warning + SolarFlare.Erupt + .9f, 5) })
            {
                var flare = SolarFlare.Begin(-1);
                PoseFlare(flare, age);
                cells[0, column] = Capture(spaceCamera);
                UnityEngine.Object.DestroyImmediate(flare.gameObject);
                var fireballs = UnityEngine.Object.FindAnyObjectByType<PSXFireballs>();
                if (fireballs != null)
                    UnityEngine.Object.DestroyImmediate(fireballs.gameObject);
            }
            Save("ForestAndFlare.png", cells);
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

    // Batch: -executeMethod RenderDistrictConditions.RouteEvents
    // The route choice out of Plains with each district's event under its name, and the opening
    // title of a district with its event beneath it.
    public static void RouteEvents()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            // A seed with both events and level modifiers on both routes: two or more on Snow.
            int Count(LevelModifier m) => ((int)m & 1) + ((int)m >> 1 & 1) + ((int)m >> 2 & 1);
            int seed = 1;
            while (seed < 100000 && (DistrictConditions.For(LevelId.Snow, seed) != DistrictCondition.Blizzard || DistrictConditions.For(LevelId.City, seed) != DistrictCondition.RushHour
                || Count(LevelModifiers.For(LevelId.Snow, seed, 2)) < 2 || Count(LevelModifiers.For(LevelId.City, seed, 2)) < 1))
                seed++;
            LevelRun.Reset(LevelId.Fields, seed);
            var cells = new Texture2D[1, 2];
            var camera = Camera(0, 8.85f);
            var choice = new GameObject("Route choice").AddComponent<LevelChoice>();
            var type = typeof(LevelChoice);
            var options = new[] { LevelId.Snow, LevelId.City };
            type.GetProperty("Options").SetValue(choice, options);
            type.GetField("captions", Hidden).SetValue(choice, new[] { "SNOW", "CITY" });
            type.GetField("accents", Hidden).SetValue(choice, Array.ConvertAll(options, LevelRun.Accent));
            type.GetField("sealedRoutes", Hidden).SetValue(choice, new[] { false, false });
            type.GetMethod("Allocate", Hidden).Invoke(choice, null);
            type.GetMethod("Build", Hidden).Invoke(choice, null);
            choice.GetComponent<CanvasGroup>().alpha = 1;
            cells[0, 0] = CaptureOverlay(camera, choice.GetComponent<Canvas>());
            UnityEngine.Object.DestroyImmediate(choice.gameObject);

            LevelRun.Reset(LevelId.Snow, seed);
            var reveal = new GameObject("Level reveal").AddComponent<LevelReveal>();
            var steps = (System.Collections.IEnumerator)typeof(LevelReveal).GetMethod("Start", Hidden).Invoke(reveal, null);
            steps.MoveNext();
            reveal.GetComponent<CanvasGroup>().alpha = 1;
            cells[0, 1] = CaptureOverlay(camera, reveal.GetComponent<Canvas>());
            Save("RouteEvents.png", cells);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            code = 1;
        }
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }

    // Batch: -executeMethod RenderDistrictConditions.SimulatorCurtain
    // The simulator's cybernetic transition over the Plains valley: jacking in, fully covered,
    // dissolving and the lingering wireframe.
    public static void SimulatorCurtain()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            DistrictConditions.Forced = DistrictCondition.Ordinary;
            LevelRun.Reset(LevelId.Fields, 424242);
            var layout = new PSXValleyLayout(424242);
            var landscape = Resources.Load<Material>("PSXValley");
            var foliage = new Material(Resources.Load<Shader>("PSXValleyFoliage")) { mainTexture = PSXValleyFoliage.CreateAtlas() };
            var section = new GameObject("Valley").transform;
            section.localPosition = new Vector3(0, -12, 88);
            Layer(section, PSXValleyGeometry.Build(-12, 24, 1, layout, 424242), landscape);
            Layer(section, PSXValleyFoliage.Build(-12, 24, layout), foliage);
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Fields), 0);
            var camera = Camera(0, 8.85f);
            var canvas = new GameObject("Curtain canvas").AddComponent<Canvas>();
            var curtain = new GameObject("Simulator curtain", typeof(RectTransform), typeof(CanvasRenderer), typeof(SimulatorCurtainGraphic));
            curtain.transform.SetParent(canvas.transform, false);
            // A fixed size in canvas units: a stretched rect is laid out before the batch-mode
            // canvas root has its size, which leaves an empty quad.
            var rect = curtain.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(720f * Width / Height, 720);
            var material = new Material(Resources.Load<Shader>("SimulatorCurtain"));
            material.SetFloat("_Aspect", Width / (float)Height);
            curtain.GetComponent<SimulatorCurtainGraphic>().material = material;
            var poses = new[] { (0f, .45f, .8f), (0f, 1f, 2.3f), (1f, .62f, 3.4f), (1f, .12f, 4.4f) };
            var cells = new Texture2D[1, poses.Length];
            for (int i = 0; i < poses.Length; i++)
            {
                material.SetFloat("_Mode", poses[i].Item1);
                material.SetFloat("_Cover", poses[i].Item2);
                material.SetFloat("_Clock", poses[i].Item3);
                cells[0, i] = CaptureOverlay(camera, canvas);
            }
            Save("SimulatorCurtain.png", cells);
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

    // Batch: -executeMethod RenderDistrictConditions.CityRuins
    // The destroyed City through the real environment build, two seeds, two stretches each,
    // with combatants for scale.
    public static void CityRuins()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            int[] seeds = { 170, 9826 };
            var cells = new Texture2D[seeds.Length, 2];
            // The first environment build in a fresh editor warms the shared level assets.
            LevelRun.Reset(LevelId.City, seeds[0]);
            UnityEngine.Object.DestroyImmediate(Environment(LevelId.City));
            for (int row = 0; row < seeds.Length; row++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
                DistrictConditions.Forced = DistrictCondition.Ordinary;
                LevelRun.Reset(LevelId.City, seeds[row]);
                var city = Environment(LevelId.City);
                var combat = Combatants(LevelProjectilePalette.Accent(LevelId.City), 0);
                for (int column = 0; column < 2; column++)
                {
                    typeof(LevelEnvironment).GetField("travel", Hidden).SetValue(city.GetComponent<LevelEnvironment>(), column * 23f);
                    typeof(LevelEnvironment).GetMethod("Update", Hidden).Invoke(city.GetComponent<LevelEnvironment>(), null);
                    cells[row, column] = Capture(Camera(0, 8.85f));
                }
                UnityEngine.Object.DestroyImmediate(combat);
            }
            Save("CityRuins.png", cells);
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

    // Batch: -executeMethod RenderDistrictConditions.Mines
    // The moored contact mine over the Underwater backdrop at play scale, and close up.
    public static void Mines()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            DistrictConditions.Forced = DistrictCondition.Ordinary;
            LevelRun.Reset(LevelId.Underwater, 424242);
            var sea = Environment(LevelId.Underwater);
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Underwater), 0);
            var mesh = IndustrialMeshFactory.CombatHull((int)CombatHullId.DepthMine);
            var material = EnemyExpansion.Material((int)CombatHullId.DepthMine);
            var cells = new Texture2D[1, 2];
            var mines = new GameObject("Mines");
            var spots = new[] { new Vector3(-2.2f, 4.6f, 12), new Vector3(1.8f, 2.4f, -20), new Vector3(-1.4f, -1.8f, 30), new Vector3(2.4f, -4.4f, 5) };
            foreach (var spot in spots)
            {
                var mine = new GameObject("Mine");
                mine.transform.SetParent(mines.transform, false);
                mine.transform.localPosition = new Vector3(spot.x, spot.y, 1);
                mine.transform.localRotation = Quaternion.Euler(8 * Mathf.Sin(spot.z), 12 * Mathf.Cos(spot.z), spot.z);
                mine.transform.localScale = Vector3.one * .62f * DistrictEnemy.PresentationScale(typeof(DepthMine));
                mine.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = mine.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = "Enemy";
            }
            cells[0, 0] = Capture(Camera(0, 8.85f));
            sea.SetActive(false);
            combat.SetActive(false);
            mines.SetActive(false);
            var close = new GameObject("Mine close up");
            close.AddComponent<MeshFilter>().sharedMesh = mesh;
            var closeRenderer = close.AddComponent<MeshRenderer>();
            closeRenderer.sharedMaterial = material;
            closeRenderer.sortingLayerName = "Enemy";
            close.transform.rotation = Quaternion.Euler(-25, 10, 0);
            var view = Camera(0, 1.1f);
            view.transform.position = new Vector3(0, -.2f, -20);
            view.backgroundColor = new Color(.04f, .16f, .3f);
            cells[0, 1] = Capture(view);
            Save("Mines.png", cells);
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

    // Batch: -executeMethod RenderDistrictConditions.PoisonCloud
    // Compost's gas clouds over the valley: a fresh small heap, a rotten heap and one fading out.
    public static void PoisonCloud()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            DistrictConditions.Forced = DistrictCondition.Ordinary;
            LevelRun.Reset(LevelId.Fields, 424242);
            var layout = new PSXValleyLayout(424242);
            var section = new GameObject("Valley").transform;
            section.localPosition = new Vector3(0, -12, 88);
            Layer(section, PSXValleyGeometry.Build(-12, 24, 1, layout, 424242), Resources.Load<Material>("PSXValley"));
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Fields), 0);
            var camera = Camera(0, 8.85f);
            var material = new Material(Resources.Load<Shader>("ToxicCloud"));
            var quad = new Mesh();
            quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(-.5f, .5f), new Vector3(.5f, .5f), new Vector3(.5f, -.5f) };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
            var cells = new Texture2D[1, 2];
            for (int frame = 0; frame < 2; frame++)
            {
                var clouds = new GameObject("Clouds");
                foreach (var (at, radius, life, strength, seed) in new[] { (new Vector2(-1.8f, 4.2f), .6f, 1f, .9f, 3f), (new Vector2(1.4f, 1.2f), .95f, 1f, .7f, 11f), (new Vector2(-1.2f, -3.2f), .95f, .45f, .15f, 23f) })
                {
                    var cloud = new GameObject("Cloud");
                    cloud.transform.SetParent(clouds.transform, false);
                    cloud.transform.localPosition = new Vector3(at.x, at.y, 0);
                    cloud.transform.localScale = Vector3.one * radius * 2.9f;
                    cloud.AddComponent<MeshFilter>().sharedMesh = quad;
                    var renderer = cloud.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.sortingLayerName = "Enemy";
                    renderer.sortingOrder = -6;
                    var look = new MaterialPropertyBlock();
                    look.SetFloat("_Life", life);
                    look.SetFloat("_Strength", strength);
                    look.SetFloat("_Seed", seed);
                    look.SetFloat("_Clock", 2 + frame * 1.7f);
                    renderer.SetPropertyBlock(look);
                }
                cells[0, frame] = Capture(camera);
                UnityEngine.Object.DestroyImmediate(clouds);
            }
            Save("PoisonCloud.png", cells);
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

    // Overlay canvases never reach a camera: draw this one through the review camera instead.
    static Texture2D CaptureOverlay(UnityEngine.Camera camera, Canvas canvas)
    {
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler != null)
            scaler.enabled = false;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 5;
        canvas.scaleFactor = Height / 720f;
        // In front of the scenery and every ship, as the real overlay is.
        canvas.sortingLayerName = "Bullet";
        canvas.sortingOrder = 500;
        camera.backgroundColor = new Color(.05f, .09f, .12f);
        // Lay the canvas out at the still's size before drawing it.
        var sizing = new RenderTexture(Width, Height, 24);
        camera.targetTexture = sizing;
        Canvas.ForceUpdateCanvases();
        foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            graphic.SetAllDirty();
        Canvas.ForceUpdateCanvases();
        var image = Capture(camera);
        sizing.Release();
        UnityEngine.Object.DestroyImmediate(sizing);
        return image;
    }

    // Batch: -executeMethod RenderDistrictConditions.Asteroids
    // Space asteroids from the great boulder down to the smallest piece over the Space backdrop,
    // with the pilot for scale, and the four rock variants close up.
    public static void Asteroids()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            var cells = new Texture2D[1, 2];
            DistrictConditions.Forced = DistrictCondition.Ordinary;
            LevelRun.Reset(LevelId.Space, 424242);
            var space = Environment(LevelId.Space);
            foreach (var behaviour in space.GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour is LevelEnvironment)
                    continue;
                var start = behaviour.GetType().GetMethod("Start", Hidden);
                if (start != null && start.GetParameters().Length == 0)
                    start.Invoke(behaviour, null);
            }
            var combat = Combatants(LevelProjectilePalette.Accent(LevelId.Space), -.6f);
            var camera = Camera(-.6f, 8.85f);
            var rocks = new GameObject("Asteroids");
            var spots = new[] { new Vector2(-1.9f, 5.2f), new Vector2(1.9f, 3.6f), new Vector2(-2.2f, 1.2f), new Vector2(2.3f, -.6f), new Vector2(-2.4f, -3.2f), new Vector2(1.2f, -5.4f) };
            for (int i = 0; i < spots.Length; i++)
                Rock(rocks.transform, i % IndustrialMeshFactory.AsteroidVariants, spots[i], SpaceAsteroid.ScaleFor(SpaceAsteroid.MaxGeneration - i), i * 47);
            cells[0, 0] = Capture(camera);
            rocks.SetActive(false);
            combat.SetActive(false);
            space.SetActive(false);
            var close = new GameObject("Asteroid variants");
            for (int v = 0; v < IndustrialMeshFactory.AsteroidVariants; v++)
                Rock(close.transform, v, new Vector2(v % 2 == 0 ? -1.15f : 1.15f, v < 2 ? 1.2f : -1.2f), 1, v * 61);
            camera.transform.position = new Vector3(0, 0, -20);
            camera.orthographicSize = 2.7f;
            camera.backgroundColor = new Color(.03f, .035f, .07f);
            cells[0, 1] = Capture(camera);
            Save("Asteroids.png", cells);
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

    static void Rock(Transform parent, int variant, Vector2 at, float scale, float tilt)
    {
        var rock = new GameObject("Rock " + variant);
        rock.transform.SetParent(parent, false);
        rock.transform.localPosition = new Vector3(at.x, at.y, 1);
        rock.transform.localRotation = Quaternion.Euler(tilt * .7f - 20, tilt, tilt * .4f);
        rock.transform.localScale = Vector3.one * scale;
        rock.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.AsteroidRock(variant);
        var renderer = rock.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = AsteroidMaterials.Rock;
        renderer.sortingLayerName = "Enemy";
    }

    // Edit mode never runs Update: replay the eruption's fireballs up to this age and freeze each
    // one partway through its life, so the still shows what play mode would.
    static void PoseFlare(SolarFlare flare, float age)
    {
        var type = typeof(SolarFlare);
        if (age >= SolarFlare.Warning)
        {
            type.GetMethod("Eruption", Hidden).Invoke(flare, null);
            for (float at = SolarFlare.Warning; at <= age; at += .03f)
            {
                type.GetField("age", Hidden).SetValue(flare, at);
                type.GetMethod("RollingFire", Hidden).Invoke(flare, null);
            }
            var pool = UnityEngine.Object.FindAnyObjectByType<PSXFireballs>();
            if (pool != null)
            {
                var active = (System.Collections.IList)typeof(PSXFireballs).GetField("active", Hidden).GetValue(pool);
                var random = new System.Random(7);
                foreach (var ball in active)
                {
                    var fields = ball.GetType();
                    float life = (float)fields.GetField("life").GetValue(ball);
                    fields.GetField("start").SetValue(ball, Time.time - life * (.12f + .4f * (float)random.NextDouble()));
                }
                typeof(PSXFireballs).GetMethod("Update", Hidden).Invoke(pool, null);
            }
        }
        flare.PoseAt(age);
    }

    // Clean, burning, poisoned and doubly afflicted hulls side by side, zoomed in, over the Plains ground.
    public static void AfflictedHulls()
    {
        int code = 0;
        try
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(Width, Height, 0, 0));
            DistrictConditions.Forced = DistrictCondition.Ordinary;
            LevelRun.Reset(LevelId.Fields, 424242);
            var layout = new PSXValleyLayout(424242);
            var section = new GameObject("Valley").transform;
            section.localPosition = new Vector3(0, -12, 88);
            Layer(section, PSXValleyGeometry.Build(-12, 24, 1, layout, 424242), Resources.Load<Material>("PSXValley"));
            var camera = Camera(0, 3.2f);
            var cells = new Texture2D[1, 5];
            int[] kinds = { 12, 14, 3 };
            for (int status = 0; status < 5; status++)
            {
                var root = new GameObject("Hulls");
                var material = new Material(CombatMaterials.Fleet);
                // Columns: clean, a throwaway poisoned capture (the batch editor compiles the variant on
                // first use and draws it clean), burning (1), poisoned (2), both (3).
                int bits = new[] { 0, 2, 1, 2, 3 }[status];
                if (bits != 0)
                {
                    material.SetFloat("_HullStatus", bits);
                    material.EnableKeyword("HULL_AFFLICTED");
                }
                for (int i = 0; i < kinds.Length; i++)
                {
                    var ship = new GameObject("Hull");
                    ship.transform.SetParent(root.transform, false);
                    ship.transform.position = new Vector3(0, 2 - i * 2, -8);
                    ship.transform.localScale = Vector3.one * (i == 1 ? 1.1f : .8f);
                    ship.transform.rotation = Quaternion.Euler(0, 0, 180);
                    ship.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(kinds[i]);
                    var hull = ship.AddComponent<MeshRenderer>();
                    hull.sharedMaterial = material;
                    hull.sortingLayerName = "Enemy";
                }
                cells[0, status] = Capture(camera);
                UnityEngine.Object.DestroyImmediate(root);
            }
            Save("AfflictedHulls.png", cells);
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
