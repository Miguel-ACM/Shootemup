using UnityEngine;

public static class SpaceMaterials
{
    static Material fleet;
    public static Material Fleet
    {
        get
        {
            if(fleet!=null)return fleet;
            fleet=new Material(CombatMaterials.Fleet){name="Orbital ivory, gold foil and blue solar cells"};
            var atlas=Object.Instantiate((Texture2D)fleet.mainTexture);atlas.name="Orbital solar-cell trim atlas";
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                bool rail=x%12<2 || y%12<2;
                var c=rail ? new Color(.33f,.44f,.57f) : Color.Lerp(new Color(.055f,.13f,.25f),new Color(.16f,.29f,.45f),(y%12)/12f);
                atlas.SetPixel(3*64+x,64+y,c);
            }
            atlas.Apply();fleet.mainTexture=atlas;return fleet;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] orbitalParts=new Mesh[4];
    static void SolarPanel(Builder b,Vector3 p,float width,float height)
    {
        b.Box(p,new Vector3(width,height,.075f),7,13);
        b.Box(p+new Vector3(0,height*.5f,-.015f),new Vector3(width+.06f,.055f,.10f),15,6);
        b.Box(p-new Vector3(0,height*.5f,.015f),new Vector3(width+.06f,.055f,.10f),15,6);
        foreach(int s in new[]{-1,1})b.Box(p+new Vector3(s*width*.5f,0,-.015f),new Vector3(.05f,height,.10f),13,6);
    }
    static void SpaceDish(Builder b,Vector3 p,float radius)
    {
        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI/5,n=(i+1)*Mathf.PI/5;
            b.Face(i%3==0 ? 6 : 15,p+new Vector3(Mathf.Cos(n)*radius,Mathf.Sin(n)*radius,-.25f),
                p+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,-.25f),p+Vector3.forward*.04f);
        }
        b.Box(p+Vector3.back*.2f,new Vector3(.045f,.045f,.5f),13,6);
        b.Ball(p+Vector3.back*.47f,Vector3.one*.11f,9);
    }
    static void BuildOrbitalScout(Builder b)
    {
        b.Prism(new[]{V(-.31f,-.57f),V(-.44f,.23f),V(-.22f,.55f),V(.22f,.55f),V(.44f,.23f),V(.31f,-.57f)},.38f,15,Vector3.zero,13);
        foreach(int s in new[]{-1,1})
        {
            b.Box(new Vector3(s*.61f,0,0),new Vector3(.75f,.075f,.10f),13,6);
            SolarPanel(b,new Vector3(s*.98f,0,.04f),.65f,.96f);
            b.Box(new Vector3(s*.19f,-.57f,-.10f),new Vector3(.13f,.22f,.18f),11,6);
        }
        b.Drum(new Vector3(0,.12f,-.26f),.24f,.12f,13,8,6);
        b.Drum(new Vector3(0,.12f,-.35f),.14f,.08f,9,8);
        b.Box(new Vector3(0,.66f,.04f),new Vector3(.035f,.4f,.06f),13,6);
    }
    static void BuildLaserSatellite(Builder b)
    {
        b.Box(Vector3.zero,new Vector3(.62f,1.15f,.43f),15,13);
        b.Box(new Vector3(0,-.78f,-.12f),new Vector3(.36f,.6f,.35f),6,11);
        b.Drum(new Vector3(0,-1.05f,-.24f),.14f,.14f,9,8,13);
        SpaceDish(b,new Vector3(0,.20f,-.28f),.52f);
        foreach(int s in new[]{-1,1})
        {
            b.Box(new Vector3(s*.72f,0,.04f),new Vector3(.85f,.08f,.1f),13,6);
            SolarPanel(b,new Vector3(s*1.02f,.05f,.08f),.62f,1.35f);
        }
    }
    static void BuildOrbitalCarrier(Builder b)
    {
        b.Drum(Vector3.zero,.62f,.44f,15,8,13);
        b.Drum(new Vector3(0,0,-.29f),.39f,.16f,11,8,6);
        b.Drum(new Vector3(0,0,-.40f),.27f,.10f,9,8);
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
        {
            b.Box(new Vector3(x*.55f,y*.43f,.06f),new Vector3(.75f,.08f,.12f),13,6);
            SolarPanel(b,new Vector3(x*.9f,y*.49f,.06f),.55f,.62f);
        }
        SpaceDish(b,new Vector3(0,.72f,-.10f),.25f);
        b.Box(new Vector3(0,-.73f,-.12f),new Vector3(.2f,.4f,.20f),11,6);
    }
    static void BuildOrbitalMoonlet(Builder b)
    {
        b.Drum(Vector3.zero,.34f,.34f,13,6,6);
        b.Ball(new Vector3(0,0,-.24f),new Vector3(.36f,.28f,.20f),15);
        b.Box(new Vector3(0,0,-.36f),new Vector3(.18f,.07f,.045f),9,11);
        foreach(int s in new[]{-1,1})SolarPanel(b,new Vector3(.05f,s*.57f,.04f),.41f,.48f);
        b.Box(new Vector3(.43f,0,.02f),new Vector3(.4f,.03f,.03f),13,6);
    }
    static void BuildSpaceCitadel(Builder b)
    {
        // An open truss station, with sockets for genuinely separate destructible modules.
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4,n=(i+1)*Mathf.PI/4;
            Vector3 u=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0),v=new Vector3(Mathf.Cos(n),Mathf.Sin(n),0);
            b.Face(15,u*.67f+Vector3.back*.22f,v*.67f+Vector3.back*.22f,v*.94f+Vector3.back*.22f,u*.94f+Vector3.back*.22f);
            b.Face(6,u*.94f+Vector3.back*.22f,v*.94f+Vector3.back*.22f,v*.94f+Vector3.forward*.2f,u*.94f+Vector3.forward*.2f);
        }
        foreach(int s in new[]{-1,1})
        {
            b.Box(new Vector3(s*1.25f,.24f,.12f),new Vector3(1.3f,.18f,.25f),13,6);
            b.Box(new Vector3(s*1.17f,-.70f,.13f),new Vector3(.18f,1.6f,.22f),15,6);
            b.Box(new Vector3(s*1.78f,.82f,.15f),new Vector3(.12f,1.0f,.13f),13,6);
            SolarPanel(b,new Vector3(s*1.42f,1.22f,.15f),.7f,.48f);
        }
        b.Box(new Vector3(0,-1.16f,-.05f),new Vector3(.55f,.65f,.5f),15,6);
        b.Drum(new Vector3(0,-1.47f,-.28f),.22f,.15f,9,10,13);
        SpaceDish(b,new Vector3(0,1.1f,.06f),.38f);
    }
    public static Mesh SpaceModule(int kind)
    {
        if(orbitalParts[kind]!=null)return orbitalParts[kind];var b=new Builder();
        if(kind==0)
        {
            b.Box(Vector3.zero,new Vector3(.52f,.76f,.4f),15,13);
            foreach(int s in new[]{-1,1})SolarPanel(b,new Vector3(s*.42f,0,.04f),.34f,1.18f);
            b.Drum(new Vector3(0,0,-.3f),.19f,.16f,10,8,13);
        }
        else if(kind==1)
        {
            b.Drum(Vector3.zero,.36f,.4f,15,8,6);
            foreach(int s in new[]{-1,1})b.Box(new Vector3(s*.16f,-.37f,-.08f),new Vector3(.16f,.65f,.24f),11,6);
            b.Drum(new Vector3(0,0,-.32f),.17f,.1f,10,8);
        }
        else if(kind==2)
        {
            b.Drum(Vector3.zero,.55f,.26f,13,12,6);
            b.Ball(new Vector3(0,0,-.24f),new Vector3(.75f,.75f,.46f),10);
            b.Drum(new Vector3(0,0,-.48f),.17f,.07f,9,10);
        }
        else b.Prism(new[]{V(-.58f,-.65f),V(-.68f,.32f),V(-.38f,.72f),V(-.04f,.60f),V(-.04f,-.60f)},.18f,7,Vector3.back*.64f,15);
        return orbitalParts[kind]=b.Finish("Orbital breakable module "+kind);
    }
}
