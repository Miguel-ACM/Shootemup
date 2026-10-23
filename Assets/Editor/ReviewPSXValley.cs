using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ReviewPSXValley
{
    const string Request="Temp/review-valley.request",Key="PSXValleyReviewStage";
    static ReviewPSXValley(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        int stage=SessionState.GetInt(Key,0);
        if(stage==0 && File.Exists(Request))
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try { File.Delete(Request); }
            catch(IOException) { return; } // A writer may still be closing the one-shot request.
            if(File.Exists("Temp/ValleyReviewResult.txt"))File.Delete("Temp/ValleyReviewResult.txt");
            SessionState.SetInt(Key,1);EditorApplication.isPlaying=true;
        }
        else if(stage==1 && EditorApplication.isPlaying)
        {
            if(Object.FindAnyObjectByType<ValleyReviewProbe>()==null)new GameObject("Temporary valley review").AddComponent<ValleyReviewProbe>();
            SessionState.SetInt(Key,2);
        }
        else if(stage==2 && File.Exists("Temp/ValleyReviewResult.txt"))
        {SessionState.SetInt(Key,0);EditorApplication.isPlaying=false;}
    }
}
