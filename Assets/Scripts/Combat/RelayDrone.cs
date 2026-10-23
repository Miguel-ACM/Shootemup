using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class RelayDrone : DistrictEnemy
{
    protected override void Configure(){hull=10;health=32;hullScale=.55f;attackInterval=2.7f;Mover<AlternatingAdvance>();}
    protected override void Start()
    {
        foreach(var other in FindObjectsByType<RelayDrone>())
            if(other!=this && other.partner==null)
            {
                partner=other;other.partner=this;
                var first=GetEntityId().CompareTo(other.GetEntityId())<0;
                GetComponent<AlternatingAdvance>().SetPartner(other.GetComponent<AlternatingAdvance>(),first);
                other.GetComponent<AlternatingAdvance>().SetPartner(GetComponent<AlternatingAdvance>(),!first);break;
            }
        base.Start();
    }
    protected override void Attack(){FireLink();}

    RelayDrone partner; PulseLaser link;
    void FireLink()
    {
        if(partner==null)foreach(var other in FindObjectsByType<RelayDrone>())if(other!=this){partner=other;break;}
        if(partner==null || partner.transform.position.y<1 || transform.position.y<1)return;
        if(GetEntityId().CompareTo(partner.GetEntityId())>0)return;
        var d=partner.transform.position-transform.position;
        link=CombatRound.Launch<PulseLaser>(transform.position,d.normalized,0);
        if(link!=null){link.length=d.magnitude;link.warning=.6f;link.activeTime=.6f;}
    }
    protected override void Update()
    {
        base.Update();if(link!=null && link.gameObject.activeSelf && (partner==null || !movement.CanFire || !partner.GetComponent<PatternMover>().CanFire))link.Vanish();
    }
    void OnDestroy(){if(link!=null && link.gameObject.activeSelf)link.Vanish();}
}
