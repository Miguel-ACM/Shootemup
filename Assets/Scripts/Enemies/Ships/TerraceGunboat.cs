using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class TerraceGunboat : DistrictEnemy
{
    protected override void Configure()
    {
        hull=66; hullScale=0.53f; health=38;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for(int s=-1;s<=1;s+=2)
            for(int i=0;i<Shots(2,3);i++) Fire<SimpleRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(16+i*16)+(volley%2==0?5:-5)),3.3f,.22f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve TerraceGunboatRoute0=Route("City / TerraceGunboat 0", Vector2.right, -1.4f,.73f,-.72f,.65f,-.05f,.57f,.62f,.65f,1.4f,.78f);
    static readonly FlightCurve TerraceGunboatRoute1=Route("City / TerraceGunboat 1", Vector2.right, 1.4f,.88f,.8f,.77f,.2f,.67f,-.45f,.72f,-1.4f,.9f);
    public static readonly WavePart[] TerraceGunboatParts={
        new WavePart("TerraceGunboat call",nameof(TerraceGunboat),TerraceGunboatRoute0,WaveShape.Train,3,3.55f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("TerraceGunboat crossing",nameof(TerraceGunboat),TerraceGunboatRoute1,WaveShape.Train,3,3.80f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("TerraceGunboat answer",nameof(TerraceGunboat),TerraceGunboatRoute0,WaveShape.Rake,3,4.05f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedTerraceGunboat;
    public static Mesh TerraceGunboatMesh()
    {
        if(cachedTerraceGunboat!=null) return cachedTerraceGunboat;
        var b=new Builder();
        BevelArmor(b,new[]{V(-.34f,-.49f),V(.34f,-.49f),V(.46f,.34f),V(.17f,.75f),V(-.17f,.75f),V(-.46f,.34f)},.34f,3);
        foreach(int s in new[]{-1,1}) {
            Armor(b,new[]{V(s*.25f,-.1f),V(s*1.05f,-.27f),V(s*1.12f,.38f),V(s*.36f,.56f)},.23f,13);
            Barrel(b,s*.82f,-.21f,.54f,.18f,-.18f); Motor(b,s*.65f,.52f,.25f,.3f,3);
            b.Box(new Vector3(s*.55f,.13f,-.21f),new Vector3(.19f,.3f,.07f),10,11);
        }
        Glass(b,0,-.03f,.38f,.25f,-.32f);
        return cachedTerraceGunboat=b.Finish("TerraceGunboat authored hull and weapon lips");
    }
}
