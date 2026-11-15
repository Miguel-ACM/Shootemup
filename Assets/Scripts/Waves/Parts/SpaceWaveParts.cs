using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve Apogee = Route("Space / Apogee curl", Vector2.right, -.72f, 1.23f, -.85f, .95f, -.42f, .72f, .19f, .67f, .64f, .88f, .77f, 1.23f);
    static readonly FlightCurve Transfer = Route("Space / Transfer orbit", Vector2.up, -1.35f, .78f, -.91f, .63f, -.22f, .78f, .41f, .66f, .93f, .77f, 1.35f, .92f);
    static readonly FlightCurve Slingshot = Route("Space / Slingshot", Vector2.right, .66f, 1.23f, .34f, .83f, -.43f, .71f, -.64f, .39f, -.09f, .17f, .63f, -.23f);
    static readonly FlightCurve Terminator = Route("Space / Terminator pass", Vector2.up, -1.35f, .96f, -.69f, .83f, -.16f, .87f, .39f, .70f, .82f, .80f, 1.35f, .93f);
    static readonly FlightCurve Halo = Route("Space / Halo carrier", Vector2.right, -.44f, 1.26f, -.52f, .90f, -.24f, .77f, .29f, .78f, .55f, .93f, .49f, 1.26f);
    static readonly FlightCurve Perigee = Route("Space / Perigee scissors", Vector2.right, .69f, 1.24f, .74f, .89f, .31f, .60f, -.22f, .73f, -.45f, 1.0f, -.47f, 1.24f);
    static readonly FlightCurve Observation = FlightCurve.Stations("Space / Observation stations", Vector2.right,
        new[] { 0f, .20f, .37f, .53f, .72f, 1f }, -.55f, 1.26f, -.55f, .83f, -.55f, .83f, .52f, .78f, .52f, .78f, .8f, 1.26f);
    static readonly FlightCurve Retrograde = Route("Space / Retrograde pass", Vector2.right, .70f, 1.50f, .56f, .90f, .10f, .70f, -.36f, .76f, -.62f, .52f, -.70f, -.40f);
    static readonly FlightCurve Ecliptic = Route("Space / Ecliptic", Vector2.up, -1.40f, .74f, -.70f, .62f, 0, .74f, .70f, .86f, 1.40f, .74f);
    static readonly FlightCurve Precession = Route("Space / Precession", Vector2.right, -.20f, 1.50f, -.55f, .90f, -.30f, .60f, .20f, .58f, .50f, .82f, .30f, 1.50f);
    // Asteroids drift through in formation and never fire; each kill splits a rock in two.
    static readonly FlightCurve BoulderDrift = Route("Space / Boulder drift", Vector2.right, -.42f, 1.32f, -.22f, .86f, .08f, .4f, .3f, -.36f);
    static readonly FlightCurve RubbleBelt = Route("Space / Rubble belt", Vector2.right, .02f, 1.32f, .12f, .78f, -.04f, .3f, .06f, -.36f);
    static readonly FlightCurve MeteorStream = Route("Space / Meteor stream", Vector2.right, .72f, 1.32f, .46f, .9f, -.08f, .42f, -.5f, -.36f);
    static readonly FlightCurve AsteroidCrossing = Route("Space / Asteroid crossing", Vector2.up, -1.42f, .9f, -.5f, .74f, .45f, .62f, 1.42f, .46f);
    public static readonly WavePart[] Space ={
        new WavePart("Lagrange shadow", "OrbitalScout", FlightCurve.Stations("Space / Lagrange shadow", Vector2.right, new[]{0f,.25f,.66f,1f}, -.27f,1.5f, -.27f,.75f, -.27f,.75f, .75f,1.5f).WithTracking(.18f,.10f), WaveShape.Solo, 1, 5.7f, 6, new[]{.34f,.48f,.62f}, WaveMotion.Pursuit),
        new WavePart("Solar escape train", "OrbitalScout", Route("Space / Solar escape", Vector2.right, -.82f,-.4f, -.74f,.15f, -.38f,.43f, .18f,.57f, .63f,.88f, .81f,1.4f), WaveShape.Train, 4, 4.4f, 2, new[]{.44f,.65f}, WaveMotion.Rising),
        new WavePart("Observation dish", "LaserSatellite", Observation, WaveShape.Solo, 1, 6.8f, 5, new[] { .24f, .57f }, motion: WaveMotion.Relay),
        new WavePart("Apogee scouts","OrbitalScout",Apogee,WaveShape.Train,4,4.0f),
        new WavePart("Transfer scouts","OrbitalScout",Transfer,WaveShape.Rank,4,3.9f),
        new WavePart("Solar slingshot","OrbitalScout",Slingshot,WaveShape.Chevron,5,4.1f),
        new WavePart("Perigee twins","OrbitalScout",Perigee,WaveShape.Diamond,4,4.1f),
        new WavePart("Terminator emitters","LaserSatellite",Terminator,WaveShape.Train,2,4.8f),
        new WavePart("Halo escort carrier","OrbitalCarrier",Halo,WaveShape.Solo,1,5.0f,6),
        new WavePart("Low transfer emitter","LaserSatellite",Transfer,WaveShape.Solo,1,4.7f),
        new WavePart("Slingshot dish escort","LaserSatellite",Slingshot,WaveShape.Solo,1,4.9f),
        new WavePart("Opposed relay scouts","OrbitalScout",Terminator,WaveShape.Rake,4,4.0f),
        new WavePart("Wide apogee carrier","OrbitalCarrier",Apogee,WaveShape.Solo,1,5.1f,8),
        new WavePart("Perigee laser rake","LaserSatellite",Perigee,WaveShape.Rake,2,4.8f,10),
        new WavePart("Retrograde scouts", "OrbitalScout", Retrograde, WaveShape.Train, 4, 4.0f, 3),
        new WavePart("Ecliptic rank", "OrbitalScout", Ecliptic, WaveShape.Rank, 4, 4.0f, 4),
        new WavePart("Precession emitters", "LaserSatellite", Precession, WaveShape.Train, 2, 4.8f, 6),
        new WavePart("Retrograde carrier", "OrbitalCarrier", Retrograde, WaveShape.Solo, 1, 5.1f, 9),
        new WavePart("Boulder drift", "SpaceBoulder", BoulderDrift, WaveShape.Solo, 1, 7.6f, 3, new float[0]),
        new WavePart("Rubble belt", "SpaceAsteroid", RubbleBelt, WaveShape.Rank, 3, 6.8f, 2, new float[0]),
        new WavePart("Meteor stream", "SpaceAsteroid", MeteorStream, WaveShape.Train, 3, 6.2f, 4, new float[0]),
        new WavePart("Asteroid crossing", "SpaceAsteroid", AsteroidCrossing, WaveShape.Rank, 2, 7.4f, 5, new float[0])
    };
}
