#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

// One combined visual/audio review followed by all forty waves and stock-gun TTK.
public sealed class PolishReviewProbe : MonoBehaviour
{
    public bool TitleAfterVisuals;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship; WaveOrchestrer flow; Keyboard keyboard;
    int frame; float nextFrame, started;
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

    void Log(string text, string trace, LogType kind)
    {
        if ((kind == LogType.Error || kind == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(text + "\n" + trace);
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
        if (keyboard != null)
            InputSystem.RemoveDevice(keyboard);
        Clear();
        string result = (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors);
        Directory.CreateDirectory("ArtSource/CombatReview/Polish");
        File.WriteAllText("ArtSource/CombatReview/Polish/Verification.txt", result);
        if (errors.Count > 0)
            File.WriteAllText("Temp/CombatResult.txt", result);
        else if (TitleAfterVisuals)
            new GameObject("Natural title startup after polish").AddComponent<TitleHudStartupProbe>();
        else
        {
            LevelRun.Reset();
            new GameObject("Full choreography after polish").AddComponent<ChoreographyProbe>();
        }
    }

    void Check(bool ok, string text)
    {
        if (!ok)
            throw new Exception(text);
        checks.Add("PASS " + text);
    }

    void Clear()
    {
        foreach (var e in FindObjectsByType<AEnemy>())
            Destroy(e.gameObject);
        foreach (var b in FindObjectsByType<AEnemyBullet>())
            b.Vanish();
        foreach (var b in FindObjectsByType<ABullet>())
            Destroy(b.gameObject);
        foreach (var p in FindObjectsByType<APickable>())
            Destroy(p.gameObject);
    }

    IEnumerator Capture(string directory)
    {
        float elapsed = Time.time - started;
        if (elapsed < nextFrame)
            yield break;
        yield return new WaitForEndOfFrame();
        var shot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(directory + "/" + frame.ToString("D4") + ".jpg", shot.EncodeToJPG(86));
        Destroy(shot);
        File.AppendAllText(directory + "/timing.csv", frame + "," + (Time.time - started).ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
        frame++;
        nextFrame = elapsed + 1f / 15;
    }

    string BeginClip(string name)
    {
        string directory = "Temp/PolishReview/" + name;
        Directory.CreateDirectory(directory);
        foreach (string f in Directory.GetFiles(directory, "????.jpg"))
            File.Delete(f);
        File.WriteAllText(directory + "/timing.csv", "frame,seconds\n");
        started = Time.time;
        nextFrame = 0;
        frame = 0;
        return directory;
    }

    DistrictEnemy Station(Type type, Vector3 position)
    {
        var e = DistrictEnemy.Spawn(type, position);
        foreach (var m in e.GetComponents<PatternMover>())
            m.enabled = false;
        return e;
    }

    IEnumerator RepeatedLanceScore()
    {
        float savedSpeed = GameRules.enemySpeed;
        foreach (bool fast in new[] { false, true })
        {
            LevelRun.Reset(LevelId.Thunder);
            if (fast)
                for (int loop = 0; loop < 8; loop++)
                {
                    LevelRun.Advance(LevelId.Fields);
                    LevelRun.Advance(LevelId.Snow);
                    LevelRun.Advance(LevelId.Thunder);
                }
            GameRules.enemySpeed = fast ? 1.2f : 1;
            float tempo = PatternMover.FlightTempo * GameRules.enemySpeed * LevelRun.EnemyTempoScale;
            var score = ThunderChoreography.Create(6).units.First(x => x.enemy == "ArcLancer").fireTimes;
            var lancer = Station(typeof(ArcLancer), new Vector3(-1.5f, 5, 1));
            yield return null;
            lancer.enabled = false;
            lancer.gameObject.AddComponent<FormationFlight>().enabled = false;
            typeof(ArcLancer).GetMethod("Attack", Hidden).Invoke(lancer, null);
            var first = FindObjectsByType<PulseLaser>().Single();
            var heading = first.Heading;
            string directory = BeginClip(fast ? "ScoredLanceFastLoop" : "ScoredLanceFirstLoop");
            float angularTravel = 0, quietestRelease = 1;
            bool observedCooling = false;
            while (Time.time - started < (score[1] - score[0]) / tempo)
            {
                angularTravel = Mathf.Max(angularTravel, Vector2.Angle(heading, first.Heading));
                if (!first.IsFiring && first.Charge >= 1 && first.Power > 0)
                {
                    observedCooling = true;
                    quietestRelease = Mathf.Min(quietestRelease, first.GetComponents<AudioSource>().Single(source => !source.loop).volume);
                }
                yield return Capture(directory);
                yield return null;
            }
            Check(!first.IsAttachedTo(lancer.Hull), "lance finishes cooling before the next authored beat at tempo " + tempo.ToString("F2"));
            Check(angularTravel > 15 && angularTravel < 22, "lance preserves its sweep arc at tempo " + tempo.ToString("F2"));
            Check(observedCooling && quietestRelease < .02f, "release audio fades before pooling at tempo " + tempo.ToString("F2"));
            typeof(ArcLancer).GetMethod("Attack", Hidden).Invoke(lancer, null);
            var second = FindObjectsByType<PulseLaser>().Single();
            Check(second.IsAttachedTo(lancer.Hull) && second.Charge < .1f, "the next scored lance actually launches at tempo " + tempo.ToString("F2"));
            yield return new WaitForSeconds(1.85f / tempo);
            Clear();
            yield return null;
        }
        GameRules.enemySpeed = savedSpeed;
        LevelRun.Reset(LevelId.Thunder);
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset();
        Time.timeScale = 1;
        if (SceneManager.GetActiveScene().name != "L1")
            yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSecondsRealtime(1.2f);
        Time.timeScale = 1;
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.health = ship.maxHealth = 10000;
        flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        keyboard = InputSystem.AddDevice<Keyboard>();
        Clear();
        yield return null;
        yield return HudStartupCapture.Run();
        checks.Add(File.ReadAllText("Temp/HudStartupReview/result.txt").Trim());
        var shader = Resources.Load<Shader>("ContinuousBeam");
        Check(shader != null && !UnityEditor.ShaderUtil.ShaderHasError(shader), "continuous beam shader is included and compiles");
        foreach (string clip in new[] { "charge", "ignite", "sustain", "release" })
        {
            var sound = Resources.Load<AudioClip>("Sounds/Beam/" + clip);
            Check(sound != null && sound.loadState == AudioDataLoadState.Loaded && sound.length > .1f, "beam " + clip + " imports with audible duration");
        }
        BGMManager.StopBGM();
        LevelRun.Reset(LevelId.Thunder);
        LevelEnvironment.Apply();
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.8f, -1);
        var drone = Station(typeof(BeamDrone), new Vector3(0, 5, 1));
        yield return null;
        drone.enabled = false;
        var laser = CombatRound.Launch<PulseLaser>(drone.transform.position, Vector2.down, 0);
        laser.Bind(drone.Hull, new Vector3(0, -.28f, -.24f));
        laser.warning = .9f;
        laser.activeTime = 1.2f;
        float health = ship.health;
        bool fired = false, heard = false;
        var samples = new float[512];
        float peak = 0;
        string directory = BeginClip("MovingLaser");
        while (Time.time - started < 2.5f)
        {
            float t = Time.time - started;
            drone.transform.position = new Vector3(Mathf.Sin(t * 2) * 1.2f, 5, 1);
            if (t < .75f && ship.health != health)
                throw new Exception("moving laser telegraph damaged the player");
            if (laser.IsFiring)
            {
                fired = true;
                heard |= laser.GetComponent<LaserSound>().Sustaining;
            }
            foreach (var source in laser.GetComponents<AudioSource>())
            {
                source.GetOutputData(samples, 0);
                foreach (float sample in samples)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            yield return Capture(directory);
            yield return null;
        }
        Check(fired && heard && peak > .0001f, "laser sustains real source audio during active fire (peak " + peak.ToString("F5") + ")");
        Clear();
        yield return null;
        Check(LaserSound.SustainingCount == 0, "pooled laser cleanup stops sustained audio");

        // Actual damage lifecycle, with the player deliberately on the firing axis.
        ship.transform.position = new Vector3(0, -4, -1);
        laser = CombatRound.Launch<PulseLaser>(new Vector3(0, 4), Vector2.down, 0);
        laser.activeTime = .9f;
        health = ship.health;
        yield return new WaitForSeconds(.7f);
        Check(ship.health == health, "warning beam cannot damage the player");
        yield return new WaitForSeconds(.5f);
        Check(ship.health < health, "energized beam damages on its visible centerline");
        laser.Vanish();
        yield return null;
        Check(LaserSound.SustainingCount == 0, "explicit beam cancellation stops its sound");

        var lancer = Station(typeof(ArcLancer), new Vector3(-1.5f, 5, 1));
        yield return null;
        lancer.enabled = false;
        typeof(ArcLancer).GetMethod("Attack", Hidden).Invoke(lancer, null);
        directory = BeginClip("LancerSweep");
        while (Time.time - started < 2.2f)
        {
            yield return Capture(directory);
            yield return null;
        }
        Clear();
        yield return null;
        var boss = Station(typeof(ThunderLanceBoss), new Vector3(0, 6, 1));
        yield return null;
        boss.damage(boss.health * .55f);
        float rotorDeadline = Time.time + 8;
        while (((ThunderLanceBoss)boss).RotorCount != 6 && Time.time < rotorDeadline)
            yield return null;
        boss.enabled = false;
        typeof(ThunderLanceBoss).GetMethod("Attack", Hidden).Invoke(boss, null);
        directory = BeginClip("BossArray");
        int peakLoops = 0;
        while (Time.time - started < 5.3f)
        {
            peakLoops = Mathf.Max(peakLoops, LaserSound.SustainingCount);
            yield return Capture(directory);
            yield return null;
        }
        Check(peakLoops == 6, "six front spokes sustain permanent beams with bounded overlapping audio");
        Clear();
        yield return null;

        yield return RepeatedLanceScore();
        foreach (var type in new[]{typeof(SiegeGunship),typeof(StormReactor),typeof(SnowSiegeCraft),typeof(FieldsHeavyWing),
            typeof(TunnelDefensePlatform),typeof(CityCommandCarrier),typeof(PatrolGunship),typeof(WallTurret),typeof(RelayDrone)})
        {
            var level = type == typeof(SiegeGunship) || type == typeof(SnowSiegeCraft) ? LevelId.Snow :
                type == typeof(FieldsHeavyWing) || type == typeof(PatrolGunship) ? LevelId.Fields :
                type == typeof(TunnelDefensePlatform) || type == typeof(WallTurret) ? LevelId.Tunnel :
                type == typeof(CityCommandCarrier) ? LevelId.City : LevelId.Thunder;
            LevelRun.Reset(level);
            LevelEnvironment.Apply();
            var platform = Station(type, new Vector3(0, 5, 1));
            yield return null;
            platform.enabled = false;
            typeof(DistrictEnemy).GetField("volley", Hidden).SetValue(platform, 1);
            type.GetMethod("Attack", Hidden).Invoke(platform, null);
            directory = BeginClip("ModelAttack" + type.Name);
            bool second = false;
            while (Time.time - started < 3.2f)
            {
                if (!second && Time.time - started > 1.6f)
                {
                    second = true;
                    typeof(DistrictEnemy).GetField("volley", Hidden).SetValue(platform, 2);
                    type.GetMethod("Attack", Hidden).Invoke(platform, null);
                }
                yield return Capture(directory);
                yield return null;
            }
            Clear();
            yield return null;
        }
        var loadout = ship.GetComponent<ShipLoadout>();
        loadout.EquipWeapon("BeamEmitter", 0);
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z));
        directory = BeginClip("PlayerBeam");
        while (Time.time - started < 3.4f)
        {
            yield return Capture(directory);
            yield return null;
        }
        loadout.Weapons[0].StopFiring();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        Check(LaserSound.SustainingCount == 0, "stopping the player beam removes its audio and visual cycle");
        Destroy(loadout.Weapons[0].gameObject);
        var stock = new GameObject("Stock cannon review");
        stock.transform.SetParent(ship.transform, false);
        var cannon = stock.AddComponent<BaseWeapon>();
        cannon.SetWeaponInput(AWeapon.WeaponInput.primary);
        loadout.Weapons[0] = cannon;
        yield return null;
        foreach (var level in new[] { LevelId.Fields, LevelId.Snow, LevelId.Thunder, LevelId.Tunnel, LevelId.City })
        {
            LevelRun.Reset(level);
            LevelEnvironment.Apply();
            ship.transform.position = new Vector3(0, Camera.DownLimit + 1.8f, -1);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z));
            directory = BeginClip("PlayerRounds" + level);
            while (Time.time - started < 2.3f)
            {
                yield return Capture(directory);
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            loadout.Weapons[0].StopFiring();
            Clear();
            yield return null;
        }
        foreach (var level in new[] { LevelId.Snow, LevelId.Thunder })
        {
            LevelRun.Reset(level);
            LevelEnvironment.Apply();
            foreach (string weapon in new[] { "NeedleCannon", "BurstLauncher", "DiscThrower", "FlameProjector", "SwarmRack" })
            {
                loadout.EquipWeapon(weapon, 0);
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Z));
                directory = BeginClip("Weapon" + weapon + level);
                while (Time.time - started < 2.7f)
                {
                    yield return Capture(directory);
                    yield return null;
                }
                Check(PlayerRound.LiveCount > 0, weapon + " produces visible projectiles on " + level);
                loadout.Weapons[0].StopFiring();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                Clear();
                yield return null;
            }
        }
        LevelRun.Reset(LevelId.Snow);
        LevelEnvironment.Apply();
        foreach (var type in new[] { typeof(AvalancheBomber), typeof(ThunderLanceBoss) })
        {
            var victim = Station(type, new Vector3(0, 3, 1));
            yield return null;
            victim.enabled = false;
            directory = BeginClip("Breakup" + type.Name);
            bool broken = false, seen = false;
            while (Time.time - started < 2.2f)
            {
                if (!broken && Time.time - started > .25f)
                {
                    PSXImpactEffects.Explosion(victim.gameObject);
                    Destroy(victim.gameObject);
                    broken = true;
                }
                seen |= FindObjectsByType<PSXPolygonDebris>().Length > 0;
                yield return Capture(directory);
                yield return null;
            }
            Check(seen && FindObjectsByType<PSXPolygonDebris>().Length == 0, type.Name + " creates independent hull debris and cleans it up");
            Clear();
            yield return null;
        }
        Check(errors.Count == 0, "all combined visual phases ran without runtime exceptions");
    }
}
#endif
