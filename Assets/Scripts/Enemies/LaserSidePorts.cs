using UnityEngine;
using System.Collections.Generic;

// Paired vents cover the visible beam, including its far edge.
public sealed class LaserSidePorts : MonoBehaviour
{
    readonly List<SpriteRenderer> mouths=new List<SpriteRenderer>();
    const float Spacing=2.3f;
    public readonly struct Layout
    {
        public readonly int Count;
        readonly float first,last;
        public Layout(float first,float last)
        {this.first=first;this.last=last;Count=Mathf.CeilToInt((last-first)/Spacing)+1;}
        public float Distance(int port)=>Count<=1 ? first : Mathf.Lerp(first,last,(port-1f)/(Count-1));
    }
    public static Layout VisibleLayout(Vector3 source,Vector2 heading,float length)
    {
        if(length<=0)return default;
        var camera=UnityEngine.Camera.main;
        float halfHeight=camera!=null ? camera.orthographicSize : (Camera.UpLimit-Camera.DownLimit)*.5f;
        Vector2 center=camera!=null ? (Vector2)camera.transform.position : new Vector2((Camera.LeftLimit+Camera.RightLimit)*.5f,(Camera.DownLimit+Camera.UpLimit)*.5f);
        float halfWidth=camera!=null ? halfHeight*405f/720f : (Camera.RightLimit-Camera.LeftLimit)*.5f;
        float enter=0,exit=length;
        if(!Clip(source.x,heading.x,center.x-halfWidth,center.x+halfWidth,ref enter,ref exit) ||
           !Clip(source.y,heading.y,center.y-halfHeight,center.y+halfHeight,ref enter,ref exit) || exit<=enter)return default;
        float last=Mathf.Max(enter,exit-.18f),first=Mathf.Min(last,Mathf.Max(enter+.18f,.9f));
        return new Layout(first,last);
    }
    static bool Clip(float origin,float direction,float low,float high,ref float enter,ref float exit)
    {
        if(Mathf.Abs(direction)<.0001f)return origin>=low && origin<=high;
        float a=(low-origin)/direction,b=(high-origin)/direction;
        enter=Mathf.Max(enter,Mathf.Min(a,b));exit=Mathf.Min(exit,Mathf.Max(a,b));return exit>=enter;
    }
    static Sprite arrow;
    public void Build()
    {
        if(arrow==null)
        {
            const int size=24;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Abs(x-11.5f);float edge=16-dx*.75f;
                if(dx<10 && ((y>edge-3 && y<edge) || (y>edge-10 && y<edge-8)))pixels[y*size+x]=Color.white;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Side vent chevrons",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels(pixels);texture.Apply();
            arrow=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }
    }
    void EnsureMouths(int count)
    {
        while(mouths.Count<count)
        {
            var go=new GameObject("Side outlet "+mouths.Count);go.transform.SetParent(transform,false);
            var sr=go.AddComponent<SpriteRenderer>();mouths.Add(sr);sr.sprite=arrow;sr.sharedMaterial=CombatVisuals.ProjectileMaterial;
            sr.sortingLayerName="Bullet";sr.sortingOrder=24;sr.enabled=false;
        }
    }
    public static Vector3 Origin(Vector3 source,Vector2 heading,float width,Layout layout,int port,int sign)
    {return source+(Vector3)(heading*layout.Distance(port)+new Vector2(-heading.y,heading.x)*sign*(width*.5f+.06f));}
    public void Hide(){foreach(var mouth in mouths)mouth.enabled=false;}
    public void Draw(Vector3 source,Vector2 heading,float width,Layout layout,float charge,bool visible)
    {
        EnsureMouths(layout.Count*2);
        Vector2 side=new Vector2(-heading.y,heading.x);Color color=HostileProjectileArt.Accent(typeof(LaserSideRound));
        for(int index=0;index<mouths.Count;index++)
        {
            var sr=mouths[index];sr.enabled=visible && index<layout.Count*2;if(!sr.enabled)continue;
            int port=index/2+1,sign=index%2==0 ? -1 : 1;
            sr.transform.position=Origin(source,heading,width,layout,port,sign)+(Vector3)(side*sign*.12f);
            sr.transform.rotation=Quaternion.FromToRotation(Vector3.up,side*sign);
            sr.transform.localScale=Vector3.one*Mathf.Lerp(.32f,.44f,charge);
            sr.color=new Color(color.r,color.g,color.b,Mathf.Lerp(.35f,.95f,charge));
        }
    }
}

public sealed class LaserSideRound : CombatRound { }
