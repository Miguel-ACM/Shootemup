using UnityEngine;

// Purpose-built trim sheet: the fleet factory's tile indices are semantic materials.
// Never reinterpret the original player's authored atlas or recolour its model.
public static class CombatMaterials
{
    static Material fleet;
    public static Material Fleet
    {
        get
        {
            if(fleet!=null)return fleet;
            fleet=new Material(Resources.Load<Material>("CombatFleet")){name="Painted district armor"};
            var palette=new[]{new Color(.24f,.35f,.4f),new Color(.78f,.83f,.79f),new Color(.35f,.43f,.3f),new Color(.95f,.38f,.12f),new Color(.32f,.48f,.55f),new Color(.54f,.18f,.16f),new Color(.53f,.61f,.62f),new Color(.26f,.23f,.39f),new Color(.2f,.36f,.4f),new Color(.16f,.83f,.91f),new Color(1,.77f,.27f),new Color(.055f,.09f,.12f),new Color(.62f,.25f,.19f),new Color(.82f,.64f,.26f),new Color(.53f,.61f,.55f),new Color(.8f,.84f,.83f)};
            var texture=new Texture2D(256,256,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Fleet painted trim atlas"};
            var pixels=new Color[256*256];
            for(int tile=0;tile<16;tile++)for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                Color c=palette[tile];bool edge=x<5 || x>58 || y<5 || y>58;
                float shade=edge ? .5f : x<7 || y>56 ? 1.24f : x>56 || y<7 ? .72f : 1;
                if(tile==9 || tile==10)shade=.7f+.3f*y/64f;
                else if(tile==11 && y%10<3)shade=.55f;
                else if(!edge && ((x==10 || x==53)&&(y==10 || y==53)))shade=1.5f;
                // Sparse panel seams and one broad painted stripe read at gameplay scale.
                else if(tile==0 || tile==1 || tile==7 || tile==12)
                {
                    if(x>43 && x<49)c=tile==1 ? palette[4] : tile==12 ? palette[1] : palette[13];
                    if(y==19 && x>10 && x<37)shade=.66f;
                }
                c*=shade;c.a=1;pixels[(tile/4*64+y)*256+tile%4*64+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();fleet.mainTexture=texture;return fleet;
        }
    }
}
