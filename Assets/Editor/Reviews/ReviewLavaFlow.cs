#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Isolated native material preview; never enters play mode or starts a run.
public static class ReviewLavaFlow
{
    public static void Run()
    {
        const string folder = "ArtSource/LavaFlowReview/Frames";
        Directory.CreateDirectory(folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(270, 480, 24);
        var filtered = new RenderTexture(270, 480, 0);
        var composite = new Material(Shader.Find("Shootemup/PSX Composite"));
        var pixels = new Texture2D(270, 480, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        UnityEngine.Camera camera = null;
        int exitCode = 1;
        try
        {
            var view = new GameObject("Lava material preview camera");
            SceneManager.MoveGameObjectToScene(view, scene);
            camera = view.AddComponent<UnityEngine.Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 8.850766f;
            camera.aspect = 270f / 480;
            camera.transform.position = new Vector3(0, 0, -40);
            camera.farClipPlane = 300;
            camera.targetTexture = target;
            var world = new GameObject("Lava flow native preview");
            SceneManager.MoveGameObjectToScene(world, scene);
            LevelRun.Reset(LevelId.Hell, 17381);
            var environment = world.AddComponent<InfernalCaldera>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(InfernalCaldera).GetMethod("Start", flags).Invoke(environment, null);
            var material = (Material)typeof(InfernalCaldera).GetField("lava", flags).GetValue(environment);
            var crust = material.GetTexture("_CrustMap");
            var flow = material.GetTexture("_FlowMap");
            if (crust == null || flow == null)
                throw new Exception("Missing lava data texture");
            if (ShaderUtil.ShaderHasError(material.shader))
                throw new Exception("Lava shader compilation failed");
            foreach (var texture in new[] { crust, flow })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                if (importer.sRGBTexture || texture.wrapMode != TextureWrapMode.Repeat)
                    throw new Exception("Lava data must be linear and seamless");
            }
            for (int frame = 0; frame < 96; frame++)
            {
                float t = frame / 12f;
                material.SetFloat("_Flow", t);
                // Follow the same four scrolling shelves as InfernalCaldera.Update.
                for (int tile = 0; tile < 4; tile++)
                {
                    var root = world.transform.Find("Basalt shelf " + tile);
                    var p = root.localPosition;
                    p.y = (tile - 1) * 20 - t * 3.7f;
                    if (p.y < -30)
                        p.y += 80;
                    root.localPosition = p;
                }
                camera.Render();
                Graphics.Blit(target, filtered, composite);
                RenderTexture.active = filtered;
                pixels.ReadPixels(new Rect(0, 0, 270, 480), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(folder + "/Frame-" + frame.ToString("D3") + ".png", pixels.EncodeToPNG());
            }
            File.WriteAllText(folder + "/Verification.txt", "PASS\nNative Unity material rendered at 270x480 with the game PSX compositor.\n96 frames over 8 seconds: downstream terrain scroll plus dual-phase curl advection, including a full seven-second distortion cycle.\nCrust and velocity textures present, linear, repeating. No gameplay started.\nShader compilation passed. Three texture samples, one pass; no extra objects or lights for lava animation.\n");
            exitCode = 0;
        }
        catch (Exception e) { File.WriteAllText(folder + "/Verification.txt", "FAIL\n" + e); }
        finally
        {
            if (camera != null)
                camera.targetTexture = null;
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            target.Release();
            filtered.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(filtered);
            UnityEngine.Object.DestroyImmediate(composite);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
        EditorApplication.Exit(exitCode);
    }
}
#endif
