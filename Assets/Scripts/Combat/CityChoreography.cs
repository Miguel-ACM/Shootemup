using UnityEngine;

public static class CityChoreography
{
    public static readonly FlightCurve Slingshot=new FlightCurve("City / Slingshot",Vector2.right,0,
        -.70f,1.18f, -.44f,.88f, -.28f,.55f, .08f,.28f, .58f,.35f, .73f,.64f, .65f,1.18f);
    public static readonly FlightCurve Overtake=new FlightCurve("City / Overtaking lanes",Vector2.right,0,
        .45f,1.18f, .42f,.92f, .15f,.67f, -.28f,.46f, -.30f,.16f, -.18f,-.18f);
    public static readonly FlightCurve Interchange=new FlightCurve("City / Interchange",Vector2.up,0,
        -1.32f,.82f, -.65f,.82f, -.28f,.68f, .05f,.59f, .52f,.69f, .58f,.88f, .23f,.95f, -.20f,.77f, .26f,.52f, 1.32f,.42f);
    public static readonly FlightCurve Flyby=new FlightCurve("City / Express flyby",Vector2.up,0,
        -1.32f,.65f, -.75f,.71f, .15f,.72f, .72f,.62f, 1.32f,.48f);
    public static Wave Create(int budget)
    {
        var d=new WaveDance(budget){Handoff=3.25f};float s=d.Side;int n=d.Extra;
        switch(d.Phrase)
        {
            case 0: // Rush-hour lanes overtake each other at different speeds.
                d.Train("AttackDrone",Overtake,0,s,6+n,.23f,4.0f,lane:-.16f);
                d.Train("CourierDiver",Overtake,.6f,s,7+n,.17f,3.05f,lane:.16f);
                d.Train("CourierDiver",Flyby,1.65f,-s,4,.18f,2.8f);break;
            case 1: // Interchange traffic is cut by a low express stream.
                d.Train("AttackDrone",Interchange,0,s,6+n,.22f,4.2f);
                d.Train("CourierDiver",Flyby,.9f,-s,8+n,.17f,2.8f,lane:-.12f);
                d.Solo("PursuitFighter",Slingshot,.35f,-s,0,4.3f,1.5f);break;
            case 2: // Mirrored slingshots peel up as fresh couriers dive through.
                d.Train("CourierDiver",Slingshot,0,s,6+n,.19f,3.45f);
                d.Train("CourierDiver",Slingshot,.65f,-s,6+n,.19f,3.45f);
                d.Train("AttackDrone",Overtake,1.45f,s,4,.22f,3.8f);break;
            case 3: // A command ship circulates; two express lanes exchange sides.
                d.Solo("CommandGunship",Interchange,0,s,.08f,5.0f,1.5f);
                d.Train("CourierDiver",Flyby,.1f,s,6+n,.17f,2.7f,lane:-.12f);
                d.Train("CourierDiver",Flyby,1.15f,-s,6+n,.17f,2.7f,lane:.10f);break;
            case 4: // Parallel ranks fan through a fast vertical interchange.
                d.Rank("AttackDrone",Overtake,0,s,4,.25f,3.8f);
                d.Rank("CourierDiver",Overtake,.8f,-s,4,.25f,3.0f);
                d.Train("CourierDiver",Slingshot,1.15f,s,7+n,.18f,3.4f);break;
            case 5: // Pursuit leaders pull two interleaving packs around the blocks.
                d.Solo("PursuitFighter",Slingshot,0,s,0,4.4f,1.5f);
                d.Solo("PursuitFighter",Slingshot,.55f,-s,0,4.4f,1.5f);
                d.Train("AttackDrone",Interchange,.45f,s,5+n,.23f,4.1f,lane:.06f);
                d.Train("CourierDiver",Interchange,1.45f,-s,5+n,.18f,3.6f,lane:-.10f);break;
            case 6: // Three cross-streets fire in ripples, with deliberate vertical gaps.
                d.Train("CourierDiver",Flyby,0,s,6+n,.17f,2.8f,lane:.13f);
                d.Train("AttackDrone",Flyby,.85f,-s,5+n,.22f,3.3f,lane:-.02f);
                d.Train("CourierDiver",Flyby,1.65f,s,6+n,.17f,2.8f,lane:-.18f);break;
            default: // A command loop is overtaken by a cascade of paired divers.
                d.Solo("CommandGunship",Interchange,0,-s,.08f,5.0f,1.45f);
                d.Train("AttackDrone",Slingshot,.25f,s,5+n,.23f,3.9f);
                d.Train("CourierDiver",Overtake,1.0f,-s,6+n,.18f,3.0f,lane:.16f);
                d.Train("CourierDiver",Overtake,1.3f,s,6+n,.18f,3.0f,lane:-.16f);break;
        }
        return d.Build();
    }
}
