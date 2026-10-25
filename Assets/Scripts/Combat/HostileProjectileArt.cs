using System;
using System.Collections.Generic;
using UnityEngine;

// Ink rims, stepped colour bands and small glints keep the established pixel ordnance style.
// Shape carries the identity as well as colour, including through the PSX filter.
public static class HostileProjectileArt
{
    enum Shape { Needle, Helix, Shell, Ricochet, Suspended, Missile, Plasma, Mine, Disc, Carrier, Side }
    static readonly Dictionary<Shape,Sprite> sprites=new Dictionary<Shape,Sprite>();
    static Shape Kind(Type type)
    {
        if(type==typeof(NeedleRound))return Shape.Needle;
        if(type==typeof(HelixRound))return Shape.Helix;
        if(type==typeof(BurstShell))return Shape.Shell;
        if(type==typeof(RicochetBolt))return Shape.Ricochet;
        if(type==typeof(SuspendedRound))return Shape.Suspended;
        if(type==typeof(GuidedMissile))return Shape.Missile;
        if(type==typeof(PlasmaOrb))return Shape.Plasma;
        if(type==typeof(ProximityMine))return Shape.Mine;
        if(type==typeof(ReturningDisc))return Shape.Disc;
        if(type==typeof(ScatterCarrier))return Shape.Carrier;
        return Shape.Side;
    }
    public static Color Accent(Type type)
    {
        if(type==typeof(SimpleRound))return new Color(1,.46f,.13f);
        switch(Kind(type))
        {
            case Shape.Needle:return new Color(1,.22f,.27f);
            case Shape.Helix:return new Color(1,.30f,.65f);
            case Shape.Shell:return new Color(1,.67f,.13f);
            case Shape.Ricochet:return new Color(.76f,.97f,.16f);
            case Shape.Suspended:return new Color(.29f,.52f,1);
            case Shape.Missile:return new Color(.70f,.36f,1);
            case Shape.Plasma:return new Color(.17f,.94f,.94f);
            case Shape.Mine:return new Color(1,.28f,.12f);
            case Shape.Disc:return new Color(.90f,.31f,.94f);
            case Shape.Carrier:return new Color(.24f,.92f,.55f);
            default:return new Color(.25f,.60f,1);
        }
    }
    static float Outline(Shape kind,float x,float y)
    {
        float ax=Mathf.Abs(x),ay=Mathf.Abs(y),r=Mathf.Sqrt(x*x+y*y);
        switch(kind)
        {
            case Shape.Needle:return Mathf.Max(ax/.77f,Mathf.Max((ay+.27f*ax)/.98f,(-y-.78f)/.12f));
            case Shape.Helix:return Mathf.Pow(ax/.86f,.75f)+Mathf.Pow(ay/.98f,1.3f);
            case Shape.Shell:return Mathf.Max(r/.96f,(ax+ay)/1.30f);
            case Shape.Ricochet:return Mathf.Max((ax+ay)/1.02f,Mathf.Max(ax,ay)/.82f);
            case Shape.Suspended:
            {
                // A joined arrowhead bows out into curved wings around a separate ball.
                float curve=.74f-.75f*ax-.75f*ax*ax,slope=.75f+1.5f*ax;
                float arrow=Mathf.Max(Mathf.Abs(y-curve)/(.14f*Mathf.Sqrt(1+slope*slope)),(ax-.77f)/.08f);
                float ball=Mathf.Sqrt(x*x+(y+.22f)*(y+.22f))/.26f;
                return Mathf.Min(arrow,ball);
            }
            case Shape.Missile:return Mathf.Max(ay/.98f,ax/(y<-.20f ? .82f : .48f*(1.2f-y*.55f)));
            case Shape.Plasma:return Mathf.Max(Mathf.Max(ax,ay)/.87f,(ax+ay)/1.23f);
            case Shape.Mine:return Mathf.Min(Mathf.Max(ax/.32f,ay/.94f),Mathf.Max(ax/.94f,ay/.32f));
            case Shape.Disc:return Mathf.Max(r/.96f,(.34f-r)/.10f);
            case Shape.Carrier:return Mathf.Max(ay/.94f,ax/.72f)+Mathf.Max(0,ay-.65f)*.6f;
            default:return Mathf.Pow(ax/.94f,.82f)+Mathf.Pow(ay/.94f,.82f);
        }
    }
    public static Sprite Sprite(Type type,bool large=false)
    {
        if(type==typeof(SimpleRound))return CombatVisuals.Projectile(false,large);
        var kind=Kind(type);if(sprites.TryGetValue(kind,out var cached) && cached!=null)return cached;
        const int size=32;var pixels=new Color[size*size];var accent=Accent(type);
        var ink=new Color(.055f,.045f,.10f);var core=Color.Lerp(accent,new Color(1,.98f,.89f),.68f);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float px=(x-15.5f)/16,py=(y-15.5f)/16;float d=Outline(kind,px,py);
            if(d>1)continue;
            Color color=d>.84f ? ink : d>.61f ? accent*.72f : accent;
            color.a=1;
            if(d<.40f)color=core;
            // Rings and ports remain legible at a few gameplay pixels.
            if(kind==Shape.Shell && Mathf.Sqrt(px*px+py*py)>.50f && (Mathf.Abs(px)<.08f || Mathf.Abs(py)<.08f))color=ink;
            if(kind==Shape.Plasma && Mathf.Abs(px)+Mathf.Abs(py)<.30f)color=Color.Lerp(core,Color.white,.35f);
            if(kind==Shape.Carrier && Mathf.Abs(px)<.20f && Mathf.Abs(py)<.65f && Mathf.Abs(py% .42f)<.13f)color=core;
            if(kind==Shape.Mine && Mathf.Abs(px)<.17f && Mathf.Abs(py)<.17f)color=new Color(1,.83f,.48f);
            if(d>.49f && d<.69f && px<-.08f && py>.18f)color=core;
            pixels[y*size+x]=color;
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=kind+" hostile pixels",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        texture.SetPixels(pixels);texture.Apply();
        var sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        sprite.name=kind+" ordnance";return sprites[kind]=sprite;
    }
}
