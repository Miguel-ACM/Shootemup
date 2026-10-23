using UnityEngine;
using System.Collections.Generic;

public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<int,Mesh> combatHulls=new Dictionary<int,Mesh>();
    public static Vector3[] CombatEngineSockets(int kind)
    {
        if(kind==0)return new[]{new Vector3(-0.55f,0.78f,0),new Vector3(0.55f,0.78f,0)};
        if(kind==1)return new[]{new Vector3(-0.64f,0.93f,0),new Vector3(0.64f,0.93f,0)};
        if(kind==2)return new[]{new Vector3(-0.95f,0.82f,0),new Vector3(0.95f,0.82f,0)};
        if(kind==5)return new[]{new Vector3(-0.87f,1.18f,0),new Vector3(0.87f,1.18f,0)};
        if(kind==6)return new[]{new Vector3(-0.55f,0.8f,0),new Vector3(0.55f,0.8f,0)};
        if(kind==7)return new[]{new Vector3(-0.76f,0.94f,0),new Vector3(0.76f,0.94f,0)};
        if(kind==8)return new[]{new Vector3(-0.55f,0.99f,0),new Vector3(0.55f,0.99f,0)};
        if(kind==9)return new[]{new Vector3(-0.3f,0.83f,0),new Vector3(0.3f,0.83f,0)};
        if(kind==10)return new[]{new Vector3(-0.42f,0.84f,0),new Vector3(0.42f,0.84f,0)};
        if(kind==11)return new[]{new Vector3(-0.42f,0.73f,0),new Vector3(0.42f,0.73f,0)};
        if(kind==12)return new[]{new Vector3(-0.43f,0.91f,0),new Vector3(0.43f,0.91f,0)};
        float x=kind==22 ? .375f : kind==2 || kind==5 || kind==7 || kind==8 || kind==14 || kind==16 || kind>=20 ? .874f : .562f;
        return new[]{new Vector3(-x,.93f,0),new Vector3(x,.93f,0)};
    }
    public static Mesh CombatHull(int kind)
    {
        if(combatHulls.TryGetValue(kind,out var cached))return cached;
        var b=new Builder();bool heavy=kind==2 || kind==5 || kind==7 || kind==8 || kind==14 || kind==16 || kind>=20;
        bool round=kind==21 || kind==20;bool slim=kind==22;
        float width=slim ? .48f : heavy ? 1.12f : .72f;
        float length=slim ? 1.55f : heavy ? 1.12f : 1.0f;
        int armor=kind==6 || kind==7 || kind==8 || kind==16 || kind==21 || kind==26 ? 1 : kind==9 || kind==10 || kind==11 || kind==17 || kind==22 || kind==27 ? 7 : kind>=12 && kind<=15 || kind==28 ? 12 : 0;
        if(kind==0){BuildScoutAirframe(b);return combatHulls[kind]=b.Finish("ScoutAirframe");}
        if(kind==1){BuildPatrolAirframe(b);return combatHulls[kind]=b.Finish("PatrolAirframe");}
        if(kind==2){BuildBomberAirframe(b);return combatHulls[kind]=b.Finish("BomberAirframe");}
        if(kind==5){BuildCarrierAirframe(b);return combatHulls[kind]=b.Finish("CarrierAirframe");}
        if(kind==6){BuildSnowFighterAirframe(b);return combatHulls[kind]=b.Finish("SnowFighterAirframe");}
        if(kind==7){BuildSnowSiegeAirframe(b);return combatHulls[kind]=b.Finish("SnowSiegeAirframe");}
        if(kind==8){BuildShieldAirframe(b);return combatHulls[kind]=b.Finish("ShieldAirframe");}
        if(kind==9){BuildBeamDroneAirframe(b);return combatHulls[kind]=b.Finish("BeamDroneAirframe");}
        if(kind==10){BuildRelayAirframe(b);return combatHulls[kind]=b.Finish("RelayAirframe");}
        if(kind==11){BuildStormSkimmerAirframe(b);return combatHulls[kind]=b.Finish("StormSkimmerAirframe");}
        if(kind==12){BuildPursuitAirframe(b);return combatHulls[kind]=b.Finish("PursuitAirframe");}
        if(BuildRoleHull(b,kind,armor))return combatHulls[kind]=b.Finish("District hull "+kind);
        if(round)
        {
            b.Drum(Vector3.zero,1.15f,.38f,armor,16,6);
            b.Drum(new Vector3(0,0,-.23f),.95f,.14f,6,16);
            b.Drum(new Vector3(0,0,-.35f),.73f,.2f,armor,12);
            b.Ball(new Vector3(0,0,-.37f),new Vector3(.68f,.68f,.28f),armor);
            b.Drum(new Vector3(0,0,-.49f),.31f,.12f,9,12);
            b.Drum(new Vector3(0,0,-.66f),.17f,.08f,11,8);
            b.Drum(new Vector3(0,0,-.72f),.11f,.04f,kind==20 ? 3 : 9,8);
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
            if(kind==21)for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.63f;
                b.Box(p+Vector3.back*.57f,new Vector3(.17f,.23f,.14f),4,6);
                b.Drum(p+Vector3.back*.67f,.055f,.09f,10,8);
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
                float wing=kind==15 ? .88f : kind==12 ? 1.8f : kind%3==0 ? 1.6f : kind%3==1 ? 1.15f : 1.35f;
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
                // Large silhouette features distinguish roles before their paint or weapon tells.
                if(kind==2 || kind==5 || kind==16 || kind==23)
                {
                    b.Box(new Vector3(side*width*.44f,.12f,-.47f),new Vector3(.25f,.8f,.19f),11);
                    for(int rack=0;rack<3;rack++)b.Drum(new Vector3(side*width*.44f,.35f-rack*.24f,-.59f),.105f,.16f,kind==16 ? 4 : 3,8,6);
                }
                if(kind==3 || kind==7 || kind==25 || kind==26)
                {
                    b.Box(new Vector3(side*.36f,-.5f,-.4f),new Vector3(.22f,1.3f,.26f),6,11);
                    b.Box(new Vector3(side*.36f,-1.13f,-.43f),new Vector3(.29f,.18f,.3f),11,3);
                }
                if(kind==4 || kind==10)
                {
                    b.Box(new Vector3(side*.87f,0,-.05f),new Vector3(.9f,.16f,.16f),6);
                    b.Drum(new Vector3(side*1.25f,0,-.1f),.26f,.15f,11,12,6);
                    b.Drum(new Vector3(side*1.25f,0,-.2f),.18f,.11f,9,12);
                }
                if(kind==8 || kind==14 || kind==28)
                {
                    b.Prism(new[]{new Vector2(side*.75f,.7f),new Vector2(side*1.4f,.5f),new Vector2(side*1.3f,-.65f),new Vector2(side*.77f,-.8f)},.23f,armor,new Vector3(0,0,-.27f),6);
                    b.Box(new Vector3(side*1.1f,-.05f,-.44f),new Vector3(.08f,.75f,.06f),kind==8 ? 9 : 10);
                }
                if(slim || kind==17)
                {
                    b.Prism(new[]{new Vector2(side*.5f,.4f),new Vector2(side*.79f,-.95f),new Vector2(side*.48f,-1.65f),new Vector2(side*.32f,-.4f)},.2f,7,new Vector3(0,0,-.3f),6);
                    for(int coil=0;coil<5;coil++)b.Box(new Vector3(side*.53f,-.35f-coil*.19f,-.45f),new Vector3(.18f,.07f,.1f),9);
                }
            }
            b.Box(new Vector3(0,length*.59f,-.33f),new Vector3(.09f,.4f,.13f),11);
            for(int side=-1;side<=1;side+=2)for(int j=0;j<3;j++)
                b.Drum(new Vector3(side*width*.38f,.4f-j*.22f,-.33f),.035f,.04f,3,6);
        }
        return combatHulls[kind]=b.Finish("District hull "+kind);
    }

    static bool BuildRoleHull(Builder b,int kind,int armor)
    {
        void Plate(float x,float y,float z,float w,float h,float d,int tile)
        {
            float c=Mathf.Min(w,h)*.17f;
            b.Prism(new[]{new Vector2(-w/2+c,-h/2),new Vector2(w/2-c,-h/2),new Vector2(w/2,-h/2+c),new Vector2(w/2,h/2-c),new Vector2(w/2-c,h/2),new Vector2(-w/2+c,h/2),new Vector2(-w/2,h/2-c),new Vector2(-w/2,-h/2+c)},d,tile,new Vector3(x,y,z),6);
        }
        void Engine(float x,float y,float span=.32f)
        {
            Plate(x,y,-.05f,span,.8f,.34f,6);Plate(x,y,-.25f,span*.72f,.55f,.08f,armor);
            for(int vent=0;vent<4;vent++)b.Box(new Vector3(x,y-.18f+vent*.12f,-.31f),new Vector3(span*.58f,.045f,.04f),11);
            b.Box(new Vector3(x,y+.43f,0),new Vector3(span*.86f,.13f,.28f),11,6);
        }
        if(kind==3 || kind==25)
        {
            // Wall-mounted gun carriage: a broad hinge, rotating turret and twin recoil barrels.
            Plate(0,.1f,0,1.65f,1.4f,.28f,11);
            foreach(int side in new[]{-1,1})
            {
                Plate(side*.73f,.05f,-.13f,.38f,1.1f,.28f,0);
                for(int lug=0;lug<3;lug++)b.Box(new Vector3(side*.88f,-.35f+lug*.4f,-.24f),new Vector3(.27f,.16f,.16f),6);
                b.Box(new Vector3(side*.27f,-.65f,-.33f),new Vector3(.19f,1.1f,.21f),11);
                Plate(side*.27f,-1.17f,-.33f,.29f,.24f,.3f,6);
                b.Box(new Vector3(side*.27f,-1.29f,-.37f),new Vector3(.15f,.05f,.12f),3);
            }
            b.Drum(new Vector3(0,.05f,-.25f),.59f,.25f,6,12);b.Drum(new Vector3(0,.05f,-.43f),.45f,.19f,0,8);
            Plate(0,.15f,-.56f,.51f,.36f,.1f,10);return true;
        }
        if(kind==4 || kind==10 || kind==13 || kind==27)
        {
            // Utility drones and relay craft have a radial chassis rather than a fighter cockpit.
            float radius=kind==27 ? .63f : kind==13 ? .31f : .44f;
            b.Drum(Vector3.zero,radius,.3f,6,8);b.Drum(new Vector3(0,0,-.21f),radius*.84f,.18f,armor,8);
            b.Drum(new Vector3(0,0,-.34f),radius*.46f,.12f,11,12);b.Drum(new Vector3(0,0,-.42f),radius*.3f,.08f,9,12);
            int count=kind==10 ? 2 : 4;
            for(int i=0;i<count;i++)
            {
                float a=count==2 ? i*Mathf.PI : i*Mathf.PI/2+Mathf.PI/4;
                var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var q=new Vector2(-p.y,p.x);float reach=kind==13 ? .82f : 1.12f;
                b.Prism(new[]{p*radius-q*.1f,p*reach-q*.16f,p*reach+q*.16f,p*radius+q*.1f},.16f,6,Vector3.zero,11);
                var tip=(Vector3)(p*reach);
                if(kind==10 || kind==27)
                {b.Drum(tip,.25f,.19f,11,12,6);b.Drum(tip+Vector3.back*.17f,.19f,.12f,9,12);b.Drum(tip+Vector3.back*.25f,.09f,.06f,10,8);}
                else
                {
                    Plate(tip.x,tip.y,-.08f,.4f,.5f,.24f,armor);
                    b.Box(tip+Vector3.back*.25f,new Vector3(.25f,.1f,.08f),kind==13 ? 3 : 10);
                }
            }
            foreach(int side in new[]{-1,1})Engine(side*.56f,.49f,.22f);
            if(kind==10)b.Box(new Vector3(0,-.72f,0),new Vector3(.08f,.65f,.1f),6);
            return true;
        }
        if(kind==2 || kind==5 || kind==7 || kind==8 || kind==14 || kind==16 || kind==23 || kind==24 || kind==26 || kind==28)
        {
            // Load-bearing transport chassis: separate sponsons, raised bridge and a payload spine.
            bool carrier=kind==5 || kind==14 || kind==28, siege=kind==7 || kind==26;
            float w=carrier ? 1.2f : siege ? .85f : 1.05f;
            Plate(0,0,0,w*1.2f,1.75f,.43f,armor);
            Plate(0,.13f,-.3f,w*.88f,1.3f,.2f,armor);
            Plate(0,-.58f,-.44f,.56f,.42f,.22f,6);
            Plate(0,-.62f,-.59f,.4f,.21f,.06f,9);
            foreach(int side in new[]{-1,1})
            {
                Plate(side*w*.85f,0,.02f,.57f,carrier ? 2.0f : 1.52f,.46f,armor);
                b.Box(new Vector3(side*w*.48f,.1f,0),new Vector3(.53f,.8f,.23f),11);
                Engine(side*.874f,.49f,.34f);
                if(carrier)
                {
                    Plate(side*w*.85f,-.34f,-.28f,.47f,.7f,.13f,11);
                    for(int slat=0;slat<5;slat++)b.Box(new Vector3(side*w*.85f,-.59f+slat*.12f,-.37f),new Vector3(.36f,.055f,.05f),6);
                    b.Box(new Vector3(side*w*1.13f,-.25f,-.28f),new Vector3(.07f,1.25f,.08f),kind==5 ? 10 : 3);
                }
                else if(siege)
                {
                    b.Box(new Vector3(side*.4f,-.72f,-.4f),new Vector3(.21f,1.7f,.26f),11,6);
                    for(int fin=0;fin<4;fin++)b.Box(new Vector3(side*.4f,-.65f-fin*.2f,-.54f),new Vector3(.3f,.08f,.06f),6);
                    Plate(side*.4f,-1.52f,-.4f,.34f,.25f,.34f,3);
                }
                else
                {
                    for(int pod=0;pod<3;pod++)
                    {
                        var p=new Vector3(side*w*.85f,-.27f-pod*.27f,-.3f);
                        b.Drum(p,.18f,.22f,6,8);b.Drum(p+Vector3.back*.16f,.12f,.12f,kind==8 ? 9 : 3,8);
                    }
                }
                if(kind==8)
                {
                    Plate(side*1.29f,0,-.13f,.46f,1.85f,.25f,1);
                    b.Box(new Vector3(side*1.29f,0,-.3f),new Vector3(.07f,1.35f,.05f),9);
                }
            }
            for(int vent=0;vent<5;vent++)b.Box(new Vector3(0,.04f+vent*.13f,-.43f),new Vector3(w*.6f,.05f,.05f),11);
            if(kind==16 || kind==23)b.Drum(new Vector3(0,.68f,-.44f),.23f,.14f,10,8);
            return true;
        }
        return false;
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
