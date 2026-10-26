using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class BaffleSpinner : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 67;
        hullScale = 0.48f;
        health = 30;
        attackInterval = Timing(2.7f, 1.9f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            Fire<ReturningDisc>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * (volley % 2 == 0 ? 35 : 55)), 2.6f, .31f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve BaffleSpinnerRoute0 = Route("Tunnel / BaffleSpinner 0", Vector2.right, -.7f, 1.25f, -.64f, .9f, -.35f, .62f, .15f, .52f, .58f, .7f, .66f, 1.25f);
    static readonly FlightCurve BaffleSpinnerRoute1 = Route("Tunnel / BaffleSpinner 1", Vector2.right, 1.4f, .7f, .7f, .62f, .15f, .46f, -.52f, .55f, -1.4f, .7f);
    public static readonly WavePart[] BaffleSpinnerParts ={
        new WavePart("BaffleSpinner call",nameof(BaffleSpinner),BaffleSpinnerRoute0,WaveShape.Rake,3,4.85f,2, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("BaffleSpinner crossing",nameof(BaffleSpinner),BaffleSpinnerRoute1,WaveShape.Rake,3,5.10f,4, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
        new WavePart("BaffleSpinner answer",nameof(BaffleSpinner),BaffleSpinnerRoute0,WaveShape.Train,3,5.35f,7, new[]{.28f,.48f,.72f},new[]{.38f,.73f}, 3),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedBaffleSpinner;
    public static Mesh BaffleSpinnerMesh()
    {
        if (cachedBaffleSpinner != null)
            return cachedBaffleSpinner;
        var b = new Builder();
        b.Drum(Vector3.zero, .38f, .3f, 6, 10, 11);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .25f, -.1f), V(s * .6f, -.51f), V(s * .97f, -.2f), V(s * .8f, .52f), V(s * .4f, .73f), V(s * .62f, .25f) }, .18f, 0);
            b.Box(new Vector3(s * .64f, -.2f, -.16f), new Vector3(.19f, .2f, .17f), 11, 6);
            Motor(b, s * .3f, .55f, .18f, .27f, 0);
        }
        b.Drum(new Vector3(0, 0, -.22f), .19f, .12f, 10, 8);
        return cachedBaffleSpinner = b.Finish("BaffleSpinner authored hull and weapon lips");
    }
}
