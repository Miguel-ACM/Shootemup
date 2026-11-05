using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    public int CardIndex { get; private set; }
    PulseLaser VoidRay(int port,Vector2 axis,float width,float hold,float sweep=0)
    {
        var beam=HeavenArmory.Lance(body.Emitter(port),Vector3.zero,axis,width,hold,1.25f);
        if(beam!=null)beam.sweep=sweep;
        return beam;
    }
    static float Across(int i,int count)=>count<=1?0:i/(count-1f)*2-1;
    CombatRound CrownedShot(bool giant,bool circular,Vector3 port,Vector2 axis,float speed,float radius=.17f,float crown=.60f,bool chevron=false)
        =>giant?(CombatRound)Fire<GiantOrb>(port,axis,speed*.72f,crown):
            chevron&&!circular?Fire<ChevronRound>(port,axis,speed,radius):ScoredShot(circular,port,axis,speed,radius);
    static int BankCount(int count,int bank)
    {
        int upper=Mathf.Max(1,Mathf.RoundToInt(count*.55f));
        return bank>0?upper:2*count-upper;
    }
    protected override IEnumerator Cards()
    {
        while(true)
        {
            CardIndex=0;yield return Teleport();yield return Prime(.65f);
            if(Form==1)yield return Witnesses();
            else if(Form==2)yield return Severing();
            else if(Form==3)yield return Hunger();
            else yield return WorldWithoutEdges();
            yield return Wait(Rest(.8f));
            CardIndex=1;yield return Teleport();yield return Prime(.65f);
            if(Form==1)yield return FuneralPsalm();
            else if(Form==2)yield return CutTheFirmament();
            else if(Form==3)yield return FalseStars();
            else yield return LastEquation();
            yield return Wait(Rest(.9f));
        }
    }

    // Veiled Monarch: paired eyelids open into long, symmetric butterfly seams.
    // Every row has the same travel clock; the gaps breathe as one shape.
    IEnumerator Witnesses()=>Every(6.2f,.26f,beat=>
    {
        if(beat%5==0)for(int port=0;port<2;port++)
            Fire<SimpleRound>(Port(port),CombatVisuals.Aim(Port(port)),2.7f,.23f);
        int count=Dense(17);
        float fold=30+23*Mathf.Sin(beat*.22f);
        for(int side=-1;side<=1;side+=2)
            for(int bank=-1;bank<=1;bank+=2)
                for(int i=0;i<BankCount(count,bank);i++)
                {
                    float u=Across(i,BankCount(count,bank)),angle=side*(fold+u*25);
                    Vector2 axis=bank>0?Vector2.up:Vector2.down;
                    CrownedShot(beat%2==0&&i%4==1,beat%3==0,Port(side<0?0:1),CombatVisuals.Rotate(axis,angle),
                        2.0f+.6f*(1-u*u),.17f,.58f).Ramp(1.35f,1.6f,1.2f).FadeAt(7.8f);
                }
    });

    // A pearl pall is laid outward, hangs intact, then folds into four diagonal aisles.
    IEnumerator FuneralPsalm()=>Every(6.2f,.88f,beat=>
    {
        for(int port=0;port<2;port++)
            Fire<SimpleRound>(Port(port),CombatVisuals.Aim(Port(port)),1.5f,.23f)
                .Pause(.4f,.35f,CombatRound.Turn.Aim,0,1.8f);
        int count=Dense(25);
        for(int side=-1;side<=1;side+=2)
            for(int bank=-1;bank<=1;bank+=2)
                for(int i=0;i<BankCount(count,bank);i++)
                {
                    float u=Across(i,BankCount(count,bank));
                    Vector2 axis=bank>0?Vector2.up:Vector2.down;
                    CrownedShot((i+beat)%4==1,true,Port(side<0?0:1),CombatVisuals.Rotate(axis,side*(42+u*30)),
                        2.5f+.7f*(1-u*u),.22f,.64f)
                        .Pause(.76f,.48f,CombatRound.Turn.Rotate,side*bank*(beat%2==0?24:-24),.9f).FadeAt(8);
                    // The choir's short answer follows the empty crease between pearl ribs.
                    if(i%3==0)
                        Fire<NeedleRound>(Port(side<0?0:1),CombatVisuals.Rotate(axis,side*(18+u*37)),
                            1.1f,.17f).Ramp(2.3f,1.1f,1.1f).FadeAt(8);
                }
    });

    // Two scythes draw continuous opposed ribbons, with visible spacing between teeth.
    IEnumerator Severing()=>Every(5.6f,.14f,beat=>
    {
        if(beat%10==0)for(int port=0;port<2;port++)
            Fire<SimpleRound>(Port(port),CombatVisuals.Aim(Port(port)),3.0f,.22f);
        int count=Dense(9);
        float stroke=74*Mathf.Sin(beat*.105f);
        for(int side=-1;side<=1;side+=2)
            for(int i=0;i<count;i++)
            {
                float u=Across(i,count);
                var axis=CombatVisuals.Rotate(Vector2.down,side*(stroke+u*14));
                CrownedShot(beat%4==0&&i%3==1,beat%3==0,Port(side<0?0:1),axis,2.35f+.42f*u,.17f,.56f).FadeAt(7.4f);
                if(beat%3==0)Fire<RicochetBolt>(Port(side<0?0:1),-axis,2.15f,.19f).Bounce(1).FadeAt(6.6f);
            }
    });

    IEnumerator CutTheFirmament()
    {
        // The blades hold their axes for the entire charge; the teleport only follows release.
        for(int side=-1;side<=1;side+=2)
            VoidRay(side<0?0:1,CombatVisuals.Rotate(Vector2.down,side*38),.48f,2.8f,-side*5);
        yield return Every(5.4f,.30f,beat=>
        {
            int count=Dense(19);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<count;i++)
            {
                float u=Across(i,count),pleat=9*Mathf.Sin(beat*.35f);
                CrownedShot(beat%2==0&&i%4==1,i%3==0,Port(side<0?0:1),CombatVisuals.Rotate(Vector2.down,side*(38+u*27+pleat)),
                    2.1f+.35f*(1-u*u),.17f,.60f,true)
                    .Pause(1.2f,.22f,i==count/2?CombatRound.Turn.Aim:CombatRound.Turn.Rotate,i==count/2?0:-side*24,1.25f).FadeAt(8);
            }
        });
        yield return Wait(Rest(.9f));
    }

    // The jaws open: nested parabolic teeth flare above and below the mouth.
    IEnumerator Hunger()=>Every(6.2f,.38f,beat=>
    {
        if(beat%4==0)for(int port=0;port<2;port++)
            Fire<SimpleRound>(Port(port),CombatVisuals.Aim(Port(port)),2.6f,.25f).Ramp(1.2f,1.2f);
        int count=Dense(21);
        for(int side=-1;side<=1;side+=2)for(int bank=-1;bank<=1;bank+=2)
            for(int i=0;i<BankCount(count,bank);i++)
            {
                float u=Across(i,BankCount(count,bank));
                var axis=CombatVisuals.Rotate(bank>0?Vector2.up:Vector2.down,side*(24+u*32+Mathf.Sin(beat*.3f)*8));
                CrownedShot((i+beat)%5==2,beat%3==0,Port(side<0?0:1),axis,1.8f+.95f*(1-u*u),.18f,.70f)
                    .Ramp(1.4f,1.5f,1.4f).FadeAt(8);
            }
        if(beat%3==0)for(int side=-1;side<=1;side+=2)
            Fire<PlasmaOrb>(Port(side<0?0:1),CombatVisuals.Rotate(Vector2.down,side*58),1.2f,.36f).FadeAt(7);
    });

    IEnumerator FalseStars()=>Every(6.2f,.85f,beat=>
    {
        for(int port=0;port<2;port++)
            Fire<SimpleRound>(Port(port),CombatVisuals.Aim(Port(port)),2.8f,.23f);
        // Mirrored comet triplets unfurl matching fans. Split the existing shard
        // budget between their large crowns rather than tripling the small shots.
        for(int side=-1;side<=1;side+=2)for(int bank=-1;bank<=1;bank+=2)
        {
            var axis=CombatVisuals.Rotate(bank>0?Vector2.up:Vector2.down,side*(38+beat%2*12));
            int shards=BankCount(Dense(29),bank);
            int comets=Dense(3);
            for(int comet=0;comet<comets;comet++)
            {
                int pieces=shards/comets+(comet<shards%comets?1:0);
                Fire<GiantOrb>(Port(side<0?0:1),CombatVisuals.Rotate(axis,Across(comet,comets)*13),1.8f,.72f)
                    .Ramp(.65f,.7f).SplitAt(1.9f+comet*.12f,pieces,82f/Mathf.Max(1,pieces-1),2.3f,.16f).FadeAt(6);
            }
            int count=BankCount(Dense(11),bank);
            for(int i=0;i<count;i++)
                CrownedShot((i+beat)%4==1,true,Port(side<0?0:1),CombatVisuals.Rotate(axis,Across(i,count)*26),1.2f,.21f,.56f).FadeAt(8);
        }
    });

    // Four fracture points stitch a square brocade. Its arms fold together on a single beat.
    IEnumerator WorldWithoutEdges()=>Every(6.2f,.29f,beat=>
    {
        if(beat%5==0)Phrase(CrossedGaze());
        int count=Dense(17);
        for(int port=0;port<4;port++)for(int i=0;i<FractureCount(count,port);i++)
        {
            Vector2 axis=FractureAxis(port);
            float u=Across(i,FractureCount(count,port)),sign=port%2==0?-1:1;
            CrownedShot(beat%2==0&&(i+port)%5==2,beat%3==0,Port(port),CombatVisuals.Rotate(axis,u*36+sign*Mathf.Sin(beat*.25f)*19),
                2.0f+.48f*(1-u*u),.17f,.64f).Pause(.95f,.22f,CombatRound.Turn.Rotate,-sign*24,1.0f).FadeAt(7.6f);
        }
    });

    IEnumerator LastEquation()=>Every(6.2f,.51f,beat=>
    {
        if(beat%3==0)Phrase(CrossedGaze());
        int count=Dense(21);
        for(int port=0;port<4;port++)for(int i=0;i<FractureCount(count,port);i++)
        {
            Vector2 axis=FractureAxis(port);
            float u=Across(i,FractureCount(count,port)),sign=port%2==0?-1:1;
            // Equal angular extent preserves moving seams on every difficulty.
            var direction=CombatVisuals.Rotate(axis,u*33+sign*(beat%2==0?9:-9));
            float speed=1.8f+.8f*Mathf.Abs(u);
            CombatRound stone=i%4==1?(CombatRound)Fire<GiantOrb>(Port(port),direction,speed*.72f,.66f):
                Fire<RicochetBolt>(Port(port),direction,speed,.18f);
            stone.Bounce(1).Ramp(1.3f,1.8f,1.4f).FadeAt(6.8f);
            if(i%4==0)Fire<SimpleRound>(Port(port),CombatVisuals.Rotate(axis,u*33),1.1f,.25f).FadeAt(7.5f);
        }
    });

    static int FractureCount(int count,int port)=>port<2?count:BankCount(count,port==2?1:-1);
    static Vector2 FractureAxis(int port)=>port==0?CombatVisuals.Rotate(Vector2.down,-42):
        port==1?CombatVisuals.Rotate(Vector2.down,42):port==2?Vector2.up:Vector2.down;

    IEnumerator CrossedGaze()
    {
        // Outward-facing star tips left the space inside/above the star untouched.
        // The opposite side tips visibly charge, then cross through the pilot's
        // recorded position. Moving after the charge opens a clear escape.
        Vector3 target=player!=null?player.position:transform.position+Vector3.down*5;
        for(float t=0;t<.45f;)
        {
            if(!Holding)t+=Time.deltaTime*LevelRun.EnemyTempoScale;
            Cue(Port(0),.45f-t);Cue(Port(1),.45f-t);
            yield return null;
        }
        int count=Dense(3)|1;
        for(int port=0;port<2;port++)
        {
            Vector3 p=Port(port);
            Vector2 aim=((Vector2)(target-p)).normalized;
            for(int i=0;i<count;i++)
                Fire<SimpleRound>(p,CombatVisuals.Rotate(aim,Across(i,count)*8),2.8f,.22f).FadeAt(6);
        }
    }
}
