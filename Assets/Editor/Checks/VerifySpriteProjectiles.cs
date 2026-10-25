#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifySpriteProjectiles
{
    static double deadline;
    static VerifySpriteProjectiles()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("SpriteReview", false))
              {
                  new GameObject("Sprite projectile review").AddComponent<SpriteProjectileReview>();
                  deadline = EditorApplication.timeSinceStartup + 600;
              }
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              const string report = "ArtSource/SpriteProjectileReview/Verification.txt";
              if (File.Exists(report))
              {
                  deadline = 0;
                  SessionState.SetBool("SpriteReview", false);
                  EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(report, "FAIL timed out");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/SpriteProjectileReview");
        File.Delete("ArtSource/SpriteProjectileReview/Verification.txt");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool("SpriteReview", true);
        EditorApplication.isPlaying = true;
    }
}
public sealed class SpriteProjectileReview : MonoBehaviour
{
    const string Folder = "ArtSource/SpriteProjectileReview/";
    static readonly Type[] Kinds = { typeof(SimpleRound), typeof(NeedleRound), typeof(HelixRound), typeof(BurstShell), typeof(RicochetBolt), typeof(SuspendedRound), typeof(GuidedMissile), typeof(PlasmaOrb), typeof(ProximityMine), typeof(ReturningDisc), typeof(ScatterCarrier), typeof(LaserSideRound) };
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
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

    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        checks.Add("PASS " + message);
    }

    static CombatRound Launch(Type type, Vector3 position, float radius = .18f, float speed = 0)
        => (CombatRound)typeof(CombatRound).GetMethod("Launch").MakeGenericMethod(type).Invoke(null, new object[] { position, Vector2.down, speed, radius });
    static void Clear()
    {
        foreach (var b in FindObjectsByType<CombatRound>())
            b.Vanish();
        foreach (var b in FindObjectsByType<ABullet>())
            Destroy(b.gameObject);
        foreach (var e in AEnemy.Live.ToArray())
            Destroy(e.gameObject);
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var t = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".jpg", t.EncodeToJPG(94));
        Destroy(t);
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            object step = null;
            bool more = false;
            Exception error = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception ex) { error = ex; }
            if (error != null)
            {
                errors.Add(error.ToString());
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
        File.WriteAllText(Folder + "Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    IEnumerator Environment(LevelId level)
    {
        var w = WaveWatcher.Current;
        w.SelectLevel(level);
        yield return new WaitForSeconds(2);
        while (!WavePreparation.Ready)
            yield return null;
        w.StopAllCoroutines();
        w.enabled = false;
        Time.timeScale = 1;
        Clear();
        yield return null;
    }

    void Gallery()
    {
        for (int i = 0; i < Kinds.Length; i++)
        {
            var pos = new Vector3(-3.5f + i % 4 * 2.25f, 5.8f - i / 4 * 4);
            var shot = Launch(Kinds[i], pos, .43f);
            shot.enabled = false;
            shot.transform.rotation = Quaternion.identity;
            var small = Launch(Kinds[i], pos + new Vector3(.75f, -1), .12f);
            small.enabled = false;
            small.transform.rotation = Quaternion.identity;
        }
    }

    void PixelChecks()
    {
        foreach (var type in Kinds)
        {
            var shot = Launch(type, Vector3.zero);
            shot.enabled = false;
            var sprite = shot.GetComponent<SpriteRenderer>().sprite;
            var texture = sprite.texture;
            Check(texture.filterMode == FilterMode.Point && texture.mipmapCount == 1, type.Name + " point sampling / no mipmaps");
            Check(texture.GetPixels32().All(p => p.r == p.g && p.g == p.b && (p.a == 0 || p.a == 255)), type.Name + " neutral source pixels and hard alpha");
            Check(shot.GetComponentInChildren<MeshRenderer>() == null && !shot.GetComponent<SpriteRenderer>().forceRenderingOff, type.Name + " visible sprite, no 3D model");
            var collider = shot.GetComponent<CustomCollider2D>();
            collider.enabled = true;
            var pixels = texture.GetPixels32();
            int checkedPixels = 0;
            foreach (float angle in new[] { 0, 45, 90 })
            {
                shot.transform.rotation = Quaternion.Euler(0, 0, angle);
                Physics2D.SyncTransforms();
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                    {
                        var local = (new Vector2(x + .5f, y + .5f) - sprite.pivot) / sprite.pixelsPerUnit;
                        bool actual = collider.OverlapPoint(shot.transform.TransformPoint(local)), expected = pixels[y * texture.width + x].a > 128;
                        if (actual != expected)
                            throw new Exception(type.Name + " collision mismatch at " + x + "," + y + " angle " + angle + " expected " + expected);
                        checkedPixels++;
                    }
            }
            Check(true, type.Name + " collision matches " + checkedPixels + " opaque/transparent pixel samples, including holes, at 3 rotations");
            Check(shot.GetComponent<SpriteRenderer>().sharedMaterial.GetColor("_SpriteTint") == shot.Accent, type.Name + " district colour supplied separately by code");
            shot.Vanish();
        }
        var round = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down);
        var original = round.GetComponent<SpriteRenderer>().sprite;
        round.GetComponent<SpriteRenderer>().color = Color.gray;
        round.Vanish();
        var reused = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down);
        Check(reused == round && reused.GetComponent<SpriteRenderer>().sprite == original && reused.GetComponent<SpriteRenderer>().color == Color.white, "pool preserves shared sprite, collider and resets fade");
        reused.Vanish();
        var side = CombatRound.Launch<LaserSideRound>(Vector3.zero, Vector2.left, 1);
        side.SendMessage("Tick", (object)0f);
        Check(Vector2.Dot(side.transform.right, Vector2.left) > .999f, "horizontal side-shot nose faces travel direction");
        side.Vanish();
        var carrier = CombatRound.Launch<ScatterCarrier>(Vector3.zero, Vector2.down, 0);
        var age = typeof(CombatRound).GetField("age", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        for (int volley = 1; volley <= 7; volley++)
        {
            age.SetValue(carrier, volley * .66f);
            carrier.SendMessage("Tick", (object)0f);
            Check(CombatRound.Live.OfType<NeedleRound>().Count() == volley * 2, "carrier emits complete side pair " + volley + ", including beyond its third volley");
        }
        foreach (var shot in CombatRound.Live.ToArray())
            shot.Vanish();
    }

    IEnumerator Run()
    {
        if (SceneManager.GetActiveScene().name != "Title")
            yield return SceneManager.LoadSceneAsync("Title");
        yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready)
            yield return null;
        WaveWatcher.Current.SetSeed(7119);
        WaveWatcher.Current.SelectView(WatcherView.Projectiles);
        yield return Environment(LevelId.Fields);
        PixelChecks();
        yield return null;
        Gallery();
        yield return null;
        Check(!ShaderUtil.ShaderHasError(CombatVisuals.ProjectileMaterial.shader), "sprite tint / status shader compiles");
        yield return Capture("Catalog");
        Clear();
        yield return null;
        foreach (string prefab in new[] { "PlayerBullet", "TwerpBullet", "EnemyBulletSmall", "EnemyBulletMedium", "EnemyBulletBig", "CircularWave" })
        {
            var obj = Instantiate(Resources.Load<GameObject>("Bullets/" + prefab), Vector3.zero, Quaternion.identity);
            yield return null;
            Check(obj.GetComponent<ProjectileSpriteTint>() != null && obj.GetComponentInChildren<MeshRenderer>() == null, prefab + " uses sprite rendering");
            Destroy(obj);
        }
        for (int i = 0; i < 6; i++)
        {
            var p = PlayerRound.Spawn(new Vector3(-3.5f + i * 1.4f, -4), Vector2.up, 1, 0, (PlayerDelivery)i);
            p.enabled = false;
            Check(p.GetComponent<ProjectileSpriteTint>() != null && p.GetComponentInChildren<MeshRenderer>() == null, "player " + p.delivery + " restored to 2D");
        }
        yield return null;
        yield return Capture("PlayerSprites");
        Clear();
        yield return null;
        var upgrades = CombatUpgrades.Current;
        upgrades.Add(UpgradeKind.Gasoline);
        upgrades.Add(UpgradeKind.Poison);
        var coated = PlayerRound.Spawn(Vector3.zero, Vector2.up, 1, 0);
        yield return null;
        var properties = new MaterialPropertyBlock();
        coated.GetComponent<SpriteRenderer>().GetPropertyBlock(properties);
        Check(properties.GetFloat("_Burn") == 1 && properties.GetFloat("_Poison") == 1, "burn and poison coatings retained on sprites");
        Clear();
        foreach (var level in new[] { LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel, LevelId.Desert, LevelId.Candyland })
        {
            yield return Environment(level);
            for (int i = 0; i < Kinds.Length; i++)
                Launch(Kinds[i], new Vector3(-3.8f + i % 6 * 1.5f, 6 - i / 6 * 3), .18f, 2.4f);
            for (int frame = 0; frame < 8; frame++)
            {
                yield return new WaitForSeconds(.35f);
                yield return Capture(level + "Motion" + frame);
            }
            checks.Add("Captured " + level + " moving patterns across eight frames.");
        }
        yield return Environment(LevelId.Snow);
        Time.timeScale = 0;
        for (int i = 0; i < CombatRound.Limit; i++)
        {
            float a = i * 2.39996f, r = Mathf.Sqrt((i + .5f) / CombatRound.Limit);
            Launch(Kinds[i % Kinds.Length], new Vector3(Mathf.Cos(a) * 4.5f * r, Mathf.Sin(a) * 7.2f * r), .12f);
        }
        for (int i = 0; i < 180; i++)
            PlayerRound.Spawn(new Vector3(-4 + i % 20 * .42f, -7 + i / 20 * 1.5f), Vector2.up, 1, 0, (PlayerDelivery)(i % 6));
        yield return null;
        Check(CombatRound.Live.Count == 280 && PlayerRound.LiveCount == 180, "both projectile caps reached: 460 sprites");
        yield return Capture("DenseSnow");
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        for (int i = 0; i < 20; i++)
            yield return null;
        var times = new List<float>();
        for (int i = 0; i < 120; i++)
        {
            yield return null;
            times.Add(Time.unscaledDeltaTime * 1000);
        }
        times.Sort();
        checks.Add("Editor render stress median " + times[60].ToString("F2") + "ms, p95 " + times[114].ToString("F2") + "ms (not a standalone GPU benchmark).");
    }
}
#endif
