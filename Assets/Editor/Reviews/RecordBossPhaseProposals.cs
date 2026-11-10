using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Records every boss phase through the Wave Watcher, once with the shipping cards and
// once with the proposed ones (DistrictBoss.ProposedCards), for side-by-side review.
// Run only in an isolated review checkout:
//   Unity.exe -projectPath <copy> -executeMethod RecordBossPhaseProposals.Current
//   Unity.exe -projectPath <copy> -executeMethod RecordBossPhaseProposals.Proposed
// Optional: -bossFilter <substring of the clip name> records only matching phases.
// Clips already encoded are skipped, so a stopped run resumes where it left off.
// Each clip is encoded to ArtSource/BossPhaseProposals/Clips/<set>/<clip>.mp4 (play field only);
// Tools/compose_boss_proposals.py puts the two sets side by side.
[InitializeOnLoad]
public static class RecordBossPhaseProposals
{
    const string Key = "RecordBossPhaseProposals";
    static RecordBossPhaseProposals()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(Key, "") != "")
                new GameObject("Boss phase proposal recording").AddComponent<BossPhaseProposalRecording>().Set = SessionState.GetString(Key, "");
        };
        if (SessionState.GetString(Key, "") != "")
        {
            KeepAwake();
            // Keep the player loop and game view ticking while the editor is in the background.
            EditorApplication.update += () =>
            {
                if (!EditorApplication.isPlaying)
                    return;
                EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            };
        }
    }

    public static void Current() => Run("current");
    public static void Proposed() => Run("proposed");
    // The chosen cards once they ship, recorded against the earlier "current" set.
    public static void Final() => Run("final");

    // Windows 11 throttles background processes (EcoQoS); opt this editor out so the
    // recording keeps its pace while Miguel works in other windows.
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    struct PowerThrottling
    {
        public uint Version, ControlMask, StateMask;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool SetProcessInformation(IntPtr process, int informationClass, ref PowerThrottling information, int size);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    static void KeepAwake()
    {
        var state = new PowerThrottling { Version = 1, ControlMask = 1 | 4, StateMask = 0 };
        SetProcessInformation(GetCurrentProcess(), 4, ref state, System.Runtime.InteropServices.Marshal.SizeOf(state));
        System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
        EditorPrefs.SetInt("InteractionMode", 1); // No throttling.
        Application.runInBackground = true;
    }

    static void Run(string set)
    {
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "BossPhaseProposals";
        PlayerSettings.runInBackground = true;
        KeepAwake();
        typeof(VerifySecondaryItems).GetMethod("NativeGameView", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        SessionState.SetString(Key, set);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
        EditorApplication.isPlaying = true;
    }
}

public sealed class BossPhaseProposalRecording : MonoBehaviour
{
    public string Set;
    const int Frames = 1800; // 60 seconds at 30 fps once the phase is under way.
    const string Ffmpeg = @"C:\Program Files\ffmpeg\bin\ffmpeg.exe";
    // The Wave Watcher frames the 9:16 play field between its two side panels.
    const string Crop = "crop=trunc(iw*270/640/2)*2:ih:trunc(iw*185/640/2)*2:0";
    static readonly MethodInfo PushToPhase = typeof(WaveWatcher).GetMethod("PushToPhase", BindingFlags.Static | BindingFlags.NonPublic);

    static string Filter()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-bossFilter")
                return args[i + 1];
        return null;
    }

    IEnumerator Start()
    {
        string root = Path.GetFullPath("ArtSource/BossPhaseProposals");
        string output = root + "/Frames/" + Set;
        Directory.CreateDirectory(output);
        string progress = root + "/Progress-" + Set + ".txt";
        DontDestroyOnLoad(gameObject);
        DistrictBoss.ProposedCards = Set == "proposed";
        string filter = Filter();
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || !WavePreparation.Ready || CloudTransition.Busy) yield return null;
        var watcher = WaveWatcher.Current;
        watcher.SelectDifficulty(RunDifficulty.Normal);
        while (!WavePreparation.Ready) yield return null;
        // REPEAT would respawn the boss every 24 s, before late phases and mid-capture.
        if (watcher.Looping)
            watcher.ToggleLoop();
        Time.captureDeltaTime = 1f / 30;
        foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            watcher.SelectLevel(level);
            while (!WavePreparation.Ready || CloudTransition.Busy) yield return null;
            watcher.SelectView(WatcherView.Bosses);
            yield return null;
            for (int index = 0; index < watcher.Entries.Count; index++)
            {
                var entry = watcher.Entries[index];
                int phase = entry.Encounter == WaveWatcher.BossOne ? 1 : entry.Encounter == WaveWatcher.BossTwo ? 2 :
                    entry.Encounter == WaveWatcher.BossThree ? 3 : entry.Encounter == WaveWatcher.BossFour ? 4 : 0;
                if (phase == 0)
                    continue;
                var type = entry.Enemy ?? DistrictWaves.Bosses(level)[0];
                string clip = level + "-" + type.Name.Replace("Boss", "") + "-" + phase;
                if (filter != null && clip.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                // Resume: a clip already encoded for this set is kept.
                if (File.Exists(root + "/Clips/" + Set + "/" + clip + ".mp4"))
                    continue;
                watcher.SelectEntry(index);
                // Wait until the boss has reached the phase and finished its spell break.
                DistrictBoss boss = null;
                float nextPush = 10;
                for (float waited = 0; waited < 30; waited += Time.deltaTime)
                {
                    boss = FindLeadBoss(phase);
                    if (boss != null && boss.CurrentPhase >= phase && !boss.Invulnerable)
                        break;
                    // Fallback for a boss the watcher does not push in time (never the City duo,
                    // whose survivor phases need the watcher's victim handling).
                    if (waited >= nextPush && boss != null && !(boss is CityDuoBoss) && !boss.Invulnerable)
                    {
                        PushToPhase.Invoke(null, new object[] { boss, null, boss.CurrentPhase + 1 });
                        nextPush = waited + 2;
                    }
                    yield return null;
                }
                string folder = output + "/" + clip;
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
                Directory.CreateDirectory(folder);
                for (int f = 0; f < Frames; f++)
                {
                    yield return new WaitForEndOfFrame();
                    var texture = ScreenCapture.CaptureScreenshotAsTexture(2);
                    File.WriteAllBytes(folder + "/" + f.ToString("D4") + ".jpg", texture.EncodeToJPG(88));
                    Destroy(texture);
                }
                Encode(folder, root + "/Clips/" + Set + "/" + clip + ".mp4");
                File.AppendAllText(progress, clip + (boss != null && boss.CurrentPhase >= phase ? "" : " (phase not reached)") + "\n");
            }
        }
        Time.captureDeltaTime = 0;
        foreach (var encoder in encoders)
            encoder.WaitForExit();
        File.AppendAllText(progress, "Complete: Normal, third phase previewed, 60 s at 30 fps, observer without player fire, silent.\n");
        EditorApplication.Exit(0);
    }

    // A minute of 2x frames is about 400 MB, so each clip is cropped and encoded in the
    // background while the next one records, and its frames are deleted once encoded.
    static readonly System.Collections.Generic.List<System.Diagnostics.Process> encoders = new System.Collections.Generic.List<System.Diagnostics.Process>();

    static void Encode(string frames, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        var start = new System.Diagnostics.ProcessStartInfo(Ffmpeg,
            "-y -loglevel error -framerate 30 -i \"" + frames + "/%04d.jpg\" -vf " + Crop +
            ",format=yuv420p -c:v libx264 -crf 21 -preset fast \"" + target + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = new System.Diagnostics.Process { StartInfo = start, EnableRaisingEvents = true };
        encoders.Add(process);
        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0 && File.Exists(target))
                Directory.Delete(frames, true);
        };
        process.Start();
    }

    static DistrictBoss FindLeadBoss(int phase)
    {
        DistrictBoss best = null;
        foreach (var boss in FindObjectsByType<DistrictBoss>(FindObjectsSortMode.None))
            if (boss.health > 0 && (best == null || boss.CurrentPhase > best.CurrentPhase))
                best = boss;
        return best;
    }
}
