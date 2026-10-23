using UnityEngine;
using System.Collections.Generic;

// Shared, inexpensive geometry for tells and projectiles; all colours stay outside the scenery palette.
public static class CombatVisuals
{
    static Sprite disc;
    static Material lineMaterial;
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
        if(lineMaterial==null) lineMaterial=new Material(Shader.Find("Sprites/Default"));
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
