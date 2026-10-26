using System.Collections.Generic;

// Hand-set formation accents. Easy leaves distinct reading and crossing beats
// for each role; it never drops arbitrary members of a Hard volley.
public static class EnemyEasyScore
{
    static readonly Dictionary<string, float[]> scores = new Dictionary<string, float[]>
    {
        {"ScoutInterceptor",new[]{.36f,.67f}}, {"PatrolGunship",new[]{.39f,.73f}},
        {"HeavyBomber",new[]{.43f,.78f}}, {"MeadowTank",new[]{.32f,.62f}},
        {"ColdWeatherFighter",new[]{.34f,.69f}}, {"SiegeGunship",new[]{.44f,.76f}},
        {"ShieldTransport",new[]{.42f,.75f}}, {"AvalancheBomber",new[]{.46f,.79f}},
        {"FrostAuger",new[]{.38f,.73f}}, {"WallTurret",new[]{.4f,.72f}},
        {"MaintenanceDrone",new[]{.36f,.7f}}, {"ArmoredCarrier",new[]{.44f,.77f}},
        {"BeamDrone",new[]{.38f,.72f}}, {"RelayDrone",new[]{.36f,.68f}},
        {"StormSkimmer",new[]{.4f,.74f}}, {"ArcLancer",new[]{.43f,.77f}},
        {"PursuitFighter",new[]{.37f,.73f}}, {"AttackDrone",new[]{.32f,.66f}},
        {"CommandGunship",new[]{.42f,.75f}}, {"CourierDiver",new[]{.38f,.71f}},
        {"SandEmitter",new[]{.39f,.7f}}, {"SandStriker",new[]{.36f,.72f}},
        {"SandWorm",new[]{.43f,.76f}}, {"BonbonSkirmisher",new[]{.34f,.68f}},
        {"WaferDart",new[]{.38f,.73f}}, {"MintSpinner",new[]{.4f,.74f}},
        {"OrbitalScout",new[]{.29f,.47f,.71f}}, {"LaserSatellite",new[]{.36f,.71f}},
        {"OrbitalCarrier",new[]{.32f,.56f,.76f}}, {"OrbitalMoonlet",new[]{.41f,.72f}},
        {"ReefDart",new[]{.38f,.72f}}, {"MantaRay",new[]{.43f,.77f}},
        {"Bathysphere",new[]{.44f,.76f}}, {"SkullRevenant",new[]{.32f,.52f,.74f}},
        {"GhostGalleon",new[]{.34f,.59f,.77f}}, {"CinderImp",new[]{.39f,.74f}}
    };
    public static float[] For(string enemy) => scores.TryGetValue(enemy, out var result) ? result : null;
}
