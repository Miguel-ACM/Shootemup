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
    public static string[] Sequence()=>new[]{"2","3","reward","4","scoreShop","4","miniboss","multiplierShop","5","6","reward","boss"};
    public static Wave Miniboss()
    {
        string[] types={"FieldsHeavyWing","TunnelDefensePlatform","SnowSiegeCraft","StormReactor","CityCommandCarrier"};
        return new Wave(6,35,1,ConflictGroup.cg.center,new List<WaveUnit>{new WaveUnit(types[(int)LevelRun.Current],0,new Vector3(0,1.2f))});
    }
    public static Wave Create(int budget)
    {
        var units=new List<WaveUnit>();int pattern=UnityEngine.Random.Range(0,4);
        float side=UnityEngine.Random.value<.5f ? -1 : 1;
        int support=Mathf.Clamp(2+budget/2+Mathf.Min(3,LevelRun.Stage-1),3,7);
        void Add(string type,float time,float x,float y=1.15f)=>units.Add(new WaveUnit(type,time,new Vector3(x,y)));
        switch(LevelRun.Current)
        {
            case LevelId.Fields:
                Add(pattern==0 ? "PatrolGunship" : pattern==1 ? "HeavyBomber" : "ScoutInterceptor",0,side*.4f);
                for(int i=0;i<support+1;i++)Add("ScoutInterceptor",1+i*.55f,-side*.55f);
                if(pattern==3)Add("HeavyBomber",6,side*.5f);
                break;
            case LevelId.Tunnel:
                Add(pattern==0 ? "ArmoredCarrier" : "WallTurret",0,side*.82f);
                Add("MaintenanceDrone",2,-side*.4f);
                if(pattern==2)Add("WallTurret",4,-side*.82f);
                if(pattern==3)Add("ArmoredCarrier",4,0);
                for(int i=0;i<support;i++)Add(i%2==0 ? "MaintenanceDrone" : "WallTurret",5+i*1.05f,(i%2==0 ? -.7f : .7f));
                break;
            case LevelId.Snow:
                Add(pattern==0 ? "ShieldTransport" : "AvalancheBomber",0,side*.42f);
                Add(pattern==2 ? "AvalancheBomber" : "SiegeGunship",3,-side*.45f);
                for(int i=0;i<Mathf.Min(4,support);i++)
                {Add("ColdWeatherFighter",5+i*1.3f,-.5f);Add("ColdWeatherFighter",5.65f+i*1.3f,.5f);}
                break;
            case LevelId.Thunder:
                if(pattern==0){Add("RelayDrone",0,-.5f);Add("RelayDrone",0,.5f);Add("BeamDrone",4,0);}
                else {Add(pattern==3 ? "ArcLancer" : "BeamDrone",0,side*.5f);Add("StormSkimmer",3,-side*.5f);}
                for(int i=0;i<support;i++)Add(i%3==0 ? "ArcLancer" : "StormSkimmer",4+i*1.2f,(i%2==0 ? -.6f : .6f));
                break;
            case LevelId.City:
                Add(pattern==0 ? "CommandGunship" : "PursuitFighter",0,side*.45f);
                for(int i=0;i<support+6;i++)Add(i%3==0 ? "AttackDrone" : "CourierDiver",1+i*.55f,(i%2==0 ? -1 : 1)*.6f);
                Add("PursuitFighter",5,-side*.5f);
                Add(pattern==2 ? "CommandGunship" : "AttackDrone",8,side*.4f);
                break;
        }
        units.Sort((a,b)=>a.time.CompareTo(b.time));
        return new Wave(budget,18,1,ConflictGroup.cg.center,units);
    }
}
