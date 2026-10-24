using UnityEngine;
public static partial class WavePartLibrary
{
    static readonly FlightCurve StormFork=Route("Thunder / Forked descent",Vector2.right,-.5f,1.24f,-.6f,.93f,-.35f,.72f,.22f,.53f,.44f,.29f,.40f,-.24f);
    static readonly FlightCurve IonBow=Route("Thunder / Ion bow",Vector2.right,-.65f,1.24f,-.65f,.88f,-.44f,.6f,-.02f,.47f,.40f,.65f,.48f,.97f,.45f,1.24f);
    static readonly FlightCurve StormRail=Route("Thunder / Storm rail",Vector2.up,-1.36f,.92f,-.73f,.95f,-.11f,.87f,.48f,.62f,1.36f,.49f);
    static readonly FlightCurve ThunderPeel=Route("Thunder / Ion peel",Vector2.right,.58f,1.24f,.65f,.89f,.45f,.57f,.05f,.40f,-.32f,.59f,-.41f,.97f,-.42f,1.24f);
    static readonly FlightCurve StaticFold=Route("Thunder / Static fold",Vector2.right,-.66f,1.24f,-.72f,.93f,-.55f,.72f,-.28f,.59f,.04f,.76f,.08f,1.24f);
    static readonly FlightCurve FlashLane=Route("Thunder / Flash lane",Vector2.up,1.36f,.75f,.73f,.71f,.13f,.55f,-.45f,.38f,-1.36f,.42f);
    public static readonly WavePart[] Thunder={
        new WavePart("Forked flight","StormSkimmer",StormFork,WaveShape.Train,4,4.5f),
        new WavePart("Ion crown","StormSkimmer",IonBow,WaveShape.Chevron,3,4.7f),
        new WavePart("Storm rail","StormSkimmer",StormRail,WaveShape.Rank,3,4.1f),
        new WavePart("Ion diamond","StormSkimmer",ThunderPeel,WaveShape.Diamond,4,4.8f),
        new WavePart("Static flight","StormSkimmer",StaticFold,WaveShape.Rake,3,4.7f),
        new WavePart("Flash procession","StormSkimmer",FlashLane,WaveShape.Train,4,4.1f),
        new WavePart("Lightning slash","StormSkimmer",ThunderChoreography.Slash,WaveShape.Train,4,4.0f),
        new WavePart("Hooking rank","StormSkimmer",ThunderChoreography.Sickle,WaveShape.Rank,3,4.4f),
        new WavePart("Capacitor wing","RelayDrone",ThunderChoreography.Relay,WaveShape.Rank,2,5.2f,4),
        new WavePart("Ion relays","RelayDrone",IonBow,WaveShape.Train,2,5.2f,5),
        new WavePart("Static relays","RelayDrone",StaticFold,WaveShape.Rank,2,5.3f,5),
        new WavePart("Peeling relays","RelayDrone",ThunderPeel,WaveShape.Rank,2,5.1f,6),
        new WavePart("Pendulum beam","BeamDrone",ThunderChoreography.Pendulum,WaveShape.Solo,1,5.5f,6),
        new WavePart("Ion beam","BeamDrone",IonBow,WaveShape.Solo,1,5.5f,7),
        new WavePart("Sweeping lance","ArcLancer",ThunderChoreography.Pendulum,WaveShape.Solo,1,5.5f,8),
        new WavePart("Hooking lance","ArcLancer",ThunderChoreography.Sickle,WaveShape.Solo,1,5.3f,10)
    };
}
