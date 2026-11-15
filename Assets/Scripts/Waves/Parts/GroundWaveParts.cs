using UnityEngine;

public static partial class WavePartLibrary
{
    static readonly FlightCurve RoadColumn = Route("Plains / Tank road column", Vector2.right, -.53f, 1.3f, -.50f, .90f, -.42f, .55f, -.56f, .18f, -.57f, -.3f);
    static readonly FlightCurve RoadDescent = Route("Plains / Fast road descent", Vector2.right, -.49f, 1.3f, -.59f, .9f, -.62f, .5f, -.47f, .1f, -.43f, -.3f);
    static readonly FlightCurve RoadRelief = Route("Plains / Relief column", Vector2.right, -.60f, 1.3f, -.58f, .86f, -.48f, .43f, -.42f, .08f, -.49f, -.3f);
    static readonly FlightCurve FrostConvoy = Route("Snow / Auger double bend", Vector2.right, -.18f, 1.3f, -.22f, .88f, .18f, .58f, .15f, .3f, -.18f, -.3f);
    static readonly FlightCurve FrostSickle = Route("Snow / Auger ice sickle", Vector2.right, .20f, 1.3f, .25f, .9f, .05f, .65f, -.24f, .25f, -.20f, -.3f);
    static readonly FlightCurve FrostSwitch = Route("Snow / Auger switchback", Vector2.right, 0, 1.3f, .22f, .88f, -.19f, .6f, .19f, .24f, 0, -.3f);
    public static readonly WavePart[] PlainsGround ={
        new WavePart("Road armor column","MeadowTank",RoadColumn,WaveShape.Train,3,4.8f),
        new WavePart("Tank downhill rush","MeadowTank",RoadDescent,WaveShape.Train,3,4.1f,4),
        new WavePart("Relief convoy","MeadowTank",RoadRelief,WaveShape.Train,3,5.1f,5),
        new WavePart("Armored road answer","MeadowTank",RoadColumn,WaveShape.Train,3,4.4f,7),
        new WavePart("Tread pursuit","MeadowTank",RoadDescent,WaveShape.Train,4,4.2f,9),
        new WavePart("Last road patrol","MeadowTank",RoadRelief,WaveShape.Train,4,4.6f,12)
    };
    // FOREST variety: no road, so tanks plough through the trees anywhere on screen: diagonal
    // sweeps, a weaving column, the east flank and a crossing from the west.
    static readonly FlightCurve ForestSweep = Route("Plains forest / Thicket sweep", Vector2.right, .55f, 1.3f, .50f, .90f, .12f, .60f, -.38f, .28f, -.50f, -.3f);
    static readonly FlightCurve ForestWeave = Route("Plains forest / Timber weave", Vector2.right, -.08f, 1.3f, .28f, .92f, -.24f, .60f, .22f, .28f, 0, -.3f);
    static readonly FlightCurve ForestEast = Route("Plains forest / East column", Vector2.right, .53f, 1.3f, .50f, .90f, .44f, .55f, .56f, .18f, .55f, -.3f);
    static readonly FlightCurve ForestCross = Route("Plains forest / Glade crossing", Vector2.right, -.55f, 1.3f, -.46f, .90f, .04f, .60f, .45f, .30f, .52f, -.3f);
    public static readonly WavePart[] PlainsForestGround ={
        new WavePart("Thicket sweep","MeadowTank",ForestSweep,WaveShape.Train,3,4.8f).Under(DistrictCondition.Forest),
        new WavePart("Timber weave","MeadowTank",ForestWeave,WaveShape.Train,3,4.6f,4).Under(DistrictCondition.Forest),
        new WavePart("East flank column","MeadowTank",ForestEast,WaveShape.Train,3,4.2f,5).Under(DistrictCondition.Forest),
        new WavePart("Glade crossing","MeadowTank",ForestCross,WaveShape.Train,4,4.6f,7).Under(DistrictCondition.Forest)
    };
    public static readonly WavePart[] SnowGround ={
        new WavePart("Powder auger column","FrostAuger",FrostConvoy,WaveShape.Train,3,5.1f),
        new WavePart("Ice sickle battery","FrostAuger",FrostSickle,WaveShape.Train,3,5.2f,4),
        new WavePart("Drift switchback","FrostAuger",FrostSwitch,WaveShape.Train,3,4.8f,5),
        new WavePart("Twin screw pursuit","FrostAuger",FrostConvoy,WaveShape.Train,3,4.6f,7),
        new WavePart("Glacier mortar train","FrostAuger",FrostSickle,WaveShape.Train,4,4.8f,9),
        new WavePart("White furrow relay","FrostAuger",FrostSwitch,WaveShape.Train,4,4.6f,12)
    };
    static WavePart[] plainsWithGround, snowWithGround, desertWithStorm;
    static WavePart[] WithGround(params WavePart[][] groups)
    {
        var parts = new System.Collections.Generic.List<WavePart>();
        foreach (var group in groups)
            parts.AddRange(group);
        return parts.ToArray();
    }
}
