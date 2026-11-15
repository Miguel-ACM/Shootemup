using UnityEngine;

// Snow blizzards and Desert sandstorms blow from one side for the whole district. Their
// formations ride that wind: every route is authored entering from the left and is
// mirrored so it always enters upwind and leaves downwind.
public static partial class WavePartLibrary
{
    static readonly FlightCurve WhiteoutGust = Route("Snow blizzard / Whiteout gust", Vector2.up,
        -1.40f, .86f, -.70f, .74f, 0, .86f, .70f, .72f, 1.40f, .84f);
    static readonly FlightCurve DriftCurl = Route("Snow blizzard / Drift curl", Vector2.right,
        -.72f, 1.24f, -.62f, .80f, -.20f, .62f, .30f, .66f, .52f, .88f, .78f, 1.24f);
    static readonly FlightCurve SnowblindPost = FlightCurve.Stations("Snow blizzard / Snowblind post", Vector2.right,
        new[] { 0f, .24f, .66f, 1f }, -1.62f, .82f, -.48f, .80f, -.48f, .80f, 1.72f, .88f);
    static readonly FlightCurve Gale = Route("Snow blizzard / Gale run", Vector2.up,
        -1.40f, .92f, -.40f, .84f, .40f, .76f, 1.40f, .70f);
    static readonly FlightCurve CrosswindFile = Route("Snow blizzard / Crosswind file", Vector2.up, -1.40f, .70f, 1.40f, .70f);
    static readonly FlightCurve StormDriven = FlightCurve.Stations("Snow blizzard / Storm-driven battery", Vector2.right,
        new[] { 0f, .18f, .36f, .52f, .70f, 1f }, -1.70f, .80f, -.60f, .78f, -.60f, .78f, .20f, .62f, .20f, .62f, 1.70f, .70f);
    static readonly FlightCurve Spindrift = Route("Snow blizzard / Spindrift dive", Vector2.right,
        -.80f, 1.24f, -.55f, .78f, -.05f, .55f, .35f, .30f, .62f, -.30f);

    public static readonly WavePart[] SnowBlizzard =
    {
        new WavePart("Whiteout gust", "ColdWeatherFighter", WhiteoutGust, WaveShape.Train, 5, 4.7f).Under(DistrictCondition.Blizzard, true),
        new WavePart("Drift curl", "ColdWeatherFighter", DriftCurl, WaveShape.Rank, 3, 5.0f).Under(DistrictCondition.Blizzard, true),
        new WavePart("Snowblind lantern", nameof(HailLantern), SnowblindPost, WaveShape.Solo, 1, 5.9f, 4, new[] { .32f, .48f, .62f }, WaveMotion.Station).Under(DistrictCondition.Blizzard, true),
        new WavePart("Gale bomber", "AvalancheBomber", Gale, WaveShape.Solo, 1, 5.8f, 5).Under(DistrictCondition.Blizzard, true),
        new WavePart("Crosswind file", "ColdWeatherFighter", CrosswindFile, WaveShape.Train, 5, 4.5f, 3, null, WaveMotion.Straight).Under(DistrictCondition.Blizzard, true),
        new WavePart("Storm-driven battery", "SiegeGunship", StormDriven, WaveShape.Solo, 1, 6.3f, 6, new[] { .29f, .60f }, WaveMotion.Relay).Under(DistrictCondition.Blizzard, true),
        new WavePart("Spindrift dive", "ColdWeatherFighter", Spindrift, WaveShape.Chevron, 3, 5.1f, 4).Under(DistrictCondition.Blizzard, true),
        new WavePart("Shielded gale", "ShieldTransport", Gale, WaveShape.Solo, 1, 6.0f, 8).Under(DistrictCondition.Blizzard, true)
    };

    static readonly FlightCurve StormWall = Route("Desert sandstorm / Out of the storm wall", Vector2.up,
        -1.65f, .80f, -.60f, .72f, .20f, .80f, .80f, .66f, 1.65f, .74f);
    static readonly FlightCurve DustDevil = Route("Desert sandstorm / Dust devil", Vector2.right,
        -.62f, 1.40f, -.20f, .92f, .25f, .78f, .30f, .55f, -.05f, .48f, -.25f, .66f, 0, .90f, .55f, 1.40f);
    static readonly FlightCurve DriftPosts = FlightCurve.Stations("Desert sandstorm / Drift posts", Vector2.right,
        new[] { 0f, .17f, .34f, .52f, .68f, 1f }, -1.80f, .78f, -.50f, .76f, -.50f, .76f, .45f, .60f, .45f, .60f, 1.80f, .70f);
    static readonly FlightCurve SandBlind = Route("Desert sandstorm / Sand-blind file", Vector2.up, -1.80f, .58f, 1.80f, .58f);
    static readonly FlightCurve DriftBury = Route("Desert sandstorm / Buried in the drift", Vector2.right,
        -.45f, 1.40f, -.52f, .90f, -.30f, .64f, .05f, .50f, .25f, .25f, .30f, -.40f);
    static readonly FlightCurve UnderStorm = Route("Desert sandstorm / Under the storm", Vector2.up,
        -1.85f, .70f, -.80f, .78f, 0, .62f, .80f, .80f, 1.85f, .72f);
    static readonly FlightCurve Shear = Route("Desert sandstorm / Wind shear", Vector2.right,
        -.78f, 1.40f, -.64f, .82f, -.18f, .66f, .36f, .72f, .66f, .46f, .74f, -.40f);

    public static readonly WavePart[] DesertSandstorm =
    {
        new WavePart("Out of the storm wall", "SandStriker", StormWall, WaveShape.Train, 5, 4.0f).Under(DistrictCondition.Sandstorm, true),
        new WavePart("Dust devil", "SandStriker", DustDevil, WaveShape.Train, 4, 4.4f).Under(DistrictCondition.Sandstorm, true),
        new WavePart("Storm-driven moth", nameof(DuneMoth), DriftPosts, WaveShape.Solo, 1, 5.7f, 4, new[] { .25f, .60f }, WaveMotion.Relay).Under(DistrictCondition.Sandstorm, true),
        new WavePart("Sand-blind file", "SandStriker", SandBlind, WaveShape.Train, 5, 3.9f, 3, null, WaveMotion.Straight).Under(DistrictCondition.Sandstorm, true),
        new WavePart("Buried in the drift", "SandEmitter", DriftBury, WaveShape.Train, 3, 4.7f, 3).Under(DistrictCondition.Sandstorm, true),
        new WavePart("Worm under the storm", "SandWorm", UnderStorm, WaveShape.Solo, 1, 5.6f, 6).Under(DistrictCondition.Sandstorm, true),
        new WavePart("Wind shear arrows", "SandStriker", Shear, WaveShape.Chevron, 3, 4.2f, 5).Under(DistrictCondition.Sandstorm, true)
    };
}
