using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh PopcornModel()
    {
        var b=new Builder();
        b.Prism(new[]{new Vector2(-.24f,-.4f),new Vector2(.24f,-.4f),new Vector2(.36f,.23f),new Vector2(-.36f,.23f)},.32f,1,Vector3.zero,12);
        foreach(float x in new[]{-.22f,0,.22f})b.Box(new Vector3(x,-.08f,-.17f),new Vector3(.09f,.57f,.025f),12);
        for(int i=0;i<9;i++)
        {
            float x=(i%3-1)*.22f,y=.23f+(i/3)*.075f;
            b.Ball(new Vector3(x,y,(i/3-1)*.13f),new Vector3(.14f,.12f,.12f),i%2==0 ? 1 : 13);
        }
        return b.Finish("Popcorn equipment");
    }
}
