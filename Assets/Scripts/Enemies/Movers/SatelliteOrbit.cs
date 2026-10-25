// Shared rotating escorts with a bounded peel-out when their carrier is destroyed.
using UnityEngine;

public sealed class SatelliteOrbit : PatternMover
{
    public Transform Anchor;
    public float Phase;
    protected override void StepPattern(float dt)
    {
        if(Anchor==null)
        {
            SetFiring(false);transform.position+=new Vector3(Mathf.Cos(Phase)*2.5f,5,0)*dt;
            if(transform.position.y>Camera.UpLimit+1.5f || Mathf.Abs(transform.position.x)>Camera.RightLimit+2)GetComponent<AEnemy>().Depart();
            return;
        }
        Phase+=dt*1.65f;
        var p=Anchor.position+new Vector3(Mathf.Cos(Phase)*1.18f,Mathf.Sin(Phase)*.83f,0);p.z=origin.z;transform.position=p;
        transform.rotation=Quaternion.Euler(0,0,Phase*Mathf.Rad2Deg);
        Arrive();SetFiring(p.y<Camera.UpLimit-1 && p.y>Camera.DownLimit+1 && Mathf.Abs(Mathf.Sin(Phase))>.35f);
    }
}
