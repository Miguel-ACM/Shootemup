using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GumdropMortar : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure()
    {
        hull=74; hullScale=0.53f; health=35;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        var shell=Fire<BurstShell>(Muzzle(volley),CombatVisuals.Rotate(Vector2.down,volley%2==0?-15:15),2.4f,.46f);
        if(shell!=null) shell.SetTravelDistance(4.3f);
    }
    
    protected override void PrepareFire(float seconds) { base.PrepareFire(seconds); firingCue.ClearWarning(); Cue(Muzzle(volley+1),seconds); }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve GumdropMortarRoute0=Route("Candyland / GumdropMortar 0", Vector2.right, .65f,1.25f,.53f,.91f,.2f,.72f,-.35f,.69f,-.62f,.94f,-.7f,1.25f);
    static readonly FlightCurve GumdropMortarRoute1=Route("Candyland / GumdropMortar 1", Vector2.right, -1.4f,.87f,-.7f,.78f,0,.69f,.7f,.79f,1.4f,.88f);
    public static readonly WavePart[] GumdropMortarParts={
        new WavePart("GumdropMortar call",nameof(GumdropMortar),GumdropMortarRoute0,WaveShape.Train,2,4.65f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("GumdropMortar crossing",nameof(GumdropMortar),GumdropMortarRoute1,WaveShape.Train,2,4.90f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
        new WavePart("GumdropMortar answer",nameof(GumdropMortar),GumdropMortarRoute0,WaveShape.Rake,2,5.15f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedGumdropMortar;
    public static Mesh GumdropMortarMesh()
    {
        if(cachedGumdropMortar!=null) return cachedGumdropMortar;
        var b=new Builder();
        b.Drum(Vector3.zero,.56f,.38f,8,10,4);
        BevelArmor(b,new[]{V(-.42f,-.34f),V(.42f,-.34f),V(.49f,.25f),V(.22f,.67f),V(-.22f,.67f),V(-.49f,.25f)},.29f,10,-.22f);
        foreach(int s in new[]{-1,1}) {
            b.Box(new Vector3(s*.46f,-.29f,-.21f),new Vector3(.21f,.58f,.19f),11,14);
            b.Drum(new Vector3(s*.57f,.18f,-.08f),.21f,.16f,3,8,4);
            Motor(b,s*.29f,.55f,.18f,.24f,4);
        }
        Glass(b,0,-.1f,.38f,.2f,-.43f);
        return cachedGumdropMortar=b.Finish("GumdropMortar authored hull and weapon lips");
    }
}
