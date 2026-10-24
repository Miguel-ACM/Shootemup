using UnityEngine;

// Three paired vents show the exact locations and outgoing directions of a side volley.
public sealed class LaserSidePorts : MonoBehaviour
{
    readonly SpriteRenderer[] mouths=new SpriteRenderer[6];
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
        for(int i=0;i<mouths.Length;i++)
        {
            var go=new GameObject("Side outlet "+i);go.transform.SetParent(transform,false);
            var sr=mouths[i]=go.AddComponent<SpriteRenderer>();sr.sprite=arrow;sr.sharedMaterial=CombatVisuals.ProjectileMaterial;
            sr.sortingLayerName="Bullet";sr.sortingOrder=24;sr.enabled=false;
        }
    }
    public static Vector3 Origin(Vector3 source,Vector2 heading,float width,int port,int sign)
    {return source+(Vector3)(heading*(port*2.3f)+new Vector2(-heading.y,heading.x)*sign*(width*.5f+.06f));}
    public void Draw(Vector3 source,Vector2 heading,float width,float charge,bool visible)
    {
        Vector2 side=new Vector2(-heading.y,heading.x);Color color=HostileProjectileArt.Accent(typeof(LaserSideRound));
        int index=0;
        for(int port=1;port<=3;port++)for(int sign=-1;sign<=1;sign+=2)
        {
            var sr=mouths[index++];sr.enabled=visible;if(!visible)continue;
            sr.transform.position=Origin(source,heading,width,port,sign)+(Vector3)(side*sign*.12f);
            sr.transform.rotation=Quaternion.FromToRotation(Vector3.up,side*sign);
            sr.transform.localScale=Vector3.one*Mathf.Lerp(.32f,.44f,charge);
            sr.color=new Color(color.r,color.g,color.b,Mathf.Lerp(.35f,.95f,charge));
        }
    }
}

public sealed class LaserSideRound : CombatRound { }
