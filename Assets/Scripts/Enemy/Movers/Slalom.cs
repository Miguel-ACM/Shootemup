using UnityEngine;

public class Slalom : PatternMover
{
    [SerializeField, Min(0)] private float amplitude = 1.6f;
    [SerializeField, Min(.1f)] private float period = 3f;
    [SerializeField, Range(0, 1)] private float phaseOffset;
    [SerializeField, Range(0, 1)] private float descentRatio = .5f;
    [SerializeField, Range(0, 1)] private float turnFiringThreshold = .8f;
    private float centre;

    protected override void InitializePattern()
    {
        // Offset the centre so any phase offset begins at the actual spawn position.
        centre = origin.x - amplitude * Mathf.Sin(phaseOffset * Mathf.PI * 2);
    }

    protected override void StepPattern(float dt)
    {
        float phase = (age / Mathf.Max(.1f, period) + phaseOffset) * Mathf.PI * 2;
        var p = transform.position;
        float desiredX = InBounds(new Vector3(centre + amplitude * Mathf.Sin(phase), 0, origin.z)).x;
        float horizontalSpeed = Mathf.Max(moveSpeed, amplitude * Mathf.PI * 2 / Mathf.Max(.1f, period));
        p.x = Mathf.MoveTowards(p.x, desiredX, horizontalSpeed * dt);
        p.y = origin.y - age * moveSpeed * descentRatio;
        transform.position = p;
        Destination = p;
        if (p.y <= Camera.UpLimit - edgeInset) Arrive();
        SetFiring(p.y <= Camera.UpLimit - edgeInset && Mathf.Abs(Mathf.Sin(phase)) >= turnFiringThreshold);
        if (p.y < Camera.DownLimit - 1f) Leave();
    }
}
