#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>Checks parallax sign, depth, coverage and recycling, and records actual rendered motion.</summary>
public sealed class BackgroundReviewProbe : MonoBehaviour
{
    const string Folder="BackgroundReview";
    readonly List<string> errors=new List<string>();
    readonly List<string> results=new List<string>();
    readonly Queue<string> captures=new Queue<string>();
    int written;
    void Awake(){Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void Log(string message,string trace,LogType type)
    {
        if(trace.Contains("UnityEditor.Search.SearchDatabase"))return;
        if(type==LogType.Error || type==LogType.Exception)if(!errors.Contains(message))errors.Add(message);
    }
    IEnumerator Start()
    {
        StartCoroutine(CaptureFrames());
        var run=Run();
        while(true)
        {
            bool more=false;object current=null;Exception failure=null;
            try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){failure=e;}
            if(failure!=null){Finish("FAIL: "+failure,1);yield break;}
            if(!more)break;
            yield return current;
        }
        Finish("PASS: all five backgrounds follow ship X in the Fields direction; layered foreground response; edge overscan; continuous 80-unit section wrap; no scenery colliders; live geometry and shaders; rendered horizontal sweep for every level. Captured "+written+" frames.\n"+string.Join("\n",results),errors.Count==0 ? 0 : 1);
    }
    IEnumerator Run()
    {
        Directory.CreateDirectory(Folder+"/Frames");
        yield return null;
        FindAnyObjectByType<NewGame>().OnAccept();
        float deadline=Time.realtimeSinceStartup+15;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!CloudTransition.Busy,"New game transition stalled");
        var ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.GetComponent<Collider2D>().enabled=false;
        foreach(var wave in FindObjectsByType<WaveOrchestrer>()){wave.enabled=false;wave.CancelInvoke();wave.StopAllCoroutines();}
        foreach(var item in FindObjectsByType<AItem>())Destroy(item.gameObject);
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        foreach(var weapon in FindObjectsByType<AWeapon>()){weapon.CancelInvoke();weapon.enabled=false;}
        var dialog=FindAnyObjectByType<Dialog>();if(dialog!=null)dialog.Hide();
        GameRules.playerDisabled=true;
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            CloudTransition.Change(()=>{LevelRun.Advance(level);LevelEnvironment.Apply();});
            deadline=Time.realtimeSinceStartup+15;
            while(CloudTransition.Busy && Time.realtimeSinceStartup<deadline)yield return null;
            Check(!CloudTransition.Busy,"Transition stalled: "+level);
            yield return new WaitForSeconds(3f);
            var field=FindAnyObjectByType<PSXGrassField>(FindObjectsInactive.Include);
            float parallax=field.sideParallax;
            var environment=FindAnyObjectByType<LevelEnvironment>();
            foreach(float x in new[]{-Camera.RightLimit,0f,Camera.RightLimit})
            {
                ship.transform.position=new Vector3(x,-4,20);yield return null;yield return null;
                float expected=-x*parallax;
                var sections=level==LevelId.Fields ? field.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Valley section ")).ToArray() : Get<List<Transform>>(environment,"sections").ToArray();
                if(level!=LevelId.Thunder)
                {
                    Check(sections.Length==4,"Missing composed sections: "+level);
                    foreach(var section in sections)
                    {
                        Check(Mathf.Abs(section.localPosition.x-expected)<.001f,"Wrong lateral movement: "+level+" at "+x);
                        var floor=section.GetComponentInChildren<MeshRenderer>();
                        Check(floor.bounds.min.x<Camera.LeftLimit && floor.bounds.max.x>Camera.RightLimit,"Visible horizontal scenery edge: "+level);
                    }
                }
                if(environment!=null)
                {
                    Check(Mathf.Abs(environment.HorizontalOffset-expected)<.001f,"Parallax differs from Fields: "+level);
                    var front=Get<List<Transform>>(environment,"foreground");
                    foreach(var part in front)Check(Mathf.Abs(part.localPosition.x-expected*1.65f)<.001f,"Foreground lacks depth response: "+level);
                    var haze=environment.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.shader.name=="Shootemup/Level Atmosphere").ToArray();
                    Check(haze.Length>0,"No atmosphere layers: "+level);
                    foreach(var layer in haze)Check(layer.bounds.min.x<Camera.LeftLimit && layer.bounds.max.x>Camera.RightLimit,"Atmosphere exposes an edge: "+level);
                    Check(environment.GetComponentsInChildren<Collider>().Length==0 && environment.GetComponentsInChildren<Collider2D>().Length==0,"Scenery changes gameplay collision");
                    if(level==LevelId.Thunder)Check(haze.Select(r=>r.transform.localPosition.x).Distinct().Count()>1 || x==0,"Storm layers have no separate parallax");
                }
                captures.Enqueue(level+"-"+(x<0 ? "left" : x>0 ? "right" : "centre"));
                yield return new WaitForSecondsRealtime(.15f);
            }
            if(environment!=null && level!=LevelId.Thunder)
            {
                var sections=Get<List<Transform>>(environment,"sections");
                var distance=typeof(LevelEnvironment).GetField("travel",BindingFlags.Instance|BindingFlags.NonPublic);
                distance.SetValue(environment,79.95f);yield return new WaitForSeconds(.08f);
                Check((float)distance.GetValue(environment)<2,"District did not wrap: "+level);
                var ys=sections.Select(t=>t.localPosition.y).OrderBy(y=>y).ToArray();
                for(int i=1;i<ys.Length;i++)Check(Mathf.Abs(ys[i]-ys[i-1]-20)<.001f,"Section gap at wrap: "+level);
            }
            for(int frame=0;frame<120;frame++)
            {
                float phase=frame/119f*Mathf.PI*2;
                ship.transform.position=new Vector3(-4.3f*Mathf.Sin(phase),-4,20);
                captures.Enqueue("Frames/"+level+"-"+frame.ToString("000"));
                yield return new WaitForSecondsRealtime(.05f);
            }
            results.Add(level+": left/centre/right parallax, full-width coverage and 120 motion frames checked.");
        }
        while(captures.Count>0)yield return null;
        Check(errors.Count==0,"Runtime render errors: "+string.Join("; ",errors));
        Check(written>=615,"Render capture did not produce the complete motion sequence");
    }
    static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    IEnumerator CaptureFrames()
    {
        while(true)
        {
            yield return new WaitForEndOfFrame();
            if(captures.Count==0)continue;
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            if(image!=null){File.WriteAllBytes(Folder+"/"+captures.Dequeue()+".png",image.EncodeToPNG());Destroy(image);written++;}
        }
    }
    void Finish(string result,int code){File.WriteAllText(Folder+"/Verification.txt",result+"\n"+string.Join("\n",errors));UnityEditor.EditorApplication.Exit(code);}
    void OnDestroy(){Application.logMessageReceived-=Log;}
}
#endif
