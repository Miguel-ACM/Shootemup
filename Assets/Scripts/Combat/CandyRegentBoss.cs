using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CandyRegentBoss : DistrictBoss
{
    readonly List<DistrictEnemy> brood=new List<DistrictEnemy>();
    float nextBrood=3.8f,flightClock;
    bool arrived;
    Transform[] doors;
    public int SpawnedMinions {get;private set;}
    public int LiveMinions {get{brood.RemoveAll(e=>e==null);return brood.Count;}}
    protected override void Configure(){hull=38;hullScale=.85f;health=880;attackInterval=.90f;nextAttack=2.3f;}
    protected override void Start()
    {
        base.Start();doors=new Transform[2];
        for(int i=0;i<2;i++)
        {
            var go=new GameObject("Mint launch hatch "+i);go.transform.SetParent(Hull,false);go.transform.localPosition=new Vector3(i==0 ? -.82f : .82f,-.2f,-.26f);
            go.transform.localScale=Vector3.one*.24f;go.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.CandyHatch();
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=CandyMaterials.Glaze;r.sortingLayerName="Enemy";doors[i]=go.transform;
        }
    }
    protected override void MoveBoss(float dt)
    {
        float half=UnityEngine.Camera.main.orthographicSize*405f/720f;
        float width=Mathf.Min(Camera.RightLimit,half)-1.4f;
        if(!arrived)
        {
            transform.position=Vector3.MoveTowards(transform.position,new Vector3(0,Camera.UpLimit-2.4f,1),dt*7.5f);
            arrived=transform.position.y<=Camera.UpLimit-2.39f;return;
        }
        flightClock+=dt*(phaseTwo ? 1.25f : 1);
        // Continuous looping dashes stay in the upper two thirds; no teleport or sudden aimed ram.
        float t=flightClock*1.9f;
        var target=new Vector3(Mathf.Sin(t)*width,Camera.UpLimit-3.3f+Mathf.Sin(t*2+.4f)*1.45f,1);
        transform.position=Vector3.MoveTowards(transform.position,target,dt*(phaseTwo ? 13 : 10));
        if(age>=nextBrood){nextBrood=age+(phaseTwo ? 5.4f : 6.8f);StartCoroutine(ReleaseBrood());}
    }
    protected override void Attack()
    {
        if(!arrived || volley%4==0)return;
        FanFrom<NeedleRound>(phaseTwo ? 3 : 2,16,7.4f,.14f,new Vector3(-.27f,-.95f,-.2f),new Vector3(.27f,-.95f,-.2f));
    }
    protected override float AttackPace=>phaseTwo ? .85f : 1;
    IEnumerator ReleaseBrood()
    {
        // Hatch expansion is the warning. Nothing spawns underneath the player's ship.
        for(float t=0;t<.7f;t+=Time.deltaTime)
        {
            foreach(var door in doors)door.localScale=Vector3.one*(.24f+.11f*Mathf.Sin(t/.7f*Mathf.PI));
            yield return null;
        }
        for(int row=0;row<(phaseTwo ? 4 : 3);row++)
        {
            foreach(int side in new[]{-1,1})
            {
                if(LiveMinions>=12)break;
                var p=Hull.TransformPoint(new Vector3(side*.82f,-.35f,-.2f));
                if(player!=null && Vector2.Distance(p,player.position)<2.5f)continue;
                var minion=Spawn(typeof(BonbonSkirmisher),p);minion.moneyDropChance=.15f;minion.lifeDropChance=minion.manaDropChance=0;minion.scoreOnKill=15;
                var flight=minion.gameObject.AddComponent<CandyMinionFlight>();flight.side=side;minion.UseMovement(flight);
                brood.Add(minion);SpawnedMinions++;
            }
            yield return new WaitForSeconds(.23f);
        }
        foreach(var door in doors)door.localScale=Vector3.one*.24f;
    }
    protected override void Die(){ClearBrood();base.Die();}
    void OnDestroy(){ClearBrood();}
    void ClearBrood(){foreach(var child in brood)if(child!=null)Destroy(child.gameObject);brood.Clear();}
}

public sealed class CandyMinionFlight : PatternMover
{
    public int side;
    protected override void StepPattern(float dt)
    {
        if(GameRules.playerDisabled)return;
        float t=age/3.8f;
        if(t>=1){GetComponent<AEnemy>().Depart();return;}
        float u=1-t;
        Vector3 a=origin,b=new Vector3(side*Camera.RightLimit*.82f,origin.y-1,origin.z),c=new Vector3(-side*Camera.RightLimit*.38f,Camera.DownLimit+4,origin.z),d=new Vector3(-side*Camera.RightLimit*.7f,Camera.DownLimit-2,origin.z);
        transform.position=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
        SetFiring(t>.24f && t<.72f);
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh candyHatch;
    public static Mesh CandyHatch(){if(candyHatch!=null)return candyHatch;var b=new Builder();b.Drum(Vector3.zero,1,.15f,9,8,11);return candyHatch=b.Finish("Mint launch aperture");}
    static void BuildCandyRegent(Builder b)
    {
        // Split wafer wings, a dark ganache keel and a crown of sugar crystals.
        BevelArmor(b,new[]{V(0,-1.2f),V(-.46f,-.53f),V(-.50f,.55f),V(0,.86f),V(.50f,.55f),V(.46f,-.53f)},.42f,4);
        foreach(int s in new[]{-1,1})
        {
            b.Prism(new[]{V(s*.34f,-.45f),V(s*1.34f,-.17f),V(s*1.51f,.65f),V(s*.90f,.37f),V(s*.48f,.84f)},.20f,13,new Vector3(0,0,-.04f),5);
            b.Prism(new[]{V(s*.58f,-.20f),V(s*1.27f,.02f),V(s*1.34f,.50f),V(s*.93f,.25f)},.13f,0,new Vector3(0,0,-.21f),14);
            b.Box(new Vector3(s*.65f,.65f,0),new Vector3(.33f,.28f,.30f),11,7);
            b.Drum(new Vector3(s*.82f,-.2f,-.19f),.33f,.16f,11,10,10);
            b.Box(new Vector3(s*.27f,-.86f,-.23f),new Vector3(.15f,.43f,.15f),11,4);
        }
        b.Ball(new Vector3(0,-.08f,-.29f),new Vector3(.39f,.50f,.24f),8);
        b.Prism(new[]{V(-.28f,0),V(-.22f,.20f),V(.22f,.20f),V(.28f,0),V(0,-.20f)},.12f,11,new Vector3(0,-.1f,-.51f),14);
        b.Box(new Vector3(0,-.08f,-.59f),new Vector3(.34f,.075f,.03f),9,11);
        for(int i=-1;i<=1;i++)b.Prism(new[]{V(-.13f,0),V(0,.44f+(i==0 ? .18f : 0)),V(.13f,0)},.18f,10,new Vector3(i*.31f,.53f,-.31f),3);
    }
}
