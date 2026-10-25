using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static bool EverydayItem(Builder b,string kind)
    {
        switch(kind)
        {
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
