using UnityEngine;
public sealed class ProximityMine : CombatRound
{
    LineRenderer radius;
    protected override void Initialize()
    {
        if(radius==null)radius=CombatVisuals.Line(transform,new Color(1,.7f,.2f,.4f),.025f);
        visual.color=Color.gray;GetComponent<Collider2D>().enabled=false;lifetime=6;
    }
    protected override void Tick(float dt)
    {
        if(age<1)transform.position+=(Vector3)heading*speed*(1-age)*dt;
        CombatVisuals.Ring(radius,transform.position,1.1f);
        var player=GameObject.FindGameObjectWithTag("Player");
        float distance=player!=null ? Vector2.Distance(player.transform.position,transform.position) : 9;
        // The mine cannot arm beneath a player; retreating gives it an arming opportunity.
        bool armed=age>=1.4f && (GetComponent<Collider2D>().enabled || distance>1.5f);
        GetComponent<Collider2D>().enabled=armed;
        visual.color=armed ? Color.Lerp(new Color(.65f,.65f,.65f),Color.white,.65f+.35f*Mathf.Sin(age*8)) : Color.gray;
        if(armed && (age>4.5f || distance<1.1f) && Ring(10,3.2f))Release();
    }
}
