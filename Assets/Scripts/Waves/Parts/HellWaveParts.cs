using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve InfernalGate = Route("Hell / Gate apparition", Vector2.right, -.63f, 1.32f, -.68f, .91f, -.35f, .64f, .24f, .68f, .61f, .89f, .64f, 1.32f);
    static readonly FlightCurve Styx = Route("Hell / Styx broadside", Vector2.up, -1.55f, .84f, -.85f, .77f, -.18f, .61f, .42f, .73f, 1.0f, .64f, 1.55f, .54f);
    static readonly FlightCurve CinderHook = Route("Hell / Cinder hook", Vector2.right, .65f, 1.32f, .70f, .88f, .39f, .54f, -.17f, .48f, -.66f, .71f, -.75f, 1.32f);
    static readonly FlightCurve HellDive = Route("Hell / Pit descent", Vector2.right, -.58f, 1.32f, -.43f, .88f, .28f, .66f, .60f, .33f, .22f, .13f, -.26f, -.32f);
    static readonly FlightCurve WraithBow = Route("Hell / Wraith bow", Vector2.right, .37f, 1.34f, .62f, .92f, .49f, .74f, -.17f, .72f, -.49f, .94f, -.39f, 1.34f);
    static readonly FlightCurve EmberScissors = Route("Hell / Ember scissors", Vector2.up, -1.55f, .54f, -.85f, .48f, -.30f, .71f, .24f, .88f, .87f, .77f, 1.55f, .67f);
    static readonly FlightCurve JawSpiral = Route("Hell / Jaw spiral", Vector2.right, -.75f, 1.34f, -.60f, .95f, -.11f, .63f, .48f, .65f, .60f, .90f, .19f, 1.34f);
    static readonly FlightCurve Brimstone = Route("Hell / Brimstone fall", Vector2.right, .58f, 1.40f, .40f, .86f, -.15f, .70f, -.45f, .44f, -.05f, .20f, .20f, -.40f);
    static readonly FlightCurve Pyre = Route("Hell / Pyre circle", Vector2.right, -.30f, 1.40f, -.62f, .86f, -.40f, .58f, .10f, .54f, .40f, .78f, .20f, 1.40f);
    static readonly FlightCurve Ashfall = Route("Hell / Ashfall drift", Vector2.up, 1.40f, .86f, .60f, .70f, -.10f, .84f, -.70f, .66f, -1.40f, .80f);
    public static readonly WavePart[] Hell ={
        new WavePart("Censer at the gate", "AshCenser", FlightCurve.Stations("Hell / Hanging censer", Vector2.right, new[]{0f,.22f,.65f,1f}, .53f,1.4f, .53f,.78f, .53f,.78f, 1.8f,.66f), WaveShape.Solo, 1, 5.2f, 4, new[]{.30f,.46f,.60f}, WaveMotion.Station),
        new WavePart("Pit-born imp ladder", "CinderImp", Route("Hell / Pit-born ladder", Vector2.right, -.82f,-.4f, -.84f,.16f, -.67f,.42f, -.30f,.68f, -.12f,1.4f), WaveShape.Train, 4, 3.9f, 2, new[]{.47f,.67f}, WaveMotion.Rising),
        new WavePart("Revenant stalk", "SkullRevenant", FlightCurve.Stations("Hell / Revenant stalk", Vector2.right, new[]{0f,.23f,.67f,1f}, -.2f,1.5f, -.2f,.68f, -.2f,.68f, -.8f,1.5f).WithTracking(.22f,.08f), WaveShape.Solo, 1, 5.5f, 6, new[]{.32f,.47f,.63f}, WaveMotion.Pursuit),
        new WavePart("Gate revenants","SkullRevenant",InfernalGate,WaveShape.Train,4,3.9f),
        new WavePart("Styx galleons","GhostGalleon",Styx,WaveShape.Train,2,4.8f),
        new WavePart("Cinder hooks","CinderImp",CinderHook,WaveShape.Chevron,5,3.2f),
        new WavePart("Pit procession","SkullRevenant",HellDive,WaveShape.Train,4,3.8f),
        new WavePart("Spectral bows","GhostGalleon",WraithBow,WaveShape.Rank,2,4.9f),
        new WavePart("Ember scissors","CinderImp",EmberScissors,WaveShape.Rake,4,3.3f),
        new WavePart("Jaw spiral","SkullRevenant",JawSpiral,WaveShape.Diamond,4,3.7f),
        new WavePart("Ghost flotilla","GhostGalleon",InfernalGate,WaveShape.Train,2,4.7f,5),
        new WavePart("Imp descent","CinderImp",HellDive,WaveShape.Train,5,3.1f,7),
        new WavePart("Ash crown","SkullRevenant",WraithBow,WaveShape.Chevron,5,3.8f,9),
        new WavePart("Burning crossing","CinderImp",Styx,WaveShape.Rank,4,3.4f,11),
        new WavePart("Last ferry","GhostGalleon",EmberScissors,WaveShape.Solo,1,4.6f,13),
        new WavePart("Brimstone fall", "SkullRevenant", Brimstone, WaveShape.Train, 4, 3.9f, 3),
        new WavePart("Pyre circle", "CinderImp", Pyre, WaveShape.Diamond, 4, 3.3f, 4),
        new WavePart("Ashfall galleons", "GhostGalleon", Ashfall, WaveShape.Rank, 2, 4.8f, 6),
        new WavePart("Brimstone imps", "CinderImp", Brimstone, WaveShape.Rake, 4, 3.2f, 9)
    };
}
