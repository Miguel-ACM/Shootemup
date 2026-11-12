using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class ThunderLanceBoss : DistrictBoss
{
    static readonly Vector3[] BeamPorts = { new Vector3(-.71f, .435f, -.3f), new Vector3(.71f, .435f, -.3f), new Vector3(0, -1.98f, -.2f) };
    readonly PulseLaser[] beams = new PulseLaser[3];
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
        hullScale = 1.25f;
        health = 1700;
        attackInterval = (.82f);
        nextAttack = 1.8f;
    }

    protected override float AttackPace => 1;
    protected override Vector2 CueAxis => Vector2.down;
    protected override void MoveBoss(float dt)
    {
        // Follow the live star's lower opening, including its slow reversals and recharge.
        // Later phases retain their independent patrol above the marching lattice.
        station.x = !phaseTwo ? StarFiringLane() : RotorCount > 0 ? transform.position.x : Mathf.Sin(motionAge * .55f) * 1.9f;
        station.y = Camera.UpLimit - 2.4f;
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 3.3f);
    }

    float StarFiringLane()
    {
        var hub = new Vector2(0, Middle + .8f);
        // Intersect the two bordering beams with the lower flight area. Clipping the
        // opening to the screen keeps the hull reachable at either end of the swing.
        float row = Camera.DownLimit + 2;
        float Boundary(int index)
        {
            var heading = CombatVisuals.Rotate(Vector2.down, index * 60);
            var origin = hub + heading;
            if (drones.Count == 6 && drones[index] != null)
            {
                heading = drones[index].BeamHeading;
                origin = drones[index].BeamOrigin;
            }
            return origin.x + (row - origin.y) * heading.x / Mathf.Min(-.01f, heading.y);
        }
        float left = Mathf.Max(Camera.LeftLimit + .4f, Boundary(5) + .4f);
        float right = Mathf.Min(Camera.RightLimit - .4f, Boundary(0) - .4f);
        return Mathf.Clamp((left + right) * .5f, -1.9f, 1.9f);
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
