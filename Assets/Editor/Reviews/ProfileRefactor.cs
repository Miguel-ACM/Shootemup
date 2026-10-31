#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Profiling;

// Bounded, reproducible full-frame review. Never runs in the player's working checkout.
[InitializeOnLoad]
public static class ProfileRefactor
{
    public const string Folder = "ArtSource/RefactorProfile";
    static double deadline;
    static ProfileRefactor()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("RefactorProfile", false)) return;
            deadline = EditorApplication.timeSinceStartup + 300;
            new GameObject("Refactor full-frame profile").AddComponent<RefactorFrameProfile>();
        };
        EditorApplication.update += () => {
            if (deadline == 0) return;
            string result = Folder + "/" + SessionState.GetString("RefactorProfileLabel", "Baseline") + "-Status.txt";
            if (File.Exists(result))
            {
                SessionState.SetBool("RefactorProfile", false);
                EditorApplication.Exit(File.ReadAllText(result).StartsWith("PASS") ? 0 : 1);
            }
            else if (EditorApplication.timeSinceStartup > deadline)
            {
                File.WriteAllText(result, "FAIL timeout");
                EditorApplication.Exit(1);
            }
        };
    }
    public static void Baseline() => Begin("Baseline", false);
    public static void Optimized() => Begin("Optimized", false);
    public static void ResumeBaseline() => Begin("Baseline", true);
    static void Begin(string label, bool resume)
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/CombatReworkReview/"))
            throw new InvalidOperationException("Use the isolated CombatReworkReview checkout.");
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/" + label + "-Status.txt");
        if (!resume)
            foreach (var level in new[] { LevelId.Fields, LevelId.Tunnel, LevelId.Space })
                File.Delete(Folder + "/" + label + "-" + level + ".csv");
        SessionState.SetString("RefactorProfileLabel", label);
        SessionState.SetBool("RefactorProfile", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorApplication.isPlaying = true;
    }
}
public sealed class RefactorFrameProfile : MonoBehaviour
{
    const int Frames = 1800;
    readonly List<string> errors = new List<string>();
    readonly List<(string name, ProfilerRecorder recorder)> counters = new List<(string, ProfilerRecorder)>();
    string label;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Screen.SetResolution(1280, 720, false);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        label = SessionState.GetString("RefactorProfileLabel", "Baseline");
        Application.logMessageReceived += Log;
    }
    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search")) errors.Add(message);
    }
    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        foreach (var counter in counters) counter.recorder.Dispose();
        Time.captureDeltaTime = 0;
    }
    void Add(ProfilerCategory category, string name)
    {
        counters.Add((name, ProfilerRecorder.StartNew(category, name, 1,
            ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame)));
    }
    IEnumerator Start()
    {
        yield return null;
        GameRules.StartNewRun();
        Difficulty.Select(RunDifficulty.Hard);
        Add(ProfilerCategory.Internal, "Main Thread");
        Add(ProfilerCategory.Memory, "GC Allocated In Frame");
        Add(ProfilerCategory.Scripts, "BehaviourUpdate");
        Add(ProfilerCategory.Physics2D, "Physics2D.Simulate");
        Add(ProfilerCategory.Scripts, "PlayerRound.Spawn");
        Add(ProfilerCategory.Scripts, "PlayerRound.Create");
        Add(ProfilerCategory.Scripts, "CombatRound.Launch");
        Add(ProfilerCategory.Scripts, "ProjectileGlow.Render");
        foreach (var level in new[] { LevelId.Fields, LevelId.Tunnel, LevelId.Space })
        {
            if (File.Exists(ProfileRefactor.Folder + "/" + label + "-" + level + ".csv")) continue;
            LevelRun.Reset(level, 17381);
            CloudTransition.Load("L1");
            yield return null;
            while (SceneManager.GetActiveScene().name != "L1" || FindAnyObjectByType<WaveOrchestrer>() == null) yield return null;
            var flow = FindAnyObjectByType<WaveOrchestrer>();
            flow.enabled = false; flow.CancelInvoke(); flow.StopAllCoroutines();
            while (CloudTransition.Busy || !WavePreparation.Ready) yield return null;
            // A reload of L1 replaces the previous flow after Load returns.
            flow = FindAnyObjectByType<WaveOrchestrer>();
            flow.enabled = false; flow.CancelInvoke(); flow.StopAllCoroutines();
            var ship = FindAnyObjectByType<Shipcontroller>();
            ship.enabled = false; ship.ObserverMode = true;
            foreach (var weapon in ship.GetComponentsInChildren<AWeapon>()) { weapon.StopFiring(); weapon.enabled = false; }
            CombatDecision.End(); GameRules.playerDisabled = false; Time.timeScale = 1;
            foreach (var enemy in FindObjectsByType<AEnemy>()) Destroy(enemy.gameObject);
            foreach (var shot in FindObjectsByType<ABullet>()) shot.Despawn();
            CombatRound.ClearAll();
            yield return null;
            UnityEngine.Random.InitState(17381);
            Time.captureDeltaTime = 1f / 60;
            ship.transform.position = new Vector3(0, Camera.DownLimit + 2, -1);
            var boss = (DistrictBoss)DistrictEnemy.Spawn(DistrictWaves.BossType, new Vector3(0, Camera.UpLimit - 2, 1));
            yield return null;
            // Exclude setup/JIT, then retain every burst frame and following physics step.
            for (int warm = 0; warm < 120; warm++) yield return null;
            var samples = new long[Frames, counters.Count];
            var dt = new double[Frames];
            var hostiles = new int[Frames];
            var friendlies = new int[Frames];
            var created = new int[Frames];
            int startCreated = PlayerRound.CreatedCount;
            double previous = Time.realtimeSinceStartupAsDouble;
            for (int frame = 0; frame < Frames; frame++)
            {
                ship.transform.position = new Vector3(Mathf.Sin(frame / 135f) * Camera.RightLimit * .65f, Camera.DownLimit + 2, -1);
                if (frame % 5 == 0)
                    for (int shot = 0; shot < 6; shot++)
                        PlayerRound.Spawn(ship.transform.position + new Vector3((shot - 2.5f) * .16f, .7f, 0),
                            CombatVisuals.Rotate(Vector2.up, (shot - 2.5f) * 4), .01f, 12, (PlayerDelivery)shot);
                if (frame == 600 || frame == 1200)
                {
                    if (boss is SpaceCitadelBoss citadel && frame == 600)
                    {
                        citadel.Parts[0].health = citadel.Parts[1].health = 0;
                        citadel.PartChanged();
                    }
                    typeof(DistrictBoss).GetMethod("EnterPhase", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boss, new object[] { frame == 600 ? 2 : 3 });
                }
                yield return null;
                double now = Time.realtimeSinceStartupAsDouble;
                dt[frame] = (now - previous) * 1000; previous = now;
                hostiles[frame] = CombatRound.Live.Count;
                friendlies[frame] = PlayerRound.LiveCount;
                created[frame] = PlayerRound.CreatedCount - startCreated;
                for (int c = 0; c < counters.Count; c++) samples[frame, c] = counters[c].recorder.Valid ? counters[c].recorder.LastValue : -1;
            }
            var csv = new StringBuilder("frame,wall_ms,hostile,friendly,friendly_created");
            foreach (var counter in counters) csv.Append(',').Append(counter.name);
            csv.AppendLine();
            for (int frame = 0; frame < Frames; frame++)
            {
                csv.Append(frame).Append(',').Append(dt[frame].ToString("F3", CultureInfo.InvariantCulture)).Append(',').Append(hostiles[frame]).Append(',').Append(friendlies[frame]).Append(',').Append(created[frame]);
                for (int c = 0; c < counters.Count; c++) csv.Append(',').Append(samples[frame, c]);
                csv.AppendLine();
            }
            File.WriteAllText(ProfileRefactor.Folder + "/" + label + "-" + level + ".csv", csv.ToString());
            DesertReview.Capture(label + "-" + level, ProfileRefactor.Folder);
            Time.captureDeltaTime = 0;
            Destroy(boss.gameObject);
            yield return null;
        }
        File.WriteAllText(ProfileRefactor.Folder + "/" + label + "-Status.txt", (errors.Count == 0 ? "PASS" : "FAIL") +
            "\nUnity " + Application.unityVersion + "; " + SystemInfo.processorType + "; " + SystemInfo.graphicsDeviceName +
            "\n1280x720, Hard, seed 17381, fixed simulation 60Hz, 1800 measured frames per boss, three phases, active physics, mixed friendly stream 72/s. Editor wall timings include frame pacing; counters are nanoseconds except allocation bytes.\n" + string.Join("\n", errors));
    }
}
#endif
