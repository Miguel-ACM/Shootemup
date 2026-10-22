using UnityEngine;

/// <summary>One continuous trajectory owns both the entrance and the repeating figure eight.</summary>
public class BossMove : AMover
{
    [SerializeField, Min(.01f)] private float lerpTime = 1.2f;
    [SerializeField] private Vector3 lerpPosition = new Vector3(0,4.92f,1);
    [SerializeField, Min(0)] private float horizontalAmplitude = 1.2f;
    [SerializeField, Min(0)] private float verticalAmplitude = 1f;
    [SerializeField, Min(.1f)] private float cycleDuration = 12f;
    [SerializeField, Min(.01f)] private float loopEaseTime = 1f;
    Vector3 startPosition;
    float entranceElapsed, loopElapsed;
    bool arrived;

    public float EntranceDuration => Mathf.Max(.01f,lerpTime);
    public float CycleDuration => Mathf.Max(.1f,cycleDuration);

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
    }

    public override void Move()
    {
        float dt=Time.deltaTime;
        float entranceStep=Mathf.Min(dt,Mathf.Max(0,EntranceDuration-entranceElapsed));
        entranceElapsed+=entranceStep;
        loopElapsed+=(dt-entranceStep)*Mathf.Max(0,GameRules.enemySpeed);
        transform.position=PositionAtTime(startPosition,entranceElapsed+loopElapsed);
        if(!arrived && entranceElapsed>=EntranceDuration)
        {
            arrived=true;
            if(shooter!=null)shooter.OnArrive();
        }
    }

    // Also used by the editor verification to sample the complete trajectory at any frame rate.
    public Vector3 PositionAtTime(Vector3 origin,float elapsed)
    {
        var centre=new Vector3(lerpPosition.x,lerpPosition.y,origin.z);
        if(elapsed<EntranceDuration)
        {
            float t=Mathf.Clamp01(elapsed/EntranceDuration);
            // Quintic easing reaches the entrance endpoint with zero velocity and acceleration.
            float ease=t*t*t*(t*(t*6-15)+10);
            return Vector3.LerpUnclamped(origin,centre,ease);
        }
        float age=elapsed-EntranceDuration;
        float ramp=Mathf.Max(.01f,loopEaseTime);
        float u=Mathf.Clamp01(age/ramp);
        // Integral of SmoothStep: phase speed starts at zero, then settles into a regular loop.
        float loopClock=age<ramp ? ramp*(u*u*u-.5f*u*u*u*u) : age-ramp*.5f;
        float phase=Mathf.Repeat(loopClock,CycleDuration)*Mathf.PI*2/CycleDuration;
        return centre+new Vector3(horizontalAmplitude*Mathf.Sin(2*phase),verticalAmplitude*Mathf.Sin(phase),0);
    }
}
