#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Native scenery and tree inspection in an isolated editor. No gameplay run.
[InitializeOnLoad]
public static class ReviewSnowLandscape
{
    public const string Folder = "ArtSource/SnowLandscapeReview";
    static readonly List<string> checks = new List<string>();
    static double deadline;
    static ReviewSnowLandscape()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("SnowLandscapeReview", false))
                  return;
              new GameObject("Snow scenery motion and ground contact review").AddComponent<SnowLandscapeRuntimeReview>();
              deadline = EditorApplication.timeSinceStartup + 180;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              if (File.Exists(Folder + "/Runtime.txt"))
              {
                  SessionState.SetBool("SnowLandscapeReview", false);
                  EditorApplication.Exit(File.ReadAllText(Folder + "/Runtime.txt").StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(Folder + "/Runtime.txt", "FAIL timeout");
                  EditorApplication.Exit(1);
              }
          };
    }

    static void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
    }

    public static void Run() => Review(false);
    public static void RunComplete() => Review(true);
    static void Review(bool runtime)
    {
        Directory.CreateDirectory(Folder);
        try
        {
            foreach (int seed in new[] { 17381, 421, 9826 })
            {
                var layout = new SnowLandscape(seed);
                Check(layout.Plants.Count > 75, "Sparse winter vegetation: " + layout.Plants.Count);
                Check(layout.Plants.Where(p => p.kind != 2).All(p => layout.Clear(p.x, p.y, p.radius)), "Tree obstructs a glade or ice hollow");
                Check(new[] { 0, 1, 3 }.All(kind => layout.Plants.Any(p => p.kind == kind)), "Missing tree variety");
                for (float y = -10; y < 70; y += .4f)
                    for (float x = -9; x < 9; x += .6f)
                        Check(Mathf.Abs(layout.Height(x, y) - layout.Height(x, y + 80)) < .00001f, "Terrain loop height discontinuity");
                var meshes = new List<Mesh>();
                for (int i = 0; i < 4; i++)
                {
                    var mesh = SnowLandscapeGeometry.Ground(i, layout);
                    meshes.Add(mesh);
                    Check(mesh.vertexCount == 2560, "Auger ground grid layout changed");
                    foreach (var asset in new[]{SnowLandscapeGeometry.Detail(i,layout),SnowLandscapeGeometry.Detail(i,layout,true),
                        PSXValleyFoliage.Build(i*20-10,20,layout.Plants,layout.Height),PSXValleyFoliage.Build(i*20-10,20,layout.NearPlants,layout.Height)})
                    {
                        Check(asset.vertexCount < 65535 && asset.vertices.All(p => float.IsFinite(p.x + p.y + p.z)), "Invalid scenery mesh");
                        UnityEngine.Object.DestroyImmediate(asset);
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    var a = meshes[i].vertices;
                    var b = meshes[(i + 1) % 4].vertices;
                    for (int col = 0; col < 16; col++)
                        foreach (int corner in new[] { 2, 3 })
                        {
                            var top = a[(39 * 16 + col) * 4 + corner];
                            var bottom = b[col * 4 + (corner == 2 ? 1 : 0)];
                            Check(Mathf.Abs(top.x - bottom.x) + Mathf.Abs(top.z - bottom.z) < .00001f, "Section terrain seam");
                        }
                }
                foreach (var mesh in meshes)
                    UnityEngine.Object.DestroyImmediate(mesh);
                checks.Add("Seed " + seed + ": " + layout.Plants.Count + " plants, birches and two evergreen silhouettes, three ice hollows; continuous four-section terrain and 80-unit loop; original auger grid order.");
                foreach (int station in new[] { 0, 20, 40, 60 })
                    Render(seed, station);
            }
            Check(Mathf.Abs(new SnowLandscape(421).Height(3, 20) - new SnowLandscape(9826).Height(3, 20)) > .001f, "Identical run seeds");
            checks.Add("Native 270x480 views rendered through the game composite at four locations for each of three seeds. Tree material uses the Plains foliage shader with a winter atlas.");
            File.WriteAllText(Folder + "/Editor.txt", "PASS\n" + string.Join("\n", checks));
            if (runtime)
            {
                File.Delete(Folder + "/Runtime.txt");
                Directory.CreateDirectory(Folder + "/Frames");
                EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
                SessionState.SetBool("SnowLandscapeReview", true);
                EditorApplication.isPlaying = true;
            }
            else
                EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Editor.txt", "FAIL\n" + e); EditorApplication.Exit(1); }
    }

    static void Render(int seed, float station)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var assets = new List<UnityEngine.Object>();
        var target = new RenderTexture(270, 480, 24);
        var filtered = new RenderTexture(270, 480, 0);
        var previous = RenderTexture.active;
        var previousSnap = Shader.GetGlobalVector("_PSXSnapResolution");
        try
        {
            var cameraObject = new GameObject("Snow landscape review camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, station, -20);
            camera.orthographic = true;
            camera.orthographicSize = 8.850766f;
            camera.aspect = 270f / 480;
            camera.farClipPlane = 120;
            camera.targetTexture = target;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var ground = new Material(Resources.Load<Shader>("SnowSurface"));
            var detail = new Material(Resources.Load<Shader>("LevelScenery"));
            var atlas = PSXValleyFoliage.CreateAtlas(true);
            var foliage = new Material(Resources.Load<Shader>("PSXValleyFoliage")) { mainTexture = atlas };
            var composite = new Material(Shader.Find("Shootemup/PSX Composite"));
            assets.AddRange(new UnityEngine.Object[] { ground, detail, atlas, foliage, composite });
            foreach (var material in new[] { ground, detail, foliage })
                Check(!ShaderUtil.ShaderHasError(material.shader), "Scenery shader error: " + material.shader.name);
            var layout = new SnowLandscape(seed);
            for (int i = 0; i < 4; i++)
            {
                float y = i * 20 + Mathf.Round((station - i * 20) / 80) * 80;
                Add(SnowLandscapeGeometry.Ground(i, layout), ground, y, 0, -50);
                Add(SnowLandscapeGeometry.Detail(i, layout), detail, y, 0, -40);
                Add(PSXValleyFoliage.Build(i * 20 - 10, 20, layout.Plants, layout.Height), foliage, y - 10, 0, -39);
                Add(SnowLandscapeGeometry.Detail(i, layout, true), detail, y, -14, -25);
                Add(PSXValleyFoliage.Build(i * 20 - 10, 20, layout.NearPlants, layout.Height), foliage, y - 10, -14, -24);
            }
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(270, 480, 0, 0));
            foliage.SetFloat("_FlowTime", 3);
            camera.Render();
            Graphics.Blit(target, filtered, composite);
            RenderTexture.active = filtered;
            var image = new Texture2D(270, 480, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 270, 480), 0, 0);
            image.Apply();
            File.WriteAllBytes(Folder + "/Seed-" + seed + "-" + station + ".png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            void Add(Mesh mesh, Material material, float y, float z, int order)
            {
                assets.Add(mesh);
                var go = new GameObject(mesh.name);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = new Vector3(0, y, z);
                go.transform.localScale = new Vector3(.8f, 1, 1);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = order;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            Shader.SetGlobalVector("_PSXSnapResolution", previousSnap);
            EditorSceneManager.ClosePreviewScene(scene);
            target.Release();
            filtered.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(filtered);
            foreach (var asset in assets)
                UnityEngine.Object.DestroyImmediate(asset);
        }
    }
}

public sealed class SnowLandscapeRuntimeReview : MonoBehaviour
{
    readonly List<string> errors = new List<string>();
    const string Folder = ReviewSnowLandscape.Folder;
    bool backfaces;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Screen.SetResolution(1280, 720, false);
        Application.logMessageReceived += Log;
        backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        Physics.queriesHitBackfaces = backfaces;
    }

    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message);
    }

    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Review());
        while (stack.Count > 0)
        {
            bool more = false;
            object value = null;
            Exception failure = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    value = stack.Peek().Current;
            }
            catch (Exception e) { failure = e; }
            if (failure != null)
            {
                errors.Add(failure.ToString());
                break;
            }
            if (!more)
            {
                stack.Pop();
                continue;
            }
            if (value is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return value;
        }
        File.WriteAllText(Folder + "/Runtime.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\nOne short isolated Snow scene review: moving textured groves, powder and haze, parallax, authored ground/air formation, pixel bullet contrast, loop wrap and 3D surface contact.\n" + string.Join("\n", errors));
    }

    IEnumerator Review()
    {
        yield return null;
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Snow, 17381);
        CloudTransition.Load("L1");
        while (SceneManager.GetActiveScene().name != "L1")
            yield return null;
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.enabled = false;
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>())
        {
            weapon.StopFiring();
            weapon.enabled = false;
        }
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        GameRules.playerDisabled = false;
        Time.timeScale = 1;
        var environment = FindAnyObjectByType<LevelEnvironment>();
        Check(environment.GetComponentsInChildren<MeshFilter>().Count(m => m.sharedMesh != null &&
            (m.sharedMesh.name.StartsWith("Sculpted snowfield") || m.sharedMesh.name.StartsWith("Ice hollows") ||
             m.sharedMesh.name.StartsWith("Near winter trunks") || m.sharedMesh.name.StartsWith("PSX cutout groves"))) == 20, "Missing batched scenery layers");
        Check(environment.GetComponentsInChildren<Collider>().Length == 0, "Scenery created gameplay colliders");
        foreach (var mesh in environment.GetComponentsInChildren<MeshFilter>())
            if (mesh.sharedMesh.name.StartsWith("Sculpted snowfield"))
            {
                mesh.gameObject.layer = 30;
                mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
            }
        var wave = WaveComposer.PreviewPart(WavePartLibrary.SnowGround[0], 0, 1, out _);
        var air = WaveComposer.PreviewPart(WavePartLibrary.Snow[0], 0, 1, out _);
        var units = wave.units.Concat(air.units).OrderBy(u => u.time).ToArray();
        int next = 0, surfaceChecks = 0, peakMarks = 0, peakGround = 0;
        var watch = typeof(LevelEnvironment).GetField("travel", BindingFlags.Instance | BindingFlags.NonPublic);
        float began = Time.time;
        for (int frame = 0; frame < 44; frame++)
        {
            float elapsed = Time.time - began;
            while (next < units.Length && units[next].time <= elapsed)
            {
                var unit = units[next++];
                var enemy = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.up * (Camera.UpLimit + 2));
                enemy.FlyPhrase(unit);
            }
            ship.transform.position = new Vector3(Mathf.Sin(frame / 43f * Mathf.PI * 2) * 3.4f, Camera.DownLimit + 2, -1);
            yield return new WaitForSeconds(.25f);
            Physics.SyncTransforms();
            for (int row = 0; row < 5; row++)
            {
                var requested = new Vector3(-3 + row * 1.5f, -6 + row * 2.8f, 1);
                Check(LevelEnvironment.TrySnowGround(requested, out var tile, out var local), "Ground sampler lost the visible snow surface");
                var point = tile.TransformPoint(local);
                Check(Physics.Raycast(new Vector3(point.x, point.y, 60), Vector3.forward, out var hit, 40, 1 << 30) && Mathf.Abs(hit.point.z - point.z) < .002f, "Auger projection differs from the rendered terrain");
                surfaceChecks++;
            }
            var vehicles = AEnemy.Live.OfType<GroundVehicle>().Where(v => v.Grounded).ToArray();
            peakGround = Mathf.Max(peakGround, vehicles.Length);
            foreach (var vehicle in vehicles)
            {
                Check(Vector2.Distance(vehicle.transform.position, vehicle.GroundSection.TransformPoint(vehicle.GroundPoint)) < .25f, "Auger floats away from scrolling terrain");
                peakMarks = Mathf.Max(peakMarks, vehicle.GetComponent<GroundTreadMarks>()?.MarkCount ?? 0);
            }
            DesertReview.Capture("Frames/" + frame.ToString("D3"), Folder);
            if (frame == 12 || frame == 30)
                DesertReview.Capture("Gameplay-" + frame, Folder);
        }
        Check(peakGround > 0 && peakMarks > 0, "Authored augers did not establish ground contact and tracks");
        watch.SetValue(environment, 79.98f);
        yield return new WaitForSeconds(.08f);
        Check((float)watch.GetValue(environment) < 1, "Snow scrolling did not wrap");
        DesertReview.Capture("LoopWrap", Folder);
        File.WriteAllText(Folder + "/GroundContact.txt", "PASS: " + surfaceChecks + " native triangle-ray comparisons during lateral parallax; " + peakGround + " grounded augers and " + peakMarks + " track marks; full eighty-unit wrap; 20 static batched meshes, no runtime scenery colliders (verification-only colliders added afterward).\n");
    }
}
#endif
