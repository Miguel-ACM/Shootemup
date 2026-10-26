using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static Mesh snowCombEmitter;
    public static Mesh SnowCombEmitter()
    {
        if (snowCombEmitter != null) return snowCombEmitter;
        var b = new Builder();
        b.Prism(new[] { new Vector2(-.3f, .25f), new Vector2(.3f, .25f), new Vector2(.23f, -.27f), new Vector2(0, -.42f), new Vector2(-.23f, -.27f) }, .25f, 11, Vector3.zero, 6);
        foreach (int side in new[] { -1, 1 })
            b.Prism(new[] { new Vector2(side * .2f, .15f), new Vector2(side * .52f, -.08f), new Vector2(side * .23f, -.13f) }, .12f, 1, Vector3.zero, 6);
        b.Box(new Vector3(0, -.18f, -.2f), new Vector3(.13f, .36f, .15f), 6);
        b.Box(new Vector3(0, -.32f, -.22f), new Vector3(.2f, .09f, .18f), 9);
        return snowCombEmitter = b.Finish("Snow deployable comb emitter");
    }
}
