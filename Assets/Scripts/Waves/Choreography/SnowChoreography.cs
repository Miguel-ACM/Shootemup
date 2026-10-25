using UnityEngine;

public static class SnowChoreography
{
    public static readonly FlightCurve Spiral = new FlightCurve("Snow / Unwinding spiral", Vector2.right, 0,
        -.72f, 1.18f, -.82f, .92f, -.72f, .67f, -.48f, .52f, -.16f, .53f, .07f, .69f, .08f, .94f, .03f, 1.18f);
    public static readonly FlightCurve Petal = new FlightCurve("Snow / Falling petal", Vector2.right, 0,
        .58f, 1.12f, .69f, .98f, .79f, .72f, .68f, .48f, .52f, .36f, .62f, .26f, .86f, .25f, 1.32f, .42f);
    public static readonly FlightCurve Crosswind = new FlightCurve("Snow / Crosswind", Vector2.up, 0,
        -1.32f, .93f, -.70f, .78f, 0, .89f, .65f, .66f, .30f, .46f, -.50f, .50f, -1.32f, .38f);
    public static readonly FlightCurve Drift = new FlightCurve("Snow / Siege arc", Vector2.right, 0,
        -.55f, 1.18f, -.60f, .90f, -.48f, .68f, -.12f, .56f, .28f, .63f, .50f, .88f, .50f, 1.18f);
    public static Wave Create(int budget)
    {
        var d = new WaveDance(budget) { Handoff = 4.4f };
        float s = d.Side;
        int n = d.Extra;
        switch (d.Phrase)
        {
            case 0: // Mirrored petals bloom together; escort fire clears before the shells.
                d.OnBeats(2.65f, 4.05f);
                d.Solo("AvalancheBomber", Drift, 0, s, 0, 5.8f);
                d.OnBeats(2.05f, 3.35f);
                d.Train("ColdWeatherFighter", Petal, 0, s, 4 + n, .46f, 5.0f);
                d.Train("ColdWeatherFighter", Petal, 0, -s, 4 + n, .46f, 5.0f);
                break;
            case 1: // A shield leads a single coil; a clear crosswind answers the coil.
                d.OnBeats(2.80f, 4.30f);
                d.Solo("ShieldTransport", Drift, 0, -s, -.25f, 5.8f);
                d.OnBeats(2.10f, 3.60f);
                d.Train("ColdWeatherFighter", Spiral, .15f, s, 6 + n, .43f, 5.8f);
                d.OnBeats(4.95f, 5.55f);
                for (int i = 0; i < 3; i++)
                    d.Solo("ColdWeatherFighter", Crosswind, 2.45f, -s, -.28f + (i - 1) * .09f, 4.6f);
                break;
            case 2: // A spacious siege echelon fires a rolling salvo, then the escorts reply.
                for (int i = 0; i < 3; i++)
                {
                    d.OnBeats(1.70f + i * .24f, 3.30f + i * .24f);
                    d.Solo("SiegeGunship", Crosswind, 0, s, (i - 1) * .18f, 5.8f);
                }
                d.OnBeats(2.70f, 4.45f);
                d.Train("ColdWeatherFighter", Petal, .55f, -s, 6 + n, .34f, 5.0f);
                break;
            case 3: // Snowflake arms turn in unison and fire on opposite halves of the beat.
                d.OnBeats(2.30f, 3.65f);
                d.Train("ColdWeatherFighter", Spiral, 0, s, 5 + n, .48f, 5.7f);
                d.OnBeats(2.85f, 4.30f);
                d.Train("ColdWeatherFighter", Spiral, 0, -s, 5 + n, .48f, 5.7f);
                d.OnBeats(3.20f, 4.85f);
                d.Solo("AvalancheBomber", Drift, .8f, s, 0, 5.6f);
                break;
            case 4: // A wide bomber crown launches one rolling cascade, then a second.
                for (int i = 0; i < 3; i++)
                {
                    d.OnBeats(1.85f + i * .22f, 3.55f + i * .22f);
                    d.Solo("AvalancheBomber", Drift, 0, s, (i - 1) * .43f, 5.9f, depth: Mathf.Abs(i - 1) * .045f);
                }
                d.OnBeats(2.80f, 4.60f);
                d.Train("ColdWeatherFighter", Crosswind, .85f, -s, 6 + n, .32f, 4.7f, lane: -.23f);
                break;
            case 5: // A low crosswind passes beneath the shield arch; a late petal peels outside it.
                d.OnBeats(1.90f, 3.50f);
                d.Rank("ShieldTransport", Drift, 0, s, 2, .62f, 5.7f);
                d.OnBeats(2.65f, 4.30f);
                d.Train("ColdWeatherFighter", Crosswind, .65f, s, 4, .38f, 4.7f, lane: -.28f);
                d.OnBeats(4.95f, 5.55f);
                d.Train("ColdWeatherFighter", Petal, 2.15f, -s, 3, .40f, 5.0f);
                break;
            case 6: // The siege coil owns one side; a continuous petal procession owns the other.
                d.OnBeats(1.75f, 3.25f, 4.75f);
                d.Solo("SiegeGunship", Spiral, 0, s, 0, 6.2f);
                d.OnBeats(2.50f, 4.00f, 5.55f);
                d.Train("ColdWeatherFighter", Petal, .25f, s, 8 + n * 2, .38f, 5.0f);
                break;
            default: // Two broad winds trade ordered salvos while a gunship turns upward.
                d.OnBeats(2.05f, 3.40f);
                d.Train("ColdWeatherFighter", Crosswind, 0, s, 5 + n, .32f, 4.9f, lane: .045f);
                d.OnBeats(2.70f, 4.05f);
                d.Train("ColdWeatherFighter", Crosswind, .55f, -s, 5 + n, .32f, 4.9f, lane: -.14f);
                d.OnBeats(3.05f, 4.75f);
                d.Solo("SiegeGunship", Drift, .9f, s, 0, 5.8f);
                break;
        }
        return d.Build();
    }
}
