using UnityEngine;

public sealed class VoidAnatomyVisual : MonoBehaviour
{
    readonly Transform[] limbs=new Transform[4];
    readonly Transform[] ports=new Transform[4];
    readonly MeshRenderer[] surfaces = new MeshRenderer[4];
    public float Fold { get; set; }
    MaterialPropertyBlock ink;
    MeshRenderer source;
    VoidSovereignBoss boss;
    EnemyFiringCue cue;
    int form;
    float clock,charge;
    public void Build(VoidSovereignBoss owner)
    {
        ink=new MaterialPropertyBlock();
        boss=owner;source=GetComponent<MeshRenderer>();cue=owner.GetComponent<EnemyFiringCue>();
        for(int i=0;i<4;i++)
        {
            var node=new GameObject("Obsidian joint "+i);node.transform.SetParent(transform,false);limbs[i]=node.transform;
            node.AddComponent<MeshFilter>();var r=node.AddComponent<MeshRenderer>();r.sharedMaterial=source.sharedMaterial;r.sortingLayerID=source.sortingLayerID;surfaces[i]=r;
            ports[i]=new GameObject("Living firing aperture "+i).transform;ports[i].SetParent(transform,false);
        }
    }
    public void SetForm(int value)
    {
        form=value;
        if (form < 0)
        {
            foreach (var limb in limbs) limb.gameObject.SetActive(false);
            return;
        }
        GetComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.VoidCore(form);
        for(int i=0;i<4;i++)
        {
            limbs[i].gameObject.SetActive(i<(form>=3?4:2));
            if(limbs[i].gameObject.activeSelf)limbs[i].GetComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.VoidLimb(form,i);
            limbs[i].localPosition=Vector3.zero;
            limbs[i].localRotation=Quaternion.identity;
        }
    }
    public Vector3 Port(int index,Vector3 local)=>form==0?transform.TransformPoint(local):limbs[index%(form>=3?4:2)].TransformPoint(local);
    public Transform SetPort(int index,Vector3 local)
    {
        ports[index].SetParent(form==0?transform:limbs[index%(form>=3?4:2)],false);
        ports[index].localPosition=local;return ports[index];
    }
    public Transform Emitter(int index)=>ports[index];
    void LateUpdate()
    {
        if(boss==null||!boss.enabled||WorldTimeStop.Frozen||GameRules.playerDisabled)return;
        clock+=Time.deltaTime;charge=Mathf.MoveTowards(charge,cue.ChargeLevel,Time.deltaTime*3);
        source.GetPropertyBlock(ink);
        for(int i=0;i<(form>=3?4:2);i++)
        {
            // Hinged silhouette changes. The central target and its collider stay still.
            float side = (i & 1) == 0 ? -1 : 1;
            float sweep = form == 1 ? 13 : form == 2 ? 9 : 7;
            float rhythm = Mathf.Sin(clock*(form==2?1.55f:1.1f) + i*.65f);
            float angle = side * (rhythm*sweep + charge*(form==1?18:9));
            // Hinges close in sequence. Opposing vanes fan apart on the way back out.
            float fold = Mathf.SmoothStep(0,1,Mathf.Clamp01(Fold*1.3f - i*.10f));
            limbs[i].localRotation = Quaternion.Euler(fold*(i<2?16:-16),
                side * (rhythm*6 + fold*54), angle + side*fold*(i<2?52:-46));
            limbs[i].localPosition = new Vector3(side*.32f, i<2?.23f:-.23f,-.17f) * fold;
            surfaces[i].SetPropertyBlock(ink);
        }
    }
}
