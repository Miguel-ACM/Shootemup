using UnityEngine;

// Separate cosmetic machinery: never scale/rotate the hull, weakpoint or collider.
// Each district has its own deployment motion, built before combat starts.
public sealed class BossPhaseEntry : MonoBehaviour
{
    readonly Transform[] vanes = new Transform[8];
    readonly Vector3[] homes = new Vector3[8];
    DistrictBoss boss;
    float clock;
    bool running;
    public void Build(DistrictBoss owner)
    {
        boss = owner;
        for (int i=0;i<vanes.Length;i++)
        {
            var go=new GameObject("Phase mechanism " + i);
            vanes[i]=go.transform;
            go.transform.SetParent(owner.Hull,false);
            go.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.PhaseVane(owner.hull);
            var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=owner.Hull.GetComponent<MeshRenderer>().sharedMaterial;
            renderer.sortingLayerName="Enemy";
            float a=i*Mathf.PI*.25f;
            homes[i]=new Vector3(Mathf.Cos(a)*.57f,Mathf.Sin(a)*.57f,-.55f);
            go.SetActive(false);
        }
    }
    public void Begin()
    {
        clock=0;running=true;
        foreach(var vane in vanes) vane.gameObject.SetActive(true);
        PSXImpactEffects.Burst(boss.transform.position,LevelProjectilePalette.Accent(LevelRun.Current),true);
    }
    void Update()
    {
        if(!running || GameRules.playerDisabled) return;
        clock+=Time.deltaTime;
        float t=Mathf.Clamp01(clock/1.45f), open=Mathf.Sin(t*Mathf.PI), settle=Mathf.SmoothStep(0,1,t);
        for(int i=0;i<vanes.Length;i++)
        {
            float s=i%2==0?-1:1, a=i*Mathf.PI*.25f;
            var p=homes[i];float turn=i*45;
            if(boss is FieldsBomberBoss) { p=new Vector3(s*(.5f+open*.7f),.45f-(i/2)*.2f,-.45f); turn=s*(25+open*65); }
            else if(boss is SnowSiegeBoss) { a+=settle*.8f;p=new Vector3(Mathf.Cos(a)*(.55f+open*.7f),Mathf.Sin(a)*(.55f+open*.7f),-.65f);turn=a*Mathf.Rad2Deg; }
            else if(boss is ThunderLanceBoss) {p=new Vector3(s*(.55f+open*.35f),.6f-(i/2)*.37f,-.4f);turn=s*open*80;}
            else if(boss is CityDuoBoss) {p=new Vector3(s*(.4f+open*.35f),.65f-(i/2)*.28f,-.4f);turn=s*open*120;}
            else if(boss is TunnelSawBoss) {a+=settle*3;p=new Vector3(Mathf.Cos(a)*(1+open*.3f),Mathf.Sin(a)*(1+open*.3f),-.35f);turn=a*Mathf.Rad2Deg;}
            else if(boss is DesertWormBoss) {p=new Vector3(s*(.35f+open*.4f),.65f-(i/2)*.25f,-.45f);turn=s*Mathf.Sin(t*Mathf.PI*2-i*.35f)*45;}
            else if(boss is CandyRegentBoss) {p=new Vector3(Mathf.Cos(a)*(.4f+open*.45f),.8f+open*.6f+Mathf.Sin(a)*.2f,-.5f);turn=i*45+settle*180;}
            else if(boss is SpaceCitadelBoss) {p=homes[i]*(1+open*1.1f);p.z=-.6f;turn=i*45+open*80;}
            else if(boss is AbyssSubmarineBoss) {p=new Vector3(s*(.55f+open*.28f),.7f-(i/2)*.45f,-.6f);turn=s*open*65;}
            else if(boss is HellCrabBoss) {p=new Vector3(s*(.6f+open*.5f),.45f-(i/2)*.28f,-.9f);turn=s*(25+open*55);}
            vanes[i].localPosition=p;
            vanes[i].localRotation=Quaternion.Euler(open*35,0,turn);
            vanes[i].localScale=Vector3.one*(.55f+open*.45f)*Mathf.Min(1,(1-t)*6);
        }
        if(t>=1) { running=false;foreach(var vane in vanes) vane.gameObject.SetActive(false); }
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly System.Collections.Generic.Dictionary<int,Mesh> phaseVanes=new System.Collections.Generic.Dictionary<int,Mesh>();
    public static Mesh PhaseVane(int hull)
    {
        if(phaseVanes.TryGetValue(hull,out var mesh)&&mesh!=null)return mesh;
        var b=new Builder();
        if(hull==38) b.Prism(new[]{V(-.1f,0),V(0,.42f),V(.1f,0),V(0,-.14f)},.12f,10,Vector3.zero,4);
        else if(hull==49) BevelArmor(b,new[]{V(-.16f,-.17f),V(.16f,-.17f),V(.22f,.12f),V(0,.34f),V(-.22f,.12f)},.14f,6);
        else {b.Box(Vector3.zero,new Vector3(.18f,.38f,.1f),hull==45?15:6,11);b.Box(Vector3.back*.07f,new Vector3(.055f,.24f,.03f),10,11);}
        return phaseVanes[hull]=b.Finish("Deploying phase vent " + hull);
    }
}
