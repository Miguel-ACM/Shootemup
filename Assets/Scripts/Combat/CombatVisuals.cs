using UnityEngine;
using System.Collections.Generic;

// Shared, inexpensive geometry for tells and projectiles; all colours stay outside the scenery palette.
public static class CombatVisuals
{
    static Sprite disc;
    static Material lineMaterial;
    static readonly Dictionary<string,Sprite> projectiles=new Dictionary<string,Sprite>();
    public static Material ProjectileMaterial=>Resources.Load<GameObject>("Bullets/EnemyBulletSmall").GetComponent<SpriteRenderer>().sharedMaterial;
    public static Sprite Projectile(bool friendly=false,bool large=false,bool energy=false)
    {
        if(friendly)return PlayerTracer();
        if(!friendly && large)return ChargedRound(energy);
        string name=friendly ? "PlayerBullet" : energy ? "EnemyBulletMedium" : large ? "EnemyBulletBig" : "EnemyBulletSmall";
        if(projectiles.TryGetValue(name,out var sprite))return sprite;
        var source=Resources.Load<GameObject>("Bullets/"+name).GetComponent<SpriteRenderer>().sprite;
        // Preserve authored pixels while giving every runtime round a unit-sized footprint.
        sprite=Sprite.Create(source.texture,source.rect,new Vector2(.5f,.5f),Mathf.Max(source.rect.width,source.rect.height));
        sprite.name=name+" normalized";return projectiles[name]=sprite;
    }
    static Sprite PlayerTracer()
    {
        const string key="white-core player tracer";
        if(projectiles.TryGetValue(key,out var cached))return cached;
        const int width=12,height=48;
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=key};
        var pixels=new Color[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            float dx=Mathf.Abs(x-5.5f),edge=y>43 ? 5.5f-(y-43)*.65f : y<5 ? 3.5f+y*.4f : 5.5f;
            pixels[y*width+x]=dx>edge ? Color.clear : dx>edge-1.2f ? new Color(.025f,.14f,.32f) : dx>2.5f ? new Color(.15f,.72f,1) : Color.white;
        }
        texture.SetPixels(pixels);texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),height);sprite.name=key;
        return projectiles[key]=sprite;
    }
    static Sprite ChargedRound(bool energy)
    {
        string key=energy ? "charged cyan" : "charged amber";
        if(projectiles.TryGetValue(key,out var cached))return cached;
        const int size=32;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name=key};
        var pixels=new Color[size*size];
        Color rim=energy ? new Color(.15f,.64f,1) : new Color(1,.38f,.08f);
        Color inner=energy ? new Color(.48f,.94f,1) : new Color(1,.83f,.3f);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            var p=new Vector2(x-15.5f,y-15.5f);float radius=p.magnitude;
            Color color=radius>15 ? Color.clear : radius>13.6f ? new Color(.09f,.1f,.2f) : radius>11.4f ? rim : radius>8.5f ? inner : new Color(1,1,.94f);
            // A small specular arc gives the shell volume without dimming its core.
            if(radius>10 && radius<12.5f && p.x<0 && p.y>0)color=new Color(.9f,1,1);
            pixels[y*size+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);sprite.name=key;
        return projectiles[key]=sprite;
    }
    public static Sprite Disc
    {
        get
        {
            if (disc != null) return disc;
            var t = new Texture2D(32,32,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point };
            var pixels=new Color[1024];
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f));
                pixels[y*32+x]=d<15 ? (d>12 ? new Color(.35f,.35f,.35f,1) : Color.white) : Color.clear;
            }
            t.SetPixels(pixels);t.Apply();disc=Sprite.Create(t,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);
            return disc;
        }
    }
    public static LineRenderer Line(Transform parent, Color color, float width)
    {
        if(lineMaterial==null) lineMaterial=ProjectileMaterial;
        var go=new GameObject("Attack tell");go.transform.SetParent(parent,false);
        var l=go.AddComponent<LineRenderer>();l.sharedMaterial=lineMaterial;l.positionCount=2;
        l.startColor=l.endColor=color;l.startWidth=l.endWidth=width;
        l.sortingLayerName="Bullet";l.sortingOrder=20;l.numCapVertices=2;return l;
    }
    public static void Ring(LineRenderer l, Vector3 p, float radius)
    {
        l.positionCount=33;
        for(int i=0;i<33;i++)l.SetPosition(i,p+new Vector3(Mathf.Cos(i*Mathf.PI/16),Mathf.Sin(i*Mathf.PI/16),-.15f)*radius);
    }
    public static Vector2 Rotate(Vector2 d,float degrees) => Quaternion.Euler(0,0,degrees)*d;
    public static Vector2 Aim(Vector3 p)
    {
        var ship=GameObject.FindGameObjectWithTag("Player");
        return ship!=null ? ((Vector2)(ship.transform.position-p)).normalized : Vector2.down;
    }
}
