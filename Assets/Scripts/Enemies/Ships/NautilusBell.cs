using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class NautilusBell : DistrictEnemy
{
    protected override void Configure()
    {
        hull=77; hullScale=0.57f; health=43;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        int n=Shots(3,5);
        for(int i=0;i<n;i++) Fire<SimpleRound>(Muzzle(i),CombatVisuals.Rotate(Vector2.down,(i-(n-1)*.5f)*29+(volley%2==0?9:-9)),1.9f,.32f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve NautilusBellRoute0=Route("Underwater / NautilusBell 0", Vector2.right, -.7f,1.25f,-.6f,.92f,-.28f,.71f,.21f,.62f,.56f,.83f,.7f,1.25f);
    static readonly FlightCurve NautilusBellRoute1=Route("Underwater / NautilusBell 1", Vector2.right, -1.4f,.77f,-.7f,.59f,.05f,.62f,.7f,.79f,1.4f,.72f);
    public static readonly WavePart[] NautilusBellParts={
        new WavePart("NautilusBell call",nameof(NautilusBell),NautilusBellRoute0,WaveShape.Rake,3,5.45f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("NautilusBell crossing",nameof(NautilusBell),NautilusBellRoute1,WaveShape.Rake,3,5.70f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("NautilusBell answer",nameof(NautilusBell),NautilusBellRoute0,WaveShape.Train,3,5.95f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedNautilusBell;
    public static Mesh NautilusBellMesh()
    {
        if(cachedNautilusBell!=null) return cachedNautilusBell;
        var b=new Builder();
        for(int i=0;i<11;i++) {
            float a=i*.61f,r=.18f+i*.052f;
            var p=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,-.05f-i*.008f);
            b.Drum(p,.18f+i*.006f,.22f,13,6,6);
        }
        b.Drum(new Vector3(0,.02f,-.3f),.2f,.1f,9,8);
        foreach(int s in new[]{-1,1}) {
            b.Prism(new[]{V(s*.35f,-.12f),V(s*.7f,-.33f),V(s*.58f,-.68f),V(s*.32f,-.53f)},.2f,15,Vector3.zero,6);
            Barrel(b,s*.44f,-.3f,.42f,.16f,-.23f);
        }
        return cachedNautilusBell=b.Finish("NautilusBell authored hull and weapon lips");
    }
}
