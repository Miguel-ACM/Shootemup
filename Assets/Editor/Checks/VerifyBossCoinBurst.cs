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

[InitializeOnLoad]
public static class VerifyBossCoinBurst
{
    public const string Folder = "ArtSource/BossCoinBurstReview";
    const string Key = "BossCoinBurstReview";
    static double deadline;
    static VerifyBossCoinBurst()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 100;
            new GameObject("Boss coin burst review").AddComponent<BossCoinBurstReview>();
        };
        EditorApplication.update += () => {
            if (deadline == 0 || !SessionState.GetBool(Key, false)) return;
            string path = Folder + "/Verification.txt";
            if (!File.Exists(path) && EditorApplication.timeSinceStartup < deadline) return;
            SessionState.SetBool(Key, false);
            if (!File.Exists(path)) File.WriteAllText(path, "FAIL timeout");
            EditorApplication.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);
        };
    }
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/")) throw new Exception("Use the isolated review project");
        Directory.CreateDirectory(Folder); File.Delete(Folder + "/Verification.txt");
        PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = Key;
        typeof(VerifySecondaryItems).GetMethod("NativeGameView", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/L1.unity");
        EditorApplication.isPlaying = true;
    }
    public static void Build()
    {
        if (!File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS")) throw new Exception("Review first");
        VerifyBossRefit.Build();
    }
}

public sealed class BossCoinBurstReview : MonoBehaviour
{
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    void Awake() { Application.runInBackground = true; Application.logMessageReceived += Log; }
    void OnDestroy() { Application.logMessageReceived -= Log; }
    void Log(string text, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search")) errors.Add(text + "\n" + trace);
    }
    void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks.Add(message); File.WriteAllLines(VerifyBossCoinBurst.Folder + "/Progress.txt", checks);
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame(); var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(VerifyBossCoinBurst.Folder + "/" + name + ".png", image.EncodeToPNG()); Destroy(image);
    }
    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (stack.Count > 0)
        {
            object step = null; bool more = false; Exception failure = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; } catch (Exception e) { failure = e; }
            if (failure != null) { errors.Add(failure.ToString()); break; }
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; } yield return step;
        }
        Time.timeScale = 1;
        File.WriteAllText(VerifyBossCoinBurst.Folder + "/Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n",checks) + "\n" + string.Join("\n",errors));
    }
    IEnumerator Run()
    {
        float until = Time.realtimeSinceStartup + 60;
        WaveOrchestrer flow;
        while ((flow = FindAnyObjectByType<WaveOrchestrer>()) == null && Time.realtimeSinceStartup < until) yield return null;
        Check(flow != null, "isolated run loads");
        flow.enabled = false; flow.StopAllCoroutines(); flow.CancelInvoke();
        while ((CloudTransition.Busy || !WavePreparation.Ready) && Time.realtimeSinceStartup < until) yield return null;
        foreach (var shop in FindObjectsByType<DecisionShop>()) shop.Abort();
        foreach (var e in AEnemy.Live.ToArray()) if (e != null) Destroy(e.gameObject);
        foreach (var p in FindObjectsByType<APickable>()) Destroy(p.gameObject);
        yield return null;
        CombatDecision.End(); Time.timeScale = 1;
        GameRules.StartNewRun(); LevelRun.Reset(LevelId.Fields, 17381);
        var ship = FindAnyObjectByType<Shipcontroller>(); ship.ObserverMode = true;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.5f, -1);
        GameRules.pickDistance = 50; // Even an extreme magnet must allow the burst to open.
        var boss = (DistrictBoss)DistrictEnemy.Spawn(typeof(FieldsBomberBoss), new Vector3(0, 3, 1));
        yield return null;
        boss.SetOrchestrer(flow, 900);
        boss.damage(1000000);
        until = Time.time + 4;
        while (FindObjectsByType<Money>().Length == 0 && Time.time < until) yield return null;
        var coins = FindObjectsByType<Money>();
        int count = coins.Length;
        Check(count >= 50 && count <= 55, "actual boss finale preserves forty stage coins plus the configured bonus coins");
        Check(coins.All(c => c.IsScattering), "all boss coins use the same outward burst");
        yield return new WaitForSeconds(.65f);
        coins = FindObjectsByType<Money>();
        Check(coins.Length == count && RunCoins.Count == 0, "the burst resists immediate collection even with extreme pickup range");
        float width = coins.Max(c => c.transform.position.x) - coins.Min(c => c.transform.position.x);
        float height = coins.Max(c => c.transform.position.y) - coins.Min(c => c.transform.position.y);
        Check(width > 4 && height > 3, "native physics spreads the payout across " + width.ToString("F1") + " by " + height.ToString("F1") + " world units");
        yield return Capture("BossBurst");
        var positions = coins.Select(c => c.transform.position).ToArray();
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(.15f);
        Check(coins.Select((c,i) => c.transform.position == positions[i]).All(v => v), "pause freezes the burst");
        Time.timeScale = 1;
        until = Time.time + 3;
        while (FindObjectsByType<Money>().Length > 0 && Time.time < until) yield return null;
        Check(RunCoins.Count == count && FindObjectsByType<Money>().Length == 0, "normal attraction resumes and each coin credits the wallet once");
        GameRules.pickDistance = .01f;
        BossCoinBurst.Spawn(new Vector3(Camera.RightLimit,Camera.UpLimit,1),200);
        yield return new WaitForSeconds(.8f);
        coins = FindObjectsByType<Money>();
        Check(coins.Length == 200 && coins.All(c => c.transform.position.x >= Camera.LeftLimit && c.transform.position.x <= Camera.RightLimit &&
            c.transform.position.y >= Camera.DownLimit && c.transform.position.y <= Camera.UpLimit), "two hundred edge-launched coins remain visible and collectible");
        yield return Capture("EdgeBurst");
        Check(errors.Count == 0, "no runtime errors");
    }
}
#endif
