using UnityEngine;

public class Zigzag : PatternMover
{
    [SerializeField, Min(.01f)] private float halfWidth = 1.8f;
    [SerializeField, Min(.01f)] private float turnDepth = 1.2f;
    [SerializeField] private bool startRight = true;
    [SerializeField, Min(1)] private int fireEveryTurns = 2;
    [SerializeField, Min(0)] private float firingWindow = .5f;
    private Vector3 waypoint;
    private int direction, turns;
    private float fireRemaining;
    private float centre;

    protected override void InitializePattern()
    {
        centre = InBounds(origin).x;
        direction = startRight ? 1 : -1;
        NextWaypoint();
    }

    private void NextWaypoint()
    {
        waypoint = new Vector3(InBounds(new Vector3(centre + direction * halfWidth, 0, origin.z)).x,
            transform.position.y - turnDepth, origin.z);
    }

    protected override void StepPattern(float dt)
    {
        fireRemaining = Mathf.Max(0, fireRemaining - dt);
        if (Travel(waypoint, dt))
        {
            Arrive();
            turns++;
            if (turns % Mathf.Max(1, fireEveryTurns) == 0) fireRemaining = firingWindow;
            direction = -direction;
            NextWaypoint();
        }
        SetFiring(fireRemaining > 0);
        if (transform.position.y < Camera.DownLimit - 1f) Leave();
    }
}
