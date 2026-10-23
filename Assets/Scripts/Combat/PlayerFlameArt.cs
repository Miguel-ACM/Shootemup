using UnityEngine;
public static partial class PlayerProjectileArt
{
    static Sprite Flame()=>Make("Friendly blue furnace flame",24,32,(x,y)=>
    {
        float side=Mathf.Abs(x-11.5f+Mathf.Sin(y*.29f)*1.2f);
        float edge=Mathf.Sin(Mathf.Clamp01((y+1)/33f)*Mathf.PI)*10.5f;
        if(side>edge || y<2)return Color.clear;
        if(side>edge-2)return new Color(.04f,.22f,.32f,.8f);
        if(side<edge*.22f && y>10 && y<23)return new Color(.93f,.69f,.26f,.95f);
        if(side>edge*.55f)return new Color(.13f,.44f,.66f,.9f);
        return new Color(.32f,.72f,.78f,.95f);
    });
}
