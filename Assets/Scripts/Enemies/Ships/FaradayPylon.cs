using System;
using System.Collections.Generic;
using UnityEngine;

// Opposite-wall emitters leave a narrow doorway in a single sweeping horizontal bar.
[UnityEngine.Scripting.Preserve]
public sealed class FaradayPylon : DistrictEnemy
{
    sealed class Pair
    {
        readonly FaradayPylon leader, follower;
        readonly int order;
        readonly float door, chargeSeconds;
        bool charging, opened, retired;

        public Pair(FaradayPylon leader, FaradayPylon follower)
        {
            this.leader = leader;
            this.follower = follower;
            order = ++nextOrder;
            door = leader.door * Camera.RightLimit;
            chargeSeconds = (Difficulty.Easy ? .75f : .62f) / (WaveComposer.Tempo * leader.MovementScale);
        }

        public void Tick(FaradayPylon caller)
        {
            if (retired || caller != (Live(leader) ? leader : follower))
                return;
            float y = caller.transform.position.y;
            if (!charging)
            {
                // Start the tell only after both wall emitters have entered the visible arena.
                if (y > Camera.UpLimit - .65f || y < Camera.DownLimit + .65f)
                    return;
                charging = true;
                if (Live(leader)) leader.ChargeBar(door, chargeSeconds);
                if (Live(follower)) follower.ChargeBar(door, chargeSeconds);
                return;
            }
            if (y > Camera.UpLimit + .1f || y < Camera.DownLimit - .1f)
            {
                Retire();
                return;
            }
            if (opened)
                return;
            if (order <= lastOpened || (!leader.HasBar && !follower.HasBar))
            {
                Retire();
                return;
            }
            if (!leader.BarReady || !follower.BarReady || !SimulatorFlight.EnemyReleaseOpen)
                return;

            // The incoming warning can overlap the old bar. Its damage starts only
            // after both halves of the outgoing bar have been turned off.
            activePair?.Retire();
            activePair = this;
            lastOpened = order;
            opened = true;
            if (leader.HasBar) leader.bar.ReleaseHold();
            if (follower.HasBar) follower.bar.ReleaseHold();
        }

        void Retire()
        {
            retired = true;
            if (leader.HasBar) leader.bar.Cease();
            if (follower.HasBar) follower.bar.Cease();
            if (activePair == this) activePair = null;
        }

        public void LostMember()
        {
            if (!Live(leader) && !Live(follower) && activePair == this)
                activePair = null;
        }

        static bool Live(FaradayPylon pylon) => pylon != null && pylon.isActiveAndEnabled && pylon.CanReceiveDamage;
    }

    static Pair activePair;
    static int nextOrder, lastOpened;
    Pair pair;
    float door;
    PulseLaser bar;
    SpriteRenderer glow;
    bool HasBar => this != null && bar != null && bar.IsAttachedTo(Hull);
    bool BarReady => !HasBar || bar.ChargeReady;
    float Inward => transform.position.x < 0 ? 1 : -1;
    protected override Vector2 CueAxis => new Vector2(Inward, 0);
    static float DoorWidth => Difficulty.Easy ? 2.25f : Difficulty.Hard ? 1.55f : 1.85f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPairs()
    {
        activePair = null;
        nextOrder = lastOpened = 0;
    }

    protected override void Configure()
    {
        hullScale = .55f;
        health = 40;
        Mover<DriftDownPattern>();
    }

    public override void FlyPhrase(WaveUnit unit)
    {
        base.FlyPhrase(unit);
        door = unit.faradayDoor;
    }

    protected override void Start()
    {
        base.Start();
        glow = ItemSpark.Sprite(Hull, "Capacitor glow", "standard", LevelProjectilePalette.Accent(LevelRun.Current), .3f);
        glow.transform.localPosition = new Vector3(0, .06f, -.42f);
        if (pair != null)
            return;

        var twin = (FaradayPylon)Spawn(typeof(FaradayPylon), new Vector3(-transform.position.x, transform.position.y, transform.position.z));
        pair = twin.pair = new Pair(this, twin);
        ShareEncounterWith(twin);
        var mirror = twin.gameObject.AddComponent<PylonMirror>();
        mirror.Leader = transform;
        twin.UseMovement(mirror);
    }

    protected override void Update()
    {
        if (CanReceiveDamage && Time.deltaTime > 0 && GameClock.ActivePlay && !FrozenHull.Holds(gameObject))
            pair?.Tick(this);
    }

    protected override void OnDisable()
    {
        if (HasBar) bar.Cease();
        pair?.LostMember();
        base.OnDisable();
    }

    protected override void Attack() { }

    void ChargeBar(float center, float chargeSeconds)
    {
        var local = EnemyWeaponLayout.Ports(hull)[Inward > 0 ? 1 : 0];
        var origin = Hull.TransformPoint(local);
        float edge = center - Inward * DoorWidth * .5f;
        bar = Fire<PulseLaser>(origin, new Vector2(Inward, 0), 0);
        if (bar == null)
            return;
        bar.Bind(Hull, local).Sustain();
        bar.HoldForRelease(chargeSeconds);
        bar.width = .2f;
        bar.length = Mathf.Max(.5f, (edge - origin.x) * Inward);
    }

    void LateUpdate()
    {
        if (glow == null)
            return;
        bool live = HasBar;
        float flicker = .5f + .5f * Mathf.Sin(Time.time * 31) * Mathf.Sin(Time.time * 17);
        float charge = live ? bar.Charge : 0, power = live ? bar.Power : 0;
        glow.transform.localScale = Vector3.one * (.24f + .08f * flicker + .16f * charge + .1f * power * flicker);
    }
}

// Hold the same height as the other emitter, including when the route changes direction.
// A surviving follower keeps travelling after the opposite hull is destroyed.
public sealed class PylonMirror : PatternMover
{
    public Transform Leader;
    Vector3 travel;
    protected override void StepPattern(float dt)
    {
        if (GameRules.playerDisabled)
            return;
        if (Leader == null)
        {
            if (travel.sqrMagnitude < 1) travel = Vector3.down * 4;
            transform.position += travel * dt;
            if (transform.position.y > Camera.UpLimit + 1.5f || transform.position.y < Camera.DownLimit - 1.5f)
                GetComponent<AEnemy>().Depart();
            return;
        }
        var target = new Vector3(-Leader.position.x, Leader.position.y, transform.position.z);
        if (dt > 0 && Mathf.Abs(target.y - transform.position.y) > .0001f)
            travel = new Vector3(0, (target.y - transform.position.y) / dt, 0);
        transform.position = target;
    }
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve FaradayDescent = Route("Thunder / Faraday descent", Vector2.right, -.93f, 1.18f, -.93f, .9f, -.93f, .62f, -.93f, .34f, -.93f, .06f, -.93f, -.22f);
    static readonly FlightCurve FaradayClimb = Route("Thunder / Faraday climb", Vector2.right, -.93f, -.22f, -.93f, .06f, -.93f, .34f, -.93f, .62f, -.93f, .9f, -.93f, 1.18f);
    static readonly FlightCurve FaradayReturn = Route("Thunder / Faraday return", Vector2.right, -.93f, 1.18f, -.93f, .82f, -.93f, .43f, -.93f, .18f, -.93f, .43f, -.93f, .82f, -.93f, 1.18f);
    static readonly float[] FaradayDoors = { -.26f, .02f, .28f, .11f, -.15f, -.37f };

    public static readonly WavePart[] FaradayPylonParts =
    {
        new WavePart("Faraday gate", nameof(FaradayPylon), FaradayDescent, WaveShape.Train, 2, 5.2f, 3),
        new WavePart("Faraday ladder", nameof(FaradayPylon), FaradayClimb, WaveShape.Train, 3, 5.35f, 8),
        new WavePart("Faraday downstroke", nameof(FaradayPylon), FaradayDescent, WaveShape.Train, 3, 5.2f, 14),
        new WavePart("Faraday updraft", nameof(FaradayPylon), FaradayClimb, WaveShape.Solo, 1, 5.2f, 4),
        new WavePart("Faraday return", nameof(FaradayPylon), FaradayDescent, WaveShape.Train, 3, 5.4f, 20),
    };

    internal static WavePart FaradayForBudget(int budget) => budget switch
    {
        3 => FaradayPylonParts[0],
        8 => FaradayPylonParts[1],
        14 => FaradayPylonParts[2],
        20 => FaradayPylonParts[4],
        _ => null
    };

    internal static List<WaveUnit> GenerateFaraday(WavePart part, int variant, float side)
    {
        var units = new List<WaveUnit>(part.Count);
        int firstDoor = variant / 2 % FaradayDoors.Length;
        for (int i = 0; i < part.Count; i++)
        {
            units.Add(new WaveUnit(part.Enemy, i * part.Seconds * .53f, Vector3.zero)
            {
                part = part.Name,
                phrase = FlightPhrase.Flow,
                curve = part == FaradayPylonParts[4] && i == part.Count - 1 ? FaradayReturn : part.Route,
                side = side,
                flightDuration = part.Seconds,
                faradayDoor = FaradayDoors[(firstDoor + i) % FaradayDoors.Length] * side
            });
        }
        float[] unusedAccents = Array.Empty<float>();
        EnemyTuning.TuneFormation(part.Enemy, units, part.Seconds, ref unusedAccents);
        return units;
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedFaradayPylon;

    // A double-ended Tesla pylon: the boss drones' capacitor body and dark dome on a column
    // of insulator discs, with a coiled emitter on each flank, so either flank can face the
    // arena from its wall.
    public static Mesh FaradayPylonMesh()
    {
        if (cachedFaradayPylon != null)
            return cachedFaradayPylon;
        var b = new Builder();
        b.Drum(Vector3.zero, .42f, .34f, 13, 8, 6);
        b.Drum(new Vector3(0, 0, -.2f), .3f, .08f, 1, 8);
        b.Ball(new Vector3(0, .06f, -.3f), new Vector3(.26f, .26f, .16f), 9);
        foreach (int s in new[] { -1, 1 })
        {
            // Insulator stack and cap above and below the body.
            for (int i = 0; i < 3; i++)
                b.Drum(new Vector3(0, s * (.5f + i * .16f), -.05f), .2f - i * .03f, .08f, 10, 8);
            b.Ball(new Vector3(0, s * .98f, -.1f), new Vector3(.2f, .2f, .14f), 9);
            // Coiled emitter on each flank.
            b.Box(new Vector3(s * .62f, 0, -.1f), new Vector3(.56f, .15f, .15f), 11);
            for (int i = 0; i < 3; i++)
                b.Drum(new Vector3(s * (.47f + i * .15f), 0, -.12f), .15f, .09f, 10, 8);
            b.Drum(new Vector3(s * .93f, 0, -.12f), .11f, .1f, 9, 8);
            // Lightning-bolt fins between the column and the emitters.
            b.Prism(new[] { V(s * .26f, .3f), V(s * .62f, .62f), V(s * .46f, .36f), V(s * .72f, .3f), V(s * .4f, .16f) }, .08f, 6, new Vector3(0, 0, -.05f), 1);
            b.Prism(new[] { V(s * .4f, -.16f), V(s * .72f, -.3f), V(s * .46f, -.36f), V(s * .62f, -.62f), V(s * .26f, -.3f) }, .08f, 6, new Vector3(0, 0, -.05f), 1);
        }
        return cachedFaradayPylon = b.Finish("Faraday pylon double-ended emitter");
    }
}
