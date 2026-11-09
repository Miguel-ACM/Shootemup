using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SeraphJudicatorBoss : DistrictBoss
{
    public HeavenAngelVisual Angel { get; private set; }
    public int CardIndex { get; private set; }
    bool anchored;
    readonly SpriteRenderer[] apparitions=new SpriteRenderer[6];
    protected override void Configure(){health=3200;hullScale=1.05f;attackInterval=.7f;}
    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled=false;
        Angel=Hull.gameObject.AddComponent<HeavenAngelVisual>();
        Angel.Build();
        FollowEyes();
        for(int i=0;i<apparitions.Length;i++)
        {
            var go=new GameObject("Unbodied choir charge");go.transform.SetParent(transform,false);
            var glow=go.AddComponent<SpriteRenderer>();glow.sprite=CombatVisuals.Disc;
            glow.sharedMaterial=CombatVisuals.ProjectileMaterial;glow.sortingLayerName="Bullet";
            glow.enabled=false;apparitions[i]=glow;
        }
    }
    protected override void Update()
    {
        if(WorldTimeStop.Frozen)return;
        if(Angel!=null&&!GameRules.playerDisabled){Angel.Step(Time.deltaTime*LevelRun.EnemyTempoScale);FollowEyes();}
        base.Update();
    }
    void FollowEyes(){for(int i=0;i<5;i++)firingCue.FollowPort(i,Angel.Emitter(i));}
    Vector3 Eye(int index)=>Angel.Emitter(index).position;
    protected override void MoveBoss(float dt)
    {
        station=new Vector3(0,Camera.UpLimit-3.1f,1);
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        if(!anchored)transform.position=Vector3.MoveTowards(transform.position,station+new Vector3(Mathf.Sin(motionAge*.4f)*1.35f,Mathf.Cos(motionAge*.6f)*.35f),dt*1.2f);
    }
    void Pray(bool value){anchored=value;Angel.Pray(value);}
    PulseLaser Beam(int port,float angle,float width,float hold,float charge=1.4f)
        =>HeavenArmory.Lance(Angel.Emitter(port),Vector3.zero,CombatVisuals.Rotate(Vector2.down,angle),width,hold,charge);
    protected override void OnPhaseEntered(int phase)
    {
        Pray(false);Angel.Phase=phase;
        foreach(var glow in apparitions)glow.enabled=false;
    }
    protected override void Attack() { if(openingComplete)OpenCards(); }
    protected override IEnumerator Cards()
    {
        while(true)
        {
            CardIndex=0;Pray(true);
            yield return Charge(Angel.Emitter(0),Vector3.zero,.65f);
            Phrase(UnbodiedChoir(0));
            if(CurrentPhase==1)
            {
                // Dawn Liturgy: a golden nave between two lances, feather-vaults above it.
                Beam(1,-14,.92f,2.7f);Beam(2,14,.92f,2.7f);
                yield return Every(4.4f,.28f,n=>Vault(n,13,29,false));
            }
            else if(CurrentPhase==2)
            {
                // Sevenfold Procession: the entire lace opens and closes around a moving aisle.
                Beam(1,-22,1.05f,3.0f);Beam(2,22,1.05f,3.0f);
                yield return Every(4.7f,.24f,n=>Vault(n,17,38,true));
            }
            else
            {
                // Last Trumpet: a central column framed by symmetrical upward lancets.
                Beam(0,0,2.1f,3.1f,1.7f);
                Beam(1,-47,.85f,2.8f,1.7f);Beam(2,47,.85f,2.8f,1.7f);
                yield return Every(5.2f,.22f,n=>Vault(n,19,52,true));
            }
            Pray(false);yield return Wait(Rest(1.25f));
            CardIndex=1;Pray(true);
            yield return Charge(Angel.Emitter(0),Vector3.zero,.65f);
            Phrase(UnbodiedChoir(1));
            if(CurrentPhase==1)
            {
                // Cathedral of Eyes: fixed, reflected arches over a slow central lance.
                Beam(0,0,1.7f,2.8f,1.6f);
                yield return Every(4.8f,.66f,n=>Window(n,19,2.15f));
            }
            else if(CurrentPhase==2)
            {
                // Judgment Pendulum: a single deliberate sweep and pearled arch ribs.
                var beam=Beam(0,-22,1.12f,3.6f,1.65f);
                if(beam!=null){beam.sweep=10;beam.reversalArc=44;}
                yield return Every(5.6f,.56f,n=>Window(n,23,2.2f));
            }
            else
            {
                // Covenant: two opposed slow scythes; the same windows now have an inner rib.
                for(int s=-1;s<=1;s+=2)
                {
                    var beam=Beam(s<0?1:2,s*30,1.0f,3.5f,1.7f);
                    if(beam!=null)beam.sweep=-s*6;
                }
                yield return Every(5.6f,.48f,n=>Window(n,27,2.3f));
            }
            Pray(false);yield return Wait(Rest(1.45f));
        }
    }
    static float Across(int i,int count)=>count<=1?0:i/(count-1f)*2-1;
    static int BankCount(int count,int bank)
    {
        int upper=Mathf.Max(1,Mathf.RoundToInt(count*.55f));
        return bank>0?upper:2*count-upper;
    }
    // The feather banks that return from the ceiling keep their lower rows but send
    // far fewer upward: not so many bounces along the top (Miguel).
    static int CeilingBankCount(int count,int bank)
        =>bank>0?Mathf.Max(1,Mathf.RoundToInt(count*.3f)):BankCount(count,bank);
    void Vault(int beat,int count,float opening,bool pearls)
    {
        if(beat%4==0)Gaze();
        int n=Dense(count);
        float breath=Mathf.Sin(beat*.18f)*18;
        for(int s=-1;s<=1;s+=2)for(int bank=-1;bank<=1;bank+=2)
        {
            int rows=CeilingBankCount(n,bank);
            for(int i=0;i<rows;i++)
            {
                float u=Across(i,rows),angle=s*(bank>0?opening+breath+u*38:opening*.6f+breath*.6f+u*32);
                // Most feathers enter the lower arena; a lighter upper arch still returns from the ceiling.
                float speed=2.35f+.85f*(1-u*u)+(CurrentPhase-1)*.12f;
                var axis=bank>0?Vector2.up:Vector2.down;
                ScoredShot(i%3==0,Eye(s<0?3:4),CombatVisuals.Rotate(axis,angle),speed,.17f)
                    .Bounce(bank>0?1:0).FadeAt(8);
                if(pearls&&bank<0&&beat%4==0&&i%3==0)
                    Fire<SimpleRound>(Eye(s<0?1:2),CombatVisuals.Rotate(Vector2.down,s*(48+u*23)),1.55f,.22f).FadeAt(7.8f);
            }
        }
    }
    void Window(int beat,int count,float speed)
    {
        if(beat%2==0)Gaze();
        int n=Dense(count);
        for(int s=-1;s<=1;s+=2)for(int bank=-1;bank<=1;bank+=2)
        {
            int rows=CeilingBankCount(n,bank);
            for(int i=0;i<rows;i++)
            {
                float u=Across(i,rows);
                float a=s*(bank>0?45+u*43+Mathf.Sin(beat*.43f)*14:35+u*32+Mathf.Sin(beat*.43f)*9);
                ScoredShot(beat%3==0,Eye(s<0?3:4),CombatVisuals.Rotate(bank>0?Vector2.up:Vector2.down,a),speed+.85f*(1-u*u),.18f)
                    .Bounce(bank>0?1:0).Ramp(1.25f,1.4f,2).FadeAt(8);
                if(CurrentPhase==3&&bank<0&&i%3==0)
                    Fire<SimpleRound>(Eye(s<0?1:2),CombatVisuals.Rotate(Vector2.down,s*(54+u*26)),1.6f+.3f*u*u,.22f).FadeAt(8);
            }
        }
    }

    void Gaze()
    {
        // The central eye looks through the moving aisle, including above the angel.
        Vector3 p=Eye(0);Vector2 aim=CombatVisuals.Aim(p);int count=Dense(5)|1;
        for(int i=0;i<count;i++)
            Fire<SimpleRound>(p,CombatVisuals.Rotate(aim,(i-(count-1)*.5f)*11),2.6f,.22f).Ramp(1.2f,1.3f);
    }

    // The angel may create fire in empty air. Six fixed, charged points draw a
    // mirrored chevron before releasing interleaved petals above and below it.
    IEnumerator UnbodiedChoir(int card)
    {
        for(int verse=0;verse<3;verse++)
        {
            Rect r=CombatRound.Border;
            for(int i=0;i<apparitions.Length;i++)
            {
                int side=i<3?-1:1,lane=i%3;
                float x=r.center.x+side*(.9f+lane*(r.width*.5f-1.65f)/2);
                float y=r.yMax-2.0f-lane*.95f-verse*.8f;
                if(card==1)y-=.8f;
                apparitions[i].transform.position=new Vector3(x,y,-.8f);
            }
            for(float t=0;t<.8f;)
            {
                if(!Holding)t+=Time.deltaTime*LevelRun.EnemyTempoScale;
                foreach(var glow in apparitions)
                {
                    glow.enabled=true;
                    glow.transform.localScale=Vector3.one*Mathf.Lerp(.65f,.15f,Mathf.Clamp01(t/.8f));
                    var ink=LevelProjectilePalette.Accent(LevelId.Heaven);ink.a=.2f+t*.55f;glow.color=ink;
                }
                yield return null;
            }
            int count=Dense(9+CurrentPhase*2);
            for(int i=0;i<apparitions.Length;i++)
            {
                var p=apparitions[i].transform.position;apparitions[i].enabled=false;
                if(player!=null&&Vector2.Distance(p,player.position)<1.6f)continue;
                int side=i<3?-1:1;
                for(int bank=-1;bank<=1;bank+=2)
                {
                    int rows=BankCount(count,bank);
                    for(int k=0;k<rows;k++)
                    {
                        float u=Across(k,rows),turn=side*(verse%2==0?17:-17);
                        ScoredShot(k%3==0,p,CombatVisuals.Rotate(bank>0?Vector2.up:Vector2.down,u*56+turn),
                            1.7f+.5f*(1-u*u),.16f).Ramp(1.35f,1.5f,1).FadeAt(6.8f);
                    }
                }
            }
            yield return Wait(Rest(.75f));
        }
    }
}
