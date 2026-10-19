using UnityEngine;

public sealed class PSXProjectileEffects : MonoBehaviour
{
    public Material tracerMaterial;
    void Start()
    {
        var trailObject=new GameObject("Hot tracer wake");trailObject.transform.SetParent(transform,false);
        trailObject.transform.localPosition=new Vector3(0,-.13f,-.01f);
        var trail=trailObject.AddComponent<TrailRenderer>();trail.sharedMaterial=tracerMaterial;
        trail.time=.045f;trail.minVertexDistance=.07f;trail.startWidth=.075f;trail.endWidth=0;
        trail.startColor=new Color(1,.74f,.32f,.85f);trail.endColor=new Color(.65f,.24f,.06f,0);
        trail.sortingLayerName="Bullet";trail.sortingOrder=1;
        trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        // A bounded lifetime also handles shots that never become visible to a camera.
        Destroy(gameObject,4f);
    }
}
