using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static bool RequestedItem(Builder b,string kind)
    {
        switch(kind)
        {
            case "ShrinkerJuice":
                b.Prism(new[]{new Vector2(-.21f,-.37f),new Vector2(.21f,-.37f),new Vector2(.24f,.12f),new Vector2(.1f,.25f),new Vector2(.1f,.44f),new Vector2(-.1f,.44f),new Vector2(-.1f,.25f),new Vector2(-.24f,.12f)},.27f,2,Vector3.zero,2);
                b.Box(new Vector3(0,.45f,0),new Vector3(.24f,.12f,.31f),13);
                b.Box(new Vector3(0,-.08f,-.15f),new Vector3(.29f,.2f,.025f),1);
                b.Prism(new[]{new Vector2(-.09f,.06f),new Vector2(.09f,.06f),new Vector2(0,-.07f)},.02f,2,new Vector3(0,-.08f,-.18f));
                b.Box(new Vector3(-.14f,.08f,-.15f),new Vector3(.035f,.2f,.02f),14);
                return true;
        }
        return false;
    }
}
