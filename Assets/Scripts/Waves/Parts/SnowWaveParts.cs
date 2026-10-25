using UnityEngine;
public static partial class WavePartLibrary
{
    static readonly FlightCurve GlacierCrown = Route("Snow / Glacier crown", Vector2.right, -.50f, 1.24f, -.55f, .89f, -.23f, .65f, .24f, .67f, .54f, .92f, .56f, 1.24f);
    static readonly FlightCurve WhiteCurl = Route("Snow / White curl", Vector2.right, .68f, 1.24f, .77f, .94f, .62f, .70f, .33f, .56f, -.04f, .58f, -.27f, .78f, -.28f, 1.24f);
    static readonly FlightCurve IceWind = Route("Snow / Ice wind", Vector2.up, -1.36f, .93f, -.70f, .97f, -.12f, .85f, .52f, .82f, 1.36f, .98f);
    static readonly FlightCurve FrostFall = Route("Snow / Frost descent", Vector2.right, -.25f, 1.24f, -.2f, .84f, .04f, .65f, -.12f, .48f, .14f, .25f, .25f, -.24f);
    static readonly FlightCurve Crystal = Route("Snow / Crystal turn", Vector2.right, 0, 1.24f, -.40f, .96f, -.62f, .74f, -.45f, .56f, -.06f, .53f, .29f, .65f, .40f, .92f, .36f, 1.24f);
    static readonly FlightCurve AvalancheArc = Route("Snow / Avalanche bow", Vector2.right, -.70f, 1.24f, -.55f, .88f, -.19f, .66f, .33f, .62f, .65f, .87f, .70f, 1.24f);
    public static readonly WavePart[] Snow ={
        new WavePart("Glacier flight","ColdWeatherFighter",GlacierCrown,WaveShape.Train,4,5.4f),
        new WavePart("White curl","ColdWeatherFighter",WhiteCurl,WaveShape.Rank,3,5.2f),
        new WavePart("Petal procession","ColdWeatherFighter",SnowChoreography.Petal,WaveShape.Train,4,5),
        new WavePart("Ice wind","ColdWeatherFighter",IceWind,WaveShape.Rank,3,4.7f),
        new WavePart("Frost descent","ColdWeatherFighter",FrostFall,WaveShape.Train,4,5.3f),
        new WavePart("Crystal diamond","ColdWeatherFighter",Crystal,WaveShape.Diamond,4,5.5f),
        new WavePart("Snow crown","ColdWeatherFighter",GlacierCrown,WaveShape.Chevron,3,5.3f),
        new WavePart("Crosswind rank","ColdWeatherFighter",SnowChoreography.Crosswind,WaveShape.Rank,3,4.9f),
        new WavePart("Avalanche flight","ColdWeatherFighter",AvalancheArc,WaveShape.Rake,3,5.5f,4),
        new WavePart("Crystal procession","ColdWeatherFighter",Crystal,WaveShape.Train,4,5.7f,4),
        new WavePart("Mortar coil","SiegeGunship",SnowChoreography.Spiral,WaveShape.Solo,1,6.2f,6),
        new WavePart("Shielded crown","ShieldTransport",GlacierCrown,WaveShape.Solo,1,5.8f,7),
        new WavePart("Avalanche bow","AvalancheBomber",AvalancheArc,WaveShape.Solo,1,5.9f,5),
        new WavePart("Heavy white curl","AvalancheBomber",WhiteCurl,WaveShape.Solo,1,5.8f,9),
        new WavePart("Siege descent","SiegeGunship",SnowChoreography.Drift,WaveShape.Solo,1,5.8f,8),
        new WavePart("Shielded crystal","ShieldTransport",Crystal,WaveShape.Solo,1,6.1f,10)
    };
}
