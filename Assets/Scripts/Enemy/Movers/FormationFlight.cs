using UnityEngine;

public enum FlightPhrase { None, Ribbon, Crossing, Chevron, Orbit, Battery, Dive }

// Every member follows one authored phrase. Delayed launches make a train;
// mirrored launches make a partner. No per-enemy destination randomization.
public sealed class FormationFlight : PatternMover
{
    public FlightPhrase phrase;
    public float side=1,lane,duration=6,fireEvery=.8f;
    float clock;
    int lastBeat=-1;
    public Vector2 Tangent {get;private set;}=Vector2.down;
    public float Progress=>clock/Mathf.Max(.1f,duration);
    public bool TakeFireBeat()
    {
        if(clock<1.15f || Progress>.82f)return false;
        int beat=Mathf.FloorToInt((clock-1.15f)/Mathf.Max(.35f,fireEvery));
        if(beat==lastBeat)return false;lastBeat=beat;return true;
    }
    protected override void StepPattern(float dt)
    {
        if(GameRules.playerDisabled)return;
        clock+=dt;float t=Progress;
        if(t>=1){GetComponent<AEnemy>().Depart();return;}
        var p=Sample(phrase,t,side,lane);
        var next=new Vector3(p.x*Camera.RightLimit,Mathf.LerpUnclamped(Camera.DownLimit,Camera.UpLimit,p.y),origin.z);
        Vector2 delta=next-transform.position;if(delta.sqrMagnitude>.0001f)Tangent=delta.normalized;
        transform.position=next;SetFiring(t>.12f && t<.82f);
    }
    static Vector2 Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
    {float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
    public static Vector2 Sample(FlightPhrase phrase,float t,float side,float lane)
    {
        t=Mathf.Clamp01(t);Vector2 p;
        switch(phrase)
        {
            case FlightPhrase.Ribbon:
                p=Bezier(new Vector2(-1.16f,1.12f),new Vector2(-.95f,.32f),new Vector2(.74f,1.02f),new Vector2(1.2f,.46f),t);break;
            case FlightPhrase.Crossing:
                p=Bezier(new Vector2(-.82f,1.12f),new Vector2(1.16f,.87f),new Vector2(.94f,.3f),new Vector2(-1.2f,.46f),t);break;
            case FlightPhrase.Orbit:
                if(t<.2f)p=Vector2.Lerp(new Vector2(-.75f,1.12f),new Vector2(-.75f,.73f),Mathf.SmoothStep(0,1,t/.2f));
                else if(t<.78f)
                {float a=(t-.2f)/.58f*Mathf.PI*2;p=new Vector2(-.75f*Mathf.Cos(a),.73f+Mathf.Sin(a)*.17f);}
                else p=Vector2.Lerp(new Vector2(-.75f,.73f),new Vector2(-1.2f,.92f),(t-.78f)/.22f);
                break;
            case FlightPhrase.Chevron:
            case FlightPhrase.Battery:
                float y=.78f-Mathf.Abs(lane)*.1f;
                if(t<.18f)p=Vector2.Lerp(new Vector2(lane,1.12f),new Vector2(lane,y),Mathf.SmoothStep(0,1,t/.18f));
                else if(t<.72f)p=new Vector2(lane+Mathf.Sin((t-.18f)/.54f*Mathf.PI*2)*(phrase==FlightPhrase.Battery ? .1f : .24f),y);
                else p=Bezier(new Vector2(lane,y),new Vector2(lane,y+.05f),new Vector2(lane*1.2f,1.05f),new Vector2(lane*1.3f,1.2f),(t-.72f)/.28f);
                break;
            case FlightPhrase.Dive:
                p=Bezier(new Vector2(-.8f,1.12f),new Vector2(.55f,.87f),new Vector2(.55f,.25f),new Vector2(-.75f,-.18f),t);break;
            default:p=new Vector2(lane,1.12f-t);break;
        }
        if(phrase!=FlightPhrase.Chevron && phrase!=FlightPhrase.Battery)p.y+=lane;
        p.x*=side;return p;
    }
}
