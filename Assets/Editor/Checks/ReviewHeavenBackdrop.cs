#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReviewHeavenBackdrop
{
    const string Folder = "C:/Users/Miguel/Shootemup/ArtSource/HeavenBackdropReview";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
    }
    public static void Before() => Render(true);
    public static void Run() => Render(false);
    static void Render(bool before)
    {
        Guard();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        LevelRun.Preview(LevelId.Heaven, 20261106, 1);
        var camera = new GameObject("Offscreen Heaven camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position = new Vector3(0, 0, -20);
        camera.orthographic = true; camera.orthographicSize = 8.850766f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 200;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.4f, .55f, .7f);
        camera.cullingMask = 1 << 31; camera.allowHDR = false;
        var target = new RenderTexture(405, 720, 24); camera.targetTexture = target;
        var root = new GameObject("Heaven scenery review");
        var backdrop = root.AddComponent<HeavenBackdrop>();
        typeof(HeavenBackdrop).GetMethod("Start", Hidden).Invoke(backdrop, null);
        foreach (var child in root.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
        var pose = typeof(HeavenBackdrop).GetMethod("Pose", Hidden);
        var legacy = before ? (Transform[])typeof(HeavenBackdrop).GetField("islands", Hidden).GetValue(backdrop) : null;
        var initial = before ? legacy.Select(t => t.localPosition).ToArray() : null;
        void At(float time)
        {
            if (pose != null) pose.Invoke(backdrop, new object[] { time });
            else
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.sharedMaterial.HasProperty("_Clock")) renderer.sharedMaterial.SetFloat("_Clock", time);
                for (int i = 0; i < legacy.Length; i++)
                    legacy[i].localPosition = new Vector3(initial[i].x, Mathf.Repeat(initial[i].y + 42 - time * .95f, 83.2f) - 42, initial[i].z);
            }
        }
        string prefix = before ? "Before" : "After";
        for (int i = 0; i < 4; i++) { At(i * 13); Capture(camera, target, Folder + "/" + prefix + "-" + i + ".png"); }
        if (!before)
        {
            string motion = Path.GetFullPath("ReviewOutput/HeavenBackdrop/Motion"); Directory.CreateDirectory(motion);
            for (int i = 0; i < 90; i++) { At(12 + i / 15f); Capture(camera, target, motion + "/" + i.ToString("D3") + ".png"); }
            foreach (var shader in new[] { "HeavenClouds", "HeavenMist", "HeavenRuins" })
                if (ShaderUtil.ShaderHasError(Resources.Load<Shader>(shader))) throw new Exception("Shader error: " + shader);
            File.WriteAllText(Folder + "/Render.txt", $"PASS native-shader offscreen renders at 405x720, times 0/13/26/39 seconds.\n{root.GetComponentsInChildren<MeshRenderer>().Length} renderers; {root.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3)} triangles; {root.GetComponentsInChildren<Collider2D>().Length} colliders; {root.GetComponentsInChildren<Light>().Length} lights.");
        }
        root.SetActive(false);
        camera.orthographicSize = 3.2f; camera.transform.position = new Vector3(0, .7f, -20);
        for (int i = 0; i < 6; i++)
        {
            var model = new GameObject("Cloud sanctuary " + i);
            model.transform.rotation = Quaternion.Euler(18, -12, 0);
            void Add(string label, Mesh mesh, Material material)
            {
                var go = new GameObject(label) { layer = 31 }; go.transform.SetParent(model.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            Add("Cloud", IndustrialMeshFactory.HeavenCloudIsland(i % 3), new Material(Resources.Load<Shader>("HeavenMist")));
            Add("Ruins", IndustrialMeshFactory.HeavenBastion(i), before ? HeavenMaterials.Ivory : new Material(Resources.Load<Shader>("HeavenRuins")));
            Capture(camera, target, Folder + "/" + prefix + "-Landmark-" + i + ".png");
            UnityEngine.Object.DestroyImmediate(model);
        }
        EditorApplication.Exit(0);
    }
    static void Capture(UnityEngine.Camera camera, RenderTexture target, string path)
    {
        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
        File.WriteAllBytes(path, pixels.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(pixels); RenderTexture.active = previous;
    }
    public static void Build()
    {
        Guard(); PlayerSettings.companyName = "DefaultCompany"; PlayerSettings.productName = "seup";
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = "Build/VoidBackdrop/Shootemup.exe", options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", result.summary.result + "\nErrors=" + result.summary.totalErrors + "; warnings=" + result.summary.totalWarnings);
        EditorApplication.Exit(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
#endif
