using UnityEngine;

// Plains at dusk and at night fly their own formations alongside the shared valley motifs.
// Dusk attacks come out of the low sun: every dusk route is authored entering from the
// left and is mirrored so it always enters from the sun's side. Night raids rise from the
// river, cross in blacked-out files and hunt with a single lantern gunship.
public static partial class WavePartLibrary
{
    static readonly FlightCurve SunsetCrossing = Route("Plains dusk / Sunset crossing", Vector2.up,
        -1.36f, .62f, -.70f, .70f, 0, .81f, .68f, .74f, 1.36f, .86f);
    static readonly FlightCurve OutOfTheSun = Route("Plains dusk / Out of the sun", Vector2.right,
        -.86f, 1.24f, -.72f, .86f, -.34f, .61f, .22f, .53f, .60f, .70f, .74f, 1.24f);
    static readonly FlightCurve HomewardFlock = Route("Plains dusk / Homeward flock", Vector2.up,
        -1.36f, .95f, -.62f, .83f, .08f, .70f, .70f, .66f, 1.36f, .58f);
    static readonly FlightCurve EveningPosts = FlightCurve.Stations("Plains dusk / Evening posts", Vector2.right,
        new[] { 0f, .18f, .36f, .52f, .70f, 1f }, -1.38f, .80f, -.46f, .76f, -.46f, .76f, .34f, .64f, .34f, .64f, 1.38f, .78f);
    static readonly FlightCurve LowSun = Route("Plains dusk / Low sun bombing run", Vector2.up,
        -1.40f, .55f, -.52f, .62f, .28f, .78f, 1.40f, .92f);
    static readonly FlightCurve LongShadow = Route("Plains dusk / Long shadow", Vector2.right,
        -.72f, 1.24f, -.66f, .84f, -.30f, .62f, .20f, .58f, .60f, .80f, .66f, 1.24f);

    public static readonly WavePart[] PlainsDusk =
    {
        new WavePart("Sunset crossing", "ScoutInterceptor", SunsetCrossing, WaveShape.Train, 5, 4.6f).Under(DistrictCondition.Dusk, true),
        new WavePart("Out of the sun", "ScoutInterceptor", OutOfTheSun, WaveShape.Chevron, 3, 4.7f).Under(DistrictCondition.Dusk, true),
        new WavePart("Homeward flock", "ScoutInterceptor", HomewardFlock, WaveShape.Rank, 3, 4.8f, 3).Under(DistrictCondition.Dusk, true),
        new WavePart("Evening posts", "PatrolGunship", EveningPosts, WaveShape.Solo, 1, 6.0f, 5, new[] { .26f, .44f, .62f }, WaveMotion.Relay).Under(DistrictCondition.Dusk, true),
        new WavePart("Low sun bombing run", "HeavyBomber", LowSun, WaveShape.Solo, 1, 5.6f, 6).Under(DistrictCondition.Dusk, true),
        new WavePart("Long shadow harrows", nameof(HarrowBomber), LongShadow, WaveShape.Rank, 2, 5.6f, 5, new[] { .34f, .66f }).Under(DistrictCondition.Dusk, true),
        new WavePart("Sundown escort", "PatrolGunship", OutOfTheSun, WaveShape.Rank, 2, 5.4f, 8).Under(DistrictCondition.Dusk, true)
    };

    static readonly FlightCurve MoonlitRiver = Route("Plains night / Moonlit river run", Vector2.right,
        .12f, -.40f, -.20f, .12f, .16f, .45f, -.10f, .80f, .06f, 1.40f);
    static readonly FlightCurve OwlStoop = Route("Plains night / Owl stoop", Vector2.right,
        -.70f, 1.24f, -.62f, .70f, -.36f, .42f, -.06f, .50f, .05f, .88f, .02f, 1.24f);
    static readonly FlightCurve LanternWatch = FlightCurve.Stations("Plains night / Lantern watch", Vector2.right,
        new[] { 0f, .22f, .68f, 1f }, -.30f, 1.26f, -.30f, .72f, -.30f, .72f, .58f, 1.26f).WithTracking(.26f, .10f);
    static readonly FlightCurve Blackout = Route("Plains night / Blackout file", Vector2.up, -1.40f, .80f, 1.40f, .80f);
    static readonly FlightCurve NightHarrow = Route("Plains night / Harrow S", Vector2.right,
        .25f, 1.24f, .35f, .85f, -.15f, .62f, -.30f, .40f, .05f, .16f, .10f, -.30f);
    static readonly FlightCurve MoonDiamond = Route("Plains night / Moonlight hook", Vector2.right,
        .55f, 1.24f, .62f, .82f, .30f, .58f, -.20f, .56f, -.55f, .80f, -.58f, 1.24f);
    static readonly FlightCurve NightRoad = Route("Plains night / Darkened road", Vector2.right,
        -.51f, 1.3f, -.55f, .88f, -.47f, .52f, -.52f, .14f, -.50f, -.3f);

    public static readonly WavePart[] PlainsNight =
    {
        new WavePart("Moonlit river run", nameof(ReedSkimmer), MoonlitRiver, WaveShape.Train, 4, 5.0f, 2, new[] { .42f, .64f }, WaveMotion.Rising).Under(DistrictCondition.Night),
        new WavePart("Owl stoop", "ScoutInterceptor", OwlStoop, WaveShape.Train, 4, 4.9f).Under(DistrictCondition.Night),
        new WavePart("Lantern watch", "PatrolGunship", LanternWatch, WaveShape.Solo, 1, 5.7f, 5, new[] { .31f, .47f, .63f }, WaveMotion.Pursuit).Under(DistrictCondition.Night),
        new WavePart("Blackout file", "ScoutInterceptor", Blackout, WaveShape.Train, 5, 4.6f, 3, null, WaveMotion.Straight).Under(DistrictCondition.Night),
        new WavePart("Night harrows", nameof(HarrowBomber), NightHarrow, WaveShape.Train, 2, 5.5f, 5, new[] { .30f, .60f }).Under(DistrictCondition.Night),
        new WavePart("Moonlight hook", "ScoutInterceptor", MoonDiamond, WaveShape.Diamond, 4, 5.2f, 4).Under(DistrictCondition.Night),
        new WavePart("Headlight convoy", "MeadowTank", NightRoad, WaveShape.Train, 3, 4.7f, 4).Under(DistrictCondition.Night),
        new WavePart("Midnight bomber", "HeavyBomber", OwlStoop, WaveShape.Solo, 1, 5.6f, 7).Under(DistrictCondition.Night)
    };
}
