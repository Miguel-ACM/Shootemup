using UnityEngine;
using System.Collections.Generic;

public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<int,Mesh> combatHulls=new Dictionary<int,Mesh>();
    public static Mesh CombatHull(int kind)
    {
        if(combatHulls.TryGetValue(kind,out var cached))return cached;
        var b=new Builder();bool heavy=kind==2 || kind==5 || kind==7 || kind==8 || kind==14 || kind>=20;
        bool round=kind==21 || kind==20;bool slim=kind==22;
        float width=slim ? .48f : heavy ? 1.12f : .72f;
        float length=slim ? 1.55f : heavy ? 1.12f : 1.0f;
        int armor=kind/3==2 ? 1 : kind/3==3 ? 7 : 0;
        if(round)
        {
            b.Drum(Vector3.zero,1.15f,.38f,armor,16,6);
            b.Drum(new Vector3(0,0,-.23f),.95f,.14f,6,16);
            b.Drum(new Vector3(0,0,-.35f),.73f,.2f,armor,12);
            b.Drum(new Vector3(0,0,-.49f),.31f,.12f,9,12);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                if(kind==20)
                    b.Prism(new[]{(Vector2)(p*1.05f),(Vector2)(Quaternion.Euler(0,0,14)*p*1.05f),(Vector2)(Quaternion.Euler(0,0,8)*p*1.43f)},.18f,3,Vector3.zero,11);
                else
                {
                    b.Drum(p*.94f+Vector3.back*.26f,.18f,.18f,i%2==0 ? 1 : 6,6);
                    b.Drum(p*.94f+Vector3.back*.4f,.08f,.08f,10,6);
                }
            }
        }
        else
        {
            // A bevelled central fuselage, swept wings, separate engine pods and raised cockpit.
            b.Prism(new[]{new Vector2(-width*.46f,length*.65f),new Vector2(width*.46f,length*.65f),new Vector2(width*.62f,-length*.18f),new Vector2(0,-length),new Vector2(-width*.62f,-length*.18f)},.42f,armor,Vector3.zero,6);
            b.Prism(new[]{new Vector2(-width*.30f,length*.45f),new Vector2(width*.30f,length*.45f),new Vector2(width*.26f,-length*.28f),new Vector2(0,-length*.72f),new Vector2(-width*.26f,-length*.28f)},.12f,1,new Vector3(0,0,-.26f),11);
            b.Prism(new[]{new Vector2(-.16f,.28f),new Vector2(.16f,.28f),new Vector2(.12f,-.12f),new Vector2(0,-.28f),new Vector2(-.12f,-.12f)},.14f,9,new Vector3(0,-length*.18f,-.37f),6);
            foreach(int side in new[]{-1,1})
            {
                float wing=kind%3==0 ? 1.6f : kind%3==1 ? 1.15f : 1.35f;
                b.Prism(new[]{new Vector2(side*width*.35f,.55f),new Vector2(side*width*wing,.35f),new Vector2(side*width*(wing-.12f),-.18f),new Vector2(side*width*.4f,-.58f)},.16f,armor,Vector3.zero,6);
                b.Box(new Vector3(side*width*.78f,.38f,-.08f),new Vector3(.32f,.90f,.42f),6,11);
                b.Box(new Vector3(side*width*.78f,.46f,-.32f),new Vector3(.24f,.60f,.08f),armor);
                b.Drum(new Vector3(side*width*.78f,.89f,0),.18f,.32f,11,8,6);
                b.Box(new Vector3(side*width*.71f,-.64f,-.10f),new Vector3(.12f,.72f,.14f),11,6);
                b.Box(new Vector3(side*width*.71f,-.98f,-.12f),new Vector3(.18f,.12f,.19f),3);
                for(int j=0;j<4;j++)b.Box(new Vector3(side*width*.78f,.25f+j*.12f,-.38f),new Vector3(.19f,.045f,.035f),11);
                b.Box(new Vector3(side*width*1.04f,.13f,-.12f),new Vector3(.11f,.22f,.05f),3);
                if(heavy)for(int j=0;j<3;j++)b.Drum(new Vector3(side*.66f,-.45f+j*.35f,-.3f),.16f,.16f,6,8);
                if(kind/3==3 || slim)for(int j=0;j<3;j++)b.Drum(new Vector3(side*width,.2f-j*.24f,-.28f),.15f,.12f,9,8);
            }
            b.Box(new Vector3(0,length*.59f,-.33f),new Vector3(.09f,.4f,.13f),11);
            for(int side=-1;side<=1;side+=2)for(int j=0;j<3;j++)
                b.Drum(new Vector3(side*width*.38f,.4f-j*.22f,-.33f),.035f,.04f,3,6);
        }
        return combatHulls[kind]=b.Finish("District hull "+kind);
    }
}
public sealed class CombatHullVisual : MonoBehaviour
{
    public Transform model;
    public float spin;
    SpriteRenderer source;
    MeshRenderer[] parts;
    MaterialPropertyBlock properties;
    Vector3 previous;
    float bank;
    void Start(){source=GetComponent<SpriteRenderer>();parts=model.GetComponentsInChildren<MeshRenderer>();properties=new MaterialPropertyBlock();previous=transform.position;}
    void LateUpdate()
    {
        if(model==null)return;
        float velocity=Time.deltaTime>0 ? (transform.position.x-previous.x)/Time.deltaTime : 0;previous=transform.position;
        bank=Mathf.Lerp(bank,Mathf.Clamp(velocity*2,-15,15),Time.deltaTime*8);
        if(spin!=0)model.Rotate(0,0,spin*Time.deltaTime,Space.Self);else model.localRotation=Quaternion.Euler(0,bank,0);
        properties.SetColor("_Tint",source!=null ? source.color : Color.white);
        foreach(var part in parts)if(part!=null)part.SetPropertyBlock(properties);
    }
}
