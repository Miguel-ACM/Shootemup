using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class EclipseSail : DistrictEnemy
{
    protected override Vector2 CueAxis=>Vector2.down;
    protected override void Configure()
    {
        hull=75; hullScale=0.57f; health=53;
        attackInterval=Timing(2.7f,1.9f); nextAttack=1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for(int s=-1;s<=1;s+=2) Fire<SimpleRound>(Muzzle(s<0?0:1),CombatVisuals.Rotate(Vector2.down,s*(volley%2==0?34:18)),1.9f,.37f);
        if(!Difficulty.Easy) Fan<NeedleRound>(3,18,3.9f,.16f);
    }
    
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve EclipseSailRoute0=Route("Space / EclipseSail 0", Vector2.right, -.65f,1.25f,-.62f,.96f,-.36f,.72f,.18f,.6f,.65f,.81f,.7f,1.25f);
    static readonly FlightCurve EclipseSailRoute1=Route("Space / EclipseSail 1", Vector2.right, -1.4f,.86f,-.82f,.66f,-.12f,.52f,.55f,.72f,1.4f,.86f);
    public static readonly WavePart[] EclipseSailParts={
        new WavePart("EclipseSail call",nameof(EclipseSail),EclipseSailRoute0,WaveShape.Rake,3,5.55f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("EclipseSail crossing",nameof(EclipseSail),EclipseSailRoute1,WaveShape.Rake,3,5.80f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("EclipseSail answer",nameof(EclipseSail),EclipseSailRoute0,WaveShape.Train,3,6.05f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedEclipseSail;
    public static Mesh EclipseSailMesh()
    {
        if(cachedEclipseSail!=null) return cachedEclipseSail;
        var b=new Builder();
        for(int i=0;i<8;i++) {
            float a=(30+i*40)*Mathf.Deg2Rad,n=(30+(i+1)*40)*Mathf.Deg2Rad;
            var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var q=new Vector3(Mathf.Cos(n),Mathf.Sin(n),0);
            b.Face(15,p*.56f+Vector3.back*.13f,q*.56f+Vector3.back*.13f,q*.92f+Vector3.back*.13f,p*.92f+Vector3.back*.13f);
            b.Face(13,p*.92f+Vector3.back*.13f,q*.92f+Vector3.back*.13f,q*.92f+Vector3.forward*.13f,p*.92f+Vector3.forward*.13f);
        }
        foreach(int s in new[]{-1,1}) {
            SolarPanel(b,new Vector3(s*.95f,.26f,.05f),.42f,.86f);
            Barrel(b,s*.52f,-.38f,.46f,.16f,-.3f);
        }
        b.Box(new Vector3(0,.55f,0),new Vector3(.25f,.48f,.35f),15,13);
        b.Drum(new Vector3(0,.42f,-.24f),.18f,.14f,9,8);
        return cachedEclipseSail=b.Finish("EclipseSail authored hull and weapon lips");
    }
}
