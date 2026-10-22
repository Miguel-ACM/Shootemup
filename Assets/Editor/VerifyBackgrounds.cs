using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Runs the render/motion review in a disposable project copy.</summary>
[InitializeOnLoad]
public static class VerifyBackgrounds
{
    const string Key="VerifyScrollingBackgrounds";
    static VerifyBackgrounds()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);
            var go=new GameObject("Background motion review");UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<BackgroundReviewProbe>().OnlyLevel=SessionState.GetString("BackgroundReviewOnlyLevel","");
        };
    }
    public static void Run()=>Begin("");
    public static void RunSnow()=>Begin("Snow");
    public static void RunCity()=>Begin("City");
    static void Begin(string onlyLevel)
    {
        SessionState.SetString("BackgroundReviewOnlyLevel",onlyLevel);
        Directory.CreateDirectory("BackgroundReview");
        if(File.Exists("BackgroundReview/Verification.txt"))File.Delete("BackgroundReview/Verification.txt");
        var atmosphere=Resources.Load<Shader>("LevelAtmosphere");
        if(atmosphere==null || ShaderUtil.ShaderHasError(atmosphere))throw new Exception("Atmosphere shader failed compilation");
        if(ShaderUtil.ShaderHasError(Resources.Load<Shader>("LevelScenery")))throw new Exception("Scenery shader failed compilation");
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        view.position=new Rect(0,0,1280,800);view.Show();view.Focus();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
}
