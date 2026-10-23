using UnityEngine;
public static partial class PlayerProjectileArt
{
    static Sprite Flame()=>Make("Friendly amber flame",24,32,(x,y)=>
    {
        float side=Mathf.Abs(x-11.5f+Mathf.Sin(y*.29f)*1.2f);
        float edge=Mathf.Sin(Mathf.Clamp01((y+1)/33f)*Mathf.PI)*10.5f;
        if(side>edge || y<2)return Color.clear;
        if(side>edge-2)return new Color(.60f,.18f,.035f,.65f);
        if(side>edge*.55f)return new Color(.87f,.36f,.06f,.9f);
        return new Color(.95f,.65f,.18f,.95f);
    });
}
