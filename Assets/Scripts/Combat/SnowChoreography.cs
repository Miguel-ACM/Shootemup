using UnityEngine;

public static class SnowChoreography
{
    public static readonly FlightCurve Spiral=new FlightCurve("Snow / Unwinding spiral",Vector2.right,0,
        -.55f,1.18f, -.64f,.85f, -.38f,.59f, .24f,.52f, .66f,.70f, .39f,.92f, -.14f,.87f, -.27f,.67f, .10f,.59f, .49f,.78f, .58f,1.18f);
    public static readonly FlightCurve Petal=new FlightCurve("Snow / Falling petal",Vector2.right,0,
        .03f,1.18f, .40f,.98f, .72f,.72f, .55f,.48f, .14f,.41f, -.12f,.58f, -.09f,.82f, -.28f,1.18f);
    public static readonly FlightCurve Crosswind=new FlightCurve("Snow / Crosswind",Vector2.up,0,
        -1.32f,.93f, -.70f,.78f, 0,.89f, .65f,.66f, .30f,.46f, -.50f,.50f, -1.32f,.38f);
    public static readonly FlightCurve Drift=new FlightCurve("Snow / Siege arc",Vector2.right,0,
        .18f,1.18f, .34f,.90f, .45f,.72f, .10f,.62f, -.37f,.69f, -.42f,.91f, -.25f,1.18f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget){Handoff=4.4f};float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // Petals open around a shell-throwing center.
                d.Solo("AvalancheBomber",Drift,0,s,0,5.8f,1.5f);
                d.Train("ColdWeatherFighter",Petal,.15f,s,4+n,.40f,5.0f);
                d.Train("ColdWeatherFighter",Petal,.65f,-s,4+n,.40f,5.0f);break;
            case 1: // One spiral coils around a mobile shield; a crosswind breaks it open.
                d.Solo("ShieldTransport",Drift,0,-s,-.15f,5.8f,1.8f);
                d.Train("ColdWeatherFighter",Spiral,.3f,s,6+n,.38f,5.8f);
                d.Train("ColdWeatherFighter",Crosswind,2.2f,-s,3,.35f,4.6f);break;
            case 2: // A diagonal echelon of siege ships drifts through a petal.
                d.Rank("SiegeGunship",Crosswind,0,s,3,.10f,5.8f,1.7f);
                d.Train("ColdWeatherFighter",Petal,1.15f,-s,6+n,.33f,4.9f);break;
            case 3: // The two halves of a snowflake unfurl on alternating beats.
                d.Train("ColdWeatherFighter",Spiral,0,s,5+n,.38f,5.7f);
                d.Train("ColdWeatherFighter",Spiral,1.05f,-s,5+n,.38f,5.7f);
                d.Solo("AvalancheBomber",Drift,.8f,s,.12f,5.6f,1.55f);break;
            case 4: // Three heavy arcs fan outward while a low wind races past.
                d.Rank("AvalancheBomber",Drift,0,s,3,.30f,5.9f,1.7f);
                d.Train("ColdWeatherFighter",Crosswind,1.1f,-s,7+n,.30f,4.6f,lane:-.10f);break;
            case 5: // Shields change sides; fighters trace the gap between them.
                d.Solo("ShieldTransport",Drift,0,s,-.20f,5.6f,1.8f);
                d.Solo("ShieldTransport",Drift,1.0f,-s,-.20f,5.6f,1.8f);
                d.Train("ColdWeatherFighter",Petal,.55f,s,5+n,.35f,4.8f);
                d.Train("ColdWeatherFighter",Crosswind,2.05f,-s,4,.30f,4.6f);break;
            case 6: // A petal opens on each side of a spiralling gunship.
                d.Solo("SiegeGunship",Spiral,0,s,0,6.2f,1.65f);
                d.Rank("ColdWeatherFighter",Petal,.4f,s,3,.22f,4.9f);
                d.Rank("ColdWeatherFighter",Petal,1.5f,-s,3,.22f,4.9f);
                d.Solo("AvalancheBomber",Drift,1.8f,-s,0,5.5f,1.6f);break;
            default: // Broad alternating winds close the passage.
                d.Train("ColdWeatherFighter",Crosswind,0,s,6+n,.30f,4.7f,lane:.04f,together:true);
                d.Train("ColdWeatherFighter",Crosswind,1.0f,-s,6+n,.30f,4.7f,lane:-.12f,together:true);
                d.Solo("SiegeGunship",Drift,1.0f,s,0,5.7f,1.6f);break;
        }
        return d.Build();
    }
}
