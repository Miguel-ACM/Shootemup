using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static bool EverydayItem(Builder b,string kind)
    {
        switch(kind)
        {
            case "PocketFan":
                b.Box(new Vector3(0,-.3f,.02f),new Vector3(.17f,.4f,.18f),1);
                b.Drum(new Vector3(0,.15f,.04f),.31f,.1f,11,16);
                for(int i=0;i<3;i++)
                {
                    float a=i*120;var points=new[]{new Vector2(.03f,.02f),new Vector2(.24f,.07f),new Vector2(.2f,.21f),new Vector2(.07f,.18f)};
                    for(int j=0;j<points.Length;j++)points[j]=Quaternion.Euler(0,0,a)*points[j];
                    b.Prism(points,.045f,9,new Vector3(0,.15f,-.04f),9);
                }
                for(int i=0;i<16;i++){float a=i*Mathf.PI/8;b.Box(new Vector3(Mathf.Cos(a)*.31f,.15f+Mathf.Sin(a)*.31f,-.07f),new Vector3(.075f,.075f,.08f),1);}
                b.Drum(new Vector3(0,.15f,-.1f),.07f,.04f,1,8);return true;
            case "StaticSweater":
                b.Prism(new[]{new Vector2(-.18f,.3f),new Vector2(-.4f,.22f),new Vector2(-.5f,-.08f),new Vector2(-.31f,-.16f),new Vector2(-.22f,.02f),new Vector2(-.23f,-.35f),new Vector2(.23f,-.35f),new Vector2(.22f,.02f),new Vector2(.31f,-.16f),new Vector2(.5f,-.08f),new Vector2(.4f,.22f),new Vector2(.18f,.3f),new Vector2(.1f,.21f),new Vector2(-.1f,.21f)},.14f,7,Vector3.zero,7);
                b.Box(new Vector3(0,-.31f,-.01f),new Vector3(.47f,.08f,.17f),1);
                b.Prism(new[]{new Vector2(.04f,.17f),new Vector2(-.12f,-.02f),new Vector2(-.02f,-.02f),new Vector2(-.06f,-.2f),new Vector2(.14f,.04f),new Vector2(.03f,.04f)},.025f,10,new Vector3(0,0,-.09f));return true;
            case "MagnifyingGlass":
                b.Drum(new Vector3(0,.14f,0),.3f,.1f,13,16);
                b.Drum(new Vector3(0,.14f,-.065f),.24f,.025f,8,16);
                b.Box(new Vector3(0,-.32f,0),new Vector3(.13f,.42f,.15f),12,12);
                b.Box(new Vector3(0,-.17f,0),new Vector3(.17f,.12f,.17f),13);
                b.Box(new Vector3(-.1f,.22f,-.085f),new Vector3(.035f,.16f,.015f),1);return true;
            case "RubberBand":
                for(int i=0;i<24;i++)
                {
                    float a=i*Mathf.PI/12,c=(i+1)*Mathf.PI/12;
                    b.Prism(new[]{new Vector2(Mathf.Cos(a)*.4f,Mathf.Sin(a)*.27f),new Vector2(Mathf.Cos(c)*.4f,Mathf.Sin(c)*.27f),new Vector2(Mathf.Cos(c)*.31f,Mathf.Sin(c)*.18f),new Vector2(Mathf.Cos(a)*.31f,Mathf.Sin(a)*.18f)},.07f,13,Vector3.zero,13);
                }return true;
            case "HotSauce":
                b.Box(new Vector3(0,-.13f,0),new Vector3(.35f,.5f,.24f),12,12);
                b.Prism(new[]{new Vector2(-.175f,.12f),new Vector2(.175f,.12f),new Vector2(.09f,.29f),new Vector2(-.09f,.29f)},.24f,12,Vector3.zero,12);
                b.Box(new Vector3(0,.34f,0),new Vector3(.2f,.16f,.26f),3,3);
                b.Box(new Vector3(0,-.11f,-.135f),new Vector3(.29f,.24f,.02f),1);
                b.Prism(new[]{new Vector2(-.07f,-.08f),new Vector2(.06f,-.05f),new Vector2(.08f,.1f),new Vector2(0,.04f)},.03f,12,new Vector3(0,-.08f,-.16f));return true;
            case "GumBlob":b.Ball(Vector3.zero,Vector3.one*.65f,7);return true;
            case "ChewingGum":
                b.Box(Vector3.zero,new Vector3(.72f,.4f,.2f),7,7);
                foreach(int side in new[]{-1,1})b.Box(new Vector3(side*.34f,0,-.01f),new Vector3(.09f,.42f,.23f),1);
                b.Drum(new Vector3(0,0,-.13f),.14f,.03f,1,12);
                b.Ball(new Vector3(.22f,.3f,-.03f),new Vector3(.15f,.15f,.1f),7);return true;
        }
        return false;
    }
}
