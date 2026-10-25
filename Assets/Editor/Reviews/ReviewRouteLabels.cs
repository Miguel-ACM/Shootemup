#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class ReviewRouteLabels
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    public static void Run()
    {
        const string folder = VerifyProjectileContacts.Folder;
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
                canvas.enabled = false;
            var go = new GameObject("Production two-route label preview");
            var choice = go.AddComponent<LevelChoice>();
            typeof(LevelChoice).GetField("captions", Hidden).SetValue(choice, new[] { "DESERT", "CANDYLAND" });
            typeof(LevelChoice).GetField("accents", Hidden).SetValue(choice, new[] { LevelRun.Accent(LevelId.Desert), LevelRun.Accent(LevelId.Candyland) });
            typeof(LevelChoice).GetMethod("Allocate", Hidden).Invoke(choice, null);
            typeof(LevelChoice).GetMethod("Build", Hidden).Invoke(choice, null);
            foreach (var group in go.GetComponentsInChildren<CanvasGroup>())
                group.alpha = 1;
            var camera = new GameObject("Route UI preview camera").AddComponent<UnityEngine.Camera>();
            camera.transform.position = new Vector3(0, 0, -40);
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var routeCanvas = go.GetComponent<Canvas>();
            routeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            routeCanvas.worldCamera = camera;
            routeCanvas.planeDistance = 1;
            foreach (var t in go.GetComponentsInChildren<Transform>())
                t.gameObject.layer = 31;
            var backdrop = new GameObject("Native gameplay background", typeof(RectTransform), typeof(Canvas));
            var backdropCanvas = backdrop.GetComponent<Canvas>();
            backdropCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            backdropCanvas.worldCamera = camera;
            backdropCanvas.planeDistance = 2;
            backdropCanvas.sortingOrder = -1;
            var picture = new GameObject("Gameplay capture", typeof(RectTransform), typeof(RawImage));
            picture.transform.SetParent(backdrop.transform, false);
            var rect = picture.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(folder + "/Snow-Balanced.png"));
            texture.filterMode = FilterMode.Point;
            picture.GetComponent<RawImage>().texture = texture;
            foreach (var t in backdrop.GetComponentsInChildren<Transform>())
                t.gameObject.layer = 31;
            var target = new RenderTexture(640, 480, 24);
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            foreach (var label in go.GetComponentsInChildren<TextMeshProUGUI>())
            {
                label.ForceMeshUpdate();
                if (label.textBounds.size.x > label.rectTransform.rect.width + .1f)
                    throw new Exception("Route label clipped: " + label.text);
            }
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(640, 480, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
            image.Apply();
            File.WriteAllBytes(folder + "/Two-Routes-Fitted.png", image.EncodeToPNG());
            RenderTexture.active = previous;
            File.WriteAllText(folder + "/RouteLabels.txt", "PASS\nEditor-only render of production LevelChoice; both labels fit their two-route panels.\n");
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(texture);
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(folder + "/RouteLabels.txt", "FAIL\n" + e); EditorApplication.Exit(1); }
    }
}
#endif
