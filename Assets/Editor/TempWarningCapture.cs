using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Temporary: an unsaved scene with only a camera and the warning; plays it and screenshots it.</summary>
[InitializeOnLoad]
public static class TempWarningCapture
{
    const string Request = "ArtSource/PSXv2/warning.request", Folder = "ArtSource/PSXv2/WarningCapture";
    static TempWarningCapture()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        int stage = SessionState.GetInt("WarnCap", 0);
        if (stage != 0 && File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt("WarnCap", 0);
            stage = 0;
        }
        if (stage == 0)
        {
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty)
                {
                    File.WriteAllText(Folder + ".txt", "scene has unsaved changes");
                    return;
                }
            File.Delete(Request);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera").AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.16f, .2f, .13f);
            camera.tag = "MainCamera";
            var holder = new GameObject("Warning");
            var warning = holder.AddComponent<Warning>();
            warning.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/prstartk.asset");
            holder.AddComponent<TempWarningShots>().warning = warning;
            EditorSceneManager.playModeStartScene = null;   // play this scene, not the title (restored below)
            SessionState.SetInt("WarnCap", 1);
            EditorApplication.isPlaying = true;
            return;
        }
        if (stage == 1 && EditorApplication.isPlaying && File.Exists(Folder + "/done.txt"))
        {
            SessionState.SetInt("WarnCap", 2);
            EditorApplication.isPlaying = false;
            return;
        }
        if (stage == 2 && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetInt("WarnCap", 0);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        }
    }
}
