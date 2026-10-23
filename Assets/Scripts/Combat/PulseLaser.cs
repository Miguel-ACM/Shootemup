using UnityEngine;
public class PulseLaser : CombatRound
{
    protected LineRenderer beam;
    public float warning=.9f, activeTime=.65f, length=22, width=.25f, sweep;
    public bool sideShots;
    float nextSide;
    protected override void Initialize()
    {
        warning=.9f;activeTime=.65f;length=22;width=.25f;sweep=0;sideShots=false;nextSide=0;
        visual.enabled=false;GetComponent<Collider2D>().enabled=false;
        if(beam==null)beam=CombatVisuals.Line(transform,Color.yellow,.045f);
        beam.enabled=true;beam.positionCount=2;lifetime=8;
    }
    protected virtual bool Active => age>=warning && age<warning+activeTime;
    protected override void Tick(float dt)
    {
        if(age>warning+activeTime){Release();return;}
        // Sweep only after the aim locks; the warning displays the actual initial direction.
        if(Active)heading=CombatVisuals.Rotate(heading,sweep*dt);
        beam.SetPosition(0,transform.position);beam.SetPosition(1,transform.position+(Vector3)heading*length);
        beam.startWidth=beam.endWidth=Active ? width : .045f;
        beam.startColor=beam.endColor=Active ? new Color(1,.32f,.7f) : new Color(1,.85f,.3f,.65f);
        var player=GameObject.FindGameObjectWithTag("Player");
        if(Active && player!=null)
        {
            Vector2 delta=player.transform.position-transform.position;
            float along=Vector2.Dot(delta,heading);
            float across=Mathf.Abs(delta.x*heading.y-delta.y*heading.x);
            if(along>=0 && along<=length && across<width*.5f+.16f)player.SendMessage("ReceiveHazardDamage",damage,SendMessageOptions.DontRequireReceiver);
        }
        if(Active && sideShots && age>=nextSide)
        {
            nextSide=age+.45f;var side=new Vector2(-heading.y,heading.x);
            for(int i=1;i<=3;i++)for(int sign=-1;sign<=1;sign+=2)
                Launch<SimpleRound>(transform.position+(Vector3)heading*(i*2.3f),side*sign,2.4f,.12f);
        }
    }
    public override void Vanish(){Release();} // Beams are cleared by encounter cleanup, not projectile blockers.
}
