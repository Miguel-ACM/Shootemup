using UnityEngine;

// A single distant surface: no star GameObjects or particle allocations during combat.
public sealed class SpaceBackdrop : MonoBehaviour
{
    Mesh plane;
    Material sky;
    float clock;
    void Start()
    {
        plane=new Mesh{name="Distant orbital sky"};plane.vertices=new[]{new Vector3(-15,-24),new Vector3(-15,24),new Vector3(15,24),new Vector3(15,-24)};
        plane.triangles=new[]{0,1,2,0,2,3};plane.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh=plane;sky=new Material(Resources.Load<Shader>("SpaceSky"));
        sky.SetFloat("_Seed",LevelRun.Seed%997);
        var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=sky;renderer.sortingLayerName="Background";renderer.sortingOrder=-80;
        transform.localPosition=new Vector3(0,0,125);
    }
    void Update()
    {
        if(sky==null)return;clock+=Time.deltaTime;
        var pilot=CombatVisuals.Player;var p=pilot!=null ? pilot.position : Vector3.zero;
        sky.SetVector("_Drift",new Vector4(-p.x*.10f,clock*.23f-p.y*.025f,clock,0));
    }
    void OnDestroy(){if(plane!=null)Destroy(plane);if(sky!=null)Destroy(sky);}
}
