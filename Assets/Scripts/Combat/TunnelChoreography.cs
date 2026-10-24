using UnityEngine;

public static class TunnelChoreography
{
    public static readonly FlightCurve Wall=new FlightCurve("Tunnel / Wall bank",Vector2.right,0,
        -.78f,1.18f, -.82f,.92f, -.74f,.68f, -.48f,.54f, -.13f,.58f, .06f,.78f, .02f,1.18f);
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
            case 0: // Mirrored wall batteries close in together; the snake threads between salvos.
                d.OnBeats(1.55f,3.10f,4.35f);
                d.Solo("WallTurret",Wall,0,s,0,5.6f);
                d.Solo("WallTurret",Wall,0,-s,0,5.6f);
                d.OnBeats(2.35f,3.85f);
                d.Train("MaintenanceDrone",Helix,.25f,s,6+n,.30f,4.8f);break;
            case 1: // Two low rebound chains exchange walls and alternate their helix volleys.
                d.OnBeats(2.15f,3.45f);
                d.Train("MaintenanceDrone",Rebound,0,s,5+n,.30f,4.9f,lane:.09f);
                d.OnBeats(2.80f,4.10f);
                d.Train("MaintenanceDrone",Rebound,.55f,-s,5+n,.30f,4.9f,lane:.20f);
                d.OnBeats(3.15f,4.75f);
                d.Solo("WallTurret",Wall,1.0f,s,0,5.4f);break;
            case 2: // A carrier and flanking drones form one moving gate before the shaft opens.
                d.OnBeats(1.65f,3.25f);
                d.Solo("ArmoredCarrier",Gate,0,s,0,5.7f);
                d.OnBeats(2.40f,4.00f);
                d.Solo("MaintenanceDrone",Gate,0,s,-.30f,5.7f);
                d.Solo("MaintenanceDrone",Gate,0,s,.30f,5.7f);
                d.OnBeats(3.65f,4.30f,4.95f);
                d.Train("MaintenanceDrone",Helix,1.45f,-s,6+n,.29f,4.7f);break;
            case 3: // Twin wall columns fold inward in unison, followed by a clean middle descent.
                d.OnBeats(2.00f,2.65f);
                d.Train("MaintenanceDrone",Wall,0,s,5+n,.32f,4.7f,lane:.08f);
                d.Train("MaintenanceDrone",Wall,0,-s,5+n,.32f,4.7f,lane:.08f);
                d.OnBeats(3.65f,4.30f,4.95f);
                d.Wedge("MaintenanceDrone",Helix,1.85f,s,1,.23f,.075f,4.8f);break;
            case 4: // A wide two-turret gate slides together above a rebounding procession.
                d.OnBeats(1.45f,3.05f,4.25f);
                d.Rank("WallTurret",Gate,0,s,2,.55f,5.5f);
                d.OnBeats(2.25f,3.75f,4.85f);
                d.Train("MaintenanceDrone",Rebound,.50f,-s,7+n,.30f,4.9f,lane:.11f);break;
            case 5: // Two helical strands share a clock with vertical clearance at each crossing.
                d.OnBeats(2.05f,3.35f);
                d.Train("MaintenanceDrone",Helix,0,s,5+n,.31f,4.8f,lane:-.06f,depth:.065f);
                d.OnBeats(2.70f,4.00f);
                d.Train("MaintenanceDrone",Helix,0,-s,5+n,.31f,4.8f,lane:-.06f,depth:-.065f);
                d.OnBeats(3.05f,4.70f);
                d.Solo("ArmoredCarrier",Gate,.85f,-s,0,5.8f);break;
            case 6: // A left-right wall battery fires a measured four-note phrase.
                for(int i=0;i<4;i++)
                {
                    d.OnBeats(1.55f+i*.35f,3.20f+i*.35f);
                    d.Solo("WallTurret",Wall,i*.40f,i%2==0 ? s : -s,0,5.3f);
                }
                d.OnBeats(3.00f,4.45f,5.05f);
                d.Train("MaintenanceDrone",Rebound,1.15f,-s,6+n,.30f,4.8f,lane:.08f);break;
            default: // A carrier moves across the opening; two diamonds spiral through its wake.
                d.OnBeats(1.55f,3.15f);
                d.Solo("ArmoredCarrier",Gate,0,s,0,5.7f);
                d.OnBeats(2.35f,3.70f);
                d.Diamond("MaintenanceDrone",Helix,.65f,s,.22f,.075f,4.9f);
                d.OnBeats(3.00f,4.35f,5.00f);
                d.Diamond("MaintenanceDrone",Helix,1.30f,-s,.22f,.075f,4.9f);break;
        }
        return d.Build();
    }
}
