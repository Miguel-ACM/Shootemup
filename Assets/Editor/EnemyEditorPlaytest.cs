#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EnemyEditorPlaytest
{
    const string Key="EnemyEditorPlaytest";
    static EnemyEditorPlaytest()
    {
        EditorApplication.update+=Update;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode || string.IsNullOrEmpty(SessionState.GetString(Key,"")))return;
            RestoreStartScene();SessionState.EraseString(Key);
        };
    }
    static void RestoreStartScene()
    {
        var previous=SessionState.GetString(Key+"PreviousScene","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
    }
    public static void Request(EnemyEditorWindow.EnemyInfo enemy,bool phaseTwo)
    {
        if(EditorApplication.isPlaying && WaveWatcher.Current==null)return;
        SessionState.SetString(Key,enemy.type.Name);SessionState.SetInt(Key+"Level",(int)enemy.level);SessionState.SetBool(Key+"Boss",enemy.boss);SessionState.SetBool(Key+"Phase",phaseTwo);SessionState.SetInt(Key+"Step",0);
        if(EditorApplication.isPlaying)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()){SessionState.EraseString(Key);return;}
        var start=EditorSceneManager.playModeStartScene;SessionState.SetString(Key+"PreviousScene",start!=null ? AssetDatabase.GetAssetPath(start) : "");
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");EditorApplication.isPlaying=true;
    }
    static void Update()
    {
        string name=SessionState.GetString(Key,"");if(string.IsNullOrEmpty(name))return;
        if(!EditorApplication.isPlaying)return;
        int step=SessionState.GetInt(Key+"Step",0);
        if(step==0 && WaveWatcher.Current==null && SceneManager.GetActiveScene().name=="Title" && !CloudTransition.Busy && Time.frameCount>2)
        {WaveWatcher.Launch();SessionState.SetInt(Key+"Step",1);return;}
        var watcher=WaveWatcher.Current;if(watcher==null || !watcher.Ready || !WavePreparation.Ready || CloudTransition.Busy)return;
        if(step<=1){watcher.SelectLevel((LevelId)SessionState.GetInt(Key+"Level",0));SessionState.SetInt(Key+"Step",2);return;}
        bool boss=SessionState.GetBool(Key+"Boss",false);watcher.SelectView(boss ? WatcherView.Bosses : WatcherView.Enemies);
        if(boss)watcher.SelectEncounter(SessionState.GetBool(Key+"Phase",false) ? WaveWatcher.BossTwo : WaveWatcher.BossOne);
        else for(int i=0;i<watcher.Entries.Count;i++)if(watcher.Entries[i].Enemy?.Name==name){watcher.SelectEntry(i);break;}
        RestoreStartScene();SessionState.EraseString(Key);
    }
}
#endif
