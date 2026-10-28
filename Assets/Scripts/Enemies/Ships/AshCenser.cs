using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class AshCenser : DistrictEnemy
{
    protected override Vector2 CueAxis => volley % 2 == 0 ? Vector2.down : Aim;
    protected override void Configure()
    {
        hull = 79;
        hullScale = 0.58f;
        health = 55;
        attackInterval = (1.75f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    // Embers thrown up that stop at their apex, then fall as fast and slow columns.
    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            int count = Dense(5);
            for (int i = 0; i < count; i++)
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.up, (i - (count - 1) * .5f) * 16), 2.8f, .24f)
                    .Pause(.55f, .3f, CombatRound.Turn.Down, 0, i % 2 == 0 ? .6f : 1.05f);
        }
        else
            for (int s = -1; s <= 1; s += 2)
                Fire<PlasmaOrb>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * (volley % 4 == 1 ? 32 : 15)), 1.7f, .38f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve AshCenserRoute0 = Route("Hell / Censer downward pendulum", Vector2.right, -0.65f, 1.25f, -0.62f, 0.86f, -0.24f, 0.58f, 0.42f, 0.42f, 0.72f, 0.17f, 0.76f, -0.25f);
    static readonly FlightCurve AshCenserRoute1 = Route("Hell / Censer funeral arch", Vector2.right, -2.2f, 0.72f, -0.7f, 0.47f, 0.0f, 0.41f, 0.67f, 0.59f, 2.2f, 0.9f);
    static readonly FlightCurve AshCenserRoute2 = Route("Hell / Censer ashen return", Vector2.right, 0.64f, 1.25f, 0.69f, 0.83f, 0.28f, 0.54f, -0.36f, 0.61f, -0.74f, 0.93f, -0.78f, 1.25f);
    public static readonly WavePart[] AshCenserParts =
    {
        new WavePart("Censer downward pendulum", nameof(AshCenser), AshCenserRoute0, WaveShape.Rake, 3, 5.2f, 2, new[] { 0.25f, 0.41f, 0.59f, 0.76f }),
        new WavePart("Censer funeral arch", nameof(AshCenser), AshCenserRoute1, WaveShape.Rank, 3, 5.1f, 4, new[] { 0.27f, 0.45f, 0.66f }),
        new WavePart("Censer ashen return", nameof(AshCenser), AshCenserRoute2, WaveShape.Train, 3, 5.4f, 7, new[] { 0.26f, 0.4f, 0.59f, 0.73f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedAshCenser;
    public static Mesh AshCenserMesh()
    {
        if (cachedAshCenser != null)
            return cachedAshCenser;
        var b = new Builder();
        b.Drum(Vector3.zero, .51f, .37f, 6, 8, 11);
        b.Drum(new Vector3(0, -.2f, -.29f), .34f, .19f, 10, 8, 13);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3;
            b.Box(new Vector3(Mathf.Cos(a) * .45f, Mathf.Sin(a) * .45f, -.24f), new Vector3(.12f, .2f, .33f), 13, 6);
        }
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .17f, .39f), V(s * .5f, .99f), V(s * .13f, .76f) }, .17f, 15, Vector3.zero, 6);
            Barrel(b, s * .41f, -.33f, .38f, .17f, -.24f);
        }
        b.Prism(new[] { V(-.24f, -.41f), V(0, -.94f), V(.24f, -.41f) }, .2f, 13, Vector3.zero, 6);
        return cachedAshCenser = b.Finish("AshCenser authored hull and weapon lips");
    }
}
