using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Run Unity -batchmode -executeMethod VerifyLevelFlow.Run in a verification copy.</summary>
[InitializeOnLoad]
public static class VerifyLevelFlow
{
    const string Key="VerifyLevelFlowRequested";
    static double deadline;
    static VerifyLevelFlow()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);
            var go=new GameObject("Level flow verification");UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<LevelFlowProbe>();
        };
        EditorApplication.update+=()=>
        {
            if(deadline>0 && EditorApplication.timeSinceStartup>deadline)
            { File.WriteAllText("Temp/LevelFlowResult.txt","FAIL: verification timed out");EditorApplication.Exit(1); }
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("FlowReview");
        if(File.Exists("FlowReview/LevelFlowResult.txt"))File.Delete("FlowReview/LevelFlowResult.txt");
        try
        {
            var charge=new RouteCharge();
            for(int i=0;i<500;i++)charge.Step(0,.02f);
            Check(charge.Side==0 && charge.Progress==0,"Centre selected a route");
            charge.Step(-.5f,1);Check(charge.Side==-1 && charge.Progress>0,"Left did not charge");
            charge.Step(.5f,.01f);Check(charge.Side==1 && charge.Progress<.01f,"Switching sides retained charge");
            charge.Step(0,1);Check(charge.Progress==0,"Centre did not cancel the hold");
            foreach(float fps in new[]{30f,60f,144f})
            {
                charge=new RouteCharge();float elapsed=0;
                while(!charge.Complete){charge.Step(1,1/fps);elapsed+=1/fps;}
                Check(elapsed>=2.59f && elapsed<2.7f,"Hold duration varies with frame rate");
            }
            var seen=new System.Collections.Generic.HashSet<LevelId>();
            for(int i=0;i<1000;i++)
            {
                LevelRun.StartRandom();seen.Add(LevelRun.Current);
                LevelRun.GetChoices(out var left,out var right);
                Check(left!=right && left!=LevelRun.Current && right!=LevelRun.Current,"Duplicate or current route offered");
            }
            Check(seen.Count==5,"Random starts did not cover all five levels");
            Check(EditorBuildSettings.scenes[0].path=="Assets/Scenes/Title.unity","Build does not start at the menu");
            Check(Resources.Load<TextAsset>("LevelKit")!=null && Resources.Load<Shader>("LevelScenery")!=null,"Missing environment resources");
            Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("LevelScenery")),"Environment shader failed compilation");
            if(!Application.isBatchMode)
            {
                var game=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                game.position=new Rect(0,0,1280,800);game.Show();game.Focus();
            }
            SessionState.SetBool(Key,true);deadline=EditorApplication.timeSinceStartup+200;
            EditorApplication.isPlaying=true;
        }
        catch(Exception e){File.WriteAllText("Temp/LevelFlowResult.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
}
