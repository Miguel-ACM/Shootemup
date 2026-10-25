using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

[InitializeOnLoad]
public static class VerifyInputMigration
{
    const string Key = "VerifyInputMigration";
    static VerifyInputMigration()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                  return;
              SessionState.SetBool(Key, false);
              var go = new GameObject("Input migration verification");
              UnityEngine.Object.DontDestroyOnLoad(go);
              go.AddComponent<InputMigrationProbe>();
          };
    }

    public static void Run()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        throw new Exception("Legacy input backend is still compiled in");
#endif
#if !ENABLE_INPUT_SYSTEM
        throw new Exception("Input System backend is not enabled");
#endif
        foreach (string path in new[] { "Assets/Scenes/Title.unity", "Assets/Scenes/L1.unity", "Assets/Scenes/PSX_Showcase.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            int modules = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentsInChildren<StandaloneInputModule>(true).Length != 0)
                    throw new Exception("Legacy UI input in " + path);
                foreach (var module in root.GetComponentsInChildren<InputSystemUIInputModule>(true))
                {
                    if (!module.enabled)
                        throw new Exception("Disabled UI module in " + path);
                    modules++;
                }
            }
            if (modules != 1)
                throw new Exception("Expected one Input System UI module in " + path);
        }
        Directory.CreateDirectory("FlowReview");
        if (File.Exists("FlowReview/InputMigration.txt"))
            File.Delete("FlowReview/InputMigration.txt");
        SessionState.SetBool(Key, true);
        CheckHarness.EnterPlayMode();
    }
}
