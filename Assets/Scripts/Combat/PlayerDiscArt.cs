using UnityEngine;
public static partial class PlayerProjectileArt
{
    static Sprite Disc()=>Make("Friendly three vane disc",28,28,(x,y)=>
    {
        var p=new Vector2(x-13.5f,y-13.5f);float r=p.magnitude;
        float angle=Mathf.Atan2(p.y,p.x);
        float edge=9.2f+3.2f*Mathf.Cos(angle*3);
        if(r>edge)return Color.clear;
        if(r>edge-1.8f || r<3)return Ink;
        if(r<4.8f)return Teal;
        return p.y>2 && p.x<0 ? Highlight : Blue;
    });
}
