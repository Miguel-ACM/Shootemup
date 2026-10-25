using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyIndustrialGameplay
{
    const string Request = "ArtSource/PSXPreview/gameplay.request", Key = "IndustrialGameplayAuditStage";
    static VerifyIndustrialGameplay()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CheckHarness.ExitPlayMode();
                return;
            }
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            new GameObject("Temporary industrial runtime audit").AddComponent<IndustrialRuntimeProbe>();
            SessionState.SetInt(Key, 1);
            CheckHarness.EnterPlayMode();
        }
        else if (stage == 1 && File.Exists("ArtSource/PSXPreview/GameplayAudit.json"))
        {
            SessionState.SetInt(Key, 2);
            CheckHarness.ExitPlayMode();
        }
        else if (stage == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/PSX_Showcase.unity");
            SessionState.SetInt(Key, 0);
            File.Delete(Request);
            CheckHarness.EnterPlayMode();
        }
    }
}
