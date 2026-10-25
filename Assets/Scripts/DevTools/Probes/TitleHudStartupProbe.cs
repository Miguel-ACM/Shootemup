#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Records the untouched Title -> NewGame.OnAccept -> cloud reveal -> L1 startup path.
// The editor watcher may stop Play after CombatResult appears; this never exits Unity.
public sealed class TitleHudStartupProbe : MonoBehaviour
{
    const string Folder="Temp/TitleHudStartupReview";
    static readonly FieldInfo Side=typeof(IndustrialHUD).GetField("sideGroup",BindingFlags.Instance|BindingFlags.NonPublic);
    readonly List<string> errors=new List<string>(),checks=new List<string>();
    readonly List<string> samples=new List<string>{"frame,seconds,scene,cloud_busy,boot_age,health_alpha,left_weapon_alpha,right_weapon_alpha"};
    int frames,dropouts;
    float firstHudAge=-1,firstHealth=-1,firstWeapon=-1,peak,previous,nextFrame;
    bool partial,partialThroughClouds,dipping;
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType kind)
    {if((kind==LogType.Error || kind==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            object value=null;bool more=false;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception ex){failure=ex;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}
            if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("ArtSource/CombatReview");
        File.WriteAllLines(Folder+"/timing.csv",samples);
        string report=(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors);
        File.WriteAllText(Folder+"/result.txt",report);
        File.WriteAllText("ArtSource/CombatReview/TitleHudStartupVerification.txt",report);
        File.WriteAllText("Temp/CombatResult.txt",report);
    }
    void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);}
    IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);
        foreach(string frame in Directory.GetFiles(Folder,"????.jpg"))File.Delete(frame);
        float deadline=Time.realtimeSinceStartup+20;
        while(CloudTransition.Busy && Time.realtimeSinceStartup<deadline)yield return null;
        Check(!CloudTransition.Busy,"previous transition has finished");
        if(SceneManager.GetActiveScene().name!="Title")
        {
            CloudTransition.Load("Title");
            while(CloudTransition.Busy)yield return null;
        }
        yield return null;yield return null;
        var newGame=FindAnyObjectByType<NewGame>();
        Check(newGame!=null && SceneManager.GetActiveScene().name=="Title","real Title menu is ready");
        float started=Time.realtimeSinceStartup;
        yield return new WaitForEndOfFrame();Capture(started);newGame.OnAccept();
        Check(CloudTransition.Busy,"NewGame.OnAccept starts the real cloud transition");
        IndustrialHUD hud=null;LoadoutHUD equipment=null;float settledAt=-1,clearAt=-1,firstEnemyAt=-1;
        while(Time.realtimeSinceStartup-started<20)
        {
            yield return new WaitForEndOfFrame();
            hud=FindAnyObjectByType<IndustrialHUD>();equipment=FindAnyObjectByType<LoadoutHUD>();
            if(SceneManager.GetActiveScene().name=="L1")
            {
                bool enemies=AEnemy.Live.Count>0,bullets=FindObjectsByType<AEnemyBullet>().Length>0;
                if(CloudTransition.Busy && (enemies || bullets))throw new Exception("opening enemies or hostile fire appeared behind the cloud curtain");
                if(!CloudTransition.Busy && clearAt<0)clearAt=Time.time;
                if(enemies && firstEnemyAt<0)firstEnemyAt=Time.time;
            }
            if(hud!=null)
            {
                var group=(CanvasGroup)Side.GetValue(hud);
                if(group!=null)
                {
                    float alpha=group.alpha;
                    if(firstHealth<0){firstHealth=alpha;firstHudAge=hud.EntranceAge;}
                    peak=Mathf.Max(peak,alpha);
                    if(!dipping && peak>.25f && alpha<peak*.65f){dropouts++;dipping=true;}
                    if(dipping && alpha>previous+.015f){dipping=false;peak=alpha;}
                    previous=alpha;
                    partial|=alpha>.1f && alpha<.9f;
                    partialThroughClouds|=CloudTransition.Busy && alpha>.1f && alpha<.9f;
                }
                if(equipment!=null && equipment.Panels[0]!=null && firstWeapon<0)
                    firstWeapon=equipment.Panels[0].GetComponent<CanvasGroup>().alpha;
            }
            if(Time.realtimeSinceStartup-started>=nextFrame)Capture(started);
            if(hud!=null && hud.EntranceComplete && !CloudTransition.Busy)
            {
                if(settledAt<0)settledAt=Time.realtimeSinceStartup;
                if(Time.realtimeSinceStartup-settledAt>1.2f && firstEnemyAt>=0)break;
            }
        }
        Check(SceneManager.GetActiveScene().name=="L1" && !CloudTransition.Busy,"Title activates L1 and the clouds clear");
        Check(clearAt>=0 && firstEnemyAt-clearAt>=WaveOrchestrer.OpeningBreather-.05f && firstEnemyAt-clearAt<WaveOrchestrer.OpeningBreather+.2f,
            "opening enemies wait for the clouds plus a short breather ("+(firstEnemyAt-clearAt).ToString("F2",CultureInfo.InvariantCulture)+"s)");
        Check(firstHudAge>=0 && firstHudAge<.20f && firstHealth<=.15f,"HUD begins dark in its first real activation frame");
        Check(firstWeapon>=0 && firstWeapon<=.15f,"equipment portraits begin dark when their real panels are created");
        Check(partial && partialThroughClouds && dropouts>=2,"instruments visibly warm and flicker through the cloud reveal ("+dropouts+" dropouts)");
        var circuits=FindObjectsByType<FluorescentStartup>(FindObjectsInactive.Include).Where(c=>c.gameObject.activeInHierarchy).ToArray();
        Check(hud!=null && hud.EntranceComplete && circuits.Length>=6 && circuits.All(c=>Mathf.Abs(c.GetComponent<CanvasGroup>().alpha-1)<.001f),"all active instruments settle at full visibility");
        Check(equipment!=null && equipment.Panels.All(p=>p!=null && p.GetComponentInChildren<RawImage>()?.texture!=null),"all four equipment panels retain their rendered portrait texture");
        var cannon=GameObject.Find("BaseWeapon portrait");
        Check(cannon!=null && cannon.GetComponent<MeshFilter>()?.sharedMesh!=null,"actual starter cannon has its 3D portrait after Title startup");
        Check(frames>=90,"captured "+frames+" full-screen frames of the natural title, clouds, startup and steady HUD");
    }
    void Capture(float started)
    {
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+"/"+frames.ToString("0000")+".jpg",image.EncodeToJPG(86));Destroy(image);
        var hud=FindAnyObjectByType<IndustrialHUD>();var equipment=FindAnyObjectByType<LoadoutHUD>();
        float elapsed=Time.realtimeSinceStartup-started;
        float alpha=hud!=null && Side.GetValue(hud) is CanvasGroup group ? group.alpha : -1;
        float left=equipment!=null && equipment.Panels[0]!=null ? equipment.Panels[0].GetComponent<CanvasGroup>().alpha : -1;
        float right=equipment!=null && equipment.Panels[1]!=null ? equipment.Panels[1].GetComponent<CanvasGroup>().alpha : -1;
        samples.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1:F4},{2},{3},{4:F4},{5:F4},{6:F4},{7:F4}",frames,elapsed,
            SceneManager.GetActiveScene().name,CloudTransition.Busy ? 1 : 0,hud!=null ? hud.EntranceAge : -1,alpha,left,right));
        frames++;nextFrame=elapsed+1f/24;
    }
}
#endif
