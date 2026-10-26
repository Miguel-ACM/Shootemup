#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Run in a disposable Unity project: model sheets, seeded seam checks, then real gameplay frames.</summary>
[InitializeOnLoad]
public static class VerifyPlainsScenery
{
    public const string Folder = "ArtSource/PlainsSceneryReview";
    const string Key = "PlainsSceneryReview";
    static double deadline;
    static VerifyPlainsScenery()
    {
        EditorApplication.playModeStateChanged += s =>
          {
              if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                  return;
              new GameObject("Plains scenery review").AddComponent<PlainsSceneryReview>();
              deadline = EditorApplication.timeSinceStartup + 240;
          };
        EditorApplication.update += () =>
          {
              if (!SessionState.GetBool(Key, false) || deadline == 0)
                  return;
              if (File.Exists(Folder + "/Runtime.txt"))
                  CheckHarness.Exit(File.ReadAllText(Folder + "/Runtime.txt").StartsWith("PASS") ? 0 : 1);
              if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(Folder + "/Runtime.txt", "FAIL timeout");
                  CheckHarness.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        if (File.Exists(Folder + "/Runtime.txt"))
            File.Delete(Folder + "/Runtime.txt");
        var notes = new List<string>();
        foreach (int seed in new[] { 17381, 421, 9826 })
        {
            var layout = new PSXValleyLayout(seed);
            Check(layout.Plants.Count > 75, "Too little vegetation: " + seed + " count " + layout.Plants.Count);
            Check(layout.Plants.All(p => layout.Clear(p.x, p.y, p.radius)), "Vegetation overlaps road, water or landmarks");
            for (float y = -24; y < 72; y += .75f)
                Check(Mathf.Abs(layout.Centre(y) - layout.Centre(y + 96)) < .0001f && Mathf.Abs(layout.HalfWidth(y) - layout.HalfWidth(y + 96)) < .0001f, "River loop seam");
            Mesh previous = null;
            for (int section = 0; section < 4; section++)
            {
                float origin = (section - 1) * 24;
                var terrain = PSXValleyGeometry.Build(origin, 24, 1, seed);
                var leaves = PSXValleyFoliage.Build(origin, 24, layout);
                var river = PSXValleyRiver.Build(origin, 24, layout);
                foreach (var mesh in new[] { terrain, leaves, river })
                {
                    Check(mesh.vertices.All(p => float.IsFinite(p.x + p.y + p.z)), "Non-finite mesh");
                    Check(mesh.vertexCount < 65535, "Scenery exceeds 16-bit index budget");
                }
                if (previous != null)
                {
                    for (int lane = 0; lane <= 8; lane++)
                    {
                        var a = previous.vertices[previous.vertexCount - 9 + lane];
                        var b = river.vertices[lane];
                        Check(Mathf.Abs(a.x - b.x) < .00001f, "Section river edge gap");
                        Check((previous.uv[previous.vertexCount - 9 + lane] - river.uv[lane]).sqrMagnitude < .000001f, "Flow UV seam");
                    }
                    UnityEngine.Object.DestroyImmediate(previous);
                }
                previous = river;
                UnityEngine.Object.DestroyImmediate(terrain);
                UnityEngine.Object.DestroyImmediate(leaves);
            }
            UnityEngine.Object.DestroyImmediate(previous);
            notes.Add("Seed " + seed + ": " + layout.Plants.Count + " plants; clear road/landmarks; matching section geometry and flow UVs; 96-unit wrap.");
        }
        Check(Mathf.Abs(new PSXValleyLayout(421).Centre(20) - new PSXValleyLayout(9826).Centre(20)) > .01f, "Seeds produce identical rivers");
        var atlas = PSXValleyFoliage.CreateAtlas();
        var leavesMaterial = new Material(Resources.Load<Shader>("PSXValleyFoliage")) { mainTexture = atlas };
        var waterMaterial = new Material(Resources.Load<Shader>("PSXValleyRiver"));
        var composite = new Material(Shader.Find("Shootemup/PSX Composite"));
        try
        {
            foreach (var mat in new[] { leavesMaterial, waterMaterial, Resources.Load<Material>("PSXValley") })
                Check(mat.shader.isSupported && !ShaderUtil.ShaderHasError(mat.shader), "Shader compilation: " + mat.shader.name);
            foreach (int seed in new[] { 17381, 421, 9826 })
            {
                var layout = new PSXValleyLayout(seed);
                waterMaterial.SetFloat("_BridgeY", layout.BridgeY);
                float bridgeWidth = layout.HalfWidth(layout.BridgeY);
                waterMaterial.SetFloat("_PierU", .5f - .37f * bridgeWidth / (bridgeWidth - .07f));
                Render(seed, layout.BridgeY, "Bridge-" + seed, leavesMaterial, waterMaterial, composite);
                Render(seed, 27, "River-" + seed, leavesMaterial, waterMaterial, composite);
                Render(seed, 72, "Seam-" + seed, leavesMaterial, waterMaterial, composite);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(atlas); UnityEngine.Object.DestroyImmediate(leavesMaterial); UnityEngine.Object.DestroyImmediate(waterMaterial); UnityEngine.Object.DestroyImmediate(composite); }
        File.WriteAllText(Folder + "/Geometry.txt", "PASS\n" + string.Join("\n", notes));
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool(Key, true);
        CheckHarness.EnterPlayMode();
    }

    static void Render(int seed, float station, string name, Material leaves, Material water, Material composite)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var meshes = new List<Mesh>();
        var target = new RenderTexture(384, 576, 24);
        var filtered = new RenderTexture(384, 576, 0);
        var oldTarget = RenderTexture.active;
        var oldSnap = Shader.GetGlobalVector("_PSXSnapResolution");
        try
        {
            var cameraObject = new GameObject("Plains review camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.scene = scene;
            camera.transform.position = new Vector3(0, station, -20);
            camera.orthographic = true;
            camera.orthographicSize = 8.1f;
            camera.aspect = 2f / 3;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.farClipPlane = 60;
            camera.targetTexture = target;
            var layout = new PSXValleyLayout(seed);
            for (int i = 0; i < 4; i++)
            {
                float origin = (i - 1) * 24, y = origin + Mathf.Round((station - (origin + 12)) / 96) * 96;
                Add(PSXValleyGeometry.Build(origin, 24, 1, seed), Resources.Load<Material>("PSXValley"), y);
                Add(PSXValleyRiver.Build(origin, 24, layout), water, y);
                Add(PSXValleyFoliage.Build(origin, 24, layout), leaves, y);
            }
            Shader.SetGlobalVector("_PSXSnapResolution", new Vector4(384, 576, 0, 0));
            water.SetFloat("_FlowTime", 3);
            leaves.SetFloat("_FlowTime", 3);
            camera.Render();
            Graphics.Blit(target, filtered, composite);
            RenderTexture.active = filtered;
            var image = new Texture2D(384, 576, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 384, 576), 0, 0);
            image.Apply();
            File.WriteAllBytes(Folder + "/" + name + ".png", image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            void Add(Mesh mesh, Material material, float y)
            {
                meshes.Add(mesh);
                var go = new GameObject(mesh.name);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = new Vector3(0, y, 0);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -40;
            }
        }
        finally
        {
            RenderTexture.active = oldTarget;
            Shader.SetGlobalVector("_PSXSnapResolution", oldSnap);
            EditorSceneManager.ClosePreviewScene(scene);
            target.Release();
            filtered.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(filtered);
            foreach (var mesh in meshes)
                UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    static void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
    }
}

public sealed class PlainsSceneryReview : MonoBehaviour
{
    readonly List<string> errors = new List<string>();
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }

    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message);
    }

    IEnumerator Start()
    {
        yield return null;
        {
            var reviewStart = FindAnyObjectByType<NewGame>();
            reviewStart.OnAccept();
            reviewStart.Choose(1);
        }
        while (CloudTransition.Busy)
            yield return null;
        yield return new WaitForSeconds(1);
        var field = FindAnyObjectByType<PSXGrassField>();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.GetComponent<Collider2D>().enabled = false;
        var sections = field.GetComponentsInChildren<MeshFilter>().Where(m => m.name.StartsWith("Valley section ")).ToArray();
        if (sections.Length != 4 || field.GetComponentsInChildren<MeshFilter>().Length < 12)
            errors.Add("Missing runtime layers");
        if (field.GetComponentsInChildren<Collider>().Length + field.GetComponentsInChildren<Collider2D>().Length > 0)
            errors.Add("Scenery has gameplay collisions");
        Directory.CreateDirectory(VerifyPlainsScenery.Folder + "/Frames");
        var travel = typeof(PSXGrassField).GetField("travel", BindingFlags.Instance | BindingFlags.NonPublic);
        var layout = new PSXValleyLayout(LevelRun.Seed);
        travel.SetValue(field, layout.BridgeY - 6f);
        File.WriteAllText(VerifyPlainsScenery.Folder + "/Frames/timing.csv", "frame,seconds\n");
        float captureStart = Time.time;
        for (int frame = 0; frame < 120; frame++)
        {
            ship.transform.position = new Vector3(Mathf.Sin(frame / 119f * Mathf.PI * 2) * 3.8f, -5, 20);
            yield return new WaitForSecondsRealtime(.05f);
            var image = Capture();
            File.AppendAllText(VerifyPlainsScenery.Folder + "/Frames/timing.csv", frame + "," + (Time.time - captureStart).ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
            File.WriteAllBytes(VerifyPlainsScenery.Folder + "/Frames/" + frame.ToString("D3") + ".png", image.EncodeToPNG());
            Destroy(image);
        }
        travel.SetValue(field, 95.9f);
        yield return new WaitForSeconds(.3f);
        var positions = sections.Select(s => s.transform.localPosition.y).OrderBy(y => y).ToArray();
        for (int i = 1; i < positions.Length; i++)
            if (Mathf.Abs(positions[i] - positions[i - 1] - 24) > .001f)
                errors.Add("Runtime section gap");
        if ((float)travel.GetValue(field) > 4)
            errors.Add("Full loop failed");
        var wrap = Capture();
        File.WriteAllBytes(VerifyPlainsScenery.Folder + "/RuntimeWrap.png", wrap.EncodeToPNG());
        Destroy(wrap);
        File.WriteAllText(VerifyPlainsScenery.Folder + "/Runtime.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\nActual filtered game: 120 moving frames, player parallax, layered foliage/current, no scenery colliders, full-loop wrap.\n" + string.Join("\n", errors));
    }
    // Batch editors do not dispatch WaitForEndOfFrame. Render the real scene and its
    // overlay canvases explicitly, without opening/focusing a Game view on the player's desktop.
    static Texture2D Capture()
    {
        var source = UnityEngine.Camera.main;
        source.Render();
        int width = Screen.width, height = Screen.height;
        var target = new RenderTexture(width, height, 24);
        var previous = RenderTexture.active;
        var uiObject = new GameObject("Offscreen review UI");
        var camera = uiObject.AddComponent<UnityEngine.Camera>();
        camera.CopyFrom(source);
        camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        camera.enabled = false;
        camera.targetTexture = target;
        camera.rect = new Rect(0, 0, 1, 1);
        camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        var overlays = new List<Canvas>();
        var layers = new Dictionary<GameObject, int>();
        try
        {
            foreach (var canvas in FindObjectsByType<Canvas>())
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    overlays.Add(canvas);
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1;
                    foreach (var child in canvas.GetComponentsInChildren<Transform>(true))
                        if (!layers.ContainsKey(child.gameObject))
                        {
                            layers.Add(child.gameObject, child.gameObject.layer);
                            child.gameObject.layer = 31;
                        }
                }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            return image;
        }
        finally
        {
            foreach (var canvas in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            foreach (var pair in layers)
                pair.Key.layer = pair.Value;
            RenderTexture.active = previous;
            camera.targetTexture = null;
            Destroy(uiObject);
            target.Release();
            Destroy(target);
        }
    }

}
#endif
