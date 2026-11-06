using System.Collections.Generic;
using UnityEngine;

// Interior moving machinery stays inside the authored damage silhouette. Each boss
// has its own mechanical phrase; neither movement paths nor weakpoint colliders move.
public sealed class BossMechanismRig : MonoBehaviour
{
    sealed class Joint
    {
        public Transform node;
        public MeshRenderer visual, source;
        public Vector3 axis, rest;
        public float speed, swing, offset, charge;
        public bool rotor;
    }
    readonly List<Joint> joints=new List<Joint>();
    MaterialPropertyBlock ink;
    DistrictBoss boss;
    EnemyFiringCue cue;
    float clock,heat;

    void Start()
    {
        ink=new MaterialPropertyBlock();
        boss=GetComponent<DistrictBoss>();cue=GetComponent<EnemyFiringCue>();
        Transform hull=boss.Hull;
        if(boss is FieldsBomberBoss)
        {
            Pair(hull,"Bomber bay shutters",0,.94f,-.29f,-.55f,.7f,12,43);
            Pair(hull,"Engine compressor",1,.74f,.62f,-.37f,110,0,0,true);
        }
        else if(boss is PlainsRedKiteBoss)
        {
            Pair(hull,"Kite intake vanes",0,.59f,.11f,-.46f,.8f,7,32);
            Pair(hull,"Kite thrust stators",1,.45f,.62f,-.40f,165,0,0,true,.56f);
        }
        else if(boss is PlainsThreshingDrumBoss)
        {
            Pair(hull,"Drum belt tensioner",1,1.21f,.16f,-.46f,92,0,0,true,.6f);
            Pair(hull,"Chaff outlet louvers",0,1.13f,-.62f,-.43f,.45f,9,40);
        }
        else if(boss is SnowIcebreakerBoss)
        {
            Pair(hull,"Icebreaker heat exchanger",0,.57f,.65f,-.60f,.36f,6,34);
            Pair(hull,"Icebreaker bow pressure valve",1,.48f,-.67f,-.40f,42,0,0,true,.48f);
        }
        else if(boss is CitySwitchyardBoss)
        {
            Pair(hull,"Switchyard cargo louvers",0,.90f,.15f,-.50f,.32f,6,50);
            Add(hull,"Turnout signal radar",1,new Vector3(.18f,.73f,-.72f),Vector3.forward,48,0,0,0,true,.66f);
        }
        else if(boss is SnowSiegeBoss)
        {
            Add(hull,"Siege concentric loading gear",1,new Vector3(0,.1f,-.65f),Vector3.forward,19,0,0,0,true,1.85f);
            Pair(hull,"Siege shell gates",0,.76f,-.27f,-.59f,.35f,8,38);
        }
        else if(boss is ThunderLanceBoss)
        {
            Pair(hull,"Lance induction turbines",1,.70f,.42f,-.40f,125,0,0,true,.68f);
            Pair(hull,"Lance heat shutters",0,.37f,-.79f,-.40f,.75f,9,47);
        }
        else if(boss is CityDuoBoss duo)
        {
            Pair(hull,duo.heavy?"Hauler cargo jaw":"Interceptor air brake",0,duo.heavy?.72f:.46f,-.12f,-.43f,duo.heavy?.4f:1.1f,duo.heavy?5:12,duo.heavy?44:24);
        }
        else if(boss is TunnelSawBoss)
            Add(hull,"Counterrotating saw bearing",1,new Vector3(0,0,-.52f),Vector3.forward,-390,0,0,0,true,1.2f);
        else if(boss is CandyRegentBoss)
        {
            Pair(hull,"Folded candy ribbon",4,.54f,.30f,-.48f,.65f,17,22);
            Add(hull,"Regent sugar spindle",1,new Vector3(0,.2f,-.58f),Vector3.forward,55,0,0,0,true,.62f);
        }
        else if(boss is AbyssSubmarineBoss)
            Pair(hull,"Pressure tube ventilation",0,.69f,.35f,-.62f,.27f,6,35);
        else if(boss is HellCrabBoss crab)
        {
            for(int i=0;i<3;i++)Pair(crab.Hull,"Breathing carapace gill",3,.92f-i*.10f,.48f-i*.27f,-.68f,.45f+i*.06f,7,13);
        }
        else if(boss is DesertWormBoss worm)
        {
            for(int i=0;i<worm.Cores.Length;i++)
            {
                Transform section=worm.Cores[i].transform.parent;
                Pair(section,"Worm articulating gill",3,.49f,-.13f,-.30f,.55f,9,18);
            }
        }
    }

    void Pair(Transform parent,string name,int kind,float x,float y,float z,float speed,float swing,float charge,bool spin=false,float size=1)
    {
        for(int side=-1;side<=1;side+=2)
            Add(parent,name+(side<0?" left":" right"),kind,new Vector3(side*x,y,z),spin?Vector3.forward*side:Vector3.up*side,
                speed,swing,charge,kind==3?y*2:0,spin,size);
    }
    void Add(Transform parent,string name,int kind,Vector3 p,Vector3 axis,float speed,float swing,float charge,float offset,bool rotor,float size)
    {
        var node=new GameObject(name).transform;node.SetParent(parent,false);node.localPosition=p;node.localScale=Vector3.one*size;
        node.gameObject.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.BossMechanism(kind);
        var visual=node.gameObject.AddComponent<MeshRenderer>();
        var source=parent.GetComponent<MeshRenderer>();
        visual.sharedMaterial=source.sharedMaterial;visual.sortingLayerID=source.sortingLayerID;visual.sortingOrder=source.sortingOrder;
        joints.Add(new Joint{node=node,visual=visual,source=source,axis=axis,rest=p,speed=speed,swing=swing,charge=charge,offset=offset,rotor=rotor});
    }
    void LateUpdate()
    {
        if(boss==null||!boss.enabled||boss.health<=0||WorldTimeStop.Frozen||GameRules.playerDisabled)return;
        float delta=Time.deltaTime;
        clock+=delta*(1+(boss.CurrentPhase-1)*.16f);
        heat=Mathf.MoveTowards(heat,cue.ChargeLevel,delta*3);
        foreach(var joint in joints)
        {
            if(joint.node==null)continue;
            float angle=joint.rotor?clock*joint.speed:Mathf.Sin(clock*joint.speed*6.283f+joint.offset)*joint.swing+heat*joint.charge;
            joint.node.localRotation=Quaternion.AngleAxis(angle,joint.axis);
            joint.node.localPosition=joint.rest+Vector3.forward*(cue.ReleaseLevel*.035f);
            joint.source.GetPropertyBlock(ink);joint.visual.SetPropertyBlock(ink);joint.visual.enabled=joint.source.enabled;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] bossMechanisms=new Mesh[5];
    public static Mesh BossMechanism(int kind)
    {
        if(bossMechanisms[kind]!=null)return bossMechanisms[kind];
        var b=new Builder();
        if(kind==1)
        {
            CutBand(b,.24f,.032f,.025f,12,6);
            CutStone(b,Vector3.back*.035f,new Vector3(.10f,.10f,.035f),11,6);
            for(int i=0;i<9;i++)
            {
                float a=i*Mathf.PI*2/9;Vector3 u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),v=new Vector3(-u.y,u.x,0);
                CutBlade(b,new[]{u*.09f,u*.16f+v*.04f,u*.25f+v*.02f},.036f,.022f,i%3==0?3:6);
            }
        }
        else if(kind==3||kind==4)
        {
            BevelArmor(b,new[]{V(0,-.29f),V(-.17f,-.12f),V(-.11f,.17f),V(0,.31f),V(.17f,.10f),V(.13f,-.15f)},.13f,kind==3?6:15,0,.18f);
            CutBlade(b,new[]{new Vector3(0,-.26f,-.07f),new Vector3(-.04f,-.08f,-.10f),new Vector3(.04f,.12f,-.10f),new Vector3(0,.28f,-.06f)},.014f,.012f,kind==3?3:9);
        }
        else
        {
            b.Box(Vector3.zero,new Vector3(.29f,.43f,.06f),11,6);
            for(int i=0;i<5;i++)b.Box(new Vector3(0,-.16f+i*.08f,-.05f),new Vector3(.25f,.04f,.055f),i==0?3:6,11);
            b.Box(new Vector3(-.14f,0,-.025f),new Vector3(.035f,.47f,.09f),6,11);
            b.Box(new Vector3(.14f,0,-.025f),new Vector3(.035f,.47f,.09f),6,11);
        }
        return bossMechanisms[kind]=b.Finish("Boss articulated mechanism "+kind);
    }
}
