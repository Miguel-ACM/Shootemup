using UnityEngine;

public sealed class VoidAnatomyVisual : MonoBehaviour
{
    readonly Transform[] limbs=new Transform[4];
    readonly Transform[] ports=new Transform[4];
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
            node.AddComponent<MeshFilter>();var r=node.AddComponent<MeshRenderer>();r.sharedMaterial=source.sharedMaterial;r.sortingLayerID=source.sortingLayerID;
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
            limbs[i].gameObject.SetActive(i<(form==3?4:2));
            if(limbs[i].gameObject.activeSelf)limbs[i].GetComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.VoidLimb(form,i);
            limbs[i].localRotation=Quaternion.identity;
        }
    }
    public Vector3 Port(int index,Vector3 local)=>form==0?transform.TransformPoint(local):limbs[index%(form==3?4:2)].TransformPoint(local);
    public Transform SetPort(int index,Vector3 local)
    {
        ports[index].SetParent(form==0?transform:limbs[index%(form==3?4:2)],false);
        ports[index].localPosition=local;return ports[index];
    }
    public Transform Emitter(int index)=>ports[index];
    void LateUpdate()
    {
        if(boss==null||!boss.enabled||boss.Hidden||WorldTimeStop.Frozen||GameRules.playerDisabled)return;
        clock+=Time.deltaTime;charge=Mathf.MoveTowards(charge,cue.ChargeLevel,Time.deltaTime*3);
        source.GetPropertyBlock(ink);
        for(int i=0;i<(form==3?4:2);i++)
        {
            // Hinged silhouette changes. The central target and its collider stay still.
            float side=i==0?-1:1;
            float swing=form==0?4:form==1?7:form==2?5:8;
            float angle=Mathf.Sin(clock*(form==2?1.8f:.85f)+i*.6f)*swing;
            angle+=charge*(form==1?12:form==2?8:4);
            limbs[i].localRotation=Quaternion.Euler(0,side*Mathf.Sin(clock*.7f)*5,angle*(form==3?1:side));
            limbs[i].GetComponent<MeshRenderer>().SetPropertyBlock(ink);
        }
    }
}
