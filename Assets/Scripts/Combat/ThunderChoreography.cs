using UnityEngine;

public static class ThunderChoreography
{
    public static readonly FlightCurve Slash=new FlightCurve("Thunder / Lightning slash",Vector2.up,0,
        -1.32f,.98f, -.72f,.88f, -.10f,.69f, .54f,.43f, .86f,.38f, 1.32f,.52f);
    public static readonly FlightCurve Sickle=new FlightCurve("Thunder / Hook strike",Vector2.right,0,
        -.1875f,1.18f, .075f,.89f, .4725f,.64f, .60f,.38f, .3375f,.32f, .0675f,.51f, .1875f,.76f, .525f,1.18f);
    public static readonly FlightCurve Pendulum=new FlightCurve("Thunder / Beam pendulum",Vector2.right,0,
        -.60f,1.18f, -.58f,.85f, .15f,.75f, .62f,.86f, .43f,.95f, -.25f,.82f, -.65f,.71f, -.72f,1.18f);
    public static readonly FlightCurve Relay=new FlightCurve("Thunder / Capacitor crescent",Vector2.right,0,
        -.48f,1.18f, -.59f,.88f, -.60f,.64f, -.38f,.46f, -.08f,.50f, .10f,.74f, .06f,1.18f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget){Handoff=4.0f};float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // Capacitor wings descend together, then a diagonal squad cuts across.
                d.OnBeats(1.55f,2.15f,3.65f);
                d.Solo("RelayDrone",Relay,0,s,-.18f,5.2f,depth:.07f);
                d.Solo("RelayDrone",Relay,0,-s,-.18f,5.2f,depth:-.07f);
                d.OnBeats(2.85f,3.35f,4.45f,5.05f);
                d.Train("StormSkimmer",Slash,.55f,s,6+n,.38f,4.0f);break;
            case 1: // A two-beam sweep is the focal strike; the hooks fire after it releases.
                d.OnBeats(1.45f);
                d.Rank("BeamDrone",Pendulum,0,s,2,.48f,5.4f);
                d.OnBeats(3.15f,3.70f,4.35f);
                d.Train("StormSkimmer",Sickle,.45f,-s,6+n,.38f,4.4f);break;
            case 2: // Two electric diagonals cross as a call and response, without beam clutter.
                d.OnBeats(2.00f,2.65f,3.25f);
                d.Train("StormSkimmer",Slash,0,s,5+n,.38f,4.0f,lane:.08f);
                d.OnBeats(3.30f,3.95f);
                d.Train("StormSkimmer",Slash,1.1f,-s,5+n,.38f,4.0f,lane:-.12f);
                d.OnBeats(2.35f,3.65f,4.65f);
                d.Solo("RelayDrone",Relay,.45f,s,0,5.0f);break;
            case 3: // Hooking diamonds open the sides; a central lance takes the following beat.
                d.OnBeats(1.55f,2.20f);
                d.Diamond("StormSkimmer",Sickle,0,s,.28f,.10f,4.3f);
                d.OnBeats(3.30f,3.95f);
                d.Diamond("StormSkimmer",Sickle,1.5f,-s,.28f,.10f,4.3f);
                d.OnBeats(1.45f);
                d.Solo("ArcLancer",Pendulum,.50f,s,0,5.3f);break;
            case 4: // One sweeping lance conducts two low, level flights.
                d.OnBeats(1.45f,3.15f);
                d.Solo("ArcLancer",Pendulum,0,s,0,5.5f);
                d.OnBeats(3.20f,3.65f);
                d.Rank("StormSkimmer",Slash,.65f,s,3,.10f,4.0f);
                d.OnBeats(4.95f,5.55f);
                d.Rank("StormSkimmer",Slash,2.35f,-s,3,.10f,4.0f);break;
            case 5: // A capacitor crescent folds around a broad hook, trading short bursts.
                d.OnBeats(1.45f,2.05f,3.55f,4.15f);
                d.Rank("RelayDrone",Relay,0,s,3,.29f,5.3f);
                d.OnBeats(2.75f,3.25f,4.85f);
                d.Train("StormSkimmer",Sickle,.55f,-s,7+n,.38f,4.5f);break;
            case 6: // Beam partners trade sides on separate beats; one clean slash passes below.
                d.OnBeats(1.40f);
                d.Solo("BeamDrone",Pendulum,0,s,-.16f,5.3f);
                d.OnBeats(2.65f);
                d.Solo("BeamDrone",Pendulum,1.25f,-s,-.16f,5.3f);
                d.OnBeats(3.70f,4.30f,4.90f,5.45f);
                d.Train("StormSkimmer",Slash,1.15f,-s,6+n,.38f,4.1f);break;
            default: // A lance opens the passage; two capacitor pairs close it in unison.
                d.OnBeats(1.50f);
                d.Solo("ArcLancer",Sickle,0,s,0,5.2f);
                d.OnBeats(2.70f,3.30f,4.45f);
                foreach(float wing in new[]{-.19f,.19f})
                {
                    d.Solo("RelayDrone",Relay,.55f,s,wing,5.1f,depth:.08f);
                    d.Solo("RelayDrone",Relay,.55f,-s,wing,5.1f,depth:-.08f);
                }
                d.OnBeats(3.95f,4.60f);
                d.Rank("StormSkimmer",Slash,1.65f,s,3,.11f,4.0f);break;
        }
        return d.Build();
    }
}
