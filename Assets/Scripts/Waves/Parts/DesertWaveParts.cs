using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve DuneCrest=Route("Desert / Crest skimming",Vector2.up,-1.65f,.92f,-.7f,.80f,-.2f,.67f,.48f,.77f,1.65f,.95f);
    static readonly FlightCurve DuneSlip=Route("Desert / Slipface dive",Vector2.right,-.69f,1.4f,-.58f,.90f,-.23f,.66f,.37f,.48f,.48f,.11f,.32f,-.4f);
    static readonly FlightCurve SiloHook=Route("Desert / Silo hook",Vector2.right,.62f,1.4f,.65f,.87f,.50f,.57f,.12f,.48f,-.22f,.71f,-.31f,1.4f);
    static readonly FlightCurve SandRidge=Route("Desert / Buried ridge",Vector2.right,-.58f,1.4f,-.62f,.9f,-.50f,.72f,-.28f,.55f,-.41f,.36f,-.63f,-.4f);
    static readonly FlightCurve SandArc=Route("Desert / Subsurface arc",Vector2.up,-1.65f,.75f,-.80f,.69f,-.32f,.61f,.15f,.65f,.7f,.79f,1.65f,.85f);
    static readonly FlightCurve OutpostWeave=Route("Desert / Outpost weave",Vector2.right,.1f,1.4f,.42f,.91f,.24f,.75f,-.30f,.59f,-.34f,.38f,.23f,-.4f);
    static readonly FlightCurve WormBend=Route("Desert / Worm oxbow",Vector2.right,-.53f,1.5f,-.62f,.91f,-.39f,.61f,.12f,.52f,.55f,.73f,.58f,1.5f);
    static readonly FlightCurve WormCross=Route("Desert / Worm dune traverse",Vector2.up,-1.85f,.85f,-.83f,.83f,-.29f,.62f,.34f,.68f,.89f,.87f,1.85f,.92f);
    static readonly FlightCurve SaltPan=Route("Desert / Salt pan sickle",Vector2.up,-1.65f,.45f,-.71f,.48f,-.21f,.70f,.18f,.85f,.71f,.89f,1.65f,.77f);
    static readonly FlightCurve BuriedSpiral=Route("Desert / Buried spiral",Vector2.right,.57f,1.4f,.26f,.91f,-.32f,.77f,-.47f,.52f,-.15f,.31f,.29f,.17f,.59f,-.4f);
    public static readonly WavePart[] Desert={
        new WavePart("Crest runners","SandStriker",DuneCrest,WaveShape.Train,5,4.0f),
        new WavePart("Slipface arrows","SandStriker",DuneSlip,WaveShape.Chevron,3,4.3f),
        new WavePart("Outpost ribbon","SandStriker",OutpostWeave,WaveShape.Train,5,4.2f),
        new WavePart("Silo flanking wing","SandStriker",SiloHook,WaveShape.Diamond,4,4.5f),
        new WavePart("Buried ridge batteries","SandEmitter",SandRidge,WaveShape.Train,3,4.6f),
        new WavePart("Eruption crescent","SandEmitter",SandArc,WaveShape.Train,3,4.3f),
        new WavePart("Parallel dune skimmers","SandStriker",DuneCrest,WaveShape.Rank,3,3.8f,4),
        new WavePart("Mirage pursuit","SandStriker",SaltPan,WaveShape.Train,4,4.1f,4),
        new WavePart("Buried outpost line","SandEmitter",OutpostWeave,WaveShape.Chevron,3,4.8f,5),
        new WavePart("Oxbow sandworm","SandWorm",WormBend,WaveShape.Solo,1,5.8f,6),
        new WavePart("Dune-traversing sandworm","SandWorm",WormCross,WaveShape.Solo,1,5.5f,6),
        new WavePart("Silo eruption fork","SandEmitter",BuriedSpiral,WaveShape.Rake,3,4.9f,8),
        new WavePart("Crest ambush","SandEmitter",DuneCrest,WaveShape.Rank,3,4.5f,9),
        new WavePart("Deep outpost serpent","SandWorm",OutpostWeave,WaveShape.Solo,1,5.8f,10),
        new WavePart("Dune diving squadron","SandStriker",DuneSlip,WaveShape.Train,6,3.8f,12),
        new WavePart("Buried diamond","SandEmitter",SandRidge,WaveShape.Diamond,4,4.8f,14)
    };
}
