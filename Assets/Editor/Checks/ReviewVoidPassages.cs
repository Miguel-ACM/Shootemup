#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class ReviewVoidPassages
{
    public const string Folder = "C:/Users/Miguel/Shootemup/ArtSource/VoidPassagesReview";
    static ReviewVoidPassages()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("VoidPassages", false)) return;
            MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/VoidPassages/Save");
            new GameObject("Void passage review").AddComponent<VoidPassageReviewRuntime>();
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetFullPath("ReviewOutput/VoidPassages/Save"));
    }
    public static void Run()
    {
        Guard(); PlayerSettings.companyName="ShootemupReview"; PlayerSettings.productName="VoidPassages";
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("VoidPassages",true); EditorApplication.isPlaying=true;
    }
    public static void Build()
    {
        Guard(); PlayerSettings.companyName="DefaultCompany"; PlayerSettings.productName="seup";
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
            target=BuildTarget.StandaloneWindows64,locationPathName="Build/VoidBackdrop/Shootemup.exe",options=BuildOptions.None
        });
        File.WriteAllText(Folder+"/Build.txt",result.summary.result+"\nErrors="+result.summary.totalErrors+"; warnings="+result.summary.totalWarnings);
        EditorApplication.Exit(result.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}

public sealed class VoidPassageReviewRuntime : MonoBehaviour
{
    readonly List<string> report=new(),errors=new();
    RenderPipelineAsset visible,invisible;
    WaveWatcher watcher;
    int movieFrame;
    void Check(bool okay,string text)
    {
        report.Add((okay?"PASS ":"FAIL ")+text);if(!okay)errors.Add(text);
        File.WriteAllLines(ReviewVoidPassages.Folder+"/Progress.txt",report);
    }
    IEnumerator Ready(){yield return null;while(CloudTransition.Busy||!WavePreparation.Ready||!watcher.Ready)yield return null;}
    void Capture(string path)
    {
        QualitySettings.renderPipeline=visible;
        var camera=UnityEngine.Camera.main;var presentation=camera.GetComponent<PSXPresentation>();camera.Render();
        var target=RenderTexture.GetTemporary(1280,720,24);Graphics.Blit(presentation.SceneTexture,target,presentation.CompositeMaterial);
        var previous=RenderTexture.active;RenderTexture.active=target;
        var pixels=new Texture2D(405,720,TextureFormat.RGB24,false);
        pixels.ReadPixels(new Rect((1280-405)/2,0,405,720),0,0);pixels.Apply();
        File.WriteAllBytes(path+".png",pixels.EncodeToPNG());Destroy(pixels);
        RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);QualitySettings.renderPipeline=invisible;
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);AudioListener.pause=true;Time.captureDeltaTime=1f/60;
        Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(message+"\n"+stack);};
        visible=QualitySettings.renderPipeline;invisible=ScriptableObject.CreateInstance<CelestialNoRenderAsset>();QualitySettings.renderPipeline=invisible;
        var work=new Stack<IEnumerator>();work.Push(Run());
        while(work.Count>0)
        {
            bool more=false;object next=null;
            try{more=work.Peek().MoveNext();if(more)next=work.Peek().Current;}
            catch(Exception ex){errors.Add(ex.ToString());break;}
            if(!more){work.Pop();continue;}
            if(next is IEnumerator nested)work.Push(nested);else yield return next;
        }
        report.AddRange(errors);File.WriteAllLines(ReviewVoidPassages.Folder+"/Checks.txt",report);
        SessionState.SetBool("VoidPassages",false);EditorApplication.Exit(errors.Count==0?0:1);
    }
    IEnumerator Run()
    {
        while(CloudTransition.Busy||Time.frameCount<5)yield return null;
        WaveWatcher.Launch();while(WaveWatcher.Current==null||!WaveWatcher.Current.Ready||CloudTransition.Busy||!WavePreparation.Ready)yield return null;
        watcher=WaveWatcher.Current;if(watcher.Looping)watcher.ToggleLoop();
        watcher.SelectView(WatcherView.Bosses);yield return Ready();
        watcher.SelectLevel(LevelId.Void);yield return Ready();
        watcher.SelectEncounter(WaveWatcher.BossOne);yield return Ready();
        string motion=Path.GetFullPath("ReviewOutput/VoidPassages/Motion");Directory.CreateDirectory(motion);
        foreach(var difficulty in new[]{RunDifficulty.Hard,RunDifficulty.Normal,RunDifficulty.Easy})
        {
            watcher.SelectDifficulty(difficulty);watcher.Replay();yield return Ready();
            VoidSovereignBoss boss=null;while(boss==null){boss=FindAnyObjectByType<VoidSovereignBoss>();yield return null;}
            for(int phase=1;phase<=boss.PhaseCount;phase++)
            {
                while(boss.Invulnerable||boss.Hidden)yield return null;
                var space=new VoidVisualRoutes();
                int created=CombatRound.CreatedCount,peak=0,giants=0,previousCard=-1;float cardAge=0;
                bool second=false,complete=false;var captures=new bool[2];var crownPeaks=new int[2];var crowded=new float[2];
                for(int frame=0;frame<3600;frame++)
                {
                    yield return null;int card=boss.CardIndex;
                    if(card==1)second=true;if(second&&card==0){complete=true;break;}
                    if(card!=previousCard){cardAge=0;previousCard=card;}if(!boss.Teleporting)cardAge+=Time.deltaTime;
                    int crowns=CombatRound.Live.Count(s=>s is GiantOrb);peak=Mathf.Max(peak,CombatRound.Live.Count);giants=Mathf.Max(giants,crowns);crownPeaks[card]=Mathf.Max(crownPeaks[card],crowns);
                    if(frame%6==0)
                    {
                        space.Sample(boss);
                        if(difficulty==RunDifficulty.Hard&&cardAge>2&&space.Coverage>crowded[card]+.04f)
                        {crowded[card]=space.Coverage;Capture(ReviewVoidPassages.Folder+"/Crowded-form-"+boss.Form+"-card-"+card);}
                    }
                    if(difficulty==RunDifficulty.Hard&&!boss.Teleporting&&cardAge>5&&!captures[card])
                    {Capture(ReviewVoidPassages.Folder+"/Form-"+boss.Form+"-Card-"+card);captures[card]=true;}
                    if(difficulty==RunDifficulty.Hard&&boss.Form==4&&card==1&&cardAge>1&&cardAge<7&&frame%4==0)
                        Capture(motion+"/"+(movieFrame++).ToString("D3"));
                }
                Check(complete&&crownPeaks.All(n=>n>=2)&&giants<=32&&CombatRound.CreatedCount==created,
                    $"{difficulty} form {boss.Form}: both cards, peak={peak}, giants={giants}, no projectile construction.");
                Check(space.SurvivingStarts==7,$"{difficulty} form {boss.Form}: full-sprite routes from left/centre/right starts={space.SurvivingStarts}/7; worst visual coverage={space.WorstCoverage:P1}.");
                if(phase==boss.PhaseCount)continue;
                while(boss.Invulnerable||boss.Teleporting)yield return null;boss.damage(1000000);while(!boss.Hidden)yield return null;
                space=new VoidVisualRoutes();int interludePeak=0;bool captured=false;int tick=0;float worst=0;
                while(boss.Hidden)
                {
                    yield return null;interludePeak=Mathf.Max(interludePeak,CombatRound.Live.Count(s=>s is GiantOrb));
                    if(tick++%6==0)
                    {
                        space.Sample(boss);
                        if(difficulty==RunDifficulty.Hard&&space.Coverage>worst+.04f)
                        {worst=space.Coverage;Capture(ReviewVoidPassages.Folder+"/Crowded-interlude-"+phase);}
                    }
                    if(difficulty==RunDifficulty.Hard&&boss.InterludeElapsed>8&&!captured)
                    {Capture(ReviewVoidPassages.Folder+"/Interlude-"+phase);captured=true;}
                }
                Check(interludePeak>=2&&interludePeak<=12&&CombatRound.CreatedCount==created,$"{difficulty} interlude {phase}: giant peak={interludePeak}; warmed pool only.");
                Check(space.SurvivingStarts==7,$"{difficulty} interlude {phase}: full-sprite routes={space.SurvivingStarts}/7; worst visual coverage={space.WorstCoverage:P1}.");
            }
        }
    }
}

// Conservative review model, not a gameplay rule: full visible sprite bounds
// plus 0.12 units of clearance, swept between 0.1-second samples. Find continuous
// routes at focus speed (4 units/s) within the lower 68% of the playable arena.
// Three bits track bottom-left, bottom-centre and bottom-right starting points.
// Aiming remains at the production watcher's chosen position; this is a recorded
// field reachability check, not proof of a human no-hit run or a reactive pilot.
public sealed class VoidVisualRoutes
{
    const float Step=.2f,Padding=.12f;
    static readonly FieldInfo age=typeof(CombatRound).GetField("age",BindingFlags.Instance|BindingFlags.NonPublic);
    readonly Dictionary<CombatRound,(Bounds bounds,float age,int sample)> previous=new();
    readonly Rect region;
    readonly int width,height;
    readonly bool[] blocked;
    byte[] reachable,next;
    int sample;
    public int SurvivingStarts{get;private set;}=7;
    public float Coverage{get;private set;}
    public float WorstCoverage{get;private set;}
    public VoidVisualRoutes()
    {
        region=Rect.MinMaxRect(Camera.LeftLimit+.4f,Camera.DownLimit+.4f,Camera.RightLimit-.4f,Mathf.Lerp(Camera.DownLimit,Camera.UpLimit,.68f));
        width=Mathf.FloorToInt(region.width/Step)+1;height=Mathf.FloorToInt(region.height/Step)+1;
        blocked=new bool[width*height];reachable=new byte[blocked.Length];next=new byte[blocked.Length];
        int y=Mathf.RoundToInt(1.4f/Step);
        for(int i=0;i<3;i++)reachable[y*width+Mathf.RoundToInt((width-1)*(.25f+i*.25f))]=(byte)(1<<i);
    }
    Vector2 Point(int x,int y)=>new Vector2(region.xMin+x*Step,region.yMin+y*Step);
    void Fill(Bounds bounds)
    {
        int x0=Mathf.Max(0,Mathf.CeilToInt((bounds.min.x-Padding-region.xMin)/Step));
        int x1=Mathf.Min(width-1,Mathf.FloorToInt((bounds.max.x+Padding-region.xMin)/Step));
        int y0=Mathf.Max(0,Mathf.CeilToInt((bounds.min.y-Padding-region.yMin)/Step));
        int y1=Mathf.Min(height-1,Mathf.FloorToInt((bounds.max.y+Padding-region.yMin)/Step));
        for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)blocked[y*width+x]=true;
    }
    public void Sample(VoidSovereignBoss boss)
    {
        Array.Clear(blocked,0,blocked.Length);sample++;
        foreach(var shot in CombatRound.Live)
        {
            if(shot is PulseLaser beam)
            {
                if(!beam.IsFiring)continue;
                Vector2 start=beam.transform.position,end=start+beam.Heading*beam.length;
                float radius=beam.width*beam.Power*.5f+Padding;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)
                {
                    Vector2 p=Point(x,y),segment=end-start;
                    float t=Mathf.Clamp01(Vector2.Dot(p-start,segment)/segment.sqrMagnitude);
                    if((p-start-segment*t).sqrMagnitude<radius*radius)blocked[y*width+x]=true;
                }
                continue;
            }
            var sprite=shot.GetComponent<SpriteRenderer>();if(!sprite.enabled)continue;
            var bounds=sprite.bounds;float currentAge=(float)age.GetValue(shot);
            if(previous.TryGetValue(shot,out var prior)&&prior.sample==sample-1&&currentAge>=prior.age)
            {bounds.Encapsulate(prior.bounds.min);bounds.Encapsulate(prior.bounds.max);}
            previous[shot]=(sprite.bounds,currentAge,sample);Fill(bounds);
        }
        var body=boss.GetComponent<CircleCollider2D>();if(body.enabled)Fill(body.bounds);
        int occupied=0;foreach(bool hit in blocked)if(hit)occupied++;
        Coverage=occupied/(float)blocked.Length;WorstCoverage=Mathf.Max(WorstCoverage,Coverage);
        Array.Clear(next,0,next.Length);int flags=0;
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int at=y*width+x;if(blocked[at])continue;byte paths=0;
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
            {
                if(dx*dx+dy*dy>4)continue;int fromX=x+dx,fromY=y+dy;
                if(fromX<0||fromY<0||fromX>=width||fromY>=height)continue;
                int source=fromY*width+fromX;if(reachable[source]==0||blocked[source])continue;
                if((Mathf.Abs(dx)==2||Mathf.Abs(dy)==2)&&blocked[(y+dy/2)*width+x+dx/2])continue;
                paths|=reachable[source];
            }
            next[at]=paths;flags|=paths;
        }
        SurvivingStarts&=flags;(reachable,next)=(next,reachable);
    }
}
#endif
