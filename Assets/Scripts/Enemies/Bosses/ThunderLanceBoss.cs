using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ThunderLanceBoss : DistrictBoss
{
    static readonly Vector3 FrontPort = new Vector3(0, -1.98f, -.2f);
    readonly PulseLaser[] rotor = new PulseLaser[6];
    Vector2 barrageAxis = Vector2.down;
    Vector3 barrageTarget;
    bool centred;
    [EnemyTunable("Phase 2 / Health threshold", .1f, .9f)] public float phaseTwoThreshold = .5f;
    [EnemyTunable("Phase 2 / Centre travel speed", 1, 8)] public float centreTravelSpeed = 3.8f;
    [EnemyTunable("Phase 2 / Laser charge", .8f, 3)] public float rotorCharge = 1.35f;
    [EnemyTunable("Phase 2 / Clockwise laser speed", 8, 35)] public float rotorSpeed = 18;
    [EnemyTunable("Phase 2 / Laser width", .1f, .32f)] public float rotorWidth = .18f;
    [EnemyTunable("Phase 2 / Firing pace", .4f, 1.5f)] public float firingPace = .8f;
    [EnemyTunable("Phase 2 / Aimed fan count", 1, 7)] public int aimedCount = 3;
    [EnemyTunable("Phase 2 / Aimed shot speed", 3, 9)] public float aimedSpeed = 5.6f;
    [EnemyTunable("Phase 2 / Outward fan count", 3, 9)] public int outwardCount = 5;
    [EnemyTunable("Phase 2 / Outward shot speed", 2, 7)] public float outwardSpeed = 3.9f;
    public bool Centred => centred;
    public int RotorCount
    {
        get
        {
            int count = 0;
            foreach (var beam in rotor)
                if (beam != null && beam.IsAttachedTo(Hull))
                    count++;
            return count;
        }
    }

    protected override void Configure()
    {
        hull = 22;
        hullScale = 1.25f;
        health = 1250;
        attackInterval = .55f;
    }

    protected override bool PhaseTwoReady => HealthFraction <= phaseTwoThreshold;
    protected override float AttackPace => phaseTwo ? firingPace : 1;
    protected override void MoveBoss(float dt)
    {
        if (!phaseTwo)
        {
            station.x = Mathf.Sin(motionAge * .7f) * 2.1f;
            base.MoveBoss(dt);
            return;
        }
        var centre = new Vector3(0, (Camera.UpLimit + Camera.DownLimit) * .5f, 1);
        // Centre the front muzzle, keeping the hull behind the rotating spokes.
        var destination = transform.position + centre - Hull.TransformPoint(FrontPort);
        transform.position = Vector3.MoveTowards(transform.position, destination, dt * centreTravelSpeed);
        if (Vector3.Distance(transform.position, destination) > .03f)
            return;
        transform.position = destination;
        if (!centred)
        {
            centred = true;
            volley = 0;
        }
        if (RotorCount != 6)
            StartRotor();
    }

    void StartRotor()
    {
        StopRotor();
        if (CombatRound.Live.Count + 6 > CombatRound.Limit)
            return;
        Vector3 port = Hull.TransformPoint(FrontPort);
        // A visible charge opens a sector around the player's current bearing.
        // All six spokes start together and rotate clockwise without timer resets.
        Vector2 axis = CombatVisuals.Aim(port);
        for (int i = 0; i < rotor.Length; i++)
        {
            var beam = Fire<PulseLaser>(port, CombatVisuals.Rotate(axis, 30 + i * 60), 0, .17f);
            if (beam == null)
                continue;
            beam.Bind(Hull, FrontPort).Sustain();
            beam.warning = rotorCharge;
            beam.width = rotorWidth;
            beam.sweep = -rotorSpeed;
            beam.reversalArc = 60;
            beam.length = 26;
            rotor[i] = beam;
        }
        nextAttack = age + rotorCharge + .45f;
    }

    void StopRotor()
    {
        for (int i = 0; i < rotor.Length; i++)
        {
            if (rotor[i] != null && rotor[i].IsAttachedTo(Hull))
                rotor[i].Cease();
            rotor[i] = null;
        }
    }

    protected override void PressureAttack()
    {
    }

    protected override void Attack()
    {
        if (!phaseTwo)
        {
            int beat = (volley - 1) % 12;
            if (beat == 0 || beat == 6)
            {
                int sign = beat == 0 ? 1 : -1;
                for (int side = -1; side <= 1; side += 2)
                {
                    var laser = Fire<PulseLaser>(transform.position, CombatVisuals.Rotate(Vector2.down, side * 40 - sign * 10), 0, .18f);
                    if (laser != null)
                    {
                        laser.Bind(Hull, new Vector3(side * .71f, .435f, -.3f));
                        laser.activeTime = 2;
                        laser.warning = 1.05f;
                        laser.sweep = sign * 12;
                        laser.width = .22f;
                    }
                }
            }
            else if (beat == 2 || beat == 3 || beat == 8 || beat == 9)
            {
                if (beat == 2 || beat == 8)
                    barrageAxis = Aim;
                PetalFan<NeedleRound>(Hull.TransformPoint(FrontPort), CombatVisuals.Rotate(barrageAxis, beat % 2 == 0 ? -2 : 2), 7, 8, 7, .13f);
            }
            return;
        }
        if (!centred || RotorCount != 6)
            return;
        int accent = (volley - 1) % 12;
        if (accent == 0)
            barrageTarget = player != null ? player.position : transform.position + Vector3.down * 6;
        if (accent < 3)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * .71f, .435f - accent * .47f, -.3f));
                var axis = ((Vector2)(barrageTarget - port)).normalized;
                PetalFan<NeedleRound>(port, CombatVisuals.Rotate(axis, (accent - 1) * 4), aimedCount, 9, aimedSpeed, .13f, .14f);
            }
        }
        else if (accent >= 5 && accent <= 7)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * .71f, .435f - (accent - 5) * .47f, -.3f));
                var axis = CombatVisuals.Rotate(Vector2.down, side * (38 + (accent - 6) * 10));
                PetalFan<SimpleRound>(port, axis, outwardCount, 18, outwardSpeed, .14f, .18f);
            }
        }
        else if (accent == 9)
        {
            // A late aimed answer catches parking in the rear sectors without filling every gap.
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * .71f, -.505f, -.3f));
                PetalFan<NeedleRound>(port, CombatVisuals.Aim(port), aimedCount, 12, aimedSpeed, .13f);
            }
        }
    }

    protected override void OnPhaseTwo()
    {
        foreach (var round in CombatRound.Live)
            if (round is PulseLaser beam && beam.IsAttachedTo(Hull))
                beam.Cease();
        centred = false;
        volley = 0;
        nextAttack = float.PositiveInfinity;
    }

    protected override void Die()
    {
        StopRotor();
        base.Die();
    }

    void OnDestroy()
    {
        StopRotor();
    }
}
