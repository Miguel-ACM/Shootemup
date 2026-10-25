using UnityEngine;

// A fixed-cost, quarter-resolution HDR bloom for the entire projectile field.
// Only radiance above white contributes, so bright snow and clouds do not fog the arena.
public sealed class ProjectileGlow : System.IDisposable
{
    readonly Material material;
    readonly RenderTexture horizontal,vertical;
    public RenderTexture Texture=>vertical;
    public ProjectileGlow(int width,int height)
    {
        material=new Material(Resources.Load<Shader>("ProjectileGlow")){name="Shared projectile radiance"};
        horizontal=Target(width,height,"Projectile glow horizontal");
        vertical=Target(width,height,"Projectile glow vertical");
    }
    static RenderTexture Target(int width,int height,string name)
    {
        var result=new RenderTexture(Mathf.Max(1,width/4),Mathf.Max(1,height/4),0,RenderTextureFormat.ARGBHalf)
            {name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,antiAliasing=1};
        result.Create();return result;
    }
    public void Render(RenderTexture source)
    {
        Graphics.Blit(source,vertical,material,0);
        material.SetVector("_Direction",new Vector4(.5f/horizontal.width,0,0,0));
        Graphics.Blit(vertical,horizontal,material,1);
        material.SetVector("_Direction",new Vector4(0,.5f/vertical.height,0,0));
        Graphics.Blit(horizontal,vertical,material,1);
    }
    public void Dispose()
    {
        horizontal.Release();vertical.Release();
        Release(horizontal);Release(vertical);Release(material);
    }
    static void Release(Object value)
    {
        if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);
    }
}
