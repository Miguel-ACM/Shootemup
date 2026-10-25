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

[InitializeOnLoad]
public static class VerifyRiverbanks
{
    public const string Folder = "ArtSource/RiverbankReview";
    static double deadline;
    static VerifyRiverbanks()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("RiverbankReview", false))
                  return;
              new GameObject("Riverbank visual review").AddComponent<RiverbankReview>();
              deadline = EditorApplication.timeSinceStartup + 420;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              var report = Folder + (SessionState.GetBool("RiverbankBaseline", false) ? "/Baseline.txt" : "/Verification.txt");
              if (File.Exists(report))
              {
                  SessionState.SetBool("RiverbankReview", false);
                  CheckHarness.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(report, "FAIL timeout");
                  CheckHarness.Exit(1);
              }
          };
    }

    public static void Baseline() => Begin(true);
    public static void Run() => Begin(false);
    static void Begin(bool baseline)
    {
        Directory.CreateDirectory(Folder + "/Frames");
        File.Delete(Folder + (baseline ? "/Baseline.txt" : "/Verification.txt"));
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("RiverbankReview", true);
        SessionState.SetBool("RiverbankBaseline", baseline);
        CheckHarness.EnterPlayMode();
    }
}
public sealed class RiverbankReview : MonoBehaviour
{
    const string Folder = VerifyRiverbanks.Folder;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
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
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message + "\n" + trace);
    }

    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        checks.Add("PASS " + message);
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            Exception failure = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception e) { failure = e; }
            if (failure != null)
            {
                errors.Add(failure.ToString());
                break;
            }
            if (!more)
            {
                stack.Pop();
                continue;
            }
            if (step is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return step;
        }
        File.WriteAllText(Folder + (SessionState.GetBool("RiverbankBaseline", false) ? "/Baseline.txt" : "/Verification.txt"), (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    IEnumerator Run()
    {
        bool baseline = SessionState.GetBool("RiverbankBaseline", false);
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Fields, 17381);
        yield return SceneManager.LoadSceneAsync("L1");
        yield return null;
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.StopAllCoroutines();
        flow.CancelInvoke();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.enabled = false;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.8f, -1);
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>())
        {
            weapon.StopFiring();
            weapon.enabled = false;
        }
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        GameRules.playerDisabled = false;
        var field = FindAnyObjectByType<PSXGrassField>();
        field.enabled = false;
        var sections = field.GetComponentsInChildren<MeshFilter>().Where(m => m.name.StartsWith("Valley section")).OrderBy(m => m.name).ToArray();
        Check(sections.Length == 4 && field.GetComponentsInChildren<Collider2D>().Length == 0 && field.GetComponentsInChildren<Collider>().Length == 0, "four batched decorative terrain sections without physics colliders");
        foreach (var shader in new[] { Resources.Load<Material>("PSXValley").shader, Resources.Load<Shader>("PSXValleyRiver") })
            Check(shader.isSupported && !ShaderUtil.ShaderHasError(shader), shader.name + " compiles");
        foreach (float station in new[] { 0f, 18f, 39f, 71f, 95.9f })
        {
            for (int i = 0; i < sections.Length; i++)
                sections[i].transform.localPosition = new Vector3(0, Mathf.Repeat((i - 1) * 24 - station + 36, 96) - 36, 88);
            yield return null;
            DesertReview.Capture((baseline ? "Before" : "After") + "-" + station.ToString("F1", System.Globalization.CultureInfo.InvariantCulture), Folder);
        }
        if (baseline)
            yield break;
        foreach (int seed in new[] { 17381, 421, 9826 })
        {
            var layout = new PSXValleyLayout(seed);
            var seam = new List<Vector3[]>();
            int total = 0;
            for (int section = 0; section < 4; section++)
            {
                float origin = (section - 1) * 24;
                var ground = PSXValleyGeometry.Build(origin, 24, 1, layout, seed);
                var river = PSXValleyRiver.Build(origin, 24, layout);
                total += ground.vertexCount;
                Check(ground.vertexCount < 65000 && ground.vertices.All(v => float.IsFinite(v.x + v.y + v.z)), seed + " section " + section + ": bounded finite bank geometry (" + ground.vertexCount + " vertices)");
                var points = river.vertices;
                int end = points.Length - 9;
                seam.Add(new[] { points[0] + Vector3.up * origin, points[8] + Vector3.up * origin, points[end] + Vector3.up * origin, points[end + 8] + Vector3.up * origin });
                UnityEngine.Object.Destroy(ground);
                UnityEngine.Object.Destroy(river);
            }
            for (int i = 0; i < 4; i++)
            {
                var next = seam[(i + 1) % 4];
                Vector3 offset = i == 3 ? Vector3.up * 96 : Vector3.zero;
                Check(Vector3.Distance(seam[i][2], next[0] + offset) < .0001f && Vector3.Distance(seam[i][3], next[1] + offset) < .0001f, seed + " river and bank section seam " + i + " is continuous");
            }
            checks.Add("MEASURE " + seed + ": " + total + " terrain vertices across four batched sections; no additional renderers or per-frame shore work");
        }
        var travel = typeof(PSXGrassField).GetField("travel", BindingFlags.Instance | BindingFlags.NonPublic);
        travel.SetValue(field, 90f);
        field.enabled = true;
        float started = Time.time, last = -1;
        int frame = 0;
        while (Time.time - started < 8)
        {
            float elapsed = Time.time - started;
            if (elapsed - last > .12f)
            {
                last = elapsed;
                DesertReview.Capture("Frames/River-" + (frame++).ToString("D4"), Folder);
            }
            yield return null;
        }
        var ys = sections.Select(m => m.transform.localPosition.y).OrderBy(y => y).ToArray();
        Check(Enumerable.Range(1, 3).All(i => Mathf.Abs(ys[i] - ys[i - 1] - 24) < .001f) && (float)travel.GetValue(field) < 40, "scrolling through the full-loop wrap keeps all terrain sections attached");
    }
}
#endif
