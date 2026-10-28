using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class MetroLancer : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 65;
        hullScale = 0.41f;
        health = 19;
        attackInterval = (1.4f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    // A skid: three rounds curling away from the aim like a car taking a corner.
    protected override void Attack()
    {
        int count = Dense(3);
        int sign = volley % 2 == 0 ? 1 : -1;
        var aim = Aim;
        for (int i = 0; i < count; i++)
        {
            var round = Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(aim, -sign * 18), 4.2f, .17f);
            if (round != null)
                round.Curl(sign * (20 + i * 10), 1.4f);
        }
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve MetroLancerRoute0 = Route("City / Metro express cut", Vector2.right, -1.4f, 0.9f, -0.65f, 0.75f, 0.08f, 0.49f, 0.7f, 0.26f, 1.4f, 0.17f);
    static readonly FlightCurve MetroLancerRoute1 = Route("City / Metro flyover climb", Vector2.right, -1.4f, 0.28f, -0.64f, 0.41f, 0.12f, 0.7f, 0.66f, 0.92f, 1.4f, 1.16f);
    static readonly FlightCurve MetroLancerRoute2 = Route("City / Metro interchange dive", Vector2.right, 0.58f, 1.25f, 0.48f, 0.85f, -0.25f, 0.62f, -0.53f, 0.37f, 0.08f, 0.13f, 0.66f, -0.25f);
    public static readonly WavePart[] MetroLancerParts =
    {
        new WavePart("Metro express cut", nameof(MetroLancer), MetroLancerRoute0, WaveShape.Train, 5, 3.05f, 2, new[] { 0.26f, 0.45f, 0.64f }),
        new WavePart("Metro flyover climb", nameof(MetroLancer), MetroLancerRoute1, WaveShape.Rake, 3, 2.95f, 4, new[] { 0.3f, 0.59f }),
        new WavePart("Metro interchange dive", nameof(MetroLancer), MetroLancerRoute2, WaveShape.Train, 4, 3.45f, 7, new[] { 0.24f, 0.43f, 0.69f }),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedMetroLancer;
    public static Mesh MetroLancerMesh()
    {
        if (cachedMetroLancer != null)
            return cachedMetroLancer;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -1.14f), V(-.2f, -.6f), V(-.24f, .86f), V(.24f, .86f), V(.2f, -.6f) }, .32f, 13);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .2f, -.53f, -.15f), new Vector3(.1f, .98f, .15f), 11, 6);
            Armor(b, new[] { V(s * .17f, .12f), V(s * .77f, .67f), V(s * .45f, .55f), V(s * .28f, -.18f) }, .12f, 3);
            Motor(b, s * .24f, .74f, .18f, .32f, 13);
        }
        Glass(b, 0, .09f, .25f, .52f, -.3f);
        return cachedMetroLancer = b.Finish("MetroLancer authored hull and weapon lips");
    }
}
