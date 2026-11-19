using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class WitnessBoss : ArmoredHullBoss
{
    static readonly Vector2[] silhouette = {
        new(0,1.13f), new(.4f,.74f), new(.78f,.47f), new(.64f,-.20f), new(.5f,-.78f),
        new(.96f,-1.34f), new(.72f,-1.96f), new(.42f,-2.45f), new(.69f,-2.98f),
        new(.52f,-3.57f), new(0,-3.99f), new(-.52f,-3.57f), new(-.69f,-2.98f),
        new(-.42f,-2.45f), new(-.72f,-1.96f), new(-.96f,-1.34f), new(-.5f,-.78f),
        new(-.64f,-.20f), new(-.78f,.47f), new(-.4f,.74f)
    };
    static readonly Color Ink = new Color(.85f, .095f, .25f);
    readonly Transform[] seals = new Transform[4];
    readonly LineRenderer[] rims = new LineRenderer[4];
    readonly int[] lastFlashFrame = new int[4];
    float accent;
    WitnessSculpture figure;
    protected override Vector2[] Outline => silhouette;
    public override int PhaseCount => 4;
    protected override bool NextPhaseReady => HealthFraction <= PhaseEnd(CurrentPhase) + .000001f;
    protected override void Configure()
    {
        health = 4500;
        hullScale = 1.20f;
        attackInterval = .48f;
    }
    public override void damage(float amount)
    {
        if (Invulnerable)
            return;
        if (CurrentPhase < 4)
            amount = Mathf.Min(amount, Mathf.Max(0, health - initialHealth * PhaseEnd(CurrentPhase)) / Mathf.Max(.0001f, GameRules.enemyDamageReceived));
        if (amount > 0)
            base.damage(amount);
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled = false;
        figure = Hull.gameObject.AddComponent<WitnessSculpture>();
        figure.Assemble();
        for (int i = 0; i < 4; i++)
        {
            var seal = new GameObject("Detached seal");
            seal.transform.SetParent(transform, false);
            seals[i] = seal.transform;
            var ring = rims[i] = CombatVisuals.Line(seal.transform, Ink, .045f);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 8;
            for (int j = 0; j < 8; j++)
            {
                float a = j * Mathf.PI / 4, r = j % 2 == 0 ? .28f : .11f;
                ring.SetPosition(j, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
            }
            firingCue.PlacePort(i, Vector3.zero, true, false);
            firingCue.FollowPort(i, seal.transform);
            seal.SetActive(false);
        }
    }

    protected override void MoveBoss(float dt)
    {
        station = new Vector3(0, Camera.UpLimit - 3.05f, 1);
        if (!openingComplete)
        {
            transform.position = Vector3.MoveTowards(transform.position, station, dt * 2.9f);
            return;
        }
        float t = motionAge;
        var mark = station + new Vector3(1.18f * Mathf.Sin(t * .68f), .4f * Mathf.Sin(t * 1.36f), 0);
        transform.position = Vector3.MoveTowards(transform.position, mark, dt * 3);
    }
    protected override void Update()
    {
        if (WorldTimeStop.Frozen)
            return;
        base.Update();
        if (GameRules.playerDisabled)
            return;
        accent = Mathf.MoveTowards(accent, 0, Time.deltaTime * 2.9f);
        figure.Pose(motionAge, CurrentPhase, accent);
        Hull.localRotation = Quaternion.Euler(3 * Mathf.Sin(motionAge), 8 * Mathf.Sin(motionAge * .64f), -5 * Mathf.Cos(motionAge * .68f));
        for (int i = 0; i < 4; i++)
        {
            bool visible = openingComplete && !PhaseTransitioning;
            seals[i].gameObject.SetActive(visible);
            if (!visible)
                continue;
            float a = motionAge * .54f + i * Mathf.PI * .5f;
            int h = (i % 2) * 2 - 1, row = i / 2;
            var pos = CurrentPhase switch
            {
                1 => new Vector2(h * (2.8f + .53f * Mathf.Sin(a)), Camera.UpLimit - 2.3f - row * 2.1f + .48f * Mathf.Cos(a)),
                2 => new Vector2(h * (Camera.RightLimit - .48f), Camera.UpLimit - 2.1f - row * 2.65f + 1.1f * Mathf.Sin(a)),
                3 => new Vector2(3.26f * Mathf.Cos(a), Camera.UpLimit - 3.8f + 1.76f * Mathf.Sin(a)),
                _ => new Vector2(3.72f * Mathf.Cos(a), Camera.UpLimit - 3.5f + 2.35f * Mathf.Sin(a))
            };
            seals[i].position = new Vector3(pos.x, pos.y, -.7f);
            rims[i].transform.localRotation = Quaternion.Euler(0, 0, h * motionAge * 36);
            rims[i].startWidth = rims[i].endWidth = .04f + .035f * accent;
        }
    }
    T Emit<T>(int port, float angle, float speed, float radius) where T : CombatRound
    {
        var p = seals[port].position;
        if (CombatVisuals.Player != null && Vector2.Distance(p, CombatVisuals.Player.position) < 1.0f)
            return null;
        UseTone(port % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
        var shot = Fire<T>(p, CombatVisuals.Rotate(Vector2.down, angle), speed, radius, false);
        if (shot != null)
        {
            shot.SetAccent(Ink);
            if (lastFlashFrame[port] != Time.frameCount)
            {
                firingCue.Flash(p, shot.Heading);
                lastFlashFrame[port] = Time.frameCount;
            }
        }
        return shot;
    }
    IEnumerator Accent(int first, int second, float span)
    {
        for (float t = 0; t < span;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(seals[first].position, span - t);
            Cue(seals[second].position, span - t);
            yield return null;
        }
        accent = 1;
    }
    protected override IEnumerator Cards()
    {
        for (int bar = 0; ; bar++)
        {
            int a = bar % 2, b = 3 - a;
            if (CurrentPhase != 2)
                yield return Accent(a, b, .40f);
            switch (CurrentPhase)
            {
                case 1:
                    yield return PetalCanon(a, b, bar);
                    break;
                case 2:
                    yield return Guillotine(bar);
                    break;
                case 3:
                    yield return Orrery(a, b, bar);
                    break;
                default:
                    yield return Eclipse(a, b, bar);
                    break;
            }
            yield return Wait(Rest(.34f));
        }
    }

    // Four-petalled flowers open in opposite directions from a diagonal pair.
    IEnumerator PetalCanon(int a, int b, int bar)
    {
        int n = Mathf.Max(12, Dense(40));
        for (int crown = 0; crown < 3; crown++)
        {
            for (int j = 0; j < n; j++)
            {
                float theta = 360f * j / n, speed = 3.2f + .96f * Mathf.Cos(theta * 4 * Mathf.Deg2Rad);
                float angle = theta + bar * 13 + crown * 8;
                Emit<SimpleRound>(a, angle, speed, .195f);
                Emit<ChevronRound>(b, -angle, speed, .18f);
            }
            yield return Wait(Rest(.30f));
        }
    }
    // Two side racks throw a shuttle across the chamber. After the pause, their
    // posted diagonals drop in opposite directions as a staggered herringbone.
    IEnumerator Guillotine(int bar)
    {
        int n = Mathf.Max(4, Dense(12));
        for (int k = 0; k < 4; k++)
        {
            var pair = k % 2 == 0 ? upperPair : lowerPair;
            yield return Accent(pair[0], pair[1], k == 0 ? .4f : .22f);
            for (int j = 0; j < n; j++)
            {
                float u = j / (n - 1f);
                foreach (int port in pair)
                {
                    int h = port % 2 == 0 ? 1 : -1;
                    Emit<NeedleRound>(port, h * (80 + k * 4), 2.0f + u * 5.0f, .19f)
                        .Pause(.85f, .40f + .26f * u, CombatRound.Turn.Down, h * (17 + 12 * Mathf.Sin(bar + k)), 1.18f).ShowTurn();
                }
            }
            yield return Wait(Rest(.25f));
        }
    }
    static readonly int[] upperPair = { 0, 1 }, lowerPair = { 2, 3 };
    // Satellite clocks walk down the screen before their stars escape tangentially.
    IEnumerator Orrery(int a, int b, int bar)
    {
        int n = Mathf.Max(8, Dense(19));
        for (int chord = 0; chord < 3; chord++)
        {
            for (int j = 0; j < n; j++)
            {
                float deg = 360f * j / n + bar * 19;
                Emit<SimpleRound>(a, 16, 1.45f, .19f)?.Orbit(1.05f, deg, 80, 7).Breathe(.24f, .55f).Sling(1.45f + chord * .24f, 1.42f);
                Emit<SimpleRound>(b, -16, 1.45f, .19f)?.Orbit(1.05f, -deg, -80, 7).Breathe(.24f, .55f).Sling(1.45f + chord * .24f, 1.42f);
                if (j % 3 == 0)
                {
                    Emit<StarRound>(a, deg, 3.2f, .22f);
                    Emit<StarRound>(b, -deg, 3.2f, .22f);
                }
            }
            yield return Wait(Rest(.68f));
        }
    }
    // Two nibs write a continuous rotating star. The opposite pair answers at
    // the crossings with crescents that the faster needles gradually overtake.
    IEnumerator Eclipse(int a, int b, int bar)
    {
        int beats = Mathf.Max(12, Dense(27));
        for (int k = 0; k < beats; k++)
        {
            float t = k / (float)(beats - 1), tilt = bar * 23 + 70 * Mathf.Sin(t * Mathf.PI * 1.5f);
            for (int arm = 0; arm < 5; arm++)
            {
                float deg = arm * 72 + tilt;
                Emit<NeedleRound>(a, deg, 2.55f, .18f).Ramp(1.48f, .85f, .7f);
                Emit<StarRound>(b, -deg, 3.15f, .205f);
            }
            if (k % 6 == 0)
                for (int j = 0; j < 9; j++)
                {
                    Emit<CrescentRound>(1 - a, j * 40 - tilt, 2.25f, .23f);
                    Emit<CrescentRound>(2 + a, j * 40 + tilt, 2.25f, .23f);
                }
            yield return Wait(Rest(2.7f / beats));
        }
    }
    protected override void Die()
    {
        if (figure != null) figure.ExtinguishAura();
        foreach (var seal in seals)
            if (seal != null)
                seal.gameObject.SetActive(false);
        base.Die();
    }
}
