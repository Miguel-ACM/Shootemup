using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Recycled industrial structures and tumbling salvage, behind gameplay.</summary>
public sealed class PSXIndustrialField : MonoBehaviour
{
    public Mesh[] debrisMeshes;
    public Material steelMaterial;
    public Material compositeMaterial;
    [Range(0.2f, 2f)] public float scrollMultiplier = 1f;
    readonly List<Drifter> objects = new List<Drifter>();
    readonly List<GameObject> owned = new List<GameObject>();
    readonly System.Random random = new System.Random(107);
    sealed class Drifter { public Transform transform; public float speed; public Vector3 spin; }

    void Start()
    {
        var camera = UnityEngine.Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = new Color(.018f,.023f,.03f);
            var presentation=camera.GetComponent<PSXPresentation>();
            if(presentation==null)presentation=camera.gameObject.AddComponent<PSXPresentation>();
            presentation.Configure(compositeMaterial);
        }
        if (debrisMeshes == null || debrisMeshes.Length < 6 || steelMaterial == null) return;
        for (int i=0;i<9;i++)
        {
            float y=-17+i*4.2f;
            Add("Shaft floor",0,new Vector3(0,y,85),new Vector3(10.4f,4.12f,1),new Color(.29f,.31f,.33f),2.2f,Vector3.zero,-40);
            foreach (float side in new[] {-1f,1f})
            {
                Add("Wall panel",0,new Vector3(side*4.55f,y,65),new Vector3(1.55f,4.05f,2),new Color(.43f,.45f,.46f),2.8f,Vector3.zero,-30);
                Add("Shaft guide rail",1,new Vector3(side*3.78f,y,60),new Vector3(.26f,2.3f,.7f),new Color(.44f,.46f,.45f),2.8f,Vector3.zero,-29);
                if(i%2==0)
                    Add("Vent housing",3,new Vector3(side*4.28f,y+.2f,57),new Vector3(.95f,1.15f,1),new Color(.45f,.47f,.47f),2.8f,Vector3.zero,-28);
            }
        }
        for (int i=0;i<22;i++)
        {
            float size=Range(.33f,.83f);
            var part=Add("Falling salvage",1+i%5,new Vector3(Range(-4.5f,4.5f),Range(-15,20),Range(36,52)),Vector3.one*size,
                Color.Lerp(new Color(.45f,.49f,.50f),new Color(.72f,.73f,.70f),Range(0,1)),Range(3.3f,5.1f),new Vector3(Range(-12,12),Range(-14,14),Range(-18,18)),-15);
            part.localRotation=Quaternion.Euler(Range(-35,35),Range(-35,35),Range(-180,180));
        }
    }

    float Range(float lo,float hi) { return Mathf.Lerp(lo,hi,(float)random.NextDouble()); }

    Transform Add(string name,int meshIndex,Vector3 position,Vector3 scale,Color tint,float speed,Vector3 spin,int order)
    {
        var part=new GameObject(name);part.transform.SetParent(transform,false);part.transform.localPosition=position;part.transform.localScale=scale;
        part.AddComponent<MeshFilter>().sharedMesh=debrisMeshes[meshIndex];
        var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=steelMaterial;
        renderer.sortingLayerName="Background";renderer.sortingOrder=order;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var properties=new MaterialPropertyBlock();properties.SetColor("_Tint",tint);renderer.SetPropertyBlock(properties);
        objects.Add(new Drifter {transform=part.transform,speed=speed,spin=spin});owned.Add(part);return part.transform;
    }

    void Update()
    {
        foreach(var part in objects)
        {
            var position=part.transform.localPosition;position.y-=part.speed*scrollMultiplier*Time.deltaTime;
            if(position.y < -21) position.y+=37.8f;
            part.transform.localPosition=position;
            part.transform.Rotate(part.spin*Time.deltaTime,Space.Self);
        }
    }
}
