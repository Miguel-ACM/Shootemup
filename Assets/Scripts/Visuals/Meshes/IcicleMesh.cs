using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh IcicleModel()
    {
        var b=new Builder();
        b.Prism(new[]{new Vector2(-.23f,.39f),new Vector2(.16f,.45f),new Vector2(.29f,.2f),new Vector2(.04f,-.63f),new Vector2(-.17f,-.03f)},.2f,8,Vector3.zero,9);
        b.Prism(new[]{new Vector2(-.18f,.34f),new Vector2(-.02f,.36f),new Vector2(.04f,-.5f)},.015f,1,new Vector3(0,0,-.115f));
        b.Prism(new[]{new Vector2(.13f,.31f),new Vector2(.3f,.3f),new Vector2(.25f,-.22f)},.13f,9,new Vector3(.03f,0,.06f),8);
        return b.Finish("Icicle equipment");
    }
}
