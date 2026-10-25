using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh whiteWormSection,whiteWormCore,whiteWormHeadCore;
    static Material whiteWormMaterial;
    public static Material WhiteWormMaterial
    {
        get
        {
            if(whiteWormMaterial!=null)return whiteWormMaterial;
            whiteWormMaterial=new Material(CombatMaterials.Fleet){name="Ivory ceramic and exposed crimson reactors"};
            var texture=Object.Instantiate((Texture2D)CombatMaterials.Fleet.mainTexture);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float shade=x<5 || x>58 || y<5 || y>58 ? .55f : .86f+.14f*y/64;
                var color=new Color(.68f,.24f,.22f)*shade;color.a=1;
                texture.SetPixel(192+x,y,color);
            }
            texture.Apply();whiteWormMaterial.mainTexture=texture;return whiteWormMaterial;
        }
    }
    public static Mesh WhiteWormCore(bool head)
    {
        var cached=head ? whiteWormHeadCore : whiteWormCore;if(cached!=null)return cached;
        var b=new Builder();float r=head ? .44f : .28f;
        b.Drum(Vector3.zero,r*1.23f,.15f,11,12,6);
        b.Drum(Vector3.back*.10f,r,.14f,3,10,5);
        b.Drum(Vector3.back*.19f,r*.57f,.06f,3,8,5);
        b.Box(new Vector3(-r*.22f,0,-.24f),new Vector3(r*.10f,r*1.2f,.025f),1,3);
        var mesh=b.Finish(head ? "Large red head reactor" : "Red segment reactor");
        if(head)whiteWormHeadCore=mesh;else whiteWormCore=mesh;return mesh;
    }
    public static Mesh WhiteWormSection()
    {
        if(whiteWormSection!=null)return whiteWormSection;var b=new Builder();
        b.Ball(Vector3.zero,new Vector3(.65f,.59f,.38f),11);
        foreach(int side in new[]{-1,1})
        {
            // Overlapping ceramic lamellae surround a recessed central reactor, with visible articulated joints.
            for(int plate=0;plate<3;plate++)
            {
                float y=.36f-plate*.29f;
                BevelArmor(b,new[]{V(side*.31f,y+.15f),V(side*.73f,y+.1f),V(side*.82f,y-.17f),V(side*.42f,y-.10f)},.26f,1,-.14f);
            }
            b.Box(new Vector3(side*.60f,-.09f,-.36f),new Vector3(.08f,.51f,.045f),6,11);
            b.Prism(new[]{V(side*.65f,.35f),V(side*.97f,.63f),V(side*.87f,-.25f),V(side*.63f,-.4f)},.12f,1,new Vector3(0,0,.02f),6);
            b.Drum(new Vector3(side*.76f,-.26f,-.31f),.10f,.09f,11,8,6);
        }
        b.Box(new Vector3(0,.52f,0),new Vector3(.43f,.30f,.28f),6,11);
        for(int i=0;i<3;i++)b.Box(new Vector3(0,.44f+i*.08f,-.17f),new Vector3(.36f,.035f,.045f),11,6);
        return whiteWormSection=b.Finish("White worm ceramic vertebra");
    }
    static void BuildWhiteWormHead(Builder b)
    {
        BevelArmor(b,new[]{V(-.63f,.75f),V(.63f,.75f),V(.88f,.18f),V(.64f,-.66f),V(0,-.9f),V(-.64f,-.66f),V(-.88f,.18f)},.62f,11);
        foreach(int side in new[]{-1,1})
        {
            BevelArmor(b,new[]{V(side*.45f,.73f),V(side*.83f,.51f),V(side*.99f,-.03f),V(side*.77f,-.72f),V(side*.52f,-.43f)},.32f,1,-.24f);
            BevelArmor(b,new[]{V(side*.36f,-.4f),V(side*.77f,-.65f),V(side*.55f,-1.34f),V(side*.18f,-1.11f)},.35f,1,-.10f);
            for(int tooth=0;tooth<3;tooth++)
            {
                float y=-.67f-tooth*.17f;
                b.Prism(new[]{V(side*.42f,y+.09f),V(side*.18f,y-.03f),V(side*.40f,y-.13f)},.18f,6,new Vector3(0,0,-.14f),11);
            }
            b.Box(new Vector3(side*.69f,-.22f,-.48f),new Vector3(.11f,.32f,.05f),3,11);
            b.Box(new Vector3(side*.52f,.60f,-.44f),new Vector3(.07f,.22f,.03f),6,11);
        }
        b.Box(new Vector3(0,.66f,-.34f),new Vector3(.76f,.14f,.12f),1,6);
    }
}
