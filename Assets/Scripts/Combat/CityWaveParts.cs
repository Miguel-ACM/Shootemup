using UnityEngine;
public static partial class WavePartLibrary
{
    static readonly FlightCurve CityRamp=Route("City / Elevated ramp",Vector2.right,-.70f,1.24f,-.66f,.9f,-.42f,.59f,.04f,.43f,.43f,.53f,.65f,.89f,.66f,1.24f);
    static readonly FlightCurve CityCanyon=Route("City / Canyon stitch",Vector2.right,.3f,1.24f,.43f,.87f,.16f,.63f,-.41f,.42f,-.32f,.16f,.12f,-.24f);
    static readonly FlightCurve Skybridge=Route("City / Skybridge",Vector2.up,-1.36f,.94f,-.8f,.91f,-.13f,.79f,.53f,.84f,1.36f,.96f);
    static readonly FlightCurve Underpass=Route("City / Underpass",Vector2.up,1.36f,.55f,.8f,.5f,.15f,.41f,-.52f,.33f,-1.36f,.42f);
    static readonly FlightCurve CityLoop=Route("City / Rooftop loop",Vector2.right,-.55f,1.24f,-.64f,.94f,-.58f,.63f,-.25f,.48f,.12f,.67f,.14f,.98f,.05f,1.24f);
    static readonly FlightCurve Junction=Route("City / Junction peel",Vector2.right,.65f,1.24f,.62f,.93f,.45f,.67f,.12f,.58f,-.12f,.75f,-.2f,1.24f);
    public static readonly WavePart[] City={
        new WavePart("Elevated traffic","CourierDiver",CityRamp,WaveShape.Train,5,3.8f),
        new WavePart("Canyon couriers","CourierDiver",CityCanyon,WaveShape.Train,5,3.6f),
        new WavePart("Skybridge traffic","CourierDiver",Skybridge,WaveShape.Rank,3,3.1f),
        new WavePart("Underpass traffic","CourierDiver",Underpass,WaveShape.Train,5,3.1f),
        new WavePart("Rooftop pack","AttackDrone",CityLoop,WaveShape.Chevron,3,4.2f),
        new WavePart("Junction drones","AttackDrone",Junction,WaveShape.Diamond,4,4.1f),
        new WavePart("Express couriers","CourierDiver",CityChoreography.Flyby,WaveShape.Train,5,2.95f),
        new WavePart("Overtaking drones","AttackDrone",CityChoreography.Overtake,WaveShape.Rake,3,4),
        new WavePart("Interchange pack","AttackDrone",CityChoreography.Interchange,WaveShape.Train,4,4.3f,4),
        new WavePart("Slingshot arrow","AttackDrone",CityChoreography.Slingshot,WaveShape.Chevron,3,4.1f,4),
        new WavePart("Bridge drones","AttackDrone",Skybridge,WaveShape.Rank,3,3.5f,5),
        new WavePart("Canyon overtakers","CourierDiver",CityCanyon,WaveShape.Diamond,4,3.8f,5),
        new WavePart("Rooftop pursuit","PursuitFighter",CityLoop,WaveShape.Solo,1,4.5f,6),
        new WavePart("Junction pursuit","PursuitFighter",Junction,WaveShape.Solo,1,4.5f,7),
        new WavePart("Command ramp","CommandGunship",CityRamp,WaveShape.Solo,1,5,8),
        new WavePart("Command interchange","CommandGunship",CityChoreography.Interchange,WaveShape.Solo,1,5,10)
    };
}
