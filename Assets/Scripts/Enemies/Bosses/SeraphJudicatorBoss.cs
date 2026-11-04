using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SeraphJudicatorBoss : ArmoredHullBoss
{
    static readonly Vector2[] outline={new(0,-1.52f),new(-.62f,-.8f),new(-1.75f,-.65f),new(-2.35f,.55f),new(-1.35f,1.25f),new(0,1.5f),new(1.35f,1.25f),new(2.35f,.55f),new(1.75f,-.65f),new(.62f,-.8f)};
    protected override Vector2[] Outline=>outline;
    bool anchored;
    protected override void Configure(){health=2850;hullScale=1.05f;attackInterval=.7f;}
    protected override void MoveBoss(float dt)
    {
        station=new Vector3(0,Camera.UpLimit-3.1f,1);
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        if(!anchored)transform.position=Vector3.MoveTowards(transform.position,station+new Vector3(Mathf.Sin(motionAge*.4f)*1.1f,Mathf.Cos(motionAge*.6f)*.35f),dt*1.2f);
    }
    PulseLaser Beam(int port,float angle,float width,float hold,float charge=1.4f)
        =>HeavenArmory.Lance(Hull,WeaponPort(port),CombatVisuals.Rotate(Vector2.down,angle),width,hold,charge);
    protected override void OnPhaseEntered(int phase){anchored=false;}
    protected override IEnumerator Cards()
    {
        while(true)
        {
            if(CurrentPhase==1)
            {
                // Gates of Dawn: open outward from a safe central aisle, then force a side change.
                anchored=true;Beam(1,-8,1.25f,2.6f);Beam(2,8,1.25f,2.6f);
                yield return Every(3.8f,.47f,n=>Feathers(n,6,2.5f,false));
                anchored=false;yield return Wait(Rest(1.0f));
                // The First Trumpet: a broad centre lance with a refracted ceiling canopy.
                anchored=true;Beam(0,0,2.0f,2.7f,1.65f);
                yield return Every(4.3f,.55f,n=>Canopy(n,7));
            }
            else if(CurrentPhase==2)
            {
                // Spear of the Firmament: sequential banks, never an instantaneous full-width wall.
                anchored=true;Beam(1,-16,1.5f,2.4f);Beam(2,16,1.5f,2.4f);
                yield return Wait(Rest(1.8f));
                Beam(0,0,1.75f,2.1f,1.5f);
                yield return Every(3.9f,.36f,n=>Feathers(n,8,2.6f,true));
                anchored=false;yield return Wait(Rest(1.1f));
                // Judgment Pendulum: the entire beam swings after a long, stationary charge.
                anchored=true;var beam=Beam(0,-24,1.45f,4.5f,1.7f);
                if(beam!=null){beam.sweep=12;beam.reversalArc=48;}
                yield return Every(6.2f,.64f,n=>Canopy(n,8));
            }
            else
            {
                // Last Trumpet: a wide centre blast between outward wing lances.
                anchored=true;Beam(0,0,2.45f,3.1f,1.8f);Beam(1,-38,1.0f,2.8f,1.8f);Beam(2,38,1.0f,2.8f,1.8f);
                yield return Every(5.0f,.44f,n=>Canopy(n,10));
                anchored=false;yield return Wait(Rest(1.2f));
                // Broken Covenant: opposing scythes of light fold inward, then leave a dark interval.
                anchored=true;
                for(int s=-1;s<=1;s+=2){var beam=Beam(s<0?1:2,s*27,1.25f,3.8f,1.65f);if(beam!=null)beam.sweep=-s*7;}
                yield return Every(5.4f,.29f,n=>Feathers(n,10,2.8f,true));
            }
            anchored=false;yield return Wait(Rest(1.4f));
        }
    }
    void Feathers(int beat,int count,float speed,bool returnFire)
    {
        int n=Dense(count);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
        {
            float a=s*(30+i*7+Mathf.Sin(beat*.6f)*15);
            var shot=Fire<NeedleRound>(Muzzle(s<0?3:4),CombatVisuals.Rotate(Vector2.down,a),speed,.18f).Ramp(1.5f,.8f,.9f);
            if(returnFire&&i%3==0)shot.Bounce(1);
        }
    }
    void Canopy(int beat,int count)
    {
        int n=Dense(count);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<n;i++)
            Fire<NeedleRound>(Muzzle(s<0?3:4),CombatVisuals.Rotate(Vector2.up,s*(15+i*8+beat%2*4)),2.6f,.19f).Bounce(1).FadeAt(8);
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh seraphJudicator;
    public static Mesh SeraphJudicatorBossMesh()
    {
        if(seraphJudicator!=null)return seraphJudicator;var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.52f),V(-.55f,-.8f),V(-.65f,.8f),V(0,1.45f),V(.65f,.8f),V(.55f,-.8f)},.65f,15);
        BevelArmor(b,new[]{V(0,-1.15f),V(-.32f,-.4f),V(-.38f,.6f),V(0,1.0f),V(.38f,.6f),V(.32f,-.4f)},.24f,7,-.42f);
        for(int s=-1;s<=1;s+=2)
        {
            for(int row=0;row<3;row++)
            {
                float y=.9f-row*.58f;
                BevelArmor(b,new[]{V(s*.48f,y),V(s*(1.9f+row*.18f),y+.6f),V(s*(2.35f-row*.18f),y+.13f),V(s*1.35f,y-.38f)},.20f,row%2==0?1:15,-.09f-row*.05f);
                b.Prism(new[]{V(s*.68f,y+.04f),V(s*2.04f,y+.37f),V(s*1.35f,y-.15f)},.06f,3,new Vector3(0,0,-.25f-row*.05f),7);
            }
            b.Box(new Vector3(s*1.5f,-.6f,-.28f),new Vector3(.52f,.7f,.47f),7,2);
            Barrel(b,s*1.5f,-.83f,.45f,.39f,-.3f);
            b.Drum(new Vector3(s*1.05f,.67f,-.32f),.24f,.12f,8,12,7);
        }
        b.Drum(new Vector3(0,-.65f,-.5f),.42f,.20f,7,16,2);
        b.Drum(new Vector3(0,-.65f,-.64f),.29f,.07f,8,16,11);
        Barrel(b,0,-1.12f,.48f,.54f,-.32f);
        b.Prism(new[]{V(-.24f,.5f),V(0,.12f),V(.24f,.5f),V(0,.87f)},.14f,9,new Vector3(0,0,-.64f),3);
        return seraphJudicator=b.Finish("Seraph Judicator / six sculpted wings and three giant lance emitters");
    }
}
