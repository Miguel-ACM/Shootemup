using UnityEngine;

public static class PlainsChoreography
{
    public static readonly FlightCurve Swoop=new FlightCurve("Plains / Swoop",Vector2.up,0,
        -1.32f,1.10f, -.84f,.80f, -.1f,.57f, .65f,.76f, 1.32f,.94f);
    public static readonly FlightCurve Loop=new FlightCurve("Plains / Figure eight",Vector2.right,0,
        -.68f,1.18f, -.64f,.81f, -.20f,.55f, .47f,.64f, .32f,.88f, -.22f,.93f, -.35f,.72f, .12f,.51f, .65f,.64f, .85f,1.18f);
    public static readonly FlightCurve Fan=new FlightCurve("Plains / Opening fan",Vector2.right,1.1f,
        0,1.18f, -.40f,1.0f, -.52f,.74f, -.32f,.53f, 0,.48f, .32f,.53f, .52f,.74f, .40f,1.0f, 0,1.18f);
    public static readonly FlightCurve Stream=new FlightCurve("Plains / River stream",Vector2.right,0,
        -.65f,1.18f, -.55f,.86f, .36f,.72f, .58f,.50f, -.38f,.38f, -.55f,.18f, -.3f,-.18f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget){Handoff=4.2f};float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // Two complete ribbons cross on separate levels, then answer.
                d.OnBeats(2.15f,2.80f,3.45f);
                d.Train("ScoutInterceptor",Swoop,0,s,5+n,.38f,4.6f,lane:.045f);
                d.OnBeats(2.50f,3.15f,3.80f);
                d.Train("ScoutInterceptor",Swoop,.40f,-s,5+n,.38f,4.6f,lane:-.11f);break;
            case 1: // A five-ship arrow opens into a fan; the bomber answers its guns.
                d.OnBeats(1.45f,2.05f,3.45f);
                d.Wedge("ScoutInterceptor",Fan,0,s,2,.205f,.065f,4.8f);
                d.OnBeats(2.75f,4.15f);
                d.Solo("HeavyBomber",Swoop,.55f,-s,-.12f,5.4f);
                d.OnBeats(3.45f,4.05f);
                d.Rank("ScoutInterceptor",Swoop,1.55f,-s,3,.085f,4.5f);break;
            case 2: // Two diamonds rotate through the eight, separated by half a turn.
                d.OnBeats(1.65f,2.35f,3.05f);
                d.Diamond("ScoutInterceptor",Loop,0,s,.23f,.075f,5.6f);
                d.OnBeats(3.45f,4.15f,4.85f);
                d.Diamond("ScoutInterceptor",Loop,1.75f,s,.23f,.075f,5.6f);break;
            case 3: // A bomber pair banks together; a narrow escort stream pulls below.
                d.OnBeats(1.80f,3.20f);
                d.Rank("HeavyBomber",Swoop,0,s,2,.16f,5.4f);
                d.OnBeats(2.55f,3.90f,4.50f);
                d.Train("ScoutInterceptor",Stream,.65f,-s,6+n,.38f,4.9f,lane:.08f);break;
            case 4: // One fan opens and the next folds through its empty center.
                d.OnBeats(1.45f,2.05f);
                d.Rank("ScoutInterceptor",Fan,0,s,4,.26f,4.6f);
                d.OnBeats(3.05f,3.70f,4.35f);
                d.Wedge("ScoutInterceptor",Fan,1.55f,-s,2,.19f,.06f,4.7f);
                d.OnBeats(2.55f,4.90f);
                d.Solo("PatrolGunship",Loop,.45f,s,.04f,6.1f);break;
            case 5: // A single figure-eight procession fires two clean ripples.
                d.OnBeats(2.65f,3.35f,4.05f);
                d.Train("ScoutInterceptor",Loop,0,s,7+n,.38f,5.5f,lane:-.08f);
                d.OnBeats(4.65f,5.25f);
                d.Rank("ScoutInterceptor",Swoop,2.15f,-s,3,.095f,4.4f);break;
            case 6: // Three gunships draw a broad staff; the escort arrow dives through it.
                d.OnBeats(1.75f,3.15f,4.20f);
                d.Rank("PatrolGunship",Swoop,0,s,3,.125f,5.5f);
                d.OnBeats(2.45f,3.80f,4.45f);
                d.Wedge("ScoutInterceptor",Stream,.75f,-s,2,.22f,.07f,4.9f);break;
            default: // The descending stream releases into a wide upward fan.
                d.OnBeats(2.35f,3.00f);
                d.Train("ScoutInterceptor",Stream,0,s,6+n,.38f,4.9f);
                d.OnBeats(3.65f,4.30f,4.95f);
                d.Wedge("ScoutInterceptor",Fan,1.9f,-s,2,.20f,.06f,4.7f);
                d.OnBeats(2.75f,4.10f);
                d.Solo("HeavyBomber",Swoop,.65f,-s,.075f,5.3f);break;
        }
        return d.Build();
    }
}
