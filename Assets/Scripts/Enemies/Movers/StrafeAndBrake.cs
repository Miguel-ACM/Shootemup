using UnityEngine;

public class StrafeAndBrake : PatternMover
{
    [SerializeField] private Vector2 attackPosition = new Vector2(0, 4f);
    [SerializeField, Min(.1f)] private float brakingDistance = 2f;
    [SerializeField, Min(0)] private float settleDuration = .4f;
    [SerializeField, Min(.1f)] private float firingDuration = 2f;
    [SerializeField, Min(0)] private float recoveryDuration = .5f;
    [SerializeField, Min(.1f)] private float exitSpeedMultiplier = 1.5f;
    private enum Phase { Enter, Settle, Fire, Recover, Exit }
    private Phase phase;
    private float clock;
    private int exitDirection;

    protected override void InitializePattern()
    {
        Destination = InBounds(new Vector3(attackPosition.x, attackPosition.y, origin.z));
        exitDirection = origin.x <= Destination.x ? 1 : -1;
    }

    protected override void StepPattern(float dt)
    {
        clock += dt;
        switch (phase)
        {
            case Phase.Enter:
                float distance = Vector3.Distance(transform.position, Destination);
                float speed = Mathf.Max(.15f, Mathf.Clamp01(distance / Mathf.Max(.1f, brakingDistance)));
                if (Travel(Destination, dt, speed)) { Arrive(); Next(Phase.Settle); }
                break;
            case Phase.Settle:
                if (clock >= settleDuration) { SetFiring(true); Next(Phase.Fire); }
                break;
            case Phase.Fire:
                if (clock >= firingDuration) { SetFiring(false); Next(Phase.Recover); }
                break;
            case Phase.Recover:
                if (clock >= recoveryDuration) Next(Phase.Exit);
                break;
            case Phase.Exit:
                transform.position += Vector3.right * exitDirection * moveSpeed * exitSpeedMultiplier * dt;
                if (transform.position.x < Camera.LeftLimit - 1 || transform.position.x > Camera.RightLimit + 1) Leave();
                break;
        }
    }

    private void Next(Phase next) { phase = next; clock = 0; }
}
