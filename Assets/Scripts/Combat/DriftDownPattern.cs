using UnityEngine;
public sealed class DriftDownPattern : PatternMover
{
    protected override void StepPattern(float dt)
    {
        transform.position+=Vector3.down*dt*1.1f;
        if(transform.position.y<Camera.UpLimit-.5f){Arrive();SetFiring(true);}
        if(transform.position.y<Camera.DownLimit-1)Leave();
    }
}
