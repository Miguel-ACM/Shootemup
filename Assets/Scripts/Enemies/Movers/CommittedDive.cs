using UnityEngine;

public class CommittedDive : PatternMover
{
    [SerializeField, Min(0)] private float topOffset = 2f;
    [SerializeField, Min(.01f)] private float windupDuration = 1f;
    [SerializeField, Min(.1f)] private float diveSpeedMultiplier = 3f;
    private enum Phase { Enter, Windup, Dive }
    private Phase phase;
    private float clock;
    private Vector3 direction;
    public Vector3 CommittedTarget { get; private set; }

    protected override void StepPattern(float dt)
    {
        switch (phase)
        {
            case Phase.Enter:
                if (Travel(InBounds(new Vector3(origin.x, Camera.UpLimit - topOffset, origin.z)), dt))
                {
                    Arrive();
                    CommittedTarget = player != null ? player.position : transform.position + Vector3.down * 5;
                    CommittedTarget = new Vector3(CommittedTarget.x, CommittedTarget.y, origin.z);
                    direction = (CommittedTarget - transform.position).normalized;
                    if (direction.sqrMagnitude < .001f) direction = Vector3.down;
                    LockAim(CommittedTarget);
                    phase = Phase.Windup;
                }
                break;
            case Phase.Windup:
                clock += dt;
                if (clock >= windupDuration) phase = Phase.Dive;
                break;
            case Phase.Dive:
                transform.position += direction * moveSpeed * diveSpeedMultiplier * dt;
                Destination = transform.position + direction;
                if (transform.position.y < Camera.DownLimit - 2 || transform.position.y > Camera.UpLimit + 2 ||
                    transform.position.x < Camera.LeftLimit - 2 || transform.position.x > Camera.RightLimit + 2) Leave();
                break;
        }
    }
}
