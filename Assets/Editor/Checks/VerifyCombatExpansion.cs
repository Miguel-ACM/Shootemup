using System.IO;
using UnityEditor;
using UnityEngine;

// One batched play session; Unity restores the user's edit-time scene when Play mode ends.
[InitializeOnLoad]
public static class VerifyCombatExpansion
{
    const string Request = "Temp/verify-combat.request", Key = "CombatExpansionVerification";
    static double deadline;
    static VerifyCombatExpansion()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
              {
                  if (SessionState.GetBool(Key + "TitleHud", false))
                      new GameObject("Natural title and HUD startup review").AddComponent<TitleHudStartupProbe>();
                  else if (SessionState.GetBool(Key + "Polish", false))
                      new GameObject("Combined visual and sound polish review").AddComponent<PolishReviewProbe>().TitleAfterVisuals = SessionState.GetBool(Key + "VisualTitle", false);
                  else if (SessionState.GetBool(Key + "Choreography", false))
                      new GameObject("District choreography verification").AddComponent<ChoreographyProbe>().SnowOnly = SessionState.GetBool(Key + "SnowDance", false);
                  else if (SessionState.GetBool(Key + "Ttk", false))
                      new GameObject("Plains base gun verification").AddComponent<PlainsTtkProbe>();
                  else if (SessionState.GetBool(Key + "Feedback", false))
                      new GameObject("Decision feedback verification").AddComponent<DecisionFeedbackProbe>();
                  else
                  {
                      var probe = new GameObject("Combat expansion verification").AddComponent<CombatExpansionProbe>();
                      probe.FlowOnly = SessionState.GetBool(Key + "Flow", false);
                  }
                  deadline = EditorApplication.timeSinceStartup + (SessionState.GetBool(Key + "Polish", false) ? 480 : SessionState.GetBool(Key + "Choreography", false) ? 1200 : 240);
              }
          };
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        if (File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            string mode;
            try
            {
                mode = File.ReadAllText(Request);
                File.Delete(Request);
            }
            catch (IOException) { return; }
            SessionState.SetBool(Key + "TitleHud", mode.Trim() == "title-hud");
            SessionState.SetBool(Key + "Feedback", mode.Trim() == "feedback");
            SessionState.SetBool(Key + "Polish", mode.Trim() == "polish" || mode.Trim() == "visuals-title");
            SessionState.SetBool(Key + "VisualTitle", mode.Trim() == "visuals-title");
            SessionState.SetBool(Key + "Ttk", mode.Trim() == "ttk");
            SessionState.SetBool(Key + "Choreography", mode.Trim() == "choreography" || mode.Trim() == "snow-choreography");
            SessionState.SetBool(Key + "SnowDance", mode.Trim() == "snow-choreography");
            SessionState.SetBool(Key + "Flow", mode.Trim() == "flow");
            Directory.CreateDirectory("ArtSource/CombatReview");
            if (File.Exists("Temp/CombatResult.txt"))
                File.Delete("Temp/CombatResult.txt");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
        if (SessionState.GetBool(Key, false) && EditorApplication.isPlaying &&
            (File.Exists("Temp/CombatResult.txt") || (deadline > 0 && EditorApplication.timeSinceStartup > deadline)))
        {
            if (!File.Exists("Temp/CombatResult.txt"))
                File.WriteAllText("Temp/CombatResult.txt", "FAIL: verification timed out");
            SessionState.SetBool(Key, false);
            EditorApplication.isPlaying = false;
        }
    }
}
