using UnityEngine;

public static class CityChoreography
{
    public static readonly FlightCurve Slingshot = new FlightCurve("City / Slingshot", Vector2.right, 0,
        -.70f, 1.18f, -.44f, .88f, -.28f, .55f, .08f, .28f, .58f, .35f, .73f, .64f, .65f, 1.18f);
    public static readonly FlightCurve Overtake = new FlightCurve("City / Overtaking lanes", Vector2.right, 0,
        .45f, 1.18f, .42f, .92f, .15f, .67f, -.28f, .46f, -.30f, .16f, -.18f, -.18f);
    public static readonly FlightCurve Interchange = new FlightCurve("City / Interchange", Vector2.up, 0,
        -1.32f, .82f, -.65f, .82f, -.28f, .68f, .05f, .59f, .52f, .69f, .58f, .88f, .23f, .95f, -.20f, .77f, .26f, .52f, 1.32f, .42f);
    public static readonly FlightCurve Flyby = new FlightCurve("City / Express flyby", Vector2.up, 0,
        -1.32f, .65f, -.75f, .71f, .15f, .72f, .72f, .62f, 1.32f, .48f);
    public static Wave Create(int budget)
    {
        var d = new WaveDance(budget) { Handoff = 3.25f };
        float s = d.Side;
        int n = d.Extra;
        switch (d.Phrase)
        {
            case 0: // A fast courier lane overtakes the drone lane on a two-note downbeat.
                d.OnBeats(1.70f, 2.85f);
                d.Train("AttackDrone", Overtake, 0, s, 6 + n, .23f, 4.0f, lane: -.20f);
                d.OnBeats(2.15f, 2.55f);
                d.Train("CourierDiver", Overtake, .50f, s, 7 + n, .17f, 3.05f, lane: .20f);
                d.OnBeats(3.50f, 3.90f);
                d.Rank("CourierDiver", Flyby, 1.65f, -s, 3, .08f, 2.85f);
                break;
            case 1: // The interchange winds up, then one express stream cuts its lower lane.
                d.OnBeats(2.10f, 3.20f);
                d.Train("AttackDrone", Interchange, 0, s, 6 + n, .23f, 4.3f, lane: .04f);
                d.OnBeats(2.65f, 3.05f, 3.65f);
                d.Train("CourierDiver", Flyby, .85f, -s, 7 + n, .18f, 2.9f, lane: -.15f);
                d.OnBeats(1.30f);
                d.Solo("PursuitFighter", Slingshot, .05f, -s, 0, 4.3f);
                break;
            case 2: // Two symmetrical slingshot packs rise together around a diving arrow.
                d.OnBeats(1.80f, 2.30f);
                d.Train("CourierDiver", Slingshot, 0, s, 6 + n, .19f, 3.55f, depth: .045f);
                d.Train("CourierDiver", Slingshot, 0, -s, 6 + n, .19f, 3.55f, depth: -.045f);
                d.OnBeats(3.05f, 3.65f, 4.25f);
                d.Wedge("AttackDrone", Overtake, 1.25f, s, 2, .20f, .055f, 3.9f);
                break;
            case 3: // A command ship conducts alternating express lanes, each firing as one.
                d.OnBeats(1.45f, 3.15f);
                d.Solo("CommandGunship", Interchange, 0, s, .08f, 5.0f);
                d.OnBeats(1.95f, 2.35f);
                d.Train("CourierDiver", Flyby, 0, s, 6 + n, .18f, 2.85f, lane: -.13f);
                d.OnBeats(3.05f, 3.45f);
                d.Train("CourierDiver", Flyby, 1.10f, -s, 6 + n, .18f, 2.85f, lane: .11f);
                break;
            case 4: // Two staggered arrows dive through, followed by a sharp upward peel.
                d.OnBeats(1.40f, 2.45f);
                d.Wedge("AttackDrone", Overtake, 0, s, 2, .21f, .055f, 3.9f);
                d.OnBeats(2.05f, 2.50f);
                d.Wedge("CourierDiver", Overtake, .70f, -s, 2, .21f, .055f, 3.15f);
                d.OnBeats(3.10f, 3.55f, 4.00f);
                d.Train("CourierDiver", Slingshot, 1.20f, s, 6 + n, .19f, 3.55f);
                break;
            case 5: // Pursuit leaders turn in synchrony; their two packs answer after the lasers.
                d.OnBeats(1.30f);
                d.Solo("PursuitFighter", Slingshot, 0, s, -.10f, 4.5f);
                d.Solo("PursuitFighter", Slingshot, 0, -s, -.10f, 4.5f);
                d.OnBeats(2.75f, 3.85f);
                d.Train("AttackDrone", Interchange, .45f, s, 5 + n, .23f, 4.2f, lane: .09f);
                d.OnBeats(3.25f, 3.70f, 4.20f);
                d.Train("CourierDiver", Interchange, 1.1f, -s, 6 + n, .19f, 3.8f, lane: -.13f);
                break;
            case 6: // Three cross-streets trade tightly spaced volleys on a regular traffic rhythm.
                d.OnBeats(1.80f, 2.20f);
                d.Train("CourierDiver", Flyby, 0, s, 6 + n, .18f, 2.9f, lane: .16f);
                d.OnBeats(2.65f, 3.55f);
                d.Train("AttackDrone", Flyby, .85f, -s, 5 + n, .23f, 3.4f, lane: 0);
                d.OnBeats(3.50f, 3.90f);
                d.Train("CourierDiver", Flyby, 1.70f, s, 6 + n, .18f, 2.9f, lane: -.16f);
                break;
            default: // Couriers pass the command ship in matched pairs, then a diamond rises out.
                d.OnBeats(1.45f, 3.15f);
                d.Solo("CommandGunship", Interchange, 0, -s, .08f, 5.0f);
                d.OnBeats(2.10f, 2.55f);
                d.Train("CourierDiver", Overtake, .45f, -s, 6 + n, .18f, 3.1f, lane: .19f);
                d.Train("CourierDiver", Overtake, .45f, s, 6 + n, .18f, 3.1f, lane: .19f);
                d.OnBeats(3.70f, 4.45f);
                d.Diamond("AttackDrone", Slingshot, 1.65f, s, .22f, .065f, 4.0f);
                break;
        }
        return d.Build();
    }
}
