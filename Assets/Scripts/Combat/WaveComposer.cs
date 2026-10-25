using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

// Seeded bags choose complete authored parts; swept model envelopes schedule their shared space.
public static class WaveComposer
{
    sealed class Motion
    {
        public WavePart Part;public List<WaveUnit> Units;public List<WaveGeometry.Track> Tracks;public float At;
        public float Span;
        public int Variant;
        public ulong[] Occupancy;public int[] Filled;
        public Motion Copy()
        {
            var copies=new List<WaveGeometry.Track>();foreach(var track in Tracks)copies.Add(new WaveGeometry.Track(track));
            return new Motion{Part=Part,Units=Units,Tracks=copies,Occupancy=Occupancy,Filled=Filled,Span=Span,Variant=Variant};
        }
        public void Place(float at){At=Mathf.Round(at/WaveGeometry.Step)*WaveGeometry.Step;foreach(var track in Tracks)track.Start=track.Unit.time+At;}
    }
    static readonly Dictionary<int,Wave> waves=new Dictionary<int,Wave>();
    static readonly Dictionary<string,int> uses=new Dictionary<string,int>();
    static readonly Dictionary<(WavePart,int,int),Motion> patterns=new Dictionary<(WavePart,int,int),Motion>();
    static int seed=-1,stage=-1,tuningRevision=-1;static LevelId level;static float width,height,tempo;
    public static float Tempo=>Mathf.Max(.01f,PatternMover.FlightTempo*GameRules.enemySpeed*LevelRun.EnemyTempoScale);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){waves.Clear();uses.Clear();patterns.Clear();seed=-1;stage=-1;}
    static int Hash(int value,int salt)
    {
        unchecked{uint x=(uint)value^(uint)salt*0x9e3779b9u;x^=x>>16;x*=0x7feb352du;x^=x>>15;x*=0x846ca68bu;return (int)((x^(x>>16))&0x7fffffffu);}
    }
    static void EnsureContext()
    {
        if(tuningRevision!=EnemyTuning.Revision){patterns.Clear();waves.Clear();uses.Clear();tuningRevision=EnemyTuning.Revision;}
        bool geometry=stage!=LevelRun.Stage || level!=LevelRun.Current || !Mathf.Approximately(width,Camera.RightLimit) || !Mathf.Approximately(height,Camera.UpLimit-Camera.DownLimit);
        if(geometry)patterns.Clear();
        if(seed!=LevelRun.Seed || stage!=LevelRun.Stage || level!=LevelRun.Current || !Mathf.Approximately(width,Camera.RightLimit) || !Mathf.Approximately(height,Camera.UpLimit-Camera.DownLimit) || !Mathf.Approximately(tempo,Tempo))
        {
            waves.Clear();uses.Clear();seed=LevelRun.Seed;stage=LevelRun.Stage;level=LevelRun.Current;
            width=Camera.RightLimit;height=Camera.UpLimit-Camera.DownLimit;tempo=Tempo;
        }
    }
    public static IEnumerator Prepare()
    {
        EnsureContext();var district=level;int difficulty=stage;
        var watch=System.Diagnostics.Stopwatch.StartNew();
        foreach(var part in WavePartLibrary.For(district))for(int variant=0;variant<=12;variant++)foreach(int side in new[]{-1,1})
        {
            if(LevelRun.Current!=district || LevelRun.Stage!=difficulty)yield break;
            EnsureContext();
            Make(part,variant,side);
            if(watch.Elapsed.TotalMilliseconds<2.5)continue;
            yield return null;watch.Restart();
        }
    }
    public static Wave Create(int budget)
    {
        EnsureContext();
        budget=Mathf.Clamp(budget,2,20);
        // Building in run order keeps previews independent of which button was clicked first.
        for(int i=2;i<=budget;i++)if(!waves.ContainsKey(i))waves[i]=Build(i);
        return (Wave)waves[budget].Clone();
    }
    static Wave Build(int budget)
    {
        for(int attempt=0;attempt<8;attempt++)
        {
            var wave=TryBuild(budget,attempt,false);if(wave!=null)return wave;
        }
        for(int attempt=8;attempt<32;attempt++)
        {
            var wave=TryBuild(budget,attempt,true);if(wave!=null)return wave;
        }
        throw new InvalidOperationException("No paced composition for "+level+" / "+seed+" / "+budget);
    }
    static Wave TryBuild(int budget,int attempt,bool compact)
    {
        var random=new System.Random(Hash(seed,stage*971+(int)level*137+budget*7919+attempt*104729));
        var previous=budget>2 ? new HashSet<string>(waves[budget-1].parts) : new HashSet<string>();
        var chosen=new HashSet<string>();var all=new List<WaveGeometry.Track>();var units=new List<WaveUnit>();var names=new List<string>();
        var occupied=Array.Empty<ulong>();
        int count=budget<5 || compact ? 2 : 3;bool heavy=false,groundUsed=false;
        if(level==LevelId.Candyland)count=budget<5 || compact ? 3 : 4;
        float window=compact ? 12.5f : budget<5 ? 8.6f : 11.5f;
        float slowest=1;foreach(var part in WavePartLibrary.For(level))slowest=Mathf.Min(slowest,EnemyTuning.MovementFor(part.Enemy));
        window/=slowest;
        for(int slot=0;slot<count;slot++)
        {
            var options=new List<(WavePart part,int score)>();
            foreach(var part in WavePartLibrary.For(level))
            {
                if(part.Unlock>budget || chosen.Contains(part.Name) || previous.Contains(part.Name) || (heavy && part.Shape==WaveShape.Solo) || (groundUsed && part.Ground))continue;
                uses.TryGetValue(part.Name,out int used);
                options.Add((part,used*24+random.Next(24)));
            }
            options.Sort((a,b)=>a.score.CompareTo(b.score));Motion best=null;
            // Prefer unused motifs that can enter soon, instead of always serializing incompatible routes.
            int inspected=0;
            foreach(var option in options)
            {
                if(inspected++>=5 && best!=null)break;
                int variant=random.Next(12);float side=random.Next(2)==0 ? 1 : -1;
                for(int variation=0;variation<2;variation++)
                {
                    var motion=Make(option.part,variant+variation,variation==0 ? side : -side);
                    if(motion==null)continue;
                    if(motion.Span>window || (compact && motion.Span>6.2f/slowest))continue;
                    if(slot==0){best=motion;break;}
                    float start=(slot-1)*.25f;
                    for(float at=start;at<=Mathf.Min((compact ? 6.25f : 5.5f)/slowest,window-motion.Span);at+=.25f)
                    {
                        motion.Place(at);
                        if(!WaveGeometry.Fits(motion.Occupancy,motion.Filled,at,occupied))continue;
                        if(best==null || motion.At<best.At)best=motion;
                        break;
                    }
                    if(best!=null && best.At<=start+1.75f)break;
                }
                if(best!=null && (slot==0 || best.At<=(slot-1)*.25f+1.75f))break;
            }
            if(best==null)return null; // Retry compatible parts rather than stretching the encounter.
            chosen.Add(best.Part.Name);names.Add(best.Part.Name);heavy|=best.Part.Shape==WaveShape.Solo;groundUsed|=best.Part.Ground;
            all.AddRange(best.Tracks);
            WaveGeometry.Reserve(best.Occupancy,best.At,ref occupied);
            foreach(var original in best.Units)
            {
                var unit=(WaveUnit)original.Clone();unit.time=(unit.time+best.At)/Tempo;
                // firePhase and score remain local to the part: the entire accent moves together.
                units.Add(unit);
            }
        }
        if(units.Count<5)return null;
        units.Sort((a,b)=>a.time.CompareTo(b.time));float duration=0;
        foreach(var name in names){uses.TryGetValue(name,out int total);uses[name]=total+1;}
        foreach(var track in all)duration=Mathf.Max(duration,track.End/Tempo);
        // The next wave cannot occupy the departing formation's space. Kills still permit early handoff.
        return new Wave(budget,duration,1,ConflictGroup.cg.center,units){parts=names.ToArray()};
    }
    // The watcher uses the same spacing retries and tempo as live compositions.
    public static Wave PreviewPart(WavePart part,int variant,int side,out int resolvedVariant)
    {
        EnsureContext();var motion=Make(part,variant,side);resolvedVariant=motion!=null ? motion.Variant : variant;
        if(motion==null)return null;
        var units=new List<WaveUnit>();
        foreach(var original in motion.Units){var unit=(WaveUnit)original.Clone();unit.time/=Tempo;units.Add(unit);}
        units.Sort((a,b)=>a.time.CompareTo(b.time));
        return new Wave(0,motion.Span/Tempo,1,ConflictGroup.cg.center,units){parts=new[]{part.Name}};
    }
    static Motion Make(WavePart part,int variant,float side)
    {
        var key=(part,variant,(int)side);
        if(patterns.TryGetValue(key,out var cached))return cached?.Copy();
        for(int retry=0;retry<4;retry++)
        {
            var units=part.Generate(variant+retry,side);var tracks=WaveGeometry.Tracks(units);
            bool offscreen=true;
            foreach(var track in tracks)if(WaveGeometry.Visible(track.At(track.Start),track.Radius) || WaveGeometry.Visible(track.At(track.End),track.Radius)){offscreen=false;break;}
            if(!offscreen)continue;
            if(WaveGeometry.Clear(tracks))
            {
                float span=0;foreach(var track in tracks)span=Mathf.Max(span,track.End);
                var template=new Motion{Part=part,Units=units,Tracks=tracks,Occupancy=WaveGeometry.Occupancy(tracks),Span=span,Variant=variant+retry};
                var filled=new List<int>();for(int i=0;i<template.Occupancy.Length;i++)if(template.Occupancy[i]!=0)filled.Add(i);
                template.Filled=filled.ToArray();patterns[key]=template;return template.Copy();
            }
        }
        patterns[key]=null;return null;
    }
}

public static partial class WavePartLibrary
{
    public static WavePart[] For(LevelId level)
    {
        switch(level)
        {
            case LevelId.Fields:return plainsWithGround ?? (plainsWithGround=WithGround(Plains,PlainsGround));
            case LevelId.Snow:return snowWithGround ?? (snowWithGround=WithGround(Snow,SnowGround));case LevelId.City:return City;
            case LevelId.Thunder:return Thunder;case LevelId.Tunnel:return Tunnel;
            case LevelId.Desert:return Desert;
            case LevelId.Candyland:return Candyland;
            case LevelId.Space:return Space;
            default:throw new ArgumentOutOfRangeException(nameof(level));
        }
    }
}
