#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

// Stop cleanly before a chat-driven script correction invalidates scene/static state.
// A fresh Play session then reopens the same seeded preview with the corrected code.
[InitializeOnLoad]
public static class WatcherReload
{
    const string Key="WaveWatcher.PendingCorrection";
    static bool failed;
    static bool awaitingCompilation,refreshNeeded;
    static bool freshPlay,deliberateExit;
    static volatile bool sourceChanged;
    static FileSystemWatcher sourceFiles;
    public static event Action<WaveWatcher> Restored;
    [Serializable]
    sealed class State
    {
        public LevelId level;
        public WatcherView view;
        public string id;
        public int seed,loop,variant,mirror,aim,index;
        public float speed;
        public bool paused,repeat;
    }
    static WatcherReload()
    {
        sourceFiles=new FileSystemWatcher(Application.dataPath,"*.cs"){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.FileName};
        sourceFiles.Changed+=(sender,args)=>sourceChanged=true;sourceFiles.Created+=(sender,args)=>sourceChanged=true;
        sourceFiles.Renamed+=(sender,args)=>sourceChanged=true;sourceFiles.Deleted+=(sender,args)=>sourceChanged=true;
        sourceFiles.EnableRaisingEvents=true;
        AssemblyReloadEvents.beforeAssemblyReload+=()=>
        {
            if(EditorApplication.isPlaying && !deliberateExit)SaveReview();
            if(EditorApplication.isPlaying && SessionState.GetString(Key,"")!="")
                foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())root.SetActive(false);
            sourceFiles.Dispose();
        };
        CompilationPipeline.compilationStarted+=_=>
        {
            failed=false;SaveReview();
        };
        CompilationPipeline.compilationFinished+=_=>awaitingCompilation=false;
        CompilationPipeline.assemblyCompilationFinished+=(path,messages)=>
        {foreach(var message in messages)if(message.type==CompilerMessageType.Error)failed=true;};
        EditorApplication.update+=()=>
        {
            // Unity can finish a synchronous import before honoring the stop request.
            // Complete that stop after the reload before opening a clean scene.
            if(!freshPlay && EditorApplication.isPlaying && SessionState.GetString(Key,"")!="")
            {EditorApplication.isPlaying=false;return;}
            if(sourceChanged)
            {
                sourceChanged=false;
                if(WaveWatcher.Current!=null && WaveWatcher.Current.Ready){SaveReview();refreshNeeded=true;}
            }
            if(refreshNeeded && !EditorApplication.isPlayingOrWillChangePlaymode)
            {refreshNeeded=false;AssetDatabase.Refresh();return;}
            if(failed || awaitingCompilation || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetString(Key,"")=="")return;
            EditorApplication.isPlaying=true;
        };
        EditorApplication.playModeStateChanged+=change=>
        {
            if(change==PlayModeStateChange.ExitingPlayMode)deliberateExit=SessionState.GetString(Key,"")=="";
            if(change==PlayModeStateChange.EnteredPlayMode){freshPlay=true;deliberateExit=false;}
            if(change!=PlayModeStateChange.EnteredPlayMode || SessionState.GetString(Key,"")=="")return;
            new GameObject("Restore corrected wave preview").AddComponent<WatcherCorrectionResume>();
        };
    }
    static void SaveReview()
    {
        var watcher=WaveWatcher.Current;if(watcher==null || !watcher.Ready)return;
        var state=new State{level=watcher.Level,view=watcher.View,id=watcher.SelectedEntry.Id,seed=LevelRun.Seed,
            loop=watcher.ReviewLoop,variant=watcher.Variant,mirror=watcher.Mirror,aim=watcher.AimSide,index=watcher.Selection,
            speed=watcher.PlaybackSpeed,paused=watcher.Paused,repeat=watcher.Looping};
        SessionState.SetString(Key,JsonUtility.ToJson(state));awaitingCompilation=true;EditorApplication.isPlaying=false;
    }
    public static IEnumerator Resume()
    {
        var state=JsonUtility.FromJson<State>(SessionState.GetString(Key,""));SessionState.EraseString(Key);
        if(state==null)yield break;
        if(SceneManager.GetActiveScene().name!="Title")yield return SceneManager.LoadSceneAsync("Title");
        yield return null;WaveWatcher.Launch();
        float deadline=Time.realtimeSinceStartup+90;
        while((WaveWatcher.Current==null || !WaveWatcher.Current.Ready) && Time.realtimeSinceStartup<deadline)yield return null;
        var watcher=WaveWatcher.Current;
        if(watcher==null || !watcher.Ready){Debug.LogWarning("Corrected Wave Watcher could not reopen; launch it from the title.");yield break;}
        watcher.SelectLoop(state.loop);watcher.SetSeed(state.seed);watcher.SelectLevel(state.level);watcher.SelectView(state.view);
        watcher.SelectEntry(Mathf.Clamp(state.index,0,watcher.Entries.Count-1));
        for(int i=0;i<watcher.Entries.Count;i++)if(watcher.Entries[i].Id==state.id){watcher.SelectEntry(i);break;}
        watcher.SelectVariant(state.variant);if(watcher.Mirror!=state.mirror)watcher.ToggleMirror();watcher.SetAim(state.aim);
        for(int i=0;i<3 && watcher.PlaybackSpeed!=state.speed;i++)watcher.CycleSpeed();
        if(watcher.Looping!=state.repeat)watcher.ToggleLoop();
        if(watcher.Paused!=state.paused)watcher.TogglePause();
        while(!WavePreparation.Ready)yield return null;
        yield return null;yield return null;Restored?.Invoke(watcher);
    }
}
public sealed class WatcherCorrectionResume : MonoBehaviour
{
    void Awake(){DontDestroyOnLoad(gameObject);}
    IEnumerator Start(){yield return WatcherReload.Resume();Destroy(gameObject);}
}
#endif
