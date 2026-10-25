using UnityEngine;
using System.Collections.Generic;
public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<string,Mesh> expansionItems=new Dictionary<string,Mesh>();
    public static Mesh ExpansionItem(string kind)
    {
        if(expansionItems.TryGetValue(kind,out var mesh))return mesh;
        var b=new Builder();
        if(RequestedItem(b,kind))return expansionItems[kind]=b.Finish(kind+" equipment");
        if(kind=="BaseWeaponItem")
        {
            // The starter is a paired ship-mounted autocannon, not a generic equipment icon.
            b.Prism(new[]{new Vector2(-.27f,-.34f),new Vector2(.27f,-.34f),new Vector2(.33f,.08f),new Vector2(.2f,.27f),new Vector2(-.2f,.27f),new Vector2(-.33f,.08f)},.25f,0,Vector3.zero,6);
            foreach(int side in new[]{-1,1})
            {
                b.Box(new Vector3(side*.19f,.3f,-.03f),new Vector3(.15f,.65f,.18f),11,6);
                b.Box(new Vector3(side*.19f,.61f,-.03f),new Vector3(.2f,.13f,.23f),6);
                b.Box(new Vector3(side*.19f,.68f,-.03f),new Vector3(.09f,.03f,.1f),11);
                for(int fin=0;fin<4;fin++)b.Box(new Vector3(side*.19f,.15f+fin*.1f,-.15f),new Vector3(.18f,.045f,.05f),6);
                b.Box(new Vector3(side*.31f,-.18f,-.02f),new Vector3(.16f,.33f,.25f),3);
            }
            b.Box(new Vector3(0,-.06f,-.18f),new Vector3(.13f,.29f,.08f),9);
            b.Box(new Vector3(0,-.32f,-.03f),new Vector3(.24f,.14f,.3f),6);
        }
        else if(kind=="LargeHealthKit")
        {
            b.Box(Vector3.zero,new Vector3(.65f,.48f,.26f),1,6);
            b.Box(new Vector3(0,0,-.16f),new Vector3(.33f,.1f,.04f),3);
            b.Box(new Vector3(0,0,-.17f),new Vector3(.1f,.33f,.04f),3);
            b.Box(new Vector3(0,.3f,0),new Vector3(.27f,.08f,.16f),11);
        }
        else if(kind=="Lighter")
        {
            b.Box(new Vector3(0,-.08f,0),new Vector3(.3f,.5f,.2f),3,6);
            b.Box(new Vector3(0,.23f,0),new Vector3(.32f,.16f,.22f),6);
            b.Drum(new Vector3(-.08f,.32f,-.05f),.07f,.15f,11,8);
            b.Prism(new[]{new Vector2(-.05f,0),new Vector2(.05f,0),new Vector2(0,.2f)},.04f,10,new Vector3(.09f,.32f,0));
        }
        else if(kind=="BallBearing")
        {b.Drum(Vector3.zero,.32f,.12f,6,16);b.Drum(new Vector3(0,0,-.09f),.25f,.08f,11,16);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;b.Ball(new Vector3(Mathf.Cos(a)*.22f,Mathf.Sin(a)*.22f,-.16f),Vector3.one*.085f,1);}}
        else if(kind=="GravityWell" || kind=="MirrorBurst" || kind=="BlankBullet")
        {
            b.Drum(Vector3.zero,.33f,.18f,11,8,6);
            b.Drum(new Vector3(0,0,-.13f),.26f,.09f,kind=="BlankBullet" ? 3 : 7,8);
            if(kind=="GravityWell")
            {
                b.Drum(new Vector3(0,0,-.2f),.17f,.07f,9,12);
                b.Drum(new Vector3(0,0,-.25f),.105f,.06f,11,12);
                foreach(int s in new[]{-1,1})b.Box(new Vector3(s*.38f,0,-.08f),new Vector3(.15f,.37f,.17f),6);
            }
            else if(kind=="MirrorBurst")
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var q=new Vector2(-p.y,p.x);
                    b.Prism(new[]{p*.22f-q*.08f,p*.48f,p*.22f+q*.08f},.12f,9,Vector3.back*.23f,6);
                }
            else
            {
                b.Box(new Vector3(0,.37f,0),new Vector3(.2f,.16f,.2f),6);
                b.Box(new Vector3(0,0,-.23f),new Vector3(.12f,.28f,.05f),10);
            }
        }
        else if(kind.EndsWith("Item"))
        {
            bool beam=kind=="BeamEmitterItem",shell=kind=="BurstLauncherItem",disc=kind=="DiscThrowerItem",flame=kind=="FlameProjectorItem";
            b.Box(new Vector3(0,-.12f,0),new Vector3(.42f,.4f,.3f),0,6);
            if(disc){b.Drum(Vector3.zero,.35f,.14f,6,12);b.Drum(new Vector3(0,0,-.1f),.15f,.08f,9,12);}
            else
            {
                int count=kind=="ScatterCannonItem" ? 3 : kind=="SwarmRackItem" || kind=="RailDriverItem" ? 2 : 1;
                for(int i=0;i<count;i++)
                {
                    float x=(i-(count-1)*.5f)*.2f;
                    b.Box(new Vector3(x,.27f,0),new Vector3(shell ? .3f : .13f,.65f,.18f),11,6);
                    b.Box(new Vector3(x,.56f,0),new Vector3(shell ? .36f : .19f,.12f,.24f),beam ? 9 : flame ? 3 : 6);
                    if(kind=="RailDriverItem")for(int coil=0;coil<4;coil++)b.Box(new Vector3(x,.12f+coil*.13f,-.13f),new Vector3(.16f,.055f,.06f),9);
                    if(kind=="SwarmRackItem")for(int rocket=0;rocket<3;rocket++)b.Drum(new Vector3(x,-.13f+rocket*.19f,-.22f),.075f,.13f,3,6);
                }
            }
            b.Box(new Vector3(0,-.17f,-.2f),new Vector3(.24f,.16f,.05f),beam ? 9 : 3);
            for(int i=0;i<3;i++)b.Box(new Vector3(-.14f+i*.14f,-.31f,-.17f),new Vector3(.045f,.08f,.04f),6);
        }
        else if(kind=="DroneLink")
        {b.Drum(Vector3.zero,.22f,.2f,0,8);foreach(int s in new[]{-1,1}){b.Box(new Vector3(s*.28f,0,0),new Vector3(.25f,.13f,.14f),6);b.Drum(new Vector3(s*.4f,0,0),.13f,.1f,9,8);}b.Drum(new Vector3(0,0,-.14f),.08f,.05f,9,8);}
        else if(kind=="FocusingLens")
        {b.Drum(Vector3.zero,.32f,.24f,6,12);b.Drum(new Vector3(0,0,-.15f),.25f,.1f,9,12);b.Drum(new Vector3(0,0,-.22f),.14f,.08f,10,12);}
        else if(kind=="Capacitor" || kind=="SpareBattery" || kind=="ReserveCell" || kind=="Overdrive")
        {
            b.Box(Vector3.zero,new Vector3(.5f,.6f,.3f),kind=="SpareBattery" ? 7 : 0,6);
            foreach(int s in new[]{-1,1})b.Box(new Vector3(s*.13f,.35f,0),new Vector3(.1f,.13f,.2f),3);
            b.Box(new Vector3(0,0,-.18f),new Vector3(.31f,.38f,.05f),11);
            for(int i=0;i<3;i++)b.Box(new Vector3(0,-.1f+i*.1f,-.22f),new Vector3(.23f,.055f,.03f),9);
        }
        else
        {
            b.Drum(Vector3.zero,.3f,.22f,6,10);
            b.Drum(new Vector3(0,0,-.15f),.2f,.1f,kind=="ChainReactor" ? 3 : 9,10);
            int count=kind=="SplitterCore" ? 2 : kind=="ReboundCoil" ? 4 : 3;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*.36f;
                b.Box(p,new Vector3(.16f,.16f,.2f),0);b.Drum(p+Vector3.back*.15f,.06f,.05f,3,6);
            }
        }
        return expansionItems[kind]=b.Finish(kind+" equipment");
    }
}
