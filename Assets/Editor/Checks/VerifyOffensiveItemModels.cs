#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Offscreen inspection of the actual pickup meshes, without starting gameplay.
public static class VerifyOffensiveItemModels
{
    public static void RenderModels()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Build/OffensiveItemsReview/"))
            throw new Exception("Use the isolated review project");
        const string folder = "ArtSource/OffensiveItemsReview";
        Directory.CreateDirectory(folder);
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        var camera = new GameObject("Item review camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position = new Vector3(1000, 0, -5);
        camera.orthographic = true;
        camera.orthographicSize = .62f;
        camera.cullingMask = 1 << 31;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .055f, .065f);
        var target = new RenderTexture(256, 256, 24);
        camera.targetTexture = target;
        string[] keys = { "RocketBouquet", "Redline", "NailBomb", "CanOpener",
            "FirecrackerBelt", "CarbonPaper", "Thunderhead", "DemolitionPlunger" };
        string only = VerifyCombatRework.Argument("-offensiveModel", "");
        if (only.Length > 0 && Array.IndexOf(keys, only) < 0)
            throw new Exception("Unknown offensive model: " + only);
        var report = new StringBuilder();
        foreach (string key in keys)
        {
            if (only.Length > 0 && key != only) continue;
            var model = new GameObject(key) { layer = 31 };
            model.transform.position = new Vector3(1000, 0, 0);
            Mesh mesh = IndustrialMeshFactory.ExpansionItem(key);
            if (mesh == null || mesh.vertexCount == 0 || mesh.triangles.Length == 0)
                throw new Exception(key + " has no mesh geometry");
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = EverydayItemMaterials.For(key);
            var size = mesh.bounds.size;
            float span = Mathf.Max(size.x, size.y, size.z);
            if (!(span > 0) || float.IsInfinity(span))
                throw new Exception(key + " has invalid bounds");
            report.AppendLine(key + ": " + mesh.vertexCount + " vertices, " +
                mesh.triangles.Length / 3 + " triangles, bounds " + size.ToString("F3"));
            model.transform.localScale = Vector3.one * (.9f / span);
            for (int view = 0; view < 2; view++)
            {
                model.transform.rotation = Quaternion.Euler(-20, view == 0 ? 0 : 35, 0);
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var png = new Texture2D(256, 256, TextureFormat.RGB24, false);
                png.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                png.Apply();
                File.WriteAllBytes(folder + "/" + key + "-" + view + ".png", png.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(png);
                RenderTexture.active = previous;
            }
            UnityEngine.Object.DestroyImmediate(model);
        }
        string reportPath = folder + "/MeshReport.txt";
        if (only.Length > 0 && File.Exists(reportPath))
        {
            string[] lines = File.ReadAllLines(reportPath);
            bool replaced = false;
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith(only + ":", StringComparison.Ordinal))
                {
                    lines[i] = report.ToString().TrimEnd();
                    replaced = true;
                    break;
                }
            if (!replaced) throw new Exception("Existing mesh report is missing " + only);
            File.WriteAllLines(reportPath, lines);
        }
        else File.WriteAllText(reportPath, report.ToString());
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(camera.gameObject);
        EditorApplication.Exit(0);
    }
}
#endif
