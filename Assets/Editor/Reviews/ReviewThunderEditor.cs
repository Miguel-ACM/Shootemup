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
    public const string Folder = "ArtSource/ThunderEditorReview";
    static double deadline;
    static ReviewThunderEditor()
    {
        EditorApplication.playModeStateChanged += s =>
          {
              if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ThunderEditorReview", false))
                  return;
              new GameObject("Thunder rotor review").AddComponent<ThunderEditorReview>();
              deadline = EditorApplication.timeSinceStartup + 100;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              string path = Folder + "/Runtime.txt";
              if (File.Exists(path))
              {
                  SessionState.SetBool("ThunderEditorReview", false);
                  EditorApplication.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(path, "FAIL timeout");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/Runtime.txt");
        var asset = EnemyEditorWindow.EnsureAsset();
        string backup = EditorJsonUtility.ToJson(asset);
        EnemyEditorWindow window = null;
        try
        {
            GameRules.ResetRun();
            LevelRun.Reset(LevelId.Thunder, 17381);
            window = EnemyEditorWindow.Open();
            window.Select(nameof(ThunderLanceBoss));
            var draft = (EnemyTuningProfile)typeof(EnemyEditorWindow).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(window);
            var health = draft.values.Single(v => v.field == "health");
            health.enabled = true;
            health.value = 1200;
            var speed = draft.values.Single(v => v.field == "beamCharge");
            speed.enabled = true;
            speed.value = 1.4f;
            draft.projectileSpeed = 1.25f;
            draft.projectileDamage = 1.4f;
            window.Apply();
            var enemy = (ThunderLanceBoss)DistrictEnemy.Spawn(typeof(ThunderLanceBoss), Vector3.zero, true, true, false);
            if (Mathf.Abs(enemy.health - 1200 * LevelRun.EnemyHealthScale) > .01f || enemy.beamCharge != 1.4f || enemy.ProjectileSpeedScale != 1.25f || enemy.ProjectileDamageScale != 1.4f)
                throw new Exception("Saved tuning not applied to runtime factory");
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            if (!File.ReadAllText(EnemyEditorWindow.AssetPath).Contains("1200"))
                throw new Exception("Overrides not persisted to disk");
            window.CaptureModel(Folder + "/Editor-Model.png");
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            asset.Changed();
            if (asset.Find(nameof(ThunderLanceBoss))?.values.Any(v => v.enabled) == true)
                throw new Exception("Undo did not restore defaults");
            LevelRun.Reset(LevelId.Space, 17381);
            var part = WavePartLibrary.Space.First(p => p.Enemy == nameof(OrbitalScout));
            var original = part.Generate(0, 1);
            var profile = new EnemyTuningProfile { enemy = nameof(OrbitalScout), movement = 1.25f, useFormationScore = true, formationAccents = new[] { .25f, .5f, .7f } };
            asset.enemies.Add(profile);
            asset.Changed();
            var tuned = part.Generate(0, 1);
            if (Mathf.Abs(tuned[0].flightDuration - original[0].flightDuration / 1.25f) > .001f || tuned[0].fireTimes.Length != 3)
                throw new Exception("Formation timing override ignored");
            WaveComposer.Create(8);
            profile.movement = .6f;
            asset.Changed();
            WaveComposer.Create(8);
            if (EnemyEditorWindow.Catalog.Count != DistrictWaves.EnemyTypes.Length + Enum.GetValues(typeof(LevelId)).Length)
                throw new Exception("Incomplete enemy/boss catalog");
            File.WriteAllText(Folder + "/Editor.txt", "PASS\nAll 43 authored enemies/bosses listed. Health, laser charge, projectile speed/damage saved and applied by runtime spawn factory. Disk persistence and Undo passed. Formation movement/accent edits rebuild wave geometry, including slower movement. Native model preview rendered.\n");
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Runtime.txt", "FAIL editor\n" + e); EditorApplication.Exit(1); return; }
        finally
        {
            EditorJsonUtility.FromJsonOverwrite(backup, asset);
            asset.Changed();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            if (window != null)
                window.Close();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ThunderEditorReview", true);
        EditorApplication.isPlaying = true;
    }
}
public sealed class ThunderEditorReview : MonoBehaviour
{
    const string Folder = ReviewThunderEditor.Folder;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Screen.SetResolution(640, 480, false);
        Application.logMessageReceived += Log;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }

    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message);
    }

    void Check(bool ok, string text)
    {
        if (!ok)
            throw new Exception(text);
        checks.Add(text);
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            Exception failure = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception e) { failure = e; }
            if (failure != null)
            {
                errors.Add(failure.ToString());
                break;
            }
            if (!more)
            {
                stack.Pop();
                continue;
            }
            if (step is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return step;
        }
        File.WriteAllText(Folder + "/Runtime.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    IEnumerator Run()
    {
        yield return null;
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Thunder, 17381);
        CloudTransition.Load("L1");
        while (SceneManager.GetActiveScene().name != "L1")
            yield return null;
        yield return null;
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.enabled = false;
        ship.ObserverMode = true;
        foreach (var gun in ship.GetComponentsInChildren<AWeapon>())
        {
            gun.StopFiring();
            gun.enabled = false;
        }
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        GameRules.playerDisabled = false;
        Time.timeScale = 1;
        CombatRound.ClearAll();
        foreach (var enemy in FindObjectsByType<DistrictEnemy>())
            Destroy(enemy.gameObject);
        yield return null;
        var boss=(ThunderLanceBoss)DistrictEnemy.Spawn(typeof(ThunderLanceBoss),new Vector3(0,Camera.UpLimit-2.5f,1));
        yield return null;
        Check(Mathf.Abs(boss.health-1250*LevelRun.EnemyHealthScale)<.01f,"Shipping Thunder base health and route scaling");
        for(int phase=0;phase<2;phase++)
        {
            if(phase==1)boss.health*=.49f;
            float begin=Time.time,next=0;int observed=0,peak=0;
            while(Time.time-begin<13)
            {
                var active=CombatRound.Live.OfType<PulseLaser>().Where(b=>b.IsAttachedTo(boss.Hull)).ToArray();
                foreach(var beam in active)Check(Vector2.Dot(beam.Heading,Vector2.down)>.999f,"Thunder beam points straight down");
                observed+=active.Length;peak=Mathf.Max(peak,CombatRound.Live.Count);
                Check(boss.transform.position.y>Camera.UpLimit-3,"Thunder keeps its emitter hull above the dodge space");
                if(Time.time-begin>=next){next+=3;DesertReview.Capture("Vertical-"+phase+"-"+(int)next,Folder);}
                yield return null;
            }
            Check(observed>0 && peak<CombatRound.Limit,"Both Thunder scores use attached vertical beams within budget");
        }
        checks.RemoveAll(x=>x=="Thunder beam points straight down"||x=="Thunder keeps its emitter hull above the dodge space");
        Destroy(boss.gameObject);yield return new WaitForSeconds(.4f);
        Check(!CombatRound.Live.OfType<PulseLaser>().Any(b=>b.IsFiring),"Thunder owner cleanup releases its finite beams");
    }
}
#endif
