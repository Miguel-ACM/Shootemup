using UnityEngine;

public static class ThunderChoreography
{
    public static readonly FlightCurve Slash=new FlightCurve("Thunder / Lightning slash",Vector2.up,0,
        -1.32f,.95f, -.65f,.86f, .55f,.51f, .74f,.36f, .25f,.42f, -.70f,.74f, -1.32f,.82f);
    public static readonly FlightCurve Sickle=new FlightCurve("Thunder / Hook strike",Vector2.right,0,
        -.25f,1.18f, .10f,.89f, .63f,.64f, .80f,.38f, .45f,.32f, .09f,.51f, .25f,.76f, .70f,1.18f);
    public static readonly FlightCurve Pendulum=new FlightCurve("Thunder / Beam pendulum",Vector2.right,0,
        -.60f,1.18f, -.58f,.85f, .15f,.75f, .62f,.86f, .43f,.95f, -.25f,.82f, -.65f,.71f, -.72f,1.18f);
    public static readonly FlightCurve Relay=new FlightCurve("Thunder / Relay shuttle",Vector2.right,0,
        -.60f,1.18f, -.65f,.95f, -.50f,.84f, -.30f,.77f, -.46f,.67f, -.70f,.75f, -.60f,1.18f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget){Handoff=4.0f};float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // A moving relay arch frames a diagonal lightning train.
                d.Solo("RelayDrone",Relay,0,s,0,5.8f,1.55f,true);
                d.Solo("RelayDrone",Relay,0,-s,0,5.8f,1.55f,true);
                d.Train("StormSkimmer",Slash,.65f,s,6+n,.25f,4.2f);break;
            case 1: // Beam launchers swing through opposite halves; hooks answer below.
                d.Solo("BeamDrone",Pendulum,0,s,-.12f,5.5f,2.1f);
                d.Solo("BeamDrone",Pendulum,.9f,-s,-.12f,5.5f,2.1f);
                d.Train("StormSkimmer",Sickle,.5f,-s,6+n,.25f,4.2f);break;
            case 2: // Two diagonal waves trade the screen on a half-beat.
                d.Train("StormSkimmer",Slash,0,s,6+n,.24f,4.1f,lane:.02f,together:true);
                d.Train("StormSkimmer",Slash,.75f,-s,6+n,.24f,4.1f,lane:-.12f,together:true);
                d.Solo("ArcLancer",Pendulum,1.5f,s,0,5.4f,1.9f);break;
            case 3: // Hooking triplets open a corridor for a beam rank.
                d.Rank("StormSkimmer",Sickle,0,s,3,.20f,4.0f);
                d.Rank("StormSkimmer",Sickle,.8f,-s,3,.20f,4.0f);
                d.Rank("BeamDrone",Pendulum,1.25f,s,3,.22f,5.2f,2.0f);break;
            case 4: // Lancers reverse across the top, followed by a low strike.
                d.Solo("ArcLancer",Pendulum,0,s,.14f,5.3f,1.8f);
                d.Solo("ArcLancer",Pendulum,.8f,-s,.14f,5.3f,1.8f);
                d.Train("StormSkimmer",Slash,1.25f,s,7+n,.24f,4.0f,lane:-.15f);break;
            case 5: // A relay shuttle rises while two hooked trains unfold.
                d.Solo("RelayDrone",Relay,0,1,0,5.7f,1.65f,true);
                d.Solo("RelayDrone",Relay,0,-1,0,5.7f,1.65f,true);
                d.Train("StormSkimmer",Sickle,.5f,s,5+n,.27f,4.0f);
                d.Train("StormSkimmer",Sickle,1.45f,-s,5+n,.27f,4.0f);break;
            case 6: // A beam ladder swings in sequence; a crosscut punctuates it.
                for(int i=0;i<4;i++)d.Solo("BeamDrone",Pendulum,i*.5f,i%2==0 ? s : -s,(i-1.5f)*.08f,5.1f,2.0f);
                d.Train("StormSkimmer",Slash,1.65f,-s,6+n,.24f,4.1f);break;
            default: // Two strike angles overlap behind the advancing lancer.
                d.Solo("ArcLancer",Sickle,0,s,0,5.1f,1.7f);
                d.Train("StormSkimmer",Slash,.25f,s,5+n,.24f,4.1f,lane:-.08f);
                d.Train("StormSkimmer",Sickle,1.35f,-s,6+n,.25f,4.1f);
                d.Solo("BeamDrone",Pendulum,1.8f,-s,0,5.2f,2.0f);break;
        }
        return d.Build();
    }
}
