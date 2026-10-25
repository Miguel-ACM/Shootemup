using UnityEngine;
public static partial class PlayerProjectileArt
{
    static Sprite Missile()=>Make("Friendly guided dart",16,32,(x,y)=>
    {
        float side=Mathf.Abs(x-7.5f);
        float edge=y<10 ? 7.5f-y*.32f : y>23 ? Mathf.Lerp(4.5f,.5f,(y-23)/8f) : 4.5f;
        if(side>edge || y<2)return Color.clear;
        if(side>edge-1.5f)return Ink;
        if(y<7 && side<2)return new Color(.26f,.67f,.76f);
        if(y>20 && x<8)return Highlight;
        return x<8 ? Teal : Blue;
    });
}
