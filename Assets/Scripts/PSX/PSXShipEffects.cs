using UnityEngine;
using UnityEngine.Rendering;

public sealed class PSXShipEffects : MonoBehaviour
{
    public Mesh flameMesh;
    public Material exhaustMaterial;
    /// <summary>Burner position in Ship local space (mirrored on X). The installer matches it to the visual.</summary>
    public Vector2 exhaustOffset=new Vector2(.2016f,-.245f);
    Transform[] flames;
    MeshRenderer[] renderers;
    SpriteRenderer source;
    Vector3 lastPosition;
    MaterialPropertyBlock properties;
    MeshRenderer[] muzzleRenderers;
    float muzzleUntil;
    public Vector3 MuzzlePosition(int index) => transform.TransformPoint(new Vector3(index==0 ? -.1134f : .1134f,.30f,-.03f));
    public void Shot() { muzzleUntil=Time.time+.045f; }

    void Start()
    {
        source=GetComponent<SpriteRenderer>();lastPosition=transform.position;
        if(flameMesh==null || exhaustMaterial==null)return;
        flames=new Transform[2];renderers=new MeshRenderer[2];properties=new MaterialPropertyBlock();
        muzzleRenderers=new MeshRenderer[2];
        for(int i=0;i<2;i++)
        {
            var flame=new GameObject(i==0 ? "Port exhaust" : "Starboard exhaust");flame.transform.SetParent(transform,false);
            flame.transform.localPosition=new Vector3(i==0 ? -exhaustOffset.x : exhaustOffset.x,exhaustOffset.y,-.08f);
            flame.AddComponent<MeshFilter>().sharedMesh=flameMesh;var renderer=flame.AddComponent<MeshRenderer>();renderer.sharedMaterial=exhaustMaterial;
            renderer.sortingLayerName="Player";renderer.sortingOrder=1;renderer.shadowCastingMode=ShadowCastingMode.Off;
            flames[i]=flame.transform;renderers[i]=renderer;
            var muzzle=new GameObject("Cannon flash");muzzle.transform.SetParent(transform,false);
            muzzle.transform.localPosition=new Vector3(i==0 ? -.1134f : .1134f,.30f,-.08f);
            muzzle.transform.localRotation=Quaternion.Euler(0,0,180);muzzle.transform.localScale=new Vector3(.45f,.19f,1);
            muzzle.AddComponent<MeshFilter>().sharedMesh=flameMesh;
            var flash=muzzle.AddComponent<MeshRenderer>();flash.sharedMaterial=exhaustMaterial;flash.sortingLayerName="Player";flash.sortingOrder=2;flash.enabled=false;muzzleRenderers[i]=flash;
        }
    }

    void LateUpdate()
    {
        if(flames==null)return;
        float speed=Time.deltaTime>0 ? (transform.position-lastPosition).magnitude/Time.deltaTime : 0;
        lastPosition=transform.position;
        float tick=Mathf.Floor(Time.time*15);
        for(int i=0;i<2;i++)
        {
            float pulse=.82f+.23f*Mathf.Sin(tick*2.39f+i*1.7f)+Mathf.Min(speed*.025f,.25f);
            flames[i].localScale=new Vector3(1f,pulse,1f);
            properties.SetColor("_Tint",new Color(1,1,1,source==null ? 1 : source.color.a));
            renderers[i].SetPropertyBlock(properties);renderers[i].enabled=!GameRules.playerDisabled;
            muzzleRenderers[i].enabled=Time.time<muzzleUntil && !GameRules.playerDisabled;
        }
    }
}
