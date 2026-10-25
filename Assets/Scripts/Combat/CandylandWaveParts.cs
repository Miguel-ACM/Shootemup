using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve SugarSpiral=Route("Candyland / Sugar ribbon",Vector2.right,-.35f,1.38f,-.68f,.92f,-.28f,.75f,.54f,.64f,.38f,.40f,-.42f,.15f,-.55f,-.38f);
    static readonly FlightCurve TaffyLoop=Route("Candyland / Taffy curl",Vector2.right,-.65f,1.4f,-.67f,.84f,-.35f,.50f,.35f,.53f,.68f,.83f,.45f,1.4f);
    static readonly FlightCurve CandyZipper=Route("Candyland / Zipper",Vector2.up,-1.6f,.88f,-.8f,.75f,-.1f,.49f,.63f,.35f,1.6f,.30f);
    static readonly FlightCurve BonbonCross=Route("Candyland / Bonbon braid",Vector2.right,.6f,1.4f,.4f,.9f,-.45f,.73f,-.4f,.48f,.42f,.3f,.6f,-.4f);
    static readonly FlightCurve WaferSlip=Route("Candyland / Wafer slice",Vector2.right,-.82f,1.4f,-.60f,.85f,-.12f,.64f,.43f,.51f,.79f,.18f,.9f,-.4f);
    static readonly FlightCurve MintWheel=Route("Candyland / Mint wheel",Vector2.up,-1.6f,.65f,-.7f,.40f,-.3f,.42f,.2f,.76f,.75f,.82f,1.6f,.66f);
    static readonly FlightCurve SugarFountain=Route("Candyland / Sugar fountain",Vector2.right,-.3f,1.4f,-.11f,.85f,.34f,.60f,.66f,.62f,.78f,.92f,.9f,1.4f);
    static readonly FlightCurve WrapperSweep=Route("Candyland / Wrapper sweep",Vector2.up,-1.6f,.39f,-.75f,.37f,-.28f,.56f,.3f,.80f,.83f,.83f,1.6f,.72f);
    static readonly FlightCurve LicoriceKnot=Route("Candyland / Licorice knot",Vector2.right,.38f,1.4f,.56f,.92f,.25f,.72f,-.42f,.66f,-.52f,.40f,.12f,.20f,.55f,-.4f);
    public static readonly WavePart[] Candyland={
        new WavePart("Sugar ribbon rush","BonbonSkirmisher",SugarSpiral,WaveShape.Train,8,3.0f),
        new WavePart("Taffy bow","BonbonSkirmisher",TaffyLoop,WaveShape.Train,7,2.9f),
        new WavePart("Wafer zipper","WaferDart",CandyZipper,WaveShape.Train,7,2.5f),
        new WavePart("Bonbon braid","BonbonSkirmisher",BonbonCross,WaveShape.Train,8,3.1f),
        new WavePart("Chocolate arrow","WaferDart",WaferSlip,WaveShape.Chevron,5,2.7f),
        new WavePart("Mint wheel procession","MintSpinner",MintWheel,WaveShape.Train,6,3.0f),
        new WavePart("Sugar fountain","BonbonSkirmisher",SugarFountain,WaveShape.Train,7,2.8f),
        new WavePart("Wrapper fan","BonbonSkirmisher",WrapperSweep,WaveShape.Rank,5,3.0f),
        new WavePart("Licorice knot","WaferDart",LicoriceKnot,WaveShape.Train,7,3.0f,4),
        new WavePart("Double wafer rake","WaferDart",CandyZipper,WaveShape.Rake,5,2.5f,4),
        new WavePart("Mint diamond","MintSpinner",TaffyLoop,WaveShape.Diamond,4,3.2f,5),
        new WavePart("Unwrapped ribbons","BonbonSkirmisher",WrapperSweep,WaveShape.Train,9,2.8f,6),
        new WavePart("Chocolate fountain","WaferDart",SugarFountain,WaveShape.Chevron,5,2.8f,7),
        new WavePart("Mint braid","MintSpinner",BonbonCross,WaveShape.Train,7,3.2f,8),
        new WavePart("Sugar needlework","WaferDart",SugarSpiral,WaveShape.Train,8,2.7f,10),
        new WavePart("Confection crown","BonbonSkirmisher",TaffyLoop,WaveShape.Chevron,5,2.6f,11),
        new WavePart("Whirling mint staff","MintSpinner",MintWheel,WaveShape.Rank,4,2.7f,12),
        new WavePart("Last wrapper rush","BonbonSkirmisher",LicoriceKnot,WaveShape.Train,9,2.8f,14)
    };
}
