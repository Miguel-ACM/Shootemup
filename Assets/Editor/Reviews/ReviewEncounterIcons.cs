#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Native UI rendering only. Does not enter Play mode or start a run.
public static class ReviewEncounterIcons
{
    const string Folder = "ArtSource/EncounterIconsReview";
    public static void Run()
    {
        Directory.CreateDirectory(Folder + "/Frames");
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                canvas.enabled = false;
            var go = new GameObject("UI preview camera");
            var camera = go.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 360;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 100;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            var target = new RenderTexture(640, 480, 24);
            camera.targetTexture = target;
            var backdrop = new GameObject("Previously captured gameplay backdrop", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var backdropCanvas = backdrop.GetComponent<Canvas>();
            backdropCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            backdropCanvas.worldCamera = camera;
            backdropCanvas.planeDistance = 2;
            var scaler = backdrop.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            var picture = new GameObject("Backdrop image", typeof(RectTransform), typeof(RawImage));
            picture.transform.SetParent(backdrop.transform, false);
            var rect = picture.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes("ArtSource/RiverbankReview/After-18.0.png"));
            texture.filterMode = FilterMode.Point;
            picture.GetComponent<RawImage>().texture = texture;
            foreach (var t in backdrop.GetComponentsInChildren<Transform>())
                t.gameObject.layer = 31;
            var player = new GameObject("Preview steering point").transform;
            var pairs = new[] { new[] { EncounterKind.Shop, EncounterKind.MultShop }, new[] { EncounterKind.Item, EncounterKind.Health }, new[] { EncounterKind.Challenge, EncounterKind.Shop } };
            int captures = 0;
            foreach (var pair in pairs)
            {
                var icons = Array.ConvertAll(pair, EncounterChoice.Icon);
                if (icons.Any(s => s == null || s.texture.filterMode != FilterMode.Point || s.texture.mipmapCount != 1))
                    throw new Exception("Icons must be present, point sampled and mip-free");
                var choice = LevelChoice.OpenEncounter(player, Array.ConvertAll(pair, EncounterChoice.Label), Array.ConvertAll(pair, EncounterChoice.Accent), _ => { }, icons);
                if (choice.GetComponentsInChildren<TextMeshProUGUI>().Length != 0)
                    throw new Exception("Encounter text was not replaced");
                var canvas = choice.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                foreach (var t in choice.GetComponentsInChildren<Transform>())
                    t.gameObject.layer = 31;
                var choiceScaler = choice.GetComponent<CanvasScaler>();
                choiceScaler.enabled = false;
                choiceScaler.enabled = true;
                var exit = typeof(LevelChoice).GetMethod("AnimateExit", BindingFlags.Instance | BindingFlags.NonPublic);
                string name = pair[0] + "-" + pair[1];
                for (int frame = 0; frame <= 18; frame++)
                {
                    float progress = frame / 18f;
                    exit.Invoke(choice, new object[] { 0, progress });
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    var previous = RenderTexture.active;
                    RenderTexture.active = target;
                    var image = new Texture2D(640, 480, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Folder + "/Frames/" + name + "-" + frame.ToString("D2") + ".png", image.EncodeToPNG());
                    if (frame == 0 || frame == 9)
                        File.WriteAllBytes(Folder + "/" + name + (frame == 0 ? "-Open" : "-Exit") + ".png", image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                    RenderTexture.active = previous;
                    captures++;
                }
                if (choice.GetComponent<CanvasGroup>().alpha != 0)
                    throw new Exception("Options remain visible after exit");
                UnityEngine.Object.DestroyImmediate(choice.gameObject);
            }
            File.WriteAllText(Folder + "/Verification.txt", "PASS\nEditor UI preview only: no Play mode or gameplay run.\nAll five icons loaded with point sampling and no mipmaps.\nAll three encounter pairs use sprites with no text labels.\nRendered " + captures + " exit-animation poses through the production layout method.\nAll options are fully transparent at the end of the animation.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + e); EditorApplication.Exit(1); }
    }
}
#endif
