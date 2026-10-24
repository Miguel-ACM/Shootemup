using UnityEngine;
public static partial class WavePartLibrary
{
    static FlightCurve Route(string name,Vector2 axis,params float[] xy)=>new FlightCurve(name,axis,0,xy);
    static readonly FlightCurve Orchard=Route("Plains / Orchard bank",Vector2.right,-.62f,1.24f,-.58f,.88f,-.2f,.65f,.35f,.68f,.65f,.89f,.68f,1.24f);
    static readonly FlightCurve Meadow=Route("Plains / Meadow hook",Vector2.right,-.65f,1.24f,-.60f,.85f,-.32f,.5f,.12f,.39f,.51f,.55f,.62f,.86f,.64f,1.24f);
    static readonly FlightCurve Ridge=Route("Plains / Ridge crossing",Vector2.up,-1.36f,.79f,-.72f,.87f,-.15f,.76f,.42f,.54f,.93f,.47f,1.36f,.6f);
    static readonly FlightCurve Crescent=Route("Plains / Meadow crescent",Vector2.right,-.66f,1.24f,-.77f,.83f,-.66f,.5f,-.20f,.36f,.34f,.45f,.82f,.66f,1.36f,.85f);
    static readonly FlightCurve RiverBraid=Route("Plains / River braid",Vector2.right,-.34f,1.24f,.12f,.87f,.43f,.64f,.13f,.42f,-.39f,.26f,-.38f,-.24f);
    static readonly FlightCurve OrchardTurn=Route("Plains / Orchard return",Vector2.right,.60f,1.24f,.58f,.89f,.40f,.71f,.1f,.64f,-.10f,.77f,-.12f,1.24f);
    public static readonly WavePart[] Plains={
        new WavePart("Orchard flight","ScoutInterceptor",Orchard,WaveShape.Train,4,5.1f),
        new WavePart("River arrow","ScoutInterceptor",PlainsChoreography.Fan,WaveShape.Chevron,3,4.8f),
        new WavePart("River procession","ScoutInterceptor",PlainsChoreography.Stream,WaveShape.Train,4,5.2f),
        new WavePart("Ridge staff","ScoutInterceptor",Ridge,WaveShape.Rank,3,4.8f),
        new WavePart("River braid","ScoutInterceptor",RiverBraid,WaveShape.Train,4,5.1f),
        new WavePart("Meadow diamond","ScoutInterceptor",Meadow,WaveShape.Diamond,4,5.4f),
        new WavePart("Figure eight","ScoutInterceptor",PlainsChoreography.Loop,WaveShape.Rank,3,5.6f),
        new WavePart("Orchard echelon","ScoutInterceptor",OrchardTurn,WaveShape.Rake,3,5.1f),
        new WavePart("Banking rank","ScoutInterceptor",PlainsChoreography.Swoop,WaveShape.Rank,3,4.6f,4),
        new WavePart("Opening crown","ScoutInterceptor",PlainsChoreography.Fan,WaveShape.Diamond,4,4.8f,4),
        new WavePart("Patrol return","PatrolGunship",OrchardTurn,WaveShape.Solo,1,5.7f,5),
        new WavePart("Gunship echelon","PatrolGunship",Meadow,WaveShape.Rank,2,5.6f,6),
        new WavePart("Bomber crescent","HeavyBomber",Crescent,WaveShape.Solo,1,5.4f,7),
        new WavePart("Ridge patrol","PatrolGunship",Ridge,WaveShape.Train,2,5.4f,6),
        new WavePart("Bomber bank","HeavyBomber",Orchard,WaveShape.Solo,1,5.4f,9),
        new WavePart("Orchard guard","PatrolGunship",Orchard,WaveShape.Rank,2,5.5f,8)
    };
}
