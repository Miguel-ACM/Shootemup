using UnityEngine;
public sealed class SegmentedBeam : CombatRound
{
    LineRenderer[] sections;
    protected override void Initialize()
    {
        visual.enabled=false;GetComponent<Collider2D>().enabled=false;
        if(sections==null){sections=new LineRenderer[5];for(int i=0;i<5;i++)sections[i]=CombatVisuals.Line(transform,Color.yellow,.04f);}
        lifetime=4;
    }
    protected override void Tick(float dt)
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        for(int i=0;i<5;i++)
        {
            Vector3 a=origin+(Vector3)heading*(i*3.1f),b=a+(Vector3)heading*2.5f;
            bool active=age>1+i*.28f && age<1.45f+i*.28f;
            sections[i].SetPosition(0,a);sections[i].SetPosition(1,b);
            sections[i].startWidth=sections[i].endWidth=active ? .3f : .045f;
            sections[i].startColor=sections[i].endColor=active ? Color.magenta : new Color(1,.8f,.2f,.5f);
            if(active && player!=null)
            {
                Vector2 delta=player.transform.position-a;float along=Vector2.Dot(delta,heading);
                if(along>0 && along<2.5f && Mathf.Abs(delta.x*heading.y-delta.y*heading.x)<.31f)
                    player.SendMessage("ReceiveHazardDamage",damage,SendMessageOptions.DontRequireReceiver);
            }
        }
        if(age>3)Release();
    }
}
