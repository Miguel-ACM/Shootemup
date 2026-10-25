using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve RoadColumn=Route("Plains / Tank road column",Vector2.right,-.53f,1.3f,-.50f,.90f,-.42f,.55f,-.56f,.18f,-.57f,-.3f);
    static readonly FlightCurve RoadDescent=Route("Plains / Fast road descent",Vector2.right,-.49f,1.3f,-.59f,.9f,-.62f,.5f,-.47f,.1f,-.43f,-.3f);
    static readonly FlightCurve RoadRelief=Route("Plains / Relief column",Vector2.right,-.60f,1.3f,-.58f,.86f,-.48f,.43f,-.42f,.08f,-.49f,-.3f);
    static readonly FlightCurve FrostConvoy=Route("Snow / Auger double bend",Vector2.right,-.18f,1.3f,-.22f,.88f,.18f,.58f,.15f,.3f,-.18f,-.3f);
    static readonly FlightCurve FrostSickle=Route("Snow / Auger ice sickle",Vector2.right,.20f,1.3f,.25f,.9f,.05f,.65f,-.24f,.25f,-.20f,-.3f);
    static readonly FlightCurve FrostSwitch=Route("Snow / Auger switchback",Vector2.right,0,1.3f,.22f,.88f,-.19f,.6f,.19f,.24f,0,-.3f);
    public static readonly WavePart[] PlainsGround={
        new WavePart("Road armor column","MeadowTank",RoadColumn,WaveShape.Train,3,4.8f),
        new WavePart("Tank downhill rush","MeadowTank",RoadDescent,WaveShape.Train,3,4.1f,4),
        new WavePart("Relief convoy","MeadowTank",RoadRelief,WaveShape.Train,3,5.1f,5),
        new WavePart("Armored road answer","MeadowTank",RoadColumn,WaveShape.Train,3,4.4f,7),
        new WavePart("Tread pursuit","MeadowTank",RoadDescent,WaveShape.Train,4,4.2f,9),
        new WavePart("Last road patrol","MeadowTank",RoadRelief,WaveShape.Train,4,4.6f,12)
    };
    public static readonly WavePart[] SnowGround={
        new WavePart("Powder auger column","FrostAuger",FrostConvoy,WaveShape.Train,3,5.1f),
        new WavePart("Ice sickle battery","FrostAuger",FrostSickle,WaveShape.Train,3,5.2f,4),
        new WavePart("Drift switchback","FrostAuger",FrostSwitch,WaveShape.Train,3,4.8f,5),
        new WavePart("Twin screw pursuit","FrostAuger",FrostConvoy,WaveShape.Train,3,4.6f,7),
        new WavePart("Glacier mortar train","FrostAuger",FrostSickle,WaveShape.Train,4,4.8f,9),
        new WavePart("White furrow relay","FrostAuger",FrostSwitch,WaveShape.Train,4,4.6f,12)
    };
    static WavePart[] plainsWithGround,snowWithGround;
    static WavePart[] WithGround(WavePart[] air,WavePart[] ground)
    {
        var parts=new WavePart[air.Length+ground.Length];air.CopyTo(parts,0);ground.CopyTo(parts,air.Length);return parts;
    }
}
