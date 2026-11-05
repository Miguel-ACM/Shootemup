#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Offscreen stills at authored times, never a live editor/game session.
public static class ReviewVoidBackdrop
{
    const string Folder="C:/Users/Miguel/Shootemup/ArtSource/VoidBackdropReview";
    public static void Build()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        string output=Path.GetFullPath("Build/VoidBackdrop/Shootemup.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None
        });
        File.WriteAllText(Folder+"/Build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
    public static void Run()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        LevelRun.Preview(LevelId.Void,20261105,1);
        var camera=new GameObject("Offscreen Void camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position=new Vector3(0,0,-20);camera.orthographic=true;camera.orthographicSize=8.850766f;
        camera.nearClipPlane=.1f;camera.farClipPlane=200;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=Color.black;camera.cullingMask=1<<31;camera.allowHDR=false;
        var target=new RenderTexture(405,720,24);camera.targetTexture=target;
        GameObject root;
        var before=Type.GetType("VoidBackdropBefore, Assembly-CSharp");
        if(before!=null)
        {
            root=new GameObject("Before / old Void"){layer=31};root.AddComponent(before).SendMessage("Start");
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            Capture(camera,target,root,"Before",6);root.SetActive(false);
        }
        root=new GameObject("After / flowing Void"){layer=31};root.AddComponent<VoidBackdrop>().SendMessage("Start");
        for(int frame=0;frame<4;frame++)Capture(camera,target,root,"After-"+frame,frame*3+1);
        Directory.CreateDirectory(Folder+"/Motion");
        for(int frame=0;frame<72;frame++)Capture(camera,target,root,"Motion/"+frame.ToString("D3"),4+frame/18f);
        foreach(string shader in new[]{"VoidFalls","VoidDrift"})
            if(ShaderUtil.ShaderHasError(Resources.Load<Shader>(shader)))throw new Exception("Shader error: "+shader);
        int renderers=root.GetComponentsInChildren<MeshRenderer>().Length;
        int triangles=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
        File.WriteAllText(Folder+"/Render.txt",$"PASS offscreen 405x720 native-shader renders at 1, 4, 7, 10 seconds.\n{renderers} scenery renderers, {triangles} triangles, {root.GetComponentsInChildren<Collider2D>().Length} colliders.\nNo gameplay session or desktop controls used.");
        EditorApplication.Exit(0);
    }
    static void Capture(UnityEngine.Camera camera,RenderTexture target,GameObject root,string label,float time)
    {
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial.SetFloat("_Clock",time);
        camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
        File.WriteAllBytes(Folder+"/"+label+".png",pixels.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(pixels);RenderTexture.active=previous;
    }
}
#endif
