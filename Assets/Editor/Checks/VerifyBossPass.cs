#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// No windows, camera rendering, input injection or changes to the player's project settings.
[InitializeOnLoad]
public static class VerifyBossPass
{
    public const string Folder="C:/Users/Miguel/Shootemup/ArtSource/BossPass";
    static double deadline;
    static VerifyBossPass()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("BossPassReview",false))return;
            deadline=EditorApplication.timeSinceStartup+1200;
            new GameObject("Headless boss review").AddComponent<BossPassRuntime>();
        };
        EditorApplication.update+=()=>{if(deadline>0&&EditorApplication.timeSinceStartup>deadline){File.WriteAllText(Folder+"/Runtime.txt","FAIL timeout");EditorApplication.Exit(1);}};
    }
    static void Guard()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').Contains("/Temp/DecisionHoverReview/"))throw new Exception("Use headless isolated review only");
    }
    public static void Run()
    {
        Guard();Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Patterns");
        PlayerSettings.companyName="ShootemupReview";PlayerSettings.productName="BossPassReview";
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<BossPassNoRenderAsset>();
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("BossPassReview",true);EditorApplication.isPlaying=true;
    }
    public static void RunFixes(){SessionState.SetBool("BossPassFixes",true);Run();}
    public static void RunVoid(){SessionState.SetBool("BossPassVoidOnly",true);Run();}
    public static void RunPool(){SessionState.SetBool("BossPassFinalOnly",true);RunVoid();}
    public static void Build()
    {
        Guard();if(!File.ReadAllText(Folder+"/Runtime.txt").StartsWith("PASS"))throw new Exception("Runtime review must pass");
        ExportFacets();
        PlayerSettings.companyName="DefaultCompany";PlayerSettings.productName="seup";
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName="C:/Users/Miguel/Shootemup/Build/BossPass/Shootemup.exe"});
        bool ok=result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder+"/Build.txt",(ok?"PASS":"FAIL")+"\nErrors="+result.summary.totalErrors+" warnings="+result.summary.totalWarnings);
        EditorApplication.Exit(ok?0:1);
    }
    static void ExportFacets()
    {
        for(int form=-1;form<4;form++)
        {
            var go=new GameObject("Final faceted anatomy");
            go.AddComponent<MeshFilter>().sharedMesh=form<0?IndustrialMeshFactory.SeraphJudicatorBossMesh():IndustrialMeshFactory.VoidForm(form);
            go.AddComponent<MeshRenderer>().sharedMaterial=form<0?HeavenAngelVisual.Surface:VoidSovereignBoss.Body;
            Export(form<0?"Heaven-Final":"Void-Final-"+(form+1),go.transform);UnityEngine.Object.DestroyImmediate(go);
        }
        foreach(var type in new[]{typeof(ChoirLancer),typeof(AegisReliquary),typeof(PrismCantor),typeof(VotiveSpear),typeof(ThroneBarge),typeof(DawnGatekeeper)})
        {
            var go=new GameObject(type.Name);go.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CombatHull((int)EnemyCatalog.Get(type).Hull);
            go.AddComponent<MeshRenderer>().sharedMaterial=HeavenMaterials.Ivory;Export(type.Name+"-Final",go.transform);UnityEngine.Object.DestroyImmediate(go);
        }
        var ruin=new GameObject("Heaven bastion");ruin.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.HeavenBastion(3);
        ruin.AddComponent<MeshRenderer>().sharedMaterial=HeavenMaterials.Ivory;Export("Heaven-Bastion-Final",ruin.transform);UnityEngine.Object.DestroyImmediate(ruin);
    }
    public static void Export(string name,Transform root)
    {
        var text=new StringBuilder();int offset=1;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
            if(mesh==null||renderer==null||!renderer.enabled||renderer.sharedMaterial==null||renderer.sharedMaterial.shader.name.Contains("Rift"))continue;
            var vertices=mesh.vertices;var uv=mesh.uv;var indices=mesh.triangles;
            text.AppendLine("o "+filter.name.Replace(' ','_'));
            foreach(var v in vertices)
            {
                Vector3 p=root.InverseTransformPoint(filter.transform.TransformPoint(v));
                if(!float.IsFinite(p.sqrMagnitude))throw new Exception("Invalid mesh "+name);
                text.AppendLine(FormattableString.Invariant($"v {p.x} {p.y} {-p.z}"));
            }
            string family=renderer.sharedMaterial.shader.name.Contains("Void Body")?"void":renderer.sharedMaterial.shader.name.Contains("Heaven")?"heaven":"hull";
            for(int i=0;i<indices.Length;i+=3)
            {
                int tile=uv.Length==vertices.Length?Mathf.Clamp(Mathf.FloorToInt(uv[indices[i]].x*4)+4*Mathf.FloorToInt(uv[indices[i]].y*4),0,15):6;
                text.AppendLine("usemtl "+family+"_"+tile);
                text.AppendLine($"f {offset+indices[i+2]} {offset+indices[i+1]} {offset+indices[i]}");
            }
            offset+=vertices.Length;
        }
        File.WriteAllText(Folder+"/Meshes/"+name+".obj",text.ToString());
    }
}

public sealed class BossPassNoRenderAsset : UnityEngine.Rendering.RenderPipelineAsset
{
    protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()=>new NoRender();
    sealed class NoRender : UnityEngine.Rendering.RenderPipeline
    {
        protected override void Render(UnityEngine.Rendering.ScriptableRenderContext context,List<UnityEngine.Camera> cameras) { }
    }
}

public sealed class BossPassRuntime : MonoBehaviour
{
    readonly List<string> notes=new List<string>(),errors=new List<string>();
    WaveWatcher watcher;
    void Check(bool okay,string message){notes.Add((okay?"PASS ":"FAIL ")+message);if(!okay)errors.Add(message);File.WriteAllLines(VerifyBossPass.Folder+"/Progress.txt",notes);}
    void Log(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception)errors.Add(message+"\n"+stack);}
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready)yield return null;yield return null;}
    IEnumerator Seconds(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;
        QualitySettings.renderPipeline=ScriptableObject.CreateInstance<BossPassNoRenderAsset>();
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();
        while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        Time.captureDeltaTime=1f/30;
        watcher.SelectDifficulty(RunDifficulty.Hard);watcher.SelectView(WatcherView.Bosses);yield return Ready();
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            if(level!=LevelId.Void&&SessionState.GetBool("BossPassVoidOnly",false))continue;
            if(level==LevelId.Heaven&&SessionState.GetBool("BossPassFixes",false))
            {Check(File.ReadAllText(VerifyBossPass.Folder+"/Progress-Initial.txt").Contains("PASS Heaven-3 retains one whole-body damage path"),"Heaven three-phase review retained from initial batch");continue;}
            watcher.SelectLevel(level);yield return Ready();
            var entries=WatcherCatalog.For(WatcherView.Bosses,level);
            foreach(var type in DistrictWaves.Bosses(level))
            {
                int wanted=SessionState.GetBool("BossPassFinalOnly",false)?WaveWatcher.BossFour:WaveWatcher.BossOne;
                int entry=entries.FindIndex(e=>(e.Enemy==type||(e.Enemy==null&&type==DistrictWaves.Bosses(level)[0]))&&e.Encounter==wanted);
                Check(entry>=0,type.Name+" catalog entry");if(entry<0)continue;
                watcher.SelectEntry(entry);yield return Ready();
                DistrictBoss boss=null;
                while(boss==null){boss=FindObjectsByType<DistrictBoss>().FirstOrDefault(b=>b.GetType()==type);yield return null;}
                while(boss.Invulnerable||(wanted==WaveWatcher.BossFour&&boss.CurrentPhase<4))yield return null;
                yield return Seconds(2);
                if(boss is SeraphJudicatorBoss||boss is VoidSovereignBoss)
                {yield return Celestial(boss);continue;}
                var moving=boss.GetComponentsInChildren<Transform>();var poses=moving.Select(t=>t.localRotation).ToArray();
                VerifyBossPass.Export(type.Name+"-0",boss.transform);
                yield return Seconds(2.7f);
                if(!(boss is SpaceCitadelBoss))Check(moving.Where((t,i)=>t!=null&&Quaternion.Angle(t.localRotation,poses[i])>1).Any(),type.Name+" visible part animation");
                VerifyBossPass.Export(type.Name+"-1",boss.transform);
                Check(boss is SpaceCitadelBoss||boss.GetComponent<BossMechanismRig>()!=null,type.Name+" authored mechanism rig");
            }
        }
        if(SessionState.GetBool("BossPassVoidOnly",false))
        {
            string earlier=File.ReadAllText(VerifyBossPass.Folder+"/Progress-Mechanical.txt");
            Check(earlier.Contains("PASS HellCrabBoss authored mechanism rig")&&!earlier.Contains("FAIL"),"Completed mechanical and Heaven checks retained");
        }
        if(SessionState.GetBool("BossPassFinalOnly",false))
        {
            string earlier=File.ReadAllText(VerifyBossPass.Folder+"/Runtime-VoidInitial.txt");
            Check(earlier.Contains("PASS Void-3 15 active second survival interlude")&&earlier.Contains("PASS Void-4 teleports"),"Completed Void form, teleport and interlude checks retained");
        }
        Check(errors.Count==0,"All completed runtime assertions passed");
        File.WriteAllText(VerifyBossPass.Folder+"/Runtime.txt",(errors.Count==0?"PASS":"FAIL")+"\nHeadless simulation; meshes exported for offline rendered review.\n"+string.Join("\n",notes)+"\n"+string.Join("\n",errors));
        SessionState.SetBool("BossPassReview",false);SessionState.SetBool("BossPassFixes",false);SessionState.SetBool("BossPassVoidOnly",false);SessionState.SetBool("BossPassFinalOnly",false);Application.logMessageReceived-=Log;EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Celestial(DistrictBoss boss)
    {
        var voidBoss=boss as VoidSovereignBoss;var angel=boss as SeraphJudicatorBoss;
        for(int phase=boss.CurrentPhase;phase<=boss.PhaseCount;phase++)
        {
            while(boss.Invulnerable||(voidBoss!=null&&(voidBoss.Hidden||voidBoss.Teleporting)))yield return null;
            string label=(voidBoss!=null?"Void":"Heaven")+"-"+phase;
            float end=Time.time+32,next=0;int frame=0,peak=0,created=CombatRound.CreatedCount;
            bool card0=false,card1=false,teleport=false,moved=false;Vector3 start=boss.transform.position;
            var snapshots=new StringBuilder("frame,time,bossX,bossY,card,x,y,width,height,angle,type\n");
            VerifyBossPass.Export(label+"-0",boss.Hull);
            while(Time.time<end)
            {
                yield return null;int card=angel!=null?angel.CardIndex:voidBoss.CardIndex;
                card0|=card==0;card1|=card==1;peak=Mathf.Max(peak,CombatRound.Live.Count);
                if(voidBoss!=null&&voidBoss.Teleporting)
                {
                    teleport=true;moved|=Vector3.Distance(start,boss.transform.position)>2;
                    if(voidBoss.CanBeTargeted||voidBoss.CanReceiveDamage||voidBoss.GetComponent<Collider2D>().enabled)errors.Add(label+" vulnerable during teleport");
                }
                if(Time.time<next)continue;next=Time.time+.5f;frame++;
                foreach(var round in CombatRound.Live)
                {
                    var p=round.transform.position;var sprite=round.GetComponent<SpriteRenderer>();var size=sprite!=null?sprite.bounds.size:Vector3.one*.2f;
                    snapshots.AppendLine(FormattableString.Invariant($"{frame},{Time.time},{boss.transform.position.x},{boss.transform.position.y},{card},{p.x},{p.y},{size.x},{size.y},{round.transform.eulerAngles.z},{round.GetType().Name}"));
                }
                if(frame==7)VerifyBossPass.Export(label+"-1",boss.Hull);
            }
            File.WriteAllText(VerifyBossPass.Folder+"/Patterns/"+label+".csv",snapshots.ToString());
            Check(card0&&card1,label+" both authored cards play");
            Check(peak>25&&peak<CombatRound.Limit,label+" live round peak "+peak);
            Check(CombatRound.CreatedCount==created,label+" uses prepared bullet pool (created "+(CombatRound.CreatedCount-created)+")");
            if(voidBoss!=null)Check(teleport&&moved,label+" teleports with damage, targeting and contact disabled");
            if(angel!=null)Check(angel.GetComponent<PolygonCollider2D>().pathCount==1,label+" retains one whole-body damage path");
            if(phase==boss.PhaseCount)continue;
            while(!boss.CanReceiveDamage)yield return null;
            boss.damage(boss.health*.51f);yield return null;
            while(boss.CurrentPhase==phase)yield return null;
            Check(FindAnyObjectByType<PhaseBreak>()!=null,label+" white spell break");
            if(voidBoss!=null)
            {
                while(voidBoss.Hidden)yield return null;
                Check(Mathf.Abs(voidBoss.InterludeElapsed-15)<.05f,label+" 15 active second survival interlude");
            }
        }
    }
}
#endif
