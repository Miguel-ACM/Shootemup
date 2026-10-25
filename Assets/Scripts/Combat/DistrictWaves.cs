using UnityEngine;
using System;
using System.Collections.Generic;

public static class DistrictWaves
{
    // Explicit types keep runtime factories safe under managed code stripping.
    public static readonly Type[] EnemyTypes={typeof(ScoutInterceptor),typeof(PatrolGunship),typeof(HeavyBomber),typeof(WallTurret),typeof(MaintenanceDrone),typeof(ArmoredCarrier),typeof(ColdWeatherFighter),typeof(SiegeGunship),typeof(ShieldTransport),typeof(BeamDrone),typeof(RelayDrone),typeof(StormSkimmer),typeof(PursuitFighter),typeof(AttackDrone),typeof(CommandGunship),typeof(CourierDiver),typeof(AvalancheBomber),typeof(ArcLancer),typeof(FieldsHeavyWing),typeof(TunnelDefensePlatform),typeof(SnowSiegeCraft),typeof(StormReactor),typeof(CityCommandCarrier),typeof(SandEmitter),typeof(SandStriker),typeof(SandWorm),typeof(BonbonSkirmisher),typeof(WaferDart),typeof(MintSpinner)};
    public static Type EnemyType(string name)=>Array.Find(EnemyTypes,t=>t.Name==name);
    public static Type BossType
    {
        get
        {
            switch(LevelRun.Current)
            {
                case LevelId.Candyland:return typeof(CandyRegentBoss);
                case LevelId.Desert:return typeof(DesertWormBoss);
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
        if(LevelRun.Current==LevelId.Candyland)return WaveComposer.Create(16);
        if(LevelRun.Current==LevelId.Desert)
        {
            var brood=new List<WaveUnit>();
            for(int i=0;i<3;i++)brood.Add(new WaveUnit(nameof(SandWorm),i*3.2f,new Vector3((i%2==0 ? -1 : 1)*.54f,1.2f,1)));
            return new Wave(6,19,1,ConflictGroup.cg.center,brood){parts=new[]{"Buried brood"}};
        }
        string[] types={"FieldsHeavyWing","TunnelDefensePlatform","SnowSiegeCraft","StormReactor","CityCommandCarrier"};
        return new Wave(6,35,1,ConflictGroup.cg.center,new List<WaveUnit>{new WaveUnit(types[(int)LevelRun.Current],0,new Vector3(0,1.2f))});
    }
    public static Wave Create(int budget)
    {
        return WaveComposer.Create(budget);
    }
}
