#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReviewArtRebuild
{
    public const string Folder = "ArtSource/CalderaBossRebuild";
    static double deadline;
    static ReviewArtRebuild()
    {
        EditorApplication.playModeStateChanged += s =>
          {
              if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ArtRebuild", false))
              {
                  new GameObject("Caldera and boss visual review").AddComponent<ArtRebuildReview>();
                  deadline = EditorApplication.timeSinceStartup + 90;
              }
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              string report = Folder + "/Runtime.txt";
              if (File.Exists(report))
              {
                  SessionState.SetBool("ArtRebuild", false);
                  EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(report, "FAIL timeout");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/Runtime.txt");
        var window = EnemyEditorWindow.Open();
        window.Select(nameof(AbyssSubmarineBoss));
        window.CaptureModel(Folder + "/Hunter-Model.png");
        window.Close();
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ArtRebuild", true);
        EditorApplication.isPlaying = true;
    }

    public static void RuntimeOnly()
    {
        File.Delete(Folder + "/Runtime.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ArtRebuild", true);
        EditorApplication.isPlaying = true;
    }

    public static void EnvironmentStill()
    {
        Directory.CreateDirectory(Folder);
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(270, 480, 24);
        var filtered = new RenderTexture(270, 480, 0);
        var previous = RenderTexture.active;
        var composite = new Material(Shader.Find("Shootemup/PSX Composite"));
        try
        {
            var view = new GameObject("Caldera preview camera");
            SceneManager.MoveGameObjectToScene(view, scene);
            var camera = view.AddComponent<UnityEngine.Camera>();
            camera.scene = scene;
            camera.orthographic = true;
            camera.orthographicSize = 8.850766f;
            camera.aspect = 270f / 480;
            camera.transform.position = new Vector3(0, 0, -40);
            camera.farClipPlane = 300;
            camera.targetTexture = target;
            var world = new GameObject("Modeled caldera preview");
            SceneManager.MoveGameObjectToScene(world, scene);
            LevelRun.Reset(LevelId.Hell, 17381);
            var environment = world.AddComponent<InfernalCaldera>();
            typeof(InfernalCaldera).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(environment, null);
            camera.Render();
            Graphics.Blit(target, filtered, composite);
            RenderTexture.active = filtered;
            var pixels = new Texture2D(270, 480, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 270, 480), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Folder + "/Hell-Final.png", pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
            bool errors = ShaderUtil.ShaderHasError(Resources.Load<Shader>("CalderaBasalt")) || ShaderUtil.ShaderHasError(Resources.Load<Shader>("CalderaLava"));
            File.WriteAllText(Folder + "/Materials.txt", errors ? "FAIL shader compilation" : "PASS native basalt and molten-bed materials rendered");
            EditorApplication.Exit(errors ? 1 : 0);
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Materials.txt", "FAIL\n" + e); EditorApplication.Exit(1); }
        finally { RenderTexture.active = previous; EditorSceneManager.ClosePreviewScene(scene); target.Release(); filtered.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(filtered); UnityEngine.Object.DestroyImmediate(composite); }
    }
}
public sealed class ArtRebuildReview : MonoBehaviour
{
    const string Folder = ReviewArtRebuild.Folder;
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

    void Log(string m, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(m);
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
            object next = null;
            Exception failure = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    next = stack.Peek().Current;
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
            if (next is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return next;
        }
        File.WriteAllText(Folder + "/Runtime.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    IEnumerator Run()
    {
        yield return null;
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Hell, 17381);
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
        ship.transform.position = new Vector3(0, -7, -1);
        foreach (var gun in ship.GetComponentsInChildren<AWeapon>())
        {
            gun.StopFiring();
            gun.enabled = false;
        }
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        GameRules.playerDisabled = false;
        Time.timeScale = 1;
        Check(FindAnyObjectByType<InfernalCaldera>() != null && FindAnyObjectByType<DepthBackdrop>() == null, "Hell uses a completely separate modeled caldera environment");
        yield return new WaitForSeconds(1);
        DesertReview.Capture("Hell-Caldera", Folder);
        var crab = (HellCrabBoss)DistrictEnemy.Spawn(typeof(HellCrabBoss), new Vector3(0, 5, 1));
        yield return new WaitForSeconds(1);
        var hands = crab.GetComponentsInChildren<Transform>().Where(t => t.name == "Jointed side pincer").ToArray();
        Check(hands.Length == 2 && hands.All(t => t.localScale.x >= 1.7f), "Crab has two substantially larger pincers");
        Check(crab.GetComponentsInChildren<MeshFilter>().Count(m => m.sharedMesh.name.Contains("pincer vertebra")) == 8, "Short crab arms use four overlapping segments per side");
        DesertReview.Capture("Crab-Rest", Folder);
        yield return new WaitForSeconds(1.9f);
        Check(hands.All(h => h.position.y > crab.transform.position.y - 2.8f), "Crab swings are deliberately limited to a short reach instead of chasing the bottom");
        DesertReview.Capture("Crab-Pinch", Folder);
        Destroy(crab.gameObject);
        CombatRound.ClearAll();
        LevelRun.Reset(LevelId.Underwater, 17381);
        LevelEnvironment.Apply();
        while (!WavePreparation.Ready)
            yield return null;
        yield return null;
        var hunter = (AbyssSubmarineBoss)DistrictEnemy.Spawn(typeof(AbyssSubmarineBoss), new Vector3(0, 4, 1));
        yield return new WaitForSeconds(1);
        Check(hunter.Hull.GetComponent<MeshFilter>().sharedMesh.name.Contains("Sculpted abyssal hunter"), "Underwater boss uses the new sculpted pressure hull");
        Check(hunter.GetComponent<CapsuleCollider2D>().size.x > 1.95f, "Hull hitbox follows the broader central body");
        DesertReview.Capture("Hunter-Patrol", Folder);
        yield return new WaitForSeconds(5.9f);
        DesertReview.Capture("Hunter-Eruption", Folder);
        Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("CalderaLava")), "Native caldera lava shader compiles");
    }
}
#endif
