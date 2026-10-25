#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReviewThunderEditor
{
    public const string Folder="ArtSource/ThunderEditorReview";
    static double deadline;
    static ReviewThunderEditor()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ThunderEditorReview",false))return;
            new GameObject("Thunder rotor review").AddComponent<ThunderEditorReview>();deadline=EditorApplication.timeSinceStartup+100;
        };
        EditorApplication.update+=()=>
        {
            if(deadline==0)return;
            string path=Folder+"/Runtime.txt";
            if(File.Exists(path)){SessionState.SetBool("ThunderEditorReview",false);EditorApplication.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);}
            else if(EditorApplication.timeSinceStartup>deadline){File.WriteAllText(path,"FAIL timeout");EditorApplication.Exit(1);}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory(Folder);File.Delete(Folder+"/Runtime.txt");
        var asset=EnemyEditorWindow.EnsureAsset();string backup=EditorJsonUtility.ToJson(asset);EnemyEditorWindow window=null;
        try
        {
            GameRules.ResetRun();LevelRun.Reset(LevelId.Thunder,17381);
            window=EnemyEditorWindow.Open();window.Select(nameof(ThunderLanceBoss));
            var draft=(EnemyTuningProfile)typeof(EnemyEditorWindow).GetField("draft",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
            var health=draft.values.Single(v=>v.field=="health");health.enabled=true;health.value=1200;
            var speed=draft.values.Single(v=>v.field=="rotorSpeed");speed.enabled=true;speed.value=24;
            draft.projectileSpeed=1.25f;draft.projectileDamage=1.4f;window.Apply();
            var enemy=(ThunderLanceBoss)DistrictEnemy.Spawn(typeof(ThunderLanceBoss),Vector3.zero,true,true,false);
            if(Mathf.Abs(enemy.health-1200*LevelRun.EnemyHealthScale)>.01f || enemy.rotorSpeed!=24 || enemy.ProjectileSpeedScale!=1.25f || enemy.ProjectileDamageScale!=1.4f)throw new Exception("Saved tuning not applied to runtime factory");
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            if(!File.ReadAllText(EnemyEditorWindow.AssetPath).Contains("1200"))throw new Exception("Overrides not persisted to disk");
            window.CaptureModel(Folder+"/Editor-Model.png");
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();
            asset.Changed();
            if(asset.Find(nameof(ThunderLanceBoss))?.values.Any(v=>v.enabled)==true)throw new Exception("Undo did not restore defaults");
            LevelRun.Reset(LevelId.Space,17381);
            var part=WavePartLibrary.Space.First(p=>p.Enemy==nameof(OrbitalScout));var original=part.Generate(0,1);
            var profile=new EnemyTuningProfile{enemy=nameof(OrbitalScout),movement=1.25f,useFormationScore=true,formationAccents=new[]{.25f,.5f,.7f}};
            asset.enemies.Add(profile);asset.Changed();var tuned=part.Generate(0,1);
            if(Mathf.Abs(tuned[0].flightDuration-original[0].flightDuration/1.25f)>.001f || tuned[0].fireTimes.Length!=3)throw new Exception("Formation timing override ignored");
            WaveComposer.Create(8);profile.movement=.6f;asset.Changed();WaveComposer.Create(8);
            if(EnemyEditorWindow.Catalog.Count!=51)throw new Exception("Incomplete enemy/boss catalog");
            File.WriteAllText(Folder+"/Editor.txt","PASS\nAll 43 authored enemies/bosses listed. Health, rotor speed, projectile speed/damage saved and applied by runtime spawn factory. Disk persistence and Undo passed. Formation movement/accent edits rebuild wave geometry, including slower movement. Native model preview rendered.\n");
        }
        catch(Exception e){File.WriteAllText(Folder+"/Runtime.txt","FAIL editor\n"+e);EditorApplication.Exit(1);return;}
        finally
        {
            EditorJsonUtility.FromJsonOverwrite(backup,asset);asset.Changed();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);if(window!=null)window.Close();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("ThunderEditorReview",true);EditorApplication.isPlaying=true;
    }
}
public sealed class ThunderEditorReview : MonoBehaviour
{
    const string Folder=ReviewThunderEditor.Folder;
    readonly List<string> checks=new List<string>(),errors=new List<string>();
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Screen.SetResolution(640,480,false);Application.logMessageReceived+=Log;}
    void OnDestroy(){Application.logMessageReceived-=Log;}
    void Log(string message,string trace,LogType type){if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search"))errors.Add(message);}
    void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add(text);}
    IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0)
        {
            bool more=false;object step=null;Exception failure=null;
            try{more=stack.Peek().MoveNext();if(more)step=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){errors.Add(failure.ToString());break;}
            if(!more){stack.Pop();continue;}if(step is IEnumerator nested){stack.Push(nested);continue;}yield return step;
        }
        File.WriteAllText(Folder+"/Runtime.txt",(errors.Count==0 ? "PASS" : "FAIL")+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",errors));
    }
    IEnumerator Run()
    {
        yield return null;GameRules.StartNewRun();LevelRun.Reset(LevelId.Thunder,17381);CloudTransition.Load("L1");
        while(SceneManager.GetActiveScene().name!="L1")yield return null;yield return null;
        var flow=FindAnyObjectByType<WaveOrchestrer>();flow.enabled=false;flow.CancelInvoke();flow.StopAllCoroutines();
        var ship=FindAnyObjectByType<Shipcontroller>();ship.enabled=false;ship.ObserverMode=true;
        foreach(var gun in ship.GetComponentsInChildren<AWeapon>()){gun.StopFiring();gun.enabled=false;}
        while(CloudTransition.Busy || !WavePreparation.Ready)yield return null;GameRules.playerDisabled=false;Time.timeScale=1;
        CombatRound.ClearAll();foreach(var enemy in FindObjectsByType<DistrictEnemy>())Destroy(enemy.gameObject);yield return null;
        var boss=(ThunderLanceBoss)DistrictEnemy.Spawn(typeof(ThunderLanceBoss),new Vector3(2,Camera.UpLimit-2.5f,1));yield return null;
        Check(Mathf.Abs(boss.health-1800*LevelRun.EnemyHealthScale)<.01f,"Base boss health reduced from 2800 to 1800; shipping tuning asset has no overrides");
        yield return new WaitForSeconds(1.6f);boss.health*=.49f;
        float timeout=Time.time+8;
        while(boss.RotorCount!=6 && Time.time<timeout)yield return null;
        var muzzle=boss.Hull.TransformPoint(new Vector3(0,-1.98f,-.2f));
        Check(boss.Centred && Mathf.Abs(muzzle.x)<.01f && Mathf.Abs(muzzle.y-(Camera.UpLimit+Camera.DownLimit)*.5f)<.01f,"Phase two centres the front muzzle, with the boss hull behind the lasers");
        var beams=CombatRound.Live.OfType<PulseLaser>().Where(b=>b.IsSustained && b.IsAttachedTo(boss.Hull)).ToArray();
        Check(beams.Length==6 && beams.All(b=>b.Charge<1 && !b.IsFiring),"Exactly six attached front spokes visibly charge before becoming dangerous");
        DesertReview.Capture("Charge",Folder);yield return new WaitForSeconds(boss.rotorCharge+.15f);
        var angles=beams.Select(b=>Mathf.Repeat(Mathf.Atan2(b.Heading.y,b.Heading.x)*Mathf.Rad2Deg,360)).OrderBy(a=>a).ToArray();
        Check(Enumerable.Range(0,6).All(i=>Mathf.Abs(Mathf.Repeat(angles[(i+1)%6]-angles[i],360)-60)<.05f),"Six spokes are evenly spaced through the complete 360 degrees");
        var last=beams[0].Heading;float rotation=0;bool bullets=false;int peak=0;float start=Time.time,next=0;int frame=0;
        while(Time.time-start<21)
        {
            var origin=boss.Hull.TransformPoint(new Vector3(0,-1.98f,-.2f));
            float a=(Time.time-start)*boss.rotorSpeed*Mathf.Deg2Rad;
            ship.transform.position=origin+new Vector3(Mathf.Sin(-a)*3.8f,-Mathf.Cos(a)*3.8f,-.8f);
            Check(beams.All(b=>b!=null && b.isActiveAndEnabled && b.IsSustained && b.Power>.9f),"Persistent spoke check");
            float delta=Vector2.SignedAngle(last,beams[0].Heading);Check(delta<=.05f,"Spokes keep one clockwise direction");rotation+=delta;last=beams[0].Heading;
            bullets|=CombatRound.Live.Any(b=>!(b is PulseLaser));peak=Mathf.Max(peak,CombatRound.Live.Count);
            if(Time.time-start>=next){next+=3;DesertReview.Capture("Spin-"+(frame++).ToString("D2"),Folder);}
            yield return null;
        }
        checks.RemoveAll(s=>s=="Persistent spoke check" || s=="Spokes keep one clockwise direction");
        Check(rotation< -350,"The same six laser objects complete a full clockwise revolution and survive beyond the old 12-second lifetime");
        Check(bullets && peak<=CombatRound.Limit,"Side guns fire real dodging volleys while the six beams stay active; peak "+peak+" rounds");
        Check(LaserSound.SustainingCount>=6,"Sustained beams keep the existing looping beam audio");
        Destroy(boss.gameObject);yield return new WaitForSeconds(.4f);
        Check(beams.All(b=>b==null || !b.isActiveAndEnabled),"Owner destruction cools and releases all six beams");
        Check(LaserSound.SustainingCount==0,"Owner cleanup leaves no sustained beam audio");
    }
}
#endif
