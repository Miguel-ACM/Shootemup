#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyBossPatterns
{
    const string Key = "BossPatternReview";
    static double deadline;
    static VerifyBossPatterns()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
              {
                  new GameObject("Boss pattern review").AddComponent<BossPatternProbe>();
                  deadline = EditorApplication.timeSinceStartup + 900;
              }
          };
        EditorApplication.update += () =>
          {
              if (!SessionState.GetBool(Key, false) || deadline == 0)
                  return;
              if (!File.Exists("Temp/BossPatterns.result") && EditorApplication.timeSinceStartup < deadline)
                  return;
              SessionState.SetBool(Key, false);
              bool passed = File.Exists("Temp/BossPatterns.result") && File.ReadAllText("Temp/BossPatterns.result").StartsWith("PASS");
              CheckHarness.Exit(passed ? 0 : 1);
          };
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Requires an idle verification copy");
        Directory.CreateDirectory("ArtSource/CombatReview/BossPatterns");
        if (File.Exists("Temp/BossPatterns.result"))
            File.Delete("Temp/BossPatterns.result");
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        CheckHarness.EnterPlayMode();
    }
}
#endif
