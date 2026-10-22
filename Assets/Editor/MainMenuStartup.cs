using UnityEditor;
using UnityEditor.SceneManagement;

// Match the build entry point regardless of the scene currently being edited.
[InitializeOnLoad]
public static class MainMenuStartup
{
    static MainMenuStartup()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
    }
}
