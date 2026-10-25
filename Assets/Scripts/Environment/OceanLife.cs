using UnityEngine;

// Two schools, their seabed shadows, and a slow ray. Ambient models never enter combat physics.
public sealed class OceanLife : MonoBehaviour
{
    struct Fish {public Transform model,shadow;public Vector3 home;public float phase,rate,size;}
    Fish[] fish;
    Material ink,shadowInk;
    float clock;
    void Start()
    {
        ink=new Material(CombatMaterials.Fleet);ink.SetColor("_Tint",new Color(.36f,.65f,.68f,.65f));
        shadowInk=new Material(CombatMaterials.Fleet);shadowInk.SetColor("_Tint",new Color(.04f,.11f,.15f,.22f));
        fish=new Fish[23];var random=new System.Random(LevelRun.Seed+7713);
        for(int i=0;i<fish.Length;i++)
        {
            bool ray=i==22;float s=ray ? 1.05f : .15f+(float)random.NextDouble()*.13f;
            var mesh=IndustrialMeshFactory.OceanAnimal(ray);
            var model=Add(ray ? "Distant manta" : "Reef fish",mesh,ink,68,-32);var shadow=Add("Soft fish shadow",mesh,shadowInk,82,-44);
            var home=new Vector3((i<11 ? -3.0f : 2.0f)+(i%4)*.45f,(i%11)*1.7f-8,0);
            fish[i]=new Fish{model=model,shadow=shadow,home=home,phase=(float)random.NextDouble()*6.28f,rate=ray ? .24f : .42f+(float)random.NextDouble()*.18f,size=s};
        }
    }
    Transform Add(string name,Mesh mesh,Material material,float z,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.forward*z;go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=order;return go.transform;
    }
    void Update()
    {
        clock+=Time.deltaTime;if(fish==null)return;
        for(int i=0;i<fish.Length;i++)
        {
            var f=fish[i];float t=clock*f.rate+f.phase;float x=Mathf.Repeat(f.home.x+clock*(i<11 ? .55f : -.45f)+9,18)-9;
            var p=new Vector3(x,f.home.y+Mathf.Sin(t)*.35f,68);float bank=90*(i<11 ? -1 : 1)+Mathf.Cos(t)*9;
            f.model.localPosition=p;f.model.localRotation=Quaternion.Euler(0,Mathf.Sin(t*3)*9,bank);f.model.localScale=new Vector3(f.size*(1+.08f*Mathf.Sin(t*4)),f.size,f.size);
            f.shadow.localPosition=p+new Vector3(.34f,-.42f,14);f.shadow.localRotation=f.model.localRotation;f.shadow.localScale=new Vector3(f.size*1.2f,f.size*1.2f,.05f);
        }
    }
    void OnDestroy(){if(ink!=null)Destroy(ink);if(shadowInk!=null)Destroy(shadowInk);}
}
