using UnityEngine;
public static partial class WavePartLibrary
{
    static readonly FlightCurve CityRamp = Route("City / Elevated ramp", Vector2.right, -.70f, 1.24f, -.66f, .9f, -.42f, .59f, .04f, .43f, .43f, .53f, .65f, .89f, .66f, 1.24f);
    static readonly FlightCurve CityCanyon = Route("City / Canyon stitch", Vector2.right, .3f, 1.24f, .43f, .87f, .16f, .63f, -.41f, .42f, -.32f, .16f, .12f, -.24f);
    static readonly FlightCurve Skybridge = Route("City / Skybridge", Vector2.up, -1.36f, .94f, -.8f, .91f, -.13f, .79f, .53f, .84f, 1.36f, .96f);
    static readonly FlightCurve Underpass = Route("City / Underpass", Vector2.up, 1.36f, .55f, .8f, .5f, .15f, .41f, -.52f, .33f, -1.36f, .42f);
    static readonly FlightCurve CityLoop = Route("City / Rooftop loop", Vector2.right, -.55f, 1.24f, -.64f, .94f, -.58f, .63f, -.25f, .48f, .12f, .67f, .14f, .98f, .05f, 1.24f);
    static readonly FlightCurve Junction = Route("City / Junction peel", Vector2.right, .65f, 1.24f, .62f, .93f, .45f, .67f, .12f, .58f, -.12f, .75f, -.2f, 1.24f);
    static readonly FlightCurve RoofWatch = FlightCurve.Stations("City / Rooftop standoff", Vector2.right,
        new[] { 0f, .22f, .68f, 1f }, -.1f, 1.26f, -.1f, .66f, -.1f, .66f, .68f, 1.26f).WithTracking(.30f, .13f);
    static readonly FlightCurve ServiceLift = Route("City / Service lift", Vector2.right,
        -.78f, -.25f, -.79f, .13f, -.63f, .46f, -.31f, .74f, .18f, 1.24f);
    static readonly FlightCurve SwitchPosts = FlightCurve.Stations("City / Two rooftop posts", Vector2.right,
        new[] { 0f, .18f, .34f, .50f, .68f, 1f }, -.62f, 1.25f, -.62f, .76f, -.62f, .76f, .55f, .69f, .55f, .69f, 1.4f, .9f);
    public static readonly WavePart[] City ={
        new WavePart("Rooftop shadow", "PursuitFighter", RoofWatch, WaveShape.Solo, 1, 5.5f, 6, new[] { .31f, .47f, .64f }, motion: WaveMotion.Pursuit),
        new WavePart("Service lift couriers", "CourierDiver", ServiceLift, WaveShape.Train, 3, 4.3f, 4, new[] { .46f, .65f }, motion: WaveMotion.Rising),
        new WavePart("Rooftop relay", "CommandGunship", SwitchPosts, WaveShape.Solo, 1, 6.2f, 8, new[] { .25f, .57f, .66f }, motion: WaveMotion.Relay),
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
