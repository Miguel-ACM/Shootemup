using UnityEngine;

public static class PlainsChoreography
{
    public static readonly FlightCurve Swoop=new FlightCurve("Plains / Swoop",Vector2.up,0,
        -1.32f,1.10f, -.84f,.80f, -.1f,.57f, .65f,.76f, 1.32f,.94f);
    public static readonly FlightCurve Loop=new FlightCurve("Plains / Figure eight",Vector2.right,0,
        -.68f,1.18f, -.64f,.81f, -.20f,.55f, .47f,.64f, .32f,.88f, -.22f,.93f, -.35f,.72f, .12f,.51f, .65f,.64f, .85f,1.18f);
    public static readonly FlightCurve Fan=new FlightCurve("Plains / Opening fan",Vector2.right,1.1f,
        0,1.18f, -.13f,.88f, -.18f,.65f, 0,.49f, .18f,.65f, .13f,.88f, 0,1.18f);
    public static readonly FlightCurve Stream=new FlightCurve("Plains / River stream",Vector2.right,0,
        -.65f,1.18f, -.55f,.86f, .36f,.72f, .58f,.50f, -.38f,.38f, -.55f,.18f, -.3f,-.18f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget);float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // Two ribbons answer each other across the valley.
                d.Train("ScoutInterceptor",Swoop,0,s,5+n,seconds:4.4f);
                d.Train("ScoutInterceptor",Swoop,1.3f,-s,5+n,seconds:4.4f,lane:-.12f);break;
            case 1: // A broad fan opens, then a bomber cuts underneath it.
                d.Rank("ScoutInterceptor",Fan,0,s,5,.20f,4.8f);
                d.Solo("HeavyBomber",Swoop,1.6f,-s,-.10f,5.3f,1.5f);break;
            case 2: // A looping train is answered by a faster cross-screen pass.
                d.Train("ScoutInterceptor",Loop,0,s,6+n,.4f,5.6f);
                d.Train("ScoutInterceptor",Swoop,1.9f,-s,4,.4f,4.2f,lane:-.16f);break;
            case 3: // Bombers weave above a descending escort stream.
                d.Solo("HeavyBomber",Swoop,0,s,.03f,5.4f,1.5f);
                d.Solo("HeavyBomber",Swoop,.8f,-s,-.10f,5.4f,1.5f);
                d.Train("ScoutInterceptor",Stream,1.25f,s,5+n,.38f,4.9f);break;
            case 4: // Two fans peel away from a mobile gunship.
                d.Rank("ScoutInterceptor",Fan,0,s,4,.24f,4.6f);
                d.Rank("ScoutInterceptor",Fan,1.5f,-s,3,.27f,4.6f);
                d.Solo("PatrolGunship",Loop,.6f,s,.05f,6.2f,1.35f);break;
            case 5: // Offset figure eights interlace without merging their leaders.
                d.Train("ScoutInterceptor",Loop,0,s,5+n,.42f,5.5f,lane:-.10f);
                d.Train("ScoutInterceptor",Loop,.9f,-s,5+n,.42f,5.5f,lane:.10f);break;
            case 6: // Three gunships bank as one; fighters ripple through their wake.
                d.Rank("PatrolGunship",Swoop,0,s,3,.11f,5.5f,1.35f);
                d.Train("ScoutInterceptor",Stream,1.5f,-s,6+n,.38f,4.7f);break;
            default: // A low stream hands the screen back to a climbing fan.
                d.Train("ScoutInterceptor",Stream,0,s,6+n,.38f,4.9f);
                d.Rank("ScoutInterceptor",Fan,2,s,5,.20f,4.5f);
                d.Solo("HeavyBomber",Swoop,1.25f,-s,.06f,5.2f,1.5f);break;
        }
        return d.Build();
    }
}
