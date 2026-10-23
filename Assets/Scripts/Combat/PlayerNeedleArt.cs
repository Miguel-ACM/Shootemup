using UnityEngine;
public static partial class PlayerProjectileArt
{
    static Sprite Needle()=>Make("Friendly piercing needle",16,48,(x,y)=>
    {
        float side=Mathf.Abs(x-7.5f);
        float edge=y>31 ? Mathf.Lerp(6.5f,.6f,(y-31)/16f) : y<6 ? 2.5f+y*.5f : 6.5f;
        if(side>edge)return Color.clear;
        if(side>edge-1.5f)return Ink;
        if(x<7 && y>21)return Highlight;
        return x<8 ? Teal : Blue;
    });
}
