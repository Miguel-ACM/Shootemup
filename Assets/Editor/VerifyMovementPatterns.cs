using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic behavior checks; does not open, save or alter a scene.</summary>
public static class VerifyMovementPatterns
{
    private static readonly List<GameObject> objects = new List<GameObject>();
    private static int checks;

    [MenuItem("Tools/Enemy Movement/Verify patterns")]
    public static void Run()
    {
        float originalSpeed = GameRules.enemySpeed;
        var randomState = UnityEngine.Random.state;
        try
        {
            GameRules.enemySpeed = 1;
            UnityEngine.Random.InitState(1729);
            var player = Make("Movement verification player", new Vector3(0, -6, 0));
            player.tag = "Player";
            Type[] types = { typeof(TopTracker), typeof(Zigzag), typeof(Slalom), typeof(FreeReposition),
                typeof(StrafeAndBrake), typeof(CommittedDive), typeof(EscortOrbit), typeof(CompressAndExpand),
                typeof(AlternatingAdvance), typeof(Reform) };
            foreach (int fps in new[] { 30, 60, 144 })
                foreach (Type type in types)
                {
                    var go = Make(type.Name, new Vector3(0, 6, 7));
                    var mover = (PatternMover)go.AddComponent(type);
                    Vector3 previous = go.transform.position;
                    for (int frame = 0; frame < fps * 12; frame++)
                    {
                        mover.Step(1f / fps);
                        Vector3 position = go.transform.position;
                        Check(float.IsFinite(position.x) && float.IsFinite(position.y) && position.z == 7, type.Name + " invalid position/depth");
                        Check(Vector3.Distance(previous, position) <= 30f / fps, type.Name + " snapped at " + fps + " FPS");
                        previous = position;
                    }
                    GameRules.enemySpeed = 0;
                    mover.Step(1);
                    Check(go.transform.position == previous, type.Name + " ignores speed pause");
                    GameRules.enemySpeed = 1;
                    mover.SetMove(false);
                    mover.Step(1);
                    Check(go.transform.position == previous, type.Name + " ignores SetMove");
                    mover.SetMove(true);
                    Tick(mover, 31);
                    Check(mover.IsLeaving && !mover.CanFire, type.Name + " has no bounded exit");
                    UnityEngine.Object.DestroyImmediate(go);
                }
            CheckTracker(player.transform);
            CheckDive(player.transform);
            CheckReservations();
            CheckOrbit();
            CheckAlternation();
            CheckReform();
            CheckShootingGate();
            string result = "PASS: " + checks + " checks; all ten patterns at 30/60/144 FPS; continuous positions, preserved depth, pause, lifetime exits, locked aim/dive, reservations, leader loss, alternating partners, formation loss and shooter gating.";
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/MovementPatternsVerification.txt", result);
            Debug.Log(result);
        }
        finally
        {
            GameRules.enemySpeed = originalSpeed;
            UnityEngine.Random.state = randomState;
            foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            objects.Clear();
            checks = 0;
        }
    }

    private static void CheckTracker(Transform player)
    {
        var go = Make("Tracker lock", new Vector3(0, 5.87f, 7));
        var probe = go.AddComponent<MovementProbeShooter>();
        var mover = go.AddComponent<TopTracker>();
        Tick(mover, 2.2f);
        Check(probe.MovementAimPoint.HasValue, "Tracker did not lock its aim");
        var lockedAim = probe.MovementAimPoint;
        Vector3 lockedPosition = go.transform.position;
        player.position += Vector3.right * 3;
        Tick(mover, .7f);
        Check(go.transform.position == lockedPosition && probe.MovementAimPoint == lockedAim, "Tracker followed player during lock/fire");
        Check(mover.CanFire, "Tracker never opened its firing window");
        Tick(mover, 2);
        Check(!mover.CanFire, "Tracker fired through recovery");
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static void CheckDive(Transform player)
    {
        var go = Make("Dive lock", new Vector3(0, 5.87f, 7));
        var mover = go.AddComponent<CommittedDive>();
        mover.Step(.01f);
        var target = mover.CommittedTarget;
        player.position += Vector3.left * 6;
        Tick(mover, 1.2f);
        var first = go.transform.position;
        mover.Step(.1f);
        Vector2 actualDirection = (go.transform.position - first).normalized;
        Check(mover.CommittedTarget == target, "Dive retargeted during windup");
        Check(Vector2.Dot(actualDirection, ((Vector2)(target - new Vector3(0, 5.87f, 7))).normalized) > .999f, "Dive changed heading");
        Check(!mover.CanFire, "Diver fired during movement");
        UnityEngine.Object.DestroyImmediate(go);
    }

    private static void CheckReservations()
    {
        var a = Make("Reposition A", new Vector3(-3, 6, 7)).AddComponent<FreeReposition>();
        var b = Make("Reposition B", new Vector3(3, 6, 7)).AddComponent<FreeReposition>();
        for (int i = 0; i < 600; i++)
        {
            a.Step(1f / 60); b.Step(1f / 60);
            Check(Vector2.Distance(a.Destination, b.Destination) >= 1.19f, "Repositioners selected conflicting destinations");
        }
        Check(a.transform.position.y >= 2 && b.transform.position.y >= 2, "Repositioner escaped upper region");
        UnityEngine.Object.DestroyImmediate(a.gameObject);
        UnityEngine.Object.DestroyImmediate(b.gameObject);
    }

    private static void CheckOrbit()
    {
        var anchor = Make("Orbit leader", new Vector3(0, 4, 7));
        var mover = Make("Escort", new Vector3(1.5f, 4, 7)).AddComponent<EscortOrbit>();
        mover.SetAnchor(anchor.transform);
        Tick(mover, 2);
        Check(Mathf.Abs(Vector2.Distance(mover.transform.position, anchor.transform.position) - 1.5f) < .03f, "Escort lost orbit radius");
        UnityEngine.Object.DestroyImmediate(anchor);
        Vector3 before = mover.transform.position;
        mover.Step(.1f);
        Vector3 delta = mover.transform.position - before;
        mover.Step(.1f);
        Check(Vector3.Distance(mover.transform.position - before, delta * 2) < .001f && !mover.CanFire, "Escort did not peel on a tangent");
        UnityEngine.Object.DestroyImmediate(mover.gameObject);
    }

    private static void CheckAlternation()
    {
        var a = Make("Advance A", new Vector3(-1, 6, 7)).AddComponent<AlternatingAdvance>();
        var b = Make("Advance B", new Vector3(1, 6, 7)).AddComponent<AlternatingAdvance>();
        a.SetPartner(b, true); b.SetPartner(a, false);
        bool sawA = false, sawB = false;
        for (int i = 0; i < 600; i++)
        {
            a.Step(1f / 60); b.Step(1f / 60);
            sawA |= a.IsAdvancing; sawB |= b.IsAdvancing;
            Check(!(a.IsAdvancing && b.IsAdvancing), "Partners advanced simultaneously");
            Check(!(a.IsAdvancing && a.CanFire) && !(b.IsAdvancing && b.CanFire), "Partner fired while advancing");
        }
        Check(sawA && sawB, "Pair never handed over movement");
        UnityEngine.Object.DestroyImmediate(b.gameObject);
        float before = a.transform.position.y;
        Tick(a, 4);
        Check(a.transform.position.y < before, "Survivor stalled after losing partner");
        UnityEngine.Object.DestroyImmediate(a.gameObject);
    }

    private static void CheckReform()
    {
        var anchor = Make("Formation anchor", new Vector3(0, 4, 7));
        var group = new Reform[3];
        for (int i = 0; i < 3; i++)
        {
            group[i] = Make("Formation member", new Vector3((i - 1) * 1.4f, 4, 7)).AddComponent<Reform>();
            Set(group[i], "formationAnchor", anchor.transform); Set(group[i], "slotOrder", i);
        }
        for (int i = 0; i < 180; i++) foreach (var mover in group) mover.Step(1f / 60);
        UnityEngine.Object.DestroyImmediate(group[1].gameObject);
        group[0].Step(.01f); group[2].Step(.01f);
        Check(!group[0].CanFire && !group[2].CanFire, "Formation fired during loss delay");
        for (int i = 0; i < 180; i++) { group[0].Step(1f / 60); group[2].Step(1f / 60); }
        Check(Mathf.Abs(group[0].transform.position.x + .7f) < .01f && Mathf.Abs(group[2].transform.position.x - .7f) < .01f,
            "Survivors did not close formation gaps");
        Check(group[0].CanFire && group[2].CanFire, "Reformed group never resumed firing");
        UnityEngine.Object.DestroyImmediate(group[0].gameObject);
        UnityEngine.Object.DestroyImmediate(group[2].gameObject);
        UnityEngine.Object.DestroyImmediate(anchor);
    }

    private static void CheckShootingGate()
    {
        var probe = Make("Gate probe", Vector3.zero).AddComponent<MovementProbeShooter>();
        var method = typeof(AShooter).GetMethod("GatedShoot", BindingFlags.Instance | BindingFlags.NonPublic);
        var sequence = (IEnumerator)method.Invoke(probe, null);
        probe.SetMovementFiringAllowed(false);
        for (int i = 0; i < 5; i++) sequence.MoveNext();
        Check(probe.Shots == 0, "Blocked shooter emitted a shot");
        probe.SetMovementFiringAllowed(true); sequence.MoveNext();
        Check(probe.Shots == 1, "Shooter did not resume");
        probe.SetMovementFiringAllowed(false);
        for (int i = 0; i < 5; i++) sequence.MoveNext();
        Check(probe.Shots == 1, "Shooter kept firing during relocation");
        (sequence as IDisposable)?.Dispose();
        UnityEngine.Object.DestroyImmediate(probe.gameObject);
    }

    private static GameObject Make(string name, Vector3 position)
    {
        var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        go.transform.position = position;
        objects.Add(go);
        return go;
    }
    private static void Tick(PatternMover mover, float seconds)
    { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) mover.Step(1f / 60); }
    private static void Set(object target, string field, object value)
    { target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
}

public class MovementProbeShooter : AShooter
{
    public int Shots { get; private set; }
    protected override IEnumerator Shoot() { while (true) { Shots++; yield return null; } }
}
