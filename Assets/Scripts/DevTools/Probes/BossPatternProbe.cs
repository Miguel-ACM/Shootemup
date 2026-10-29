#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BossPatternProbe : MonoBehaviour
{
    const string Folder = "ArtSource/CombatReview/BossPatterns";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> results = new List<string>(), errors = new List<string>();
    Shipcontroller ship;
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

    static FieldInfo Field(Type type, string name)
    {
        while (type != null)
        {
            var field = type.GetField(name, Hidden | BindingFlags.Public);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        throw new Exception(name);
    }

    static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
    static T Get<T>(object target, string name) => (T)Field(target.GetType(), name).GetValue(target);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        results.Add("PASS " + message);
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
            catch (Exception ex) { failure = ex; }
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
        string report = (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", results) + "\n" + string.Join("\n", errors);
        File.WriteAllText(Folder + "/Verification.txt", report);
        File.WriteAllText("Temp/BossPatterns.result", report);
    }

    void ClearRounds()
    {
        foreach (var round in CombatRound.Live.ToArray())
            round.Vanish();
    }

    void Clear()
    {
        ClearRounds();
        foreach (var enemy in FindObjectsByType<AEnemy>(FindObjectsInactive.Include))
            if (enemy != null)
                Destroy(enemy.gameObject);
        foreach (var item in FindObjectsByType<APickable>())
            Destroy(item.gameObject);
        foreach (var drone in FindObjectsByType<ThunderLaserBot>())
            Destroy(drone.gameObject);
    }

    BurstShell Shell(float distance, float speed = 3)
    {
        var shell = CombatRound.Launch<BurstShell>(new Vector3(0, 5), Vector2.down, speed, .4f).SetTravelDistance(distance);
        return shell;
    }

    static float Stop(BurstShell shell) => Get<float>(shell, "cruise") + Get<float>(shell, "brake");
    static void ShellTick(BurstShell shell, float time, float dt)
    {
        Set(shell, "age", time);
        Call(shell, "Tick", dt);
    }

    void ShellChecks()
    {
        foreach (int fps in new[] { 30, 60, 144 })
            foreach (float distance in new[] { 2.85f, 5.2f, 8.4f })
                foreach (float speed in new[] { 3f, 7f })
                {
                    var shell = Shell(distance, speed);
                    Vector3 start = shell.transform.position;
                    float stop = Stop(shell), clock = 0;
                    while (clock < stop)
                    {
                        float dt = Mathf.Min(1f / fps, stop - clock);
                        clock += dt;
                        ShellTick(shell, clock, dt);
                    }
                    Check(Mathf.Abs(Vector3.Distance(start, shell.transform.position) - distance) < .002f, "selected shell range " + distance + " at " + fps + " FPS / speed " + speed);
                    ClearRounds();
                }
        foreach (float phase in new[] { 0f, 27f, 359f, 725f, -411f })
        {
            var shell = Shell(2);
            Set(shell, "heading", CombatVisuals.Rotate(Vector2.down, phase));
            ShellTick(shell, Stop(shell) + .86f, Stop(shell) + .86f);
            var fragments = CombatRound.Live.Where(x => x.generation == 1).ToArray();
            Check(fragments.Length == 12, "complete twelve-shot ring at phase " + phase);
            var angles = fragments.Select(x => Mathf.Repeat(Vector2.SignedAngle(Vector2.down, x.Heading), 360)).OrderBy(x => x).ToArray();
            for (int i = 0; i < 12; i++)
                Check(Mathf.Abs(Mathf.Repeat(angles[(i + 1) % 12] - angles[i], 360) - 30) < .002f, "uniform ring angle including wraparound");
            ClearRounds();
        }
        var waiting = Shell(2);
        for (int i = 0; i < CombatRound.Limit - 6; i++)
            CombatRound.Launch<SimpleRound>(new Vector3(0, 2), Vector2.down, 0);
        ShellTick(waiting, Stop(waiting) + .86f, Stop(waiting) + .86f);
        Check(waiting.gameObject.activeSelf && CombatRound.Live.All(x => x.generation == 0), "full projectile budget defers the whole burst");
        foreach (var round in CombatRound.Live.Where(x => x != waiting).Take(10).ToArray())
            round.Vanish();
        ShellTick(waiting, Stop(waiting) + .87f, .01f);
        Check(!waiting.gameObject.activeSelf && CombatRound.Live.Count(x => x.generation == 1) == 12 && CombatRound.Live.Count <= CombatRound.Limit, "deferred burst releases all twelve without exceeding cap");
        ClearRounds();
        var recycled = CombatRound.Launch<BurstShell>(new Vector3(0, 5), Vector2.down, 3);
        Check(Mathf.Approximately(recycled.TravelDistance, 3.8f), "pooled shells reset their previous enemy's range");
        ClearRounds();
    }

    IEnumerator ScoreChecks()
    {
        foreach (var entry in new[] { (typeof(AvalancheBomber), 8.4f, 1), (typeof(SnowSiegeCraft), 7.2f, 1) })
        {
            var enemy = DistrictEnemy.Spawn(entry.Item1, new Vector3(0, 5, 1));
            yield return null;
            enemy.enabled = false;
            Set(enemy, "volley", entry.Item3);
            Call(enemy, "Attack");
            var shells = CombatRound.Live.OfType<BurstShell>().ToArray();
            Check(shells.Length > 0 && shells.All(x => Mathf.Approximately(x.TravelDistance, entry.Item2)), entry.Item1.Name + " selects its own shell range");
            Clear();
            yield return null;
        }
        // Every boss owns a phrased score: real output, attached charged beams and deliberate rests.
        // Spell-card bosses only play while enabled; the timed clips below cover them.
        foreach (var type in new[] { typeof(ThunderLanceBoss) })
        {
            var boss = (DistrictBoss)DistrictEnemy.Spawn(type, new Vector3(0, 5, 1));
            yield return null;
            boss.enabled = false;
            foreach (bool second in new[] { false, true })
            {
                Set(boss, "phaseTwo", second);
                int total = 0, rests = 0;
                for (int i = 1; i <= 16; i++)
                {
                    Set(boss, "volley", i);
                    Call(boss, "Attack");
                    int shots = CombatRound.Live.Count(x => !(x is PulseLaser beam && beam.IsSustained));
                    total += shots;
                    if (shots == 0)
                        rests++;
                    foreach (var shell in CombatRound.Live.OfType<BurstShell>())
                        Check(shell.TravelDistance >= 4.4f, "boss shell has an explicit scored range");
                    foreach (var beam in CombatRound.Live.OfType<PulseLaser>())
                        Check(beam.IsAttachedTo(boss.Hull) && beam.warning >= 1, "beam has a charged attached emitter");
                    ClearRounds();
                }
                // Multi-frame phrases do not start on the disabled boss; this counts only immediate volleys.
                Check(total > 8 && rests >= 2, type.Name + " phase " + (second ? 2 : 1) + " scored output " + total + ", rests " + rests);
            }
            Clear();
            yield return null;
        }
        var city = (CityDuoBoss)DistrictEnemy.Spawn(typeof(CityDuoBoss), new Vector3(-2, 5, 1));
        yield return null;
        yield return null;
        city.enabled = false;
        var heavy = city.partner;
        heavy.enabled = false;
        for (int beat = 1; beat <= 12; beat++)
        {
            Set(city, "volley", beat);
            Call(city, "Attack");
            int fast = CombatRound.Live.Count;
            ClearRounds();
            Set(heavy, "volley", beat);
            Call(heavy, "Attack");
            int slow = CombatRound.Live.Count;
            ClearRounds();
            Check(fast == 0 || slow == 0, "city duo trades beat " + beat);
        }
        float initial = Get<float>(heavy, "initialHealth");
        heavy.health = initial * .4f;
        int pickups = FindObjectsByType<APickable>().Length;
        city.damage(city.health + 1);
        yield return null;
        Check(Mathf.Abs(heavy.health - initial * .7f) < .01f && heavy.partner == null, "city survivor heals half its missing health");
        Check(Get<bool>(heavy, "enraged") && FindObjectsByType<APickable>().Length == pickups, "first city death drops nothing and enrages survivor");
        Set(heavy, "volley", 3);
        Call(heavy, "Attack");
        Check(CombatRound.Live.OfType<NeedleRound>().Any(), "city survivor inherits its partner's headlight rays");
        Clear();
        yield return null;
        var saw = (TunnelSawBoss)DistrictEnemy.Spawn(typeof(TunnelSawBoss), new Vector3(0, 2, 1));
        yield return null;
        saw.enabled = false;
        Set(saw, "entered", true);
        Call(saw, "MoveBoss", .01f);
        saw.damage(1);
        Check(CombatRound.Live.Count == 0 && saw.GetComponent<EnemyThrusters>() == null, "saw emits nothing when hit and has no thrusters");
        Rect arena = Get<Rect>(saw, "arena");
        float radius = saw.Hull.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * saw.Hull.localScale.x;
        saw.transform.position = new Vector3(arena.xMax - radius - .001f, 0, 1);
        Set(saw, "velocity", Vector2.right);
        Call(saw, "MoveBoss", .1f);
        int needles = CombatRound.Live.OfType<NeedleRound>().Count(), teeth = CombatRound.Live.OfType<SimpleRound>().Count();
        Check(needles > 1 && needles == teeth + 1, "saw bounce pairs fast needles with slow teeth");
        Check(CombatRound.Live.All(x => x.Heading.x < .2f), "right-wall saw spray faces back into the arena");
        ClearRounds();
        Set(saw, "phaseTwo", true);
        Call(saw, "OnPhaseTwo");
        yield return null;
        Check(FindObjectsByType<TunnelSawBoss>().Length == 1, "second-phase saw deploys no minions");
        saw.transform.position = new Vector3(arena.xMax - radius - .001f, 0, 1);
        Set(saw, "velocity", Vector2.right);
        Set(saw, "haltUntil", 0f);
        Set(saw, "runners", true);
        Call(saw, "MoveBoss", .1f);
        Check(CombatRound.Live.OfType<SimpleRound>().Count() > CombatRound.Live.OfType<NeedleRound>().Count(), "Rim Runners bounces also throw wall runners");
        Clear();
        yield return null;
    }

    IEnumerator Clip(string name, float seconds)
    {
        Directory.CreateDirectory(Folder + "/" + name);
        float start = Time.time, next = 0;
        int frame = 0, peak = 0;
        while (Time.time - start < seconds)
        {
            ship.transform.position = new Vector3(Mathf.Sin((Time.time - start) * .8f) * 2.8f, Camera.DownLimit + 1.5f, -1);
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            CheckCap();
            if (Time.time - start >= next)
            {
                // Batch editors do not reliably dispatch WaitForEndOfFrame.
                // Render the real gameplay camera and its existing PSX composite directly.
                var camera = UnityEngine.Camera.main;
                camera.Render();
                var presentation = camera.GetComponent<PSXPresentation>();
                var source = presentation.SceneTexture;
                var output = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, output, presentation.CompositeMaterial);
                var previous = RenderTexture.active;
                RenderTexture.active = output;
                var picture = new Texture2D(output.width, output.height, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, output.width, output.height), 0, 0);
                picture.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Folder + "/" + name + "/" + (frame++).ToString("D4") + ".jpg", picture.EncodeToJPG(86));
                Destroy(picture);
                next += .1f;
                RenderTexture.ReleaseTemporary(output);
            }
            yield return null;
        }
        Check(peak > 10, name + " live pressure; peak " + peak + "; " + frame + " captured frames");
    }

    void CheckCap()
    {
        if (CombatRound.Live.Count > CombatRound.Limit)
            throw new Exception("Projectile cap exceeded");
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset();
        Time.timeScale = 1;
        if (SceneManager.GetActiveScene().name != "L1")
            yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSeconds(2);
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.enabled = false;
        ship.StopAllCoroutines();
        foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
            renderer.forceRenderingOff = true;
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.StopAllCoroutines();
        flow.CancelInvoke();
        while (CloudTransition.Busy)
            yield return null;
        Clear();
        yield return null;
        // Plains shells open as a complete twelve-shard ring.
        LevelRun.Reset(LevelId.Fields);
        ShellChecks();
        yield return ScoreChecks();
        foreach (var entry in new[] { (LevelId.Fields, typeof(FieldsBomberBoss)), (LevelId.Snow, typeof(SnowSiegeBoss)), (LevelId.City, typeof(CityDuoBoss)), (LevelId.Thunder, typeof(ThunderLanceBoss)), (LevelId.Tunnel, typeof(TunnelSawBoss)),
            (LevelId.Desert, typeof(DesertWormBoss)), (LevelId.Candyland, typeof(CandyRegentBoss)), (LevelId.Space, typeof(SpaceCitadelBoss)), (LevelId.Underwater, typeof(AbyssSubmarineBoss)), (LevelId.Hell, typeof(HellCrabBoss)) })
        {
            LevelRun.Reset(entry.Item1);
            LevelEnvironment.Apply();
            var boss = (DistrictBoss)DistrictEnemy.Spawn(entry.Item2, new Vector3(0, Camera.UpLimit - 2.3f, 1));
            yield return null;
            yield return null;
            // Long enough for both phase-one spell cards.
            yield return Clip(entry.Item1 + "One", 17);
            float lethal = 1e6f;
            if (boss is CityDuoBoss duo)
            {
                if (duo.partner != null)
                    duo.partner.damage(duo.partner.health + 1);
            }
            else if (boss is DesertWormBoss worm)
                for (int i = 1; i <= 4; i++)
                    worm.Cores[i].damage(lethal);
            else if (boss is SpaceCitadelBoss citadel)
                for (int i = 0; i < 2; i++)
                    citadel.Parts[i].damage(lethal);
            else if (boss is HellCrabBoss crab)
                crab.Eyes[0].damage(lethal);
            else
                boss.damage(boss.health * .55f);
            yield return Clip(entry.Item1 + "Two", 17);
            Clear();
            yield return null;
        }
    }
}
#endif
