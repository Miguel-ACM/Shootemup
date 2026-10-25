using UnityEngine;

public class EscortOrbit : PatternMover
{
    [SerializeField] private Transform anchor;
    [SerializeField, Min(.1f)] private float radius = 1.5f;
    [SerializeField, Min(.1f)] private float orbitPeriod = 4f;
    [SerializeField, Range(0, 1)] private float phaseOffset;
    [SerializeField, Min(.1f)] private float peelSpeedMultiplier = 1.5f;
    private float angle;
    private Vector3 tangent;
    private bool peeling, joined;

    public void SetAnchor(Transform leader)
    {
        anchor = leader;
    }

    protected override void InitializePattern()
    {
        angle = phaseOffset * Mathf.PI * 2;
        tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
    }

    protected override void StepPattern(float dt)
    {
        if (anchor == null || !anchor.gameObject.activeInHierarchy)
        {
            peeling = true;
            SetFiring(false);
        }
        if (peeling)
        {
            transform.position += tangent * moveSpeed * peelSpeedMultiplier * dt;
            if (transform.position.y < Camera.DownLimit - 2 || transform.position.y > Camera.UpLimit + 2 ||
                transform.position.x < Camera.LeftLimit - 2 || transform.position.x > Camera.RightLimit + 2)
                Leave();
            return;
        }
        if (joined)
            angle += dt * Mathf.PI * 2 / Mathf.Max(.1f, orbitPeriod);
        var target = anchor.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        target.z = origin.z;
        tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
        if (Travel(target, dt))
        {
            joined = true;
            Arrive();
        }
        SetFiring(joined);
    }
}
