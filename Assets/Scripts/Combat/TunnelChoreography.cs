using UnityEngine;

public static class TunnelChoreography
{
    public static readonly FlightCurve Wall=new FlightCurve("Tunnel / Wall bank",Vector2.right,0,
        -.82f,1.18f, -.88f,.86f, -.88f,.54f, -.65f,.39f, -.40f,.48f, -.48f,.70f, -.56f,1.18f);
    public static readonly FlightCurve Rebound=new FlightCurve("Tunnel / Rebound chain",Vector2.up,0,
        -1.32f,.95f, -.62f,.87f, .72f,.72f, .86f,.58f, .42f,.48f, -.72f,.36f, -.79f,.24f, .20f,.20f, 1.32f,.10f);
    public static readonly FlightCurve Helix=new FlightCurve("Tunnel / Descending helix",Vector2.right,0,
        -.44f,1.18f, -.63f,.92f, 0,.82f, .64f,.69f, .20f,.55f, -.60f,.44f, -.35f,.30f, .45f,.22f, .60f,-.18f);
    public static readonly FlightCurve Gate=new FlightCurve("Tunnel / Sliding gate",Vector2.right,0,
        -.55f,1.18f, -.65f,.89f, -.30f,.73f, .26f,.76f, .53f,.89f, .26f,1.18f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget){Handoff=4.1f};float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // Wall batteries bank inward as a helix threads the shaft.
                d.Solo("WallTurret",Wall,0,s,0,5.6f,1.25f,true);
                d.Solo("WallTurret",Wall,.65f,-s,0,5.6f,1.25f,true);
                d.Train("MaintenanceDrone",Helix,.25f,s,6+n,.29f,4.7f);break;
            case 1: // Two rebound chains trade walls at different heights.
                d.Train("MaintenanceDrone",Rebound,0,s,5+n,.29f,4.8f,lane:.05f);
                d.Train("MaintenanceDrone",Rebound,.9f,-s,5+n,.29f,4.8f,lane:.14f);
                d.Solo("WallTurret",Wall,1.35f,s,0,5.4f,1.3f);break;
            case 2: // Carrier and escorts slide aside, then a chain pours through.
                d.Solo("ArmoredCarrier",Gate,0,s,0,5.7f,1.9f);
                d.Train("MaintenanceDrone",Helix,.75f,-s,7+n,.28f,4.7f);
                d.Solo("WallTurret",Wall,1.1f,-s,0,5.4f,1.3f);break;
            case 3: // Staggered wall banks turn into a braided descent.
                d.Train("MaintenanceDrone",Wall,0,s,5+n,.31f,4.6f,lane:.10f,together:true);
                d.Train("MaintenanceDrone",Wall,.6f,-s,5+n,.31f,4.6f,lane:.10f,together:true);
                d.Train("MaintenanceDrone",Helix,1.9f,s,4,.28f,4.5f);break;
            case 4: // Sliding turrets form a gate while drones ricochet beneath.
                d.Solo("WallTurret",Gate,0,s,-.12f,5.4f,1.25f,true);
                d.Solo("WallTurret",Gate,.7f,-s,-.12f,5.4f,1.25f,true);
                d.Train("MaintenanceDrone",Rebound,.65f,s,7+n,.29f,4.8f,lane:.07f);break;
            case 5: // The double helix descends past a banking carrier.
                d.Train("MaintenanceDrone",Helix,0,s,5+n,.30f,4.6f,lane:-.08f);
                d.Train("MaintenanceDrone",Helix,.75f,-s,5+n,.30f,4.6f,lane:.08f);
                d.Solo("ArmoredCarrier",Gate,1.1f,-s,0,5.8f,1.8f);break;
            case 6: // An alternating wall battery hands off to a fast rebound.
                for(int i=0;i<4;i++)d.Solo("WallTurret",Wall,i*.45f,i%2==0 ? s : -s,0,5.2f,1.3f,true);
                d.Train("MaintenanceDrone",Rebound,1.4f,-s,6+n,.27f,4.5f,lane:.06f);break;
            default: // A carrier leads two curved ranks through the shaft.
                d.Solo("ArmoredCarrier",Gate,0,s,0,5.6f,1.8f);
                d.Rank("MaintenanceDrone",Helix,.6f,s,3,.23f,4.7f);
                d.Rank("MaintenanceDrone",Helix,1.45f,-s,3,.23f,4.7f);
                d.Train("MaintenanceDrone",Wall,1.8f,s,4+n,.29f,4.5f,lane:.1f);break;
        }
        return d.Build();
    }
}
