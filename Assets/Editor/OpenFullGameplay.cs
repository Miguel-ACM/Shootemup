using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class OpenFullGameplay
{
    const string Request="ArtSource/PSXPreview/open-gameplay.request";
    static OpenFullGameplay(){EditorApplication.update+=Check;}
    static void Check()
    {
        if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete(Request);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        var roots=scene.GetRootGameObjects();
        var waves=roots.SelectMany(r=>r.GetComponentsInChildren<WaveOrchestrer>(true)).Single();
        var boss=new SerializedObject(waves).FindProperty("boss").objectReferenceValue;
        bool gameplay=waves.enabled && waves.gameObject.activeInHierarchy;
        bool dialog=roots.SelectMany(r=>r.GetComponentsInChildren<Dialog>(true)).Any(d=>d.gameObject.activeInHierarchy);
        bool demo=roots.SelectMany(r=>r.GetComponentsInChildren<PSXShowcase>(true)).Any();
        File.WriteAllText("ArtSource/PSXPreview/GameplaySceneReport.json","{\"scene\":\"L1\",\"wavesEnabled\":"+B(gameplay)+",\"shopDialogPresent\":"+B(dialog)+",\"bossAssigned\":"+B(boss!=null)+",\"showcasePresent\":"+B(demo)+"}");
        Debug.Log("Full L1 gameplay opened: normal waves, shop and boss; no showcase autopilot.");
        EditorApplication.isPlaying=true;
    }
    static string B(bool value)=>value ? "true" : "false";
}
