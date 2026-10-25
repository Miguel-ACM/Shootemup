using System;
using System.Collections;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerifyDebugMenu
{
    static VerifyDebugMenu()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DebugMenuReview", false))
                  new GameObject("Debug menu review").AddComponent<DebugMenuReview>();
          };
        EditorApplication.update += () =>
          {
              if (!SessionState.GetBool("DebugMenuReview", false) || !File.Exists("DebugMenuReview/result.txt"))
                  return;
              SessionState.SetBool("DebugMenuReview", false);
              if (Application.isBatchMode)
                  EditorApplication.Exit(File.ReadAllText("DebugMenuReview/result.txt").StartsWith("PASS") ? 0 : 1);
              else
                  EditorApplication.isPlaying = false;
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory("DebugMenuReview");
        if (File.Exists("DebugMenuReview/result.txt"))
            File.Delete("DebugMenuReview/result.txt");
        SessionState.SetBool("DebugMenuReview", true);
        EditorApplication.isPlaying = true;
    }
}

public sealed class DebugMenuReview : MonoBehaviour
{
    const string Folder = "DebugMenuReview/";
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

    void Log(string text, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
            File.AppendAllText(Folder + "errors.txt", text + "\n" + stack + "\n");
    }

    static void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        Debug.Log("Debug review PASS: " + message);
    }

    IEnumerator Start()
    {
        var routine = Run();
        while (true)
        {
            object next;
            try
            {
                if (!routine.MoveNext())
                    break;
                next = routine.Current;
            }
            catch (Exception error) { File.WriteAllText(Folder + "result.txt", "FAIL\n" + error); yield break; }
            yield return next;
        }
        File.WriteAllText(Folder + "result.txt", File.Exists(Folder + "errors.txt") ? "FAIL\n" + File.ReadAllText(Folder + "errors.txt") : "PASS\nCatalogue, both slots, passive stacks, chosen district, clean normal start and rendered menu verified.");
    }

    IEnumerator Run()
    {
        if (File.Exists(Folder + "errors.txt"))
            File.Delete(Folder + "errors.txt");
        yield return new WaitForSecondsRealtime(8);
        var debug = FindAnyObjectByType<DebugRunMenu>();
        Check(debug != null, "debug entry exists on title");
        Check(GameObject.Find("Title tunnel backdrop") != null, "title uses tunnel scenery");
        Capture("title.png");
        yield return new WaitForSecondsRealtime(.5f);
        debug.OnAccept();
        yield return null;
        Check(FindObjectsByType<Button>().Length >= 50, "all item and district buttons exist");
        Click("SLOT 1");
        Click("Rail Driver");
        Click("Gravity Well");
        Click("SLOT 2");
        Click("Flame Projector");
        Click("Mirror Burst");
        Click("Another arm");
        Click("Another arm");
        Click("Twerp");
        Click("TUNNEL");
        yield return null;
        Capture("debug.png");
        yield return new WaitForSecondsRealtime(.5f);
        Click("START DEBUG RUN");
        float deadline = Time.realtimeSinceStartup + 45;
        while ((SceneManager.GetActiveScene().name != "L1" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(SceneManager.GetActiveScene().name == "L1" && !CloudTransition.Busy, "debug launch completes");
        Check(LevelRun.Current == LevelId.Tunnel && LevelRun.Stage == 3, "selected district has proper progression tier");
        var ship = FindAnyObjectByType<Shipcontroller>();
        var loadout = ship.GetComponent<ShipLoadout>();
        Check(loadout.Weapons[0] is RailDriver && loadout.Weapons[1] is FlameProjector, "both weapon slots applied");
        Check(loadout.Actives[0] is GravityWell && loadout.Actives[1] is MirrorBurst, "both active slots applied");
        Check(GameRules.playerShootNumber == 4, "passive stacks applied");
        Check(GameObject.Find("Twerp(Clone)") != null, "companion applied");
        yield return new WaitForSecondsRealtime(1);
        Capture("run.png");
        yield return new WaitForSecondsRealtime(.5f);
        CloudTransition.Load("Title");
        deadline = Time.realtimeSinceStartup + 30;
        while ((SceneManager.GetActiveScene().name != "Title" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        FindAnyObjectByType<NewGame>().OnAccept();
        deadline = Time.realtimeSinceStartup + 30;
        while ((SceneManager.GetActiveScene().name != "L1" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(LevelRun.Current == LevelId.Fields && GameRules.playerShootNumber == 2, "normal start resets debug setup");
        Check(FindAnyObjectByType<Shipcontroller>().GetComponent<ShipLoadout>().Actives[0] == null, "debug actives do not leak");
    }

    static void Click(string label)
    {
        foreach (var button in FindObjectsByType<Button>())
            if (button.gameObject.name == label)
            {
                button.onClick.Invoke();
                return;
            }
        throw new Exception("Missing button: " + label);
    }

    static void Capture(string filename)
    {
        var camera = UnityEngine.Camera.main;
        var target = new RenderTexture(1280, 720, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var canvases = FindObjectsByType<Canvas>();
        var overlays = new System.Collections.Generic.List<Canvas>();
        var layers = new System.Collections.Generic.Dictionary<GameObject, int>();
        var uiObject = new GameObject("Review overlay camera");
        var uiCamera = uiObject.AddComponent<UnityEngine.Camera>();
        uiCamera.CopyFrom(camera);
        uiCamera.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        uiCamera.enabled = false;
        uiCamera.targetTexture = target;
        uiCamera.cullingMask = 1 << 31;
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = Color.clear;
        foreach (var canvas in canvases)
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                overlays.Add(canvas);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1;
                foreach (var child in canvas.GetComponentsInChildren<Transform>(true))
                    if (!layers.ContainsKey(child.gameObject))
                    {
                        layers.Add(child.gameObject, child.gameObject.layer);
                        child.gameObject.layer = 31;
                    }
            }
        int mask = camera.cullingMask;
        camera.cullingMask &= ~(1 << 31);
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        var sceneImage = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        sceneImage.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        sceneImage.Apply();
        var scenePixels = sceneImage.GetPixels();
        uiCamera.Render();
        camera.cullingMask = mask;
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        var uiPixels = image.GetPixels();
        for (int i = 0; i < uiPixels.Length; i++)
        {
            float alpha = uiPixels[i].a;
            uiPixels[i] += scenePixels[i] * (1 - alpha);
            uiPixels[i].a = 1;
        }
        image.SetPixels(uiPixels);
        image.Apply();
        File.WriteAllBytes(Folder + filename, image.EncodeToPNG());
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        foreach (var canvas in overlays)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }
        foreach (var pair in layers)
            pair.Key.layer = pair.Value;
        Destroy(sceneImage);
        Destroy(uiObject);
        Destroy(image);
        target.Release();
        Destroy(target);
    }
}
