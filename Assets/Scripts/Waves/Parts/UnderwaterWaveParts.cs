using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve Thermocline = Route("Ocean / Thermocline", Vector2.right, -.64f, 1.32f, -.56f, .93f, -.19f, .70f, .36f, .71f, .65f, .94f, .60f, 1.32f);
    static readonly FlightCurve ReefShelf = Route("Ocean / Reef shelf", Vector2.up, -1.55f, .88f, -.91f, .78f, -.37f, .58f, .24f, .55f, .85f, .70f, 1.55f, .87f);
    static readonly FlightCurve Upwelling = Route("Ocean / Upwelling", Vector2.right, .62f, 1.33f, .68f, .90f, .35f, .56f, -.25f, .55f, -.62f, .88f, -.57f, 1.33f);
    static readonly FlightCurve Undertow = Route("Ocean / Undertow", Vector2.right, -.68f, 1.33f, -.57f, .94f, .10f, .73f, .57f, .45f, .38f, .20f, -.14f, -.30f);
    static readonly FlightCurve CurrentCross = Route("Ocean / Crossing current", Vector2.up, -1.55f, .44f, -.90f, .45f, -.27f, .63f, .32f, .84f, .97f, .78f, 1.55f, .64f);
    static readonly FlightCurve Trench = Route("Ocean / Trench turn", Vector2.right, .42f, 1.33f, .51f, .92f, .13f, .67f, -.48f, .63f, -.64f, .82f, -.36f, 1.33f);
    public static readonly WavePart[] Underwater ={
        new WavePart("Thermocline school","ReefDart",Thermocline,WaveShape.Train,4,4.3f),
        new WavePart("Reef shelf rays","MantaRay",ReefShelf,WaveShape.Train,3,4.7f),
        new WavePart("Brass upwelling","Bathysphere",Upwelling,WaveShape.Train,2,5.2f),
        new WavePart("Undertow darts","ReefDart",Undertow,WaveShape.Chevron,3,4.1f),
        new WavePart("Cross current wings","MantaRay",CurrentCross,WaveShape.Rake,3,4.6f),
        new WavePart("Trench sentries","Bathysphere",Trench,WaveShape.Rank,2,5.1f),
        new WavePart("Rising reef diamond","ReefDart",Upwelling,WaveShape.Diamond,4,4.2f),
        new WavePart("Deep manta bow","MantaRay",Thermocline,WaveShape.Rank,3,4.5f,5),
        new WavePart("Bell descent","Bathysphere",Undertow,WaveShape.Solo,1,5.0f,6),
        new WavePart("Tidal needlework","ReefDart",CurrentCross,WaveShape.Rake,4,4.0f,8),
        new WavePart("Trench ribbon","MantaRay",Trench,WaveShape.Train,3,4.5f,10),
        new WavePart("Sunken procession","Bathysphere",ReefShelf,WaveShape.Train,2,4.8f,12)
    };
}
