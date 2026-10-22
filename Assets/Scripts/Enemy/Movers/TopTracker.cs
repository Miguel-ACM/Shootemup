using UnityEngine;

public class TopTracker : PatternMover
{
    [SerializeField, Min(0)] private float topOffset = 2f;
    [SerializeField, Min(.01f)] private float trackingDuration = 2f;
    [SerializeField, Min(0)] private float lockWarning = .6f;
    [SerializeField, Min(.01f)] private float firingDuration = 1.5f;
    [SerializeField, Min(0)] private float recoveryDuration = .8f;
    private enum Phase { Enter, Track, Lock, Fire, Recover }
    private Phase phase;
    private float clock;

    protected override void StepPattern(float dt)
    {
        clock += dt;
        switch (phase)
        {
            case Phase.Enter:
                if (Travel(InBounds(new Vector3(origin.x, Camera.UpLimit - topOffset, origin.z)), dt))
                { Arrive(); Next(Phase.Track); }
                break;
            case Phase.Track:
                var target = transform.position;
                if (player != null) target.x = player.position.x;
                Travel(InBounds(target), dt);
                if (clock >= trackingDuration)
                {
                    LockAim(player != null ? player.position : transform.position + Vector3.down);
                    Next(Phase.Lock);
                }
                break;
            case Phase.Lock:
                if (clock >= lockWarning) { Next(Phase.Fire); SetFiring(true); }
                break;
            case Phase.Fire:
                if (clock >= firingDuration) { SetFiring(false); Next(Phase.Recover); }
                break;
            case Phase.Recover:
                if (clock >= recoveryDuration) { LockAim(null); Next(Phase.Track); }
                break;
        }
    }

    private void Next(Phase next) { phase = next; clock = 0; }
}
