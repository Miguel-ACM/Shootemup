#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Command-line measurement of hostile pressure along the campaign. Every district's ordinary
// waves and boss phases play through the Wave Watcher with the pilot in observer mode, so
// enemies fly their full routes and fire throughout. Pressure is a relative curve, not a
// proof of fairness: it ignores the player's growing loadout and how aimed each volley is.
//   Unity -batchmode -projectPath <isolated copy> -executeMethod MeasureDifficultyCurve.Run
[InitializeOnLoad]
public static class MeasureDifficultyCurve
{
    const string Flag = "MeasureDifficultyCurve";
    public static string Output => Path.GetFullPath("ArtSource/DifficultyCurveReview");

    static MeasureDifficultyCurve()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false))
                return;
            MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/DifficultyCurve/Save");
            new GameObject("Difficulty curve measurement").AddComponent<DifficultyCurveRuntime>();
        };
    }

    public static void Run()
    {
        if (!Application.isBatchMode || Application.dataPath.Replace('\\', '/').StartsWith("C:/Users/Miguel/Shootemup/"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Path.GetFullPath("ReviewOutput/DifficultyCurve/Save"));
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "DifficultyCurve";
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool(Flag, true);
        EditorApplication.isPlaying = true;
    }

    public static void Finish(int code)
    {
        SessionState.SetBool(Flag, false);
        EditorApplication.Exit(code);
    }
}

public sealed class DifficultyCurveRuntime : MonoBehaviour
{
    static readonly LevelId[] Campaign =
    {
        LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel, LevelId.Desert,
        LevelId.Candyland, LevelId.Space, LevelId.Underwater, LevelId.Hell, LevelId.Heaven, LevelId.Void
    };
    readonly List<string> rows = new List<string> { "level,tier,encounter,seconds,launched,per_second,mean_live,peak_live,enemy_health" };
    readonly List<string> errors = new List<string>();
    readonly HashSet<CombatRound> seen = new HashSet<CombatRound>();
    WaveWatcher watcher;

    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        AudioListener.pause = true;
        Time.captureDeltaTime = 1f / 60;
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (type == LogType.Exception)
                errors.Add(message + "\n" + stack);
        };
        QualitySettings.renderPipeline = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        while (CloudTransition.Busy || Time.frameCount < 5)
            yield return null;
        PlayerPrefs.SetInt("WaveWatcher.Difficulty.v2", (int)RunDifficulty.Normal);
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        watcher = WaveWatcher.Current;
        if (watcher.Looping)
            watcher.ToggleLoop();
        string difficulty = Environment.GetCommandLineArgs().SkipWhile(a => a != "-curveDifficulty").Skip(1).FirstOrDefault() ?? "Normal";
        watcher.SelectDifficulty((RunDifficulty)Enum.Parse(typeof(RunDifficulty), difficulty));
        yield return Ready();
        foreach (var level in Campaign)
        {
            watcher.SelectLevel(level);
            yield return Ready();
            if (level != LevelId.Void)
                foreach (string step in DistrictWaves.Sequence())
                    if (int.TryParse(step, out int wave))
                        yield return Wave(level, wave);
            int phases = level == LevelId.Void ? (Difficulty.Easy ? 3 : 4) : Difficulty.Easy ? 2 : 3;
            for (int phase = 1; phase <= phases; phase++)
                yield return BossPhase(level, phase);
            File.WriteAllLines(MeasureDifficultyCurve.Output + "/" + difficulty + ".csv", rows);
        }
        rows.AddRange(errors.Select(e => "# " + e.Replace('\n', ' ')));
        File.WriteAllLines(MeasureDifficultyCurve.Output + "/" + difficulty + ".csv", rows);
        MeasureDifficultyCurve.Finish(errors.Count == 0 ? 0 : 1);
    }

    IEnumerator Ready()
    {
        yield return null;
        while (CloudTransition.Busy || !WavePreparation.Ready || !watcher.Ready)
            yield return null;
    }

    int CountLaunches()
    {
        int launched = 0;
        foreach (var round in CombatRound.Live)
            if (seen.Add(round))
                launched++;
        seen.IntersectWith(CombatRound.Live);
        return launched;
    }

    IEnumerator Wave(LevelId level, int wave)
    {
        watcher.SelectEncounter(wave);
        yield return Ready();
        seen.Clear();
        var counted = new HashSet<AEnemy>();
        float health = 0, seconds = 0, live = 0, quiet = 0;
        int launched = 0, peak = 0, frames = 0;
        while (seconds < 90)
        {
            yield return null;
            launched += CountLaunches();
            foreach (var enemy in AEnemy.Live)
                if (counted.Add(enemy))
                    health += enemy.health;
            if (counted.Count == 0)
                continue;
            seconds += Time.deltaTime;
            live += CombatRound.Live.Count;
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            frames++;
            quiet = AEnemy.Live.Count == 0 && CombatRound.Live.Count == 0 ? quiet + Time.deltaTime : 0;
            if (quiet > 1.5f)
                break;
        }
        seconds -= quiet;
        Record(level, "wave " + wave, seconds, launched, live / Mathf.Max(1, frames), peak, health);
    }

    IEnumerator BossPhase(LevelId level, int phase)
    {
        watcher.SelectEncounter(phase == 1 ? WaveWatcher.BossOne : phase == 2 ? WaveWatcher.BossTwo : phase == 3 ? WaveWatcher.BossThree : WaveWatcher.BossFour);
        yield return Ready();
        DistrictBoss boss = null;
        while (boss == null || boss.CurrentPhase < phase || boss.Invulnerable)
        {
            yield return null;
            boss = AEnemy.Live.OfType<DistrictBoss>().FirstOrDefault(b => b.health > 0);
        }
        float share = phase == 1 ? 1 - boss.PhaseThreshold(1) : boss.PhaseThreshold(phase - 1) - boss.PhaseThreshold(phase);
        float health = boss is CityDuoBoss duo && duo.partner != null ? boss.health + duo.partner.health : boss.health / Mathf.Max(.01f, boss.HealthFraction) * share;
        CountLaunches();
        float seconds = 0, live = 0;
        int launched = 0, peak = 0, frames = 0;
        while (seconds < 24)
        {
            yield return null;
            launched += CountLaunches();
            seconds += Time.deltaTime;
            live += CombatRound.Live.Count;
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            frames++;
        }
        Record(level, "boss " + phase, seconds, launched, live / Mathf.Max(1, frames), peak, health);
    }

    void Record(LevelId level, string encounter, float seconds, int launched, float live, int peak, float health)
    {
        var invariant = CultureInfo.InvariantCulture;
        rows.Add(string.Join(",", level, LevelRun.Tier(level), encounter, seconds.ToString("0.0", invariant), launched,
            (launched / Mathf.Max(1, seconds)).ToString("0.0", invariant), live.ToString("0.0", invariant), peak, health.ToString("0", invariant)));
    }
}
#endif
