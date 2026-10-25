using System;
using System.Collections.Generic;
using UnityEngine;

public enum WatcherView { Waves, Formations, Enemies, Bosses, Projectiles }

// Explicit runtime types preserve every preview in player builds with code stripping.
public static class WatcherCatalog
{
    public sealed class Entry
    {
        public string Id,Name;
        public int Encounter;
        public Type Enemy;
        public WavePart Part;
        public Action<Vector3,Transform> Fire;
    }
    public static Type[] Enemies(LevelId level)
    {
        switch(level)
        {
            case LevelId.Hell:return new[]{typeof(SkullRevenant),typeof(GhostGalleon),typeof(CinderImp)};
            case LevelId.Underwater:return new[]{typeof(ReefDart),typeof(MantaRay),typeof(Bathysphere)};
            case LevelId.Space:return new[]{typeof(OrbitalScout),typeof(LaserSatellite),typeof(OrbitalCarrier),typeof(OrbitalMoonlet)};
            case LevelId.Candyland:return new[]{typeof(BonbonSkirmisher),typeof(WaferDart),typeof(MintSpinner)};
            case LevelId.Desert:return new[]{typeof(SandEmitter),typeof(SandStriker),typeof(SandWorm)};
            case LevelId.Fields:return new[]{typeof(ScoutInterceptor),typeof(PatrolGunship),typeof(HeavyBomber),typeof(FieldsHeavyWing),typeof(MeadowTank)};
            case LevelId.Tunnel:return new[]{typeof(WallTurret),typeof(MaintenanceDrone),typeof(ArmoredCarrier),typeof(TunnelDefensePlatform)};
            case LevelId.Snow:return new[]{typeof(ColdWeatherFighter),typeof(SiegeGunship),typeof(ShieldTransport),typeof(AvalancheBomber),typeof(SnowSiegeCraft),typeof(FrostAuger)};
            case LevelId.Thunder:return new[]{typeof(BeamDrone),typeof(RelayDrone),typeof(StormSkimmer),typeof(ArcLancer),typeof(StormReactor)};
            default:return new[]{typeof(PursuitFighter),typeof(AttackDrone),typeof(CommandGunship),typeof(CourierDiver),typeof(CityCommandCarrier)};
        }
    }
    public static List<Entry> For(WatcherView view,LevelId level)
    {
        var entries=new List<Entry>();
        switch(view)
        {
            case WatcherView.Waves:
                var run=DistrictWaves.Sequence();
                for(int i=WaveWatcher.FirstWave;i<=WaveWatcher.LastWave;i++)
                {
                    bool inRun=Array.IndexOf(run,i.ToString())>=0;
                    entries.Add(new Entry{Id="wave/"+i,Name="WAVE "+i+(inRun ? "" : "  /  EXTRA"),Encounter=i});
                }
                break;
            case WatcherView.Formations:
                foreach(var part in WavePartLibrary.For(level))entries.Add(new Entry{Id="formation/"+part.Name,Name=part.Name,Part=part});
                break;
            case WatcherView.Enemies:
                foreach(var enemy in Enemies(level))entries.Add(new Entry{Id="enemy/"+enemy.Name,Name=Pretty(enemy.Name),Enemy=enemy});
                break;
            case WatcherView.Bosses:
                entries.Add(new Entry{Id="miniboss",Name="MINIBOSS",Encounter=WaveWatcher.Miniboss});
                entries.Add(new Entry{Id="boss/phase-1",Name=level==LevelId.City ? "BOTH HULLS" : "BOSS / PHASE 1",Encounter=WaveWatcher.BossOne});
                entries.Add(new Entry{Id="boss/phase-2",Name=level==LevelId.City ? "HEAVY SURVIVOR" : "BOSS / PHASE 2",Encounter=WaveWatcher.BossTwo});
                if(level==LevelId.City)entries.Add(new Entry{Id="boss/fast-survivor",Name="FAST SURVIVOR",Encounter=WaveWatcher.BossOther});
                break;
            case WatcherView.Projectiles:
                Add<SimpleRound>(entries);Add<NeedleRound>(entries);Add<BurstShell>(entries,.38f);
                Add<ScatterCarrier>(entries,.3f);Add<GuidedMissile>(entries);Add<HelixRound>(entries);
                Add<PlasmaOrb>(entries,.3f);Add<ReturningDisc>(entries,.28f);Add<ProximityMine>(entries,.3f);
                Add<RicochetBolt>(entries);Add<SuspendedRound>(entries);Add<LaserSideRound>(entries);
                Beam(entries,"beam", "ATTACHED BEAM",0,false);
                Beam(entries,"sweep", "SWEEPING BEAM",28,false);
                Beam(entries,"side-ports", "BEAM / SIDE PORTS",0,true);
                break;
        }
        return entries;
    }
    static void Add<T>(List<Entry> entries,float radius=.16f) where T:CombatRound
    {
        entries.Add(new Entry{Id="projectile/"+typeof(T).Name,Name=Pretty(typeof(T).Name),Fire=(p,emitter)=>
        {
            for(int i=-1;i<=1;i++)CombatRound.Launch<T>(p,CombatVisuals.Rotate(Vector2.down,i*19),4,radius);
        }});
    }
    static void Beam(List<Entry> entries,string id,string name,float sweep,bool sideShots)
    {
        entries.Add(new Entry{Id="projectile/"+id,Name=name,Fire=(p,emitter)=>
        {
            var beam=CombatRound.Launch<PulseLaser>(p,CombatVisuals.Rotate(Vector2.down,-sweep*.35f),0);
            if(beam!=null){beam.Bind(emitter,new Vector3(0,-.28f,-.24f));beam.sweep=sweep;beam.sideShots=sideShots;beam.activeTime=sideShots ? 2 : .9f;}
        }});
    }
    public static string Pretty(string name)=>System.Text.RegularExpressions.Regex.Replace(name,"(?<=[a-z0-9])(?=[A-Z])"," ");
}
