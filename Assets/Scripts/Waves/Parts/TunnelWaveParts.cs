using UnityEngine;
public static partial class WavePartLibrary
{
    static readonly FlightCurve ShaftScrew = Route("Tunnel / Shaft screw", Vector2.right, -.48f, 1.24f, -.66f, .96f, -.29f, .78f, .45f, .61f, .53f, .38f, -.2f, .18f, -.38f, -.24f);
    static readonly FlightCurve ServiceLoop = Route("Tunnel / Service loop", Vector2.right, -.66f, 1.24f, -.76f, .93f, -.68f, .71f, -.44f, .57f, -.11f, .56f, .14f, .73f, .18f, 1.24f);
    static readonly FlightCurve LowRail = Route("Tunnel / Low rail", Vector2.up, -1.36f, .52f, -.76f, .58f, -.08f, .54f, .60f, .39f, 1.36f, .32f);
    static readonly FlightCurve PipeBank = Route("Tunnel / Pipe bank", Vector2.right, .66f, 1.24f, .77f, .94f, .71f, .70f, .55f, .56f, .15f, .52f, -.20f, .66f, -.25f, .95f, -.25f, 1.24f);
    static readonly FlightCurve UpperRail = Route("Tunnel / Upper rail", Vector2.up, 1.36f, .96f, .76f, .86f, .09f, .8f, -.6f, .88f, -1.36f, .98f);
    static readonly FlightCurve ServiceGate = Route("Tunnel / Service gate", Vector2.right, 0, 1.24f, -.35f, 1.0f, -.55f, .78f, -.36f, .64f, .02f, .60f, .43f, .74f, .58f, .98f, .60f, 1.24f);
    static readonly FlightCurve Conduit = Route("Tunnel / Conduit hop", Vector2.right, .64f, 1.24f, .70f, .86f, .40f, .64f, -.10f, .70f, -.45f, .52f, -.60f, -.30f);
    static readonly FlightCurve Crawlway = Route("Tunnel / Crawlway", Vector2.up, 1.36f, .80f, .60f, .72f, -.10f, .82f, -.70f, .70f, -1.36f, .78f);
    static readonly FlightCurve Sump = Route("Tunnel / Sump climb", Vector2.right, .70f, -.40f, .72f, .14f, .46f, .40f, .58f, .74f, .40f, 1.40f);
    // Fewer drones and more of the other tunnel ships (Miguel, 15 November 2026): drone files
    // fly three ships, a few drone routes now carry spinners or mantises, and carriers arrive earlier.
    public static readonly WavePart[] Tunnel ={
        new WavePart("Maintenance elevator", "MaintenanceDrone", Route("Tunnel / Elevator return", Vector2.right, -.73f,-.4f, -.73f,1.4f), WaveShape.Train, 3, 5.0f, 2, new[]{.45f,.65f}, WaveMotion.Rising),
        new WavePart("Switchbox battery", "WallTurret", FlightCurve.Stations("Tunnel / Switchbox", Vector2.right, new[]{0f,.22f,.66f,1f}, -.65f,1.4f, -.65f,.68f, -.65f,.68f, -1.7f,.55f), WaveShape.Solo, 1, 5.8f, 4, new[]{.31f,.47f,.61f}, WaveMotion.Station),
        new WavePart("Rivet inspection stops", "RivetMantis", FlightCurve.Stations("Tunnel / Inspection rail", Vector2.right, new[]{0f,.19f,.34f,.51f,.70f,1f}, .57f,1.4f, .57f,.83f, .57f,.83f, .57f,.49f, .57f,.49f, .57f,-.4f), WaveShape.Solo, 1, 6.1f, 6, new[]{.29f,.60f}, WaveMotion.Relay),
        new WavePart("Shaft procession","MaintenanceDrone",ShaftScrew,WaveShape.Train,3,5.1f),
        new WavePart("Service crown","MaintenanceDrone",ServiceLoop,WaveShape.Chevron,3,5.1f),
        new WavePart("Low rail","MaintenanceDrone",LowRail,WaveShape.Rank,3,4.5f),
        new WavePart("Pipe diamond","MaintenanceDrone",PipeBank,WaveShape.Diamond,4,5.2f),
        new WavePart("Upper baffle procession","BaffleSpinner",UpperRail,WaveShape.Train,3,4.7f),
        new WavePart("Service echelon","MaintenanceDrone",ServiceGate,WaveShape.Rake,3,5.1f),
        new WavePart("Descending snake","MaintenanceDrone",TunnelChoreography.Helix,WaveShape.Train,3,4.8f),
        new WavePart("Wall rank","MaintenanceDrone",TunnelChoreography.Wall,WaveShape.Rank,3,4.9f),
        new WavePart("Rebound flight","MaintenanceDrone",TunnelChoreography.Rebound,WaveShape.Train,3,4.9f,4),
        new WavePart("Sliding chevron","MaintenanceDrone",TunnelChoreography.Gate,WaveShape.Chevron,3,5.0f,4),
        new WavePart("Wall battery","WallTurret",TunnelChoreography.Wall,WaveShape.Solo,1,5.6f,5),
        new WavePart("Pipe battery","WallTurret",PipeBank,WaveShape.Rank,2,5.6f,6),
        new WavePart("Gate battery","WallTurret",ServiceGate,WaveShape.Rank,2,5.5f,7),
        new WavePart("Upper battery","WallTurret",UpperRail,WaveShape.Rank,2,5.4f,8),
        new WavePart("Carrier passage","ArmoredCarrier",ServiceGate,WaveShape.Solo,1,5.8f,5),
        new WavePart("Carrier bank","ArmoredCarrier",PipeBank,WaveShape.Solo,1,5.8f,7),
        new WavePart("Carrier screw","ArmoredCarrier",ShaftScrew,WaveShape.Solo,1,5.9f,3),
        new WavePart("Conduit mantis hop", "RivetMantis", Conduit, WaveShape.Train, 2, 5.2f, 3),
        new WavePart("Crawlway baffles", "BaffleSpinner", Crawlway, WaveShape.Rank, 2, 4.9f, 4),
        new WavePart("Sump climb", "MaintenanceDrone", Sump, WaveShape.Train, 3, 5.0f, 5, new[] { .45f, .65f }, WaveMotion.Rising),
        new WavePart("Conduit battery", "WallTurret", Conduit, WaveShape.Rank, 2, 5.5f, 9)
    };
}
