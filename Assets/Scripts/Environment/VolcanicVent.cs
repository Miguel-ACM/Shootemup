using UnityEngine;

// Scenery hazard: no enemy registry entry, reward or wave-clear requirement.
public sealed class VolcanicVent : MonoBehaviour
{
    public float phase;
    float readyAt=-1,next,charge,embers;
    ParticleSystem fire;
    public int Eruptions {get;private set;}
    void Start()
    {
        fire=PSXImpactEffects.Create(transform,"Crater fire",80,true);var renderer=fire.GetComponent<ParticleSystemRenderer>();renderer.sortingLayerName="Background";renderer.sortingOrder=-34;
    }
    void Update()
    {
        if(fire==null)return;
        float dt=Time.deltaTime;var p=transform.position;
        bool visible=p.y>Camera.DownLimit+1.8f && p.y<Camera.UpLimit-1.2f && Mathf.Abs(p.x)<5.2f;
        bool combat=visible && !GameRules.playerDisabled && !CloudTransition.Busy && WavePreparation.Ready && LevelRun.Current==LevelId.Hell;
        if(!combat){readyAt=-1;charge=0;return;}
        if(readyAt<0){readyAt=Time.time+WaveOrchestrer.OpeningBreather;next=readyAt+.8f+phase*.22f;}
        charge=Mathf.Clamp01(1-(next-Time.time)/1.15f);
        embers+=dt*(8+charge*30);
        while(embers>=1)
        {
            embers--;var e=new ParticleSystem.EmitParams();e.position=p+new Vector3(Random.Range(-.18f,.18f),Random.Range(-.15f,.15f),-.7f);
            e.velocity=new Vector3(Random.Range(-.25f,.25f),.6f+charge*1.1f,-.15f);e.startSize=Random.Range(.16f,.36f)*(1+charge*.5f);e.startLifetime=.65f;
            e.startColor=Color.Lerp(new Color(.65f,.20f,.035f,.40f),new Color(.95f,.57f,.15f,.65f),charge);fire.Emit(e,1);
        }
        if(Time.time<next || Time.time<readyAt)return;
        next=Time.time+7.5f+phase;Eruptions++;
        var axis=CombatVisuals.Rotate(Vector2.down,Mathf.Sign(p.x)*-24);
        for(int i=-3;i<=3;i++)CombatRound.Launch<SimpleRound>(p,CombatVisuals.Rotate(axis,i*17),3.3f+(.18f*(3-Mathf.Abs(i))),.18f);
        SoundManager.PlaySound("enemyExplosion1",.055f);
    }
}
