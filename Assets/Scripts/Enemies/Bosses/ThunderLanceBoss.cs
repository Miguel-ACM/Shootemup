using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class ThunderLanceBoss : DistrictBoss
{
    static readonly Vector3[] BeamPorts = { new Vector3(-.71f, .435f, -.3f), new Vector3(.71f, .435f, -.3f), new Vector3(0, -1.98f, -.2f) };
    readonly PulseLaser[] beams = new PulseLaser[3];
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
}
