using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ThunderLanceBoss : DistrictBoss
{
    static readonly Vector3[] BeamPorts = { new Vector3(-.71f, .435f, -.3f), new Vector3(.71f, .435f, -.3f), new Vector3(0, -1.98f, -.2f) };
    readonly PulseLaser[] beams = new PulseLaser[3];
    Vector2 answerAxis = Vector2.down;
    [EnemyTunable("Phase 2 / Health threshold", .1f, .9f)] public float phaseTwoThreshold = .5f;
    [EnemyTunable("Laser / Charge", .8f, 3)] public float beamCharge = 1.2f;
    [EnemyTunable("Laser / Width", .1f, .4f)] public float beamWidth = .24f;
    public bool Centred => enteredFight;
    public int RotorCount
    {
        get
        {
            int n = 0;
            foreach (var beam in beams)
                if (beam != null && beam.IsAttachedTo(Hull))
                    n++;
            return n;
        }
    }
    protected override void Configure()
    {
        hull = 22;
        hullScale = 1.25f;
        health = 1250;
        attackInterval = (.82f);
        nextAttack = 1.8f;
    }
    protected override bool PhaseTwoReady => HealthFraction <= phaseTwoThreshold;
    protected override float AttackPace => 1;
    protected override Vector2 CueAxis => Vector2.down;
    protected override void MoveBoss(float dt)
    {
        // A laser owns a stationary lane through its entire charge and release.
        station.x = RotorCount > 0 ? transform.position.x : Mathf.Sin(motionAge * .55f) * 1.9f;
        station.y = Camera.UpLimit - 2.4f;
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 3.3f);
    }
    void Beam(int index)
    {
        if (beams[index] != null && beams[index].IsAttachedTo(Hull))
            return;
        var beam = Fire<PulseLaser>(Hull.TransformPoint(BeamPorts[index]), Vector2.down, 0);
        if (beam == null)
            return;
        beams[index] = beam;
        beam.Bind(Hull, BeamPorts[index]);
        beam.warning = beamCharge;
        beam.activeTime = (1.45f);
        beam.width = beamWidth * (index == 2 ? 1.25f : 1);
        beam.length = 24;
    }
    protected override void Attack()
    {
        int beat = (volley - 1) % 14;
        if (!phaseTwo)
        {
            if (beat == 0)
            {
                Beam(0);
                Beam(1);
            }
            else if (beat == 6)
                Beam(2);
            else if (beat == 3 || beat == 4 || beat == 9 || beat == 10)
            {
                if (beat == 3 || beat == 9)
                    answerAxis = Aim;
                PetalFan<NeedleRound>(Hull.TransformPoint(BeamPorts[2]), CombatVisuals.Rotate(answerAxis, beat % 2 == 0 ? 7 : -7), (7), 16, 4.2f, .2f);
            }
            else if (beat == 12)
                PetalFan<SimpleRound>(Hull.TransformPoint(BeamPorts[2]), Vector2.down, (9), 21, 1.9f, .31f, .2f);
            return;
        }
        // Three vertical lanes light in sequence, then mirrored slow side petals.
        if (beat == 0)
            Beam(0);
        else if (beat == 2)
            Beam(1);
        else if (beat == 4)
            Beam(2);
        else if (beat == 6 || beat == 7)
        {
            if (beat == 6)
                answerAxis = Aim;
            PetalFan<NeedleRound>(Hull.TransformPoint(BeamPorts[2]), CombatVisuals.Rotate(answerAxis, beat == 6 ? -7 : 7), (9), 13, 4.5f, .19f);
        }
        else if (beat == 10 || beat == 11)
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(BeamPorts[side < 0 ? 0 : 1]);
                PetalFan<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, side * (beat == 10 ? 29 : 43)), (5), 23, 2.1f, .3f, .2f);
            }
    }
    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 14;
        if (beat == 0)
        {
            Cue(Muzzle(0), seconds);
            if (!phaseTwo)
                Cue(Muzzle(1), seconds);
        }
        else if (phaseTwo && beat == 2)
            Cue(Muzzle(1), seconds);
        else if (phaseTwo && beat == 4 || !phaseTwo && beat == 6)
            Cue(Muzzle(2), seconds);
        else if (phaseTwo && (beat == 6 || beat == 7) || !phaseTwo && (beat == 3 || beat == 4 || beat == 9 || beat == 10 || beat == 12))
            Cue(Muzzle(2), seconds);
        else if (phaseTwo && (beat == 10 || beat == 11))
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
    }
    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1.5f;
    }
    void StopBeams()
    {
        foreach (var beam in beams)
            if (beam != null && beam.IsAttachedTo(Hull))
                beam.Cease();
    }
    protected override void Die()
    {
        StopBeams();
        base.Die();
    }
    void OnDestroy()
    {
        StopBeams();
    }
}
