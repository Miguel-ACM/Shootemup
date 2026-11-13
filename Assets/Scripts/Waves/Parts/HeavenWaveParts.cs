using UnityEngine;

public static partial class WavePartLibrary
{
    public static readonly WavePart[] Heaven ={
        new WavePart("Choir held note", "ChoirLancer", FlightCurve.Stations("Heaven / Held note", Vector2.right, new[]{0f,.20f,.65f,1f}, -.53f,1.5f, -.53f,.81f, -.53f,.81f, -1.8f,.75f), WaveShape.Solo, 1, 7.2f, 2, new[]{.27f,.51f}, WaveMotion.Station),
        new WavePart("Cantor antiphon", "PrismCantor", FlightCurve.Stations("Heaven / Antiphon balconies", Vector2.right, new[]{0f,.17f,.35f,.52f,.72f,1f}, .54f,1.5f, .54f,.86f, .54f,.86f, -.38f,.72f, -.38f,.72f, -.38f,1.5f), WaveShape.Solo, 1, 8.2f, 4, new[]{.23f,.58f}, WaveMotion.Relay),
        new WavePart("Votive ascension", "VotiveSpear", Route("Heaven / Votive ascension", Vector2.right, .77f,-.5f, .80f,.15f, .72f,.48f, .41f,.82f, .28f,1.5f), WaveShape.Train, 3, 6.0f, 6, new[]{.48f,.68f}, WaveMotion.Rising)
    };
}
