using UnityEngine;
using System;
using System.Collections.Generic;

public static class DistrictWaves
{
    // Explicit types keep runtime factories safe under managed code stripping.
    public static readonly Type[] EnemyTypes={typeof(ScoutInterceptor),typeof(PatrolGunship),typeof(HeavyBomber),typeof(WallTurret),typeof(MaintenanceDrone),typeof(ArmoredCarrier),typeof(ColdWeatherFighter),typeof(SiegeGunship),typeof(ShieldTransport),typeof(BeamDrone),typeof(RelayDrone),typeof(StormSkimmer),typeof(PursuitFighter),typeof(AttackDrone),typeof(CommandGunship),typeof(CourierDiver),typeof(AvalancheBomber),typeof(ArcLancer),typeof(FieldsHeavyWing),typeof(TunnelDefensePlatform),typeof(SnowSiegeCraft),typeof(StormReactor),typeof(CityCommandCarrier)};
    public static Type EnemyType(string name)=>Array.Find(EnemyTypes,t=>t.Name==name);
    public static Type BossType
    {
        get
        {
            switch(LevelRun.Current)
            {
                case LevelId.Tunnel:return typeof(TunnelSawBoss);
                case LevelId.Snow:return typeof(SnowSiegeBoss);
                case LevelId.Thunder:return typeof(ThunderLanceBoss);
                case LevelId.City:return typeof(CityDuoBoss);
                default:return typeof(FieldsBomberBoss);
            }
        }
    }
    // Ten combat phrases (including the miniboss), spanning the full difficulty ramp.
    public static string[] Sequence()=>new[]{"2","4","6","reward","8","10","12",
        LevelRun.Stage%2==1 ? "scoreShop" : "multiplierShop","14","miniboss","17","20","boss"};
    public static Wave Miniboss()
    {
        string[] types={"FieldsHeavyWing","TunnelDefensePlatform","SnowSiegeCraft","StormReactor","CityCommandCarrier"};
        return new Wave(6,35,1,ConflictGroup.cg.center,new List<WaveUnit>{new WaveUnit(types[(int)LevelRun.Current],0,new Vector3(0,1.2f))});
    }
    public static Wave Create(int budget)
    {
        return WaveComposer.Create(budget);
    }
}
