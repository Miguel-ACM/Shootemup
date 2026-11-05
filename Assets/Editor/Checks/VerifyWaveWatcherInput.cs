#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Headless pointer regression through the actual additive title-to-watcher handoff.
[InitializeOnLoad]
public static class VerifyWaveWatcherInput
{
    const string Key = "VerifyWaveWatcherInput";
    public const string Folder = "ReviewOutput/WaveWatcherInput";
    static double deadline;

    static VerifyWaveWatcherInput()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            deadline = EditorApplication.timeSinceStartup + 240;
            new GameObject("Watcher pointer regression").AddComponent<WaveWatcherInputProbe>();
        };
        EditorApplication.update += () =>
        {
            if (deadline == 0 || EditorApplication.timeSinceStartup < deadline) return;
            File.WriteAllText(Folder + "/Verification.txt", "FAIL: timed out");
            EditorApplication.Exit(1);
        };
    }

    public static void Run()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new InvalidOperationException("Use the isolated headless review project.");
        Directory.CreateDirectory(Folder);
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "WaveWatcherInputReview";
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
}

public sealed class WaveWatcherInputProbe : MonoBehaviour
{
    readonly List<string> checks = new List<string>();
    Mouse mouse;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
        UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        Exception error = null;
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception exception) { error = exception; }
            if (error != null) break;
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; }
            yield return step;
        }
        File.WriteAllText(VerifyWaveWatcherInput.Folder + "/Verification.txt",
            (error == null ? "PASS\n" : "FAIL\n") + string.Join("\n", checks) + "\n" + error);
        EditorApplication.Exit(error == null ? 0 : 1);
    }

    void Check(bool valid, string message)
    {
        checks.Add((valid ? "PASS " : "FAIL ") + message);
        File.WriteAllLines(VerifyWaveWatcherInput.Folder + "/Progress.txt", checks);
        if (!valid) throw new Exception(message);
    }

    void CheckActions(string context)
    {
        var events = EventSystem.current;
        var module = events != null ? events.currentInputModule as InputSystemUIInputModule : null;
        Check(module != null && module.point != null && module.point.action != null && module.point.action.enabled &&
            module.leftClick != null && module.leftClick.action != null && module.leftClick.action.enabled &&
            module.move != null && module.move.action != null && module.move.action.enabled &&
            module.submit != null && module.submit.action != null && module.submit.action.enabled,
            context + ": live pointer and navigation actions");
    }

    IEnumerator Click(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        Check(hits.Count > 0 && (hits[0].gameObject == target.gameObject || hits[0].gameObject.transform.IsChildOf(target)),
            "Pointer reaches " + target.name);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return new WaitForSecondsRealtime(.08f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
        yield return new WaitForSecondsRealtime(.08f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return new WaitForSecondsRealtime(.08f);
    }

    RectTransform Button(string name) => WaveWatcher.Current.GetComponentsInChildren<Button>().Single(button => button.name == name).GetComponent<RectTransform>();

    IEnumerator Run()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse = InputSystem.AddDevice<Mouse>();
        yield return SceneManager.LoadSceneAsync("Title");
        yield return null;
        yield return null;
        CheckActions("Title");
        yield return Click(FindAnyObjectByType<WaveWatcherMenu>().GetComponent<RectTransform>());
        Check(CloudTransition.Busy, "Title mouse click starts the transition");
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready) yield return null;
        yield return null;
        CheckActions("Watcher after title unload");
        var watcher = WaveWatcher.Current;
        yield return Click(Button("PAUSE"));
        Check(watcher.Paused && Time.timeScale == 0, "Mouse pauses playback");
        yield return Click(Button("PAUSE"));
        Check(!watcher.Paused && Time.timeScale > 0, "Mouse resumes while simulation is paused");
        yield return Click(Button("SNOW"));
        Check(watcher.Level == LevelId.Snow, "Mouse switches districts");
        yield return Click(Button("ENEMIES"));
        Check(watcher.View == WatcherView.Enemies, "Mouse switches catalogs");
        string next = watcher.Entries[1].Id;
        yield return Click(Button(next));
        Check(watcher.Selection == 1, "Mouse selects a rebuilt catalog entry");
        yield return Click(Button("BACK"));
        while (CloudTransition.Busy || SceneManager.GetActiveScene().name != "Title") yield return null;
        yield return null;
        Check(WaveWatcher.Current == null, "Back closes the watcher");
        CheckActions("Returned title");
        yield return Click(FindAnyObjectByType<WaveWatcherMenu>().GetComponent<RectTransform>());
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready) yield return null;
        yield return null;
        CheckActions("Reopened watcher");
        yield return Click(Button("PAUSE"));
        Check(WaveWatcher.Current.Paused, "Mouse works after reopening");
    }

    void OnDestroy()
    {
        if (mouse != null) InputSystem.RemoveDevice(mouse);
    }
}
#endif
