using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve Apogee=Route("Space / Apogee curl",Vector2.right,-.72f,1.23f,-.85f,.95f,-.42f,.72f,.19f,.67f,.64f,.88f,.77f,1.23f);
    static readonly FlightCurve Transfer=Route("Space / Transfer orbit",Vector2.up,-1.35f,.78f,-.91f,.63f,-.22f,.78f,.41f,.66f,.93f,.77f,1.35f,.92f);
    static readonly FlightCurve Slingshot=Route("Space / Slingshot",Vector2.right,.66f,1.23f,.34f,.83f,-.43f,.71f,-.64f,.39f,-.09f,.17f,.63f,-.23f);
    static readonly FlightCurve Terminator=Route("Space / Terminator pass",Vector2.up,-1.35f,.96f,-.69f,.83f,-.16f,.87f,.39f,.70f,.82f,.80f,1.35f,.93f);
    static readonly FlightCurve Halo=Route("Space / Halo carrier",Vector2.right,-.44f,1.26f,-.52f,.90f,-.24f,.77f,.29f,.78f,.55f,.93f,.49f,1.26f);
    static readonly FlightCurve Perigee=Route("Space / Perigee scissors",Vector2.right,.69f,1.24f,.74f,.89f,.31f,.60f,-.22f,.73f,-.45f,1.0f,-.47f,1.24f);
    public static readonly WavePart[] Space={
        new WavePart("Apogee scouts","OrbitalScout",Apogee,WaveShape.Train,4,4.0f),
        new WavePart("Transfer scouts","OrbitalScout",Transfer,WaveShape.Rank,4,3.9f),
        new WavePart("Solar slingshot","OrbitalScout",Slingshot,WaveShape.Chevron,5,4.1f),
        new WavePart("Perigee twins","OrbitalScout",Perigee,WaveShape.Diamond,4,4.1f),
        new WavePart("Terminator emitters","LaserSatellite",Terminator,WaveShape.Train,2,4.8f),
        new WavePart("Halo escort carrier","OrbitalCarrier",Halo,WaveShape.Solo,1,5.0f,6),
        new WavePart("Low transfer emitter","LaserSatellite",Transfer,WaveShape.Solo,1,4.7f,4),
        new WavePart("Opposed relay scouts","OrbitalScout",Terminator,WaveShape.Rake,4,4.0f),
        new WavePart("Wide apogee carrier","OrbitalCarrier",Apogee,WaveShape.Solo,1,5.1f,8),
        new WavePart("Perigee laser rake","LaserSatellite",Perigee,WaveShape.Rake,2,4.8f,10)
    };
}
