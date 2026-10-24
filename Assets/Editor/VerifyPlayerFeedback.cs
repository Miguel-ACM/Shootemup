#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class VerifyPlayerFeedback
{
    const string Key="PlayerFeedbackReview";
    static double deadline;
    static VerifyPlayerFeedback()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {
                new GameObject("Player feedback review").AddComponent<PlayerFeedbackProbe>();
                deadline=EditorApplication.timeSinceStartup+480;
            }
        };
        EditorApplication.update+=()=>
        {
            if(!SessionState.GetBool(Key,false) || deadline==0)return;
            if(File.Exists("Temp/PlayerFeedback.result"))
            {
                SessionState.SetBool(Key,false);
                EditorApplication.Exit(File.ReadAllText("Temp/PlayerFeedback.result").StartsWith("PASS") ? 0 : 1);
            }
            else if(EditorApplication.timeSinceStartup>deadline)
            {
                File.WriteAllText("Temp/PlayerFeedback.result","FAIL review timed out");
                EditorApplication.Exit(1);
            }
        };
    }
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Review requires an idle editor");
        Directory.CreateDirectory("ArtSource/CombatReview/PlayerFeedback");
        RenderFleetReview.PlainsBoss();
        if(File.Exists("Temp/PlayerFeedback.result"))File.Delete("Temp/PlayerFeedback.result");
        if(File.Exists("Temp/CombatResult.txt"))File.Delete("Temp/CombatResult.txt");
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        var view=EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));view.Show();view.Focus();
        EditorApplication.isPlaying=true;
    }
}
#endif
