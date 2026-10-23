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
    public static string[] Sequence()=>new[]{"2","3","4","5","6","7","reward","8","9","10","11","12",
        LevelRun.Stage%2==1 ? "scoreShop" : "multiplierShop","13","14","15","miniboss","16","17","18","19","20","boss"};
    public static Wave Miniboss()
    {
        string[] types={"FieldsHeavyWing","TunnelDefensePlatform","SnowSiegeCraft","StormReactor","CityCommandCarrier"};
        return new Wave(6,35,1,ConflictGroup.cg.center,new List<WaveUnit>{new WaveUnit(types[(int)LevelRun.Current],0,new Vector3(0,1.2f))});
    }
    public static Wave Create(int budget)
    {
        var units=new List<WaveUnit>();int phrase=budget%4;
        float side=UnityEngine.Random.value<.5f ? -1 : 1;
        int extra=Mathf.Min(2,LevelRun.Stage-1);
        void Flight(string type,float at,FlightPhrase path,float mirror,float lane=0,float duration=6,float beat=1.2f)
        {
            units.Add(new WaveUnit(type,at,new Vector3(lane,1.12f)){phrase=path,side=mirror,lane=lane,flightDuration=duration*.76f,fireEvery=beat*.82f});
        }
        void Train(string type,float at,FlightPhrase path,float mirror,int count,float interval=.22f,float duration=5.8f,float beat=1.15f,float lane=0)
        {if(type=="ScoutInterceptor")interval=Mathf.Max(interval,.38f);for(int i=0;i<count;i++)Flight(type,at+i*interval,path,mirror,lane,duration,beat);}
        void Row(string type,float at,int count,float spacing,float duration=6.2f,float beat=1.4f)
        {for(int i=0;i<count;i++)Flight(type,at,FlightPhrase.Chevron,side,(i-(count-1)*.5f)*spacing,duration,beat);}
        switch(LevelRun.Current)
        {
            case LevelId.Fields:return PlainsChoreography.Create(budget);
            case LevelId.Tunnel:
                Flight("WallTurret",0,FlightPhrase.Battery,1,-.83f,7,1.5f);
                Flight("WallTurret",.75f,FlightPhrase.Battery,1,.83f,7,1.5f);
                if(phrase%2==0)
                {Train("MaintenanceDrone",.8f,FlightPhrase.Orbit,side,4+extra,.33f,6.1f,1.5f);}
                else
                {Flight("ArmoredCarrier",0,FlightPhrase.Battery,1,0,7,2.2f);Train("MaintenanceDrone",1.5f,FlightPhrase.Crossing,side,4,.28f,5.5f,1.3f);}
                break;
            case LevelId.Snow:return SnowChoreography.Create(budget);
            case LevelId.Thunder:
                if(phrase==0)
                {
                    units.Add(new WaveUnit("RelayDrone",0,new Vector3(-.55f,1.12f)));
                    units.Add(new WaveUnit("RelayDrone",0,new Vector3(.55f,1.12f)));
                }
                else
                {
                    Flight(phrase==3 ? "ArcLancer" : "BeamDrone",0,FlightPhrase.Battery,1,-.6f,6.7f,2.6f);
                    Flight("BeamDrone",1.3f,FlightPhrase.Battery,1,.6f,6.7f,2.6f);
                }
                Train("StormSkimmer",.9f,FlightPhrase.Ribbon,side,5+extra,.26f,5.8f,1.5f);
                break;
            case LevelId.City:
                if(phrase%2==0)
                {
                    Train("CourierDiver",0,FlightPhrase.Dive,side,7+extra,.16f,3.7f,1.1f);
                    Train("CourierDiver",.65f,FlightPhrase.Dive,-side,7+extra,.16f,3.7f,1.1f);
                    Train("AttackDrone",1.9f,FlightPhrase.Ribbon,side,5,.2f,4.6f,1.25f);
                }
                else
                {
                    Flight(phrase==1 ? "PursuitFighter" : "CommandGunship",0,FlightPhrase.Battery,1,0,6.2f,2.4f);
                    Train("AttackDrone",0,FlightPhrase.Orbit,side,5+extra,.24f,5.3f,1.3f);
                    Train("CourierDiver",2.1f,FlightPhrase.Crossing,-side,6,.16f,3.9f,1.1f);
                }
                break;
        }
        units.Sort((a,b)=>a.time.CompareTo(b.time));
        return new Wave(budget,LevelRun.Current==LevelId.City ? 3.4f : 4.4f,1,ConflictGroup.cg.center,units);
    }
}
