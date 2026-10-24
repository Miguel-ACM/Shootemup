using UnityEngine;
public static partial class WavePartLibrary
{
    static readonly FlightCurve ShaftScrew=Route("Tunnel / Shaft screw",Vector2.right,-.48f,1.24f,-.66f,.96f,-.29f,.78f,.45f,.61f,.53f,.38f,-.2f,.18f,-.38f,-.24f);
    static readonly FlightCurve ServiceLoop=Route("Tunnel / Service loop",Vector2.right,-.66f,1.24f,-.76f,.91f,-.68f,.64f,-.34f,.49f,-.08f,.67f,-.12f,.98f,-.14f,1.24f);
    static readonly FlightCurve LowRail=Route("Tunnel / Low rail",Vector2.up,-1.36f,.52f,-.76f,.58f,-.08f,.54f,.60f,.39f,1.36f,.32f);
    static readonly FlightCurve PipeBank=Route("Tunnel / Pipe bank",Vector2.right,.66f,1.24f,.77f,.91f,.65f,.62f,.35f,.45f,.05f,.62f,-.05f,.95f,-.05f,1.24f);
    static readonly FlightCurve UpperRail=Route("Tunnel / Upper rail",Vector2.up,1.36f,.96f,.76f,.86f,.09f,.8f,-.6f,.88f,-1.36f,.98f);
    static readonly FlightCurve ServiceGate=Route("Tunnel / Service gate",Vector2.right,0,1.24f,-.15f,.94f,-.18f,.7f,.05f,.6f,.23f,.82f,.22f,1.24f);
    public static readonly WavePart[] Tunnel={
        new WavePart("Shaft procession","MaintenanceDrone",ShaftScrew,WaveShape.Train,4,5.1f),
        new WavePart("Service crown","MaintenanceDrone",ServiceLoop,WaveShape.Chevron,3,5.1f),
        new WavePart("Low rail","MaintenanceDrone",LowRail,WaveShape.Rank,3,4.5f),
        new WavePart("Pipe diamond","MaintenanceDrone",PipeBank,WaveShape.Diamond,4,5.2f),
        new WavePart("Upper procession","MaintenanceDrone",UpperRail,WaveShape.Train,4,4.5f),
        new WavePart("Service echelon","MaintenanceDrone",ServiceGate,WaveShape.Rake,3,5.1f),
        new WavePart("Descending snake","MaintenanceDrone",TunnelChoreography.Helix,WaveShape.Train,4,4.8f),
        new WavePart("Wall rank","MaintenanceDrone",TunnelChoreography.Wall,WaveShape.Rank,3,4.9f),
        new WavePart("Rebound flight","MaintenanceDrone",TunnelChoreography.Rebound,WaveShape.Train,4,4.9f,4),
        new WavePart("Sliding diamond","MaintenanceDrone",TunnelChoreography.Gate,WaveShape.Diamond,4,5.0f,4),
        new WavePart("Wall battery","WallTurret",TunnelChoreography.Wall,WaveShape.Solo,1,5.6f,5),
        new WavePart("Pipe battery","WallTurret",PipeBank,WaveShape.Rank,2,5.6f,6),
        new WavePart("Gate battery","WallTurret",ServiceGate,WaveShape.Rank,2,5.5f,7),
        new WavePart("Upper battery","WallTurret",UpperRail,WaveShape.Rank,2,5.4f,8),
        new WavePart("Carrier passage","ArmoredCarrier",ServiceGate,WaveShape.Solo,1,5.8f,9),
        new WavePart("Carrier bank","ArmoredCarrier",PipeBank,WaveShape.Solo,1,5.8f,10)
    };
}
