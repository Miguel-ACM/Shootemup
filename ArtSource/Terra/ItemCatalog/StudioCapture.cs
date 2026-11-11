#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TerraItemStudioCapture
{
    const int Size = 768;
    static readonly Vector3 Hero = new Vector3(-18, 28, -5);
    static readonly Vector3 Rear = new Vector3(-16, 156, 4);

    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Temp/ItemCatalogReview/"))
            throw new InvalidOperationException("Use the isolated item catalog review project.");
        var args = Environment.GetCommandLineArgs();
        int outputIndex = Array.IndexOf(args, "-item-render-output") + 1;
        if (outputIndex <= 0 || outputIndex >= args.Length)
            throw new ArgumentException("Provide -item-render-output.");
        string folder = args[outputIndex];
        int keyIndex = Array.IndexOf(args, "-item-render-keys") + 1;
        string[] keys = keyIndex > 0 && keyIndex < args.Length ? args[keyIndex].Split(',') : Array.Empty<string>();
        Directory.CreateDirectory(folder);
        string normals = Directory.CreateDirectory(Path.Combine(folder, "Normals")).FullName;
        var records = new StringBuilder();
        var audit = new StringBuilder();
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        var shader = Shader.Find("Shootemup Review/Item Studio");
        if (shader == null)
            throw new InvalidOperationException("The studio shader is missing.");
        using var cleanup = new SceneCleanup(scene, target);
        var cameraObject = new GameObject("Item studio camera");
        SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<UnityEngine.Camera>();
        camera.scene = scene;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 30;
        camera.targetTexture = target;

        foreach (var definition in ItemCatalog.All)
        {
            if (!Includes(definition.Key)) continue;
            var probe = new GameObject("Read item description");
            probe.SetActive(false);
            try
            {
                var item = definition.Attach(probe);
                if (string.IsNullOrEmpty(item.GetData()[0]))
                    item.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(item, null);
                var data = item.GetData();
                Render(definition.Key, data[0], item.Kind.ToString(), definition.Rarity.ToString(), data[2]);
            }
            finally { Object.DestroyImmediate(probe); }
        }
        if (Includes("BaseWeaponItem")) Render("BaseWeaponItem", "Base gun", "weapon", "Special", "");
        if (Includes("life")) Render("life", "Health pickup", "pickup", "Special", "");
        if (Includes("mana")) Render("mana", "Energy pickup", "pickup", "Special", "");
        if (Includes("money")) Render("money", "Coin", "pickup", "Special", "");
        if (records.Length == 0)
            throw new ArgumentException("No item model matched.");
        File.WriteAllText(Path.Combine(folder, keys.Length == 0 ? "items.tsv" : "partial-items.tsv"), records.ToString());
        File.WriteAllText(Path.Combine(folder, keys.Length == 0 ? "mesh-audit.tsv" : "partial-mesh-audit.tsv"), audit.ToString());
        camera.targetTexture = null;
        Debug.Log("Rendered Terra items to " + folder);
        if (audit.ToString().Contains("\tFAIL"))
            throw new InvalidOperationException("One or more meshes failed the triangle and normals checks. See mesh-audit.tsv.");

        bool Includes(string key) => keys.Length == 0 || Array.IndexOf(keys, key) >= 0;

        void Render(string key, string name, string kind, string rarity, string description)
        {
            string resource = "TerraModels/Item" + key;
            var source = Resources.Load<TextAsset>(resource);
            var mesh = IndustrialMeshFactory.ExpansionItem(key);
            var palette = EverydayItemMaterials.For(key);
            if (source == null || mesh == null || palette == null || !mesh.name.StartsWith("Terra Item"))
                throw new InvalidOperationException("The revised mesh or palette is missing for " + key);
            int expected = 0;
            foreach (string line in source.text.Split('\n'))
                if (line.Length > 0 && char.IsDigit(line[0]))
                    expected += (line.Split(' ').Length - 1) / 3 - 2;
            VerifyMesh(key, mesh, expected, audit);
            var model = new GameObject(key);
            SceneManager.MoveGameObjectToScene(model, scene);
            var material = new Material(shader) { mainTexture = palette.mainTexture };
            try
            {
                model.AddComponent<MeshFilter>().sharedMesh = mesh;
                model.AddComponent<MeshRenderer>().sharedMaterial = material;
                Capture(Hero, Path.Combine(folder, key + ".png"));
                Capture(Vector3.zero, Path.Combine(folder, key + "-Front.png"));
                Capture(Rear, Path.Combine(folder, key + "-Rear.png"));
                material.SetFloat("_Normals", 1);
                Capture(Hero, Path.Combine(normals, key + ".png"));
                Capture(Rear, Path.Combine(normals, key + "-Rear.png"));
                records.AppendLine(string.Join("\t", key, Clean(name), kind, rarity, "ok", Clean(description)));
            }
            finally
            {
                Object.DestroyImmediate(model);
                Object.DestroyImmediate(material);
            }

            void Capture(Vector3 euler, string path)
            {
                model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(euler));
                model.transform.localScale = Vector3.one;
                var bounds = model.GetComponent<MeshRenderer>().bounds;
                float scale = 1.62f / Mathf.Max(bounds.size.x, bounds.size.y);
                model.transform.localScale = Vector3.one * scale;
                model.transform.position = -bounds.center * scale;
                camera.Render();
                var previous = RenderTexture.active;
                var pixels = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(path, pixels.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = previous;
                    Object.DestroyImmediate(pixels);
                }
            }
        }
    }

    static void VerifyMesh(string key, Mesh mesh, int expected, StringBuilder audit)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;
        int invalid = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            var normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (normal.sqrMagnitude < 1e-14f || Vector3.Dot(normal.normalized, normals[a]) < .995f ||
                Vector3.Dot(normal.normalized, normals[b]) < .995f || Vector3.Dot(normal.normalized, normals[c]) < .995f)
                invalid++;
        }
        bool ok = triangles.Length / 3 == expected && invalid == 0;
        audit.AppendLine(string.Join("\t", key, expected, triangles.Length / 3, invalid, ok ? "ok" : "FAIL"));
    }

    static string Clean(string s) => (s ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    sealed class SceneCleanup : IDisposable
    {
        readonly Scene scene;
        readonly RenderTexture target;

        public SceneCleanup(Scene scene, RenderTexture target)
        {
            this.scene = scene;
            this.target = target;
        }

        public void Dispose()
        {
            target.Release();
            Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
#endif
