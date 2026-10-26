using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class TerraceGunboat : DistrictEnemy
{
    protected override Vector2 CueAxis => Vector2.down;
    protected override void Configure()
    {
        hull = 66;
        hullScale = 0.53f;
        health = 38;
        attackInterval = Timing(2.8f, 2.0f);
        nextAttack = 1.4f;
        Mover<Slalom>();
    }
    protected override void Attack()
    {
        for (int s = -1; s <= 1; s += 2)
            for (int i = 0; i < Shots(2, 3); i++)
                Fire<SimpleRound>(Muzzle(s < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, s * (16 + i * 16) + (volley % 2 == 0 ? 5 : -5)), 3.3f, .22f);
    }

}

public static partial class WavePartLibrary
{
    static readonly FlightCurve TerraceGunboatRoute0 = Route("City / Terrace balcony sweep", Vector2.right, -1.4f, 0.87f, -0.61f, 0.69f, 0.16f, 0.67f, 0.72f, 0.83f, 1.4f, 0.96f);
    static readonly FlightCurve TerraceGunboatRoute1 = Route("City / Terrace descending avenue", Vector2.right, -0.52f, 1.25f, -0.5f, 0.84f, -0.17f, 0.62f, 0.44f, 0.48f, 1.4f, 0.4f);
    static readonly FlightCurve TerraceGunboatRoute2 = Route("City / Terrace rooftop turn", Vector2.right, 1.4f, 0.63f, 0.68f, 0.56f, 0.08f, 0.68f, -0.49f, 0.9f, -0.62f, 1.25f);
    public static readonly WavePart[] TerraceGunboatParts =
    {
        new WavePart("Terrace balcony sweep", nameof(TerraceGunboat), TerraceGunboatRoute0, WaveShape.Rank, 2, 4.0f, 2, new[] { 0.3f, 0.62f }, new[] { 0.46f }, 2),
        new WavePart("Terrace descending avenue", nameof(TerraceGunboat), TerraceGunboatRoute1, WaveShape.Rake, 2, 4.15f, 4, new[] { 0.27f, 0.49f, 0.7f }, new[] { 0.39f, 0.72f }, 2),
        new WavePart("Terrace rooftop turn", nameof(TerraceGunboat), TerraceGunboatRoute2, WaveShape.Train, 2, 3.95f, 7, new[] { 0.32f, 0.65f }, new[] { 0.48f }, 2),
    };
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedTerraceGunboat;
    public static Mesh TerraceGunboatMesh()
    {
        if (cachedTerraceGunboat != null)
            return cachedTerraceGunboat;
        var b = new Builder();
        BevelArmor(b, new[] { V(-.34f, -.49f), V(.34f, -.49f), V(.46f, .34f), V(.17f, .75f), V(-.17f, .75f), V(-.46f, .34f) }, .34f, 3);
        foreach (int s in new[] { -1, 1 })
        {
            Armor(b, new[] { V(s * .25f, -.1f), V(s * 1.05f, -.27f), V(s * 1.12f, .38f), V(s * .36f, .56f) }, .23f, 13);
            Barrel(b, s * .82f, -.21f, .54f, .18f, -.18f);
            Motor(b, s * .65f, .52f, .25f, .3f, 3);
            b.Box(new Vector3(s * .55f, .13f, -.21f), new Vector3(.19f, .3f, .07f), 10, 11);
        }
        Glass(b, 0, -.03f, .38f, .25f, -.32f);
        return cachedTerraceGunboat = b.Finish("TerraceGunboat authored hull and weapon lips");
    }
}
