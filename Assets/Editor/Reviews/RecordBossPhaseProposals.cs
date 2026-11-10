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
// Frames land in ArtSource/BossPhaseProposals/Frames/<set>/<clip>; Tools/compose_boss_proposals.py
// turns them into side-by-side videos.
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
    }

    public static void Current() => Run("current");
    public static void Proposed() => Run("proposed");

    static void Run(string set)
    {
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "BossPhaseProposals";
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
    const int Frames = 600; // 20 seconds at 30 fps once the phase is under way.
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
                File.AppendAllText(progress, clip + (boss != null && boss.CurrentPhase >= phase ? "" : " (phase not reached)") + "\n");
            }
        }
        Time.captureDeltaTime = 0;
        File.AppendAllText(progress, "Complete: Normal, third phase previewed, 30 fps, observer without player fire, silent.\n");
        EditorApplication.Exit(0);
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
