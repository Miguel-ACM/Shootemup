using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static bool EverydayItem(Builder b,string kind)
    {
        switch(kind)
        {
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
