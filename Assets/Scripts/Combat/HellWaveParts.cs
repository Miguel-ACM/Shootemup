using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve InfernalGate=Route("Hell / Gate apparition",Vector2.right,-.63f,1.32f,-.68f,.91f,-.35f,.64f,.24f,.68f,.61f,.89f,.64f,1.32f);
    static readonly FlightCurve Styx=Route("Hell / Styx broadside",Vector2.up,-1.55f,.84f,-.85f,.77f,-.18f,.61f,.42f,.73f,1.0f,.64f,1.55f,.54f);
    static readonly FlightCurve CinderHook=Route("Hell / Cinder hook",Vector2.right,.65f,1.32f,.70f,.88f,.39f,.54f,-.17f,.48f,-.66f,.71f,-.75f,1.32f);
    static readonly FlightCurve HellDive=Route("Hell / Pit descent",Vector2.right,-.58f,1.32f,-.43f,.88f,.28f,.66f,.60f,.33f,.22f,.13f,-.26f,-.32f);
    static readonly FlightCurve WraithBow=Route("Hell / Wraith bow",Vector2.right,.37f,1.34f,.62f,.92f,.49f,.74f,-.17f,.72f,-.49f,.94f,-.39f,1.34f);
    static readonly FlightCurve EmberScissors=Route("Hell / Ember scissors",Vector2.up,-1.55f,.54f,-.85f,.48f,-.30f,.71f,.24f,.88f,.87f,.77f,1.55f,.67f);
    static readonly FlightCurve JawSpiral=Route("Hell / Jaw spiral",Vector2.right,-.75f,1.34f,-.60f,.95f,-.11f,.63f,.48f,.65f,.60f,.90f,.19f,1.34f);
    public static readonly WavePart[] Hell={
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
        new WavePart("Last ferry","GhostGalleon",EmberScissors,WaveShape.Solo,1,4.6f,13)
    };
}
