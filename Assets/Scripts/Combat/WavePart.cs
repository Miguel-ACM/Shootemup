using System;
using System.Collections.Generic;
using UnityEngine;

public enum WaveShape { Train, Rank, Rake, Chevron, Diamond, Solo }

// A whole scored formation. Variation changes the group; never independent wandering ships.
public sealed class WavePart
{
    public readonly string Name,Enemy;
    public readonly FlightCurve Route;
    public readonly WaveShape Shape;
    public readonly int Count,Unlock;
    public readonly float Seconds;
    public bool Ground=>Enemy=="MeadowTank" || Enemy=="FrostAuger";
    public WavePart(string name,string enemy,FlightCurve route,WaveShape shape,int count,float seconds,int unlock=2)
    {Name=name;Enemy=enemy;Route=route;Shape=shape;Count=count;Seconds=seconds;Unlock=unlock;}
    public List<WaveUnit> Generate(int variant,float side)
    {
        float radius=WaveGeometry.Radius(Enemy);
        float gap=(2*radius+.3f)/Mathf.Max(.1f,Route.LaneScale);
        float depth=(2*radius+.3f)/(Camera.UpLimit-Camera.DownLimit);
        float interval=Mathf.Max(.30f,(2*radius+.32f)/Mathf.Max(1,Route.Length/Seconds)*1.25f)*(1+(variant/2%3)*.12f);
        int count=Shape==WaveShape.Solo ? 1 : Mathf.Max(Count==2 ? 2 : 3,Count+variant%3-1+Mathf.Min(1,(LevelRun.Stage-1)/3));
        WaveShape shape=Shape;
        if(Ground){count=Mathf.Min(4,Count+variant%2);interval=Mathf.Max(interval,.80f);}
        // Two authored arrangements for each motif, with shared routes and score.
        if((variant&1)!=0 && !Ground)
            shape=Shape==WaveShape.Train ? WaveShape.Rake : Shape==WaveShape.Rake ? WaveShape.Train : Shape==WaveShape.Chevron ? WaveShape.Diamond : Shape==WaveShape.Diamond ? WaveShape.Chevron : Shape;
        if(!Route.HorizontalLanes && (shape==WaveShape.Chevron || shape==WaveShape.Diamond))shape=WaveShape.Rake;
        var units=new List<WaveUnit>();
        float lane=Ground ? 0 : (variant/3%3-1)*.045f;
        void Add(float at,float offset,float z=0)
        {
            units.Add(new WaveUnit(Enemy,at,Vector3.zero){phrase=FlightPhrase.Flow,curve=Route,side=side,lane=lane+offset,
                formationDepth=z,fixedDepth=true,flightDuration=Seconds,firePhase=at,syncFire=true,part=Name});
        }
        switch(shape)
        {
            case WaveShape.Train:for(int i=0;i<count;i++)Add(i*interval,0);break;
            case WaveShape.Rank:for(int i=0;i<count;i++)Add(0,(i-(count-1)*.5f)*gap);break;
            case WaveShape.Rake:for(int i=0;i<count;i++)Add(0,(i-(count-1)*.5f)*gap,(i-(count-1)*.5f)*depth*.4f);break;
            case WaveShape.Chevron:
                Add(0,0,-depth);int wings=Count>=5 ? 2 : 1;
                for(int i=1;i<=wings;i++)foreach(int wing in new[]{-1,1})Add(0,wing*i*gap,(i-1)*depth);break;
            case WaveShape.Diamond:Add(0,0,-depth);Add(0,-gap);Add(0,gap);Add(0,0,depth);break;
            default:Add(0,0);break;
        }
        float last=units[units.Count-1].time;
        float first=Mathf.Max(Seconds*.31f,last+.72f);
        // Call, rest, answer. A train shares accents even with staggered entrances.
        float[] score={first,first+Seconds*.15f,first+Seconds*.31f};
        if(Ground)score=new[]{1.35f,2.45f,3.55f,last+1.35f};
        if(Enemy=="MeadowTank")score=new[]{.95f,1.60f,2.25f,last+.95f};
        if(Enemy=="BonbonSkirmisher" || Enemy=="WaferDart" || Enemy=="MintSpinner")
            score=new[]{Seconds*.36f,Seconds*.65f,last+Seconds*.48f};
        if(Enemy=="OrbitalScout")score=new[]{Seconds*.24f,Seconds*.34f,Seconds*.48f,Seconds*.61f,Seconds*.72f};
        if(Enemy=="OrbitalCarrier")score=new[]{Seconds*.25f,Seconds*.39f,Seconds*.58f,Seconds*.72f};
        if(Enemy=="LaserSatellite")score=new[]{Seconds*.28f,Seconds*.51f,Seconds*.72f};
        EnemyTuning.TuneFormation(Enemy,units,Seconds,ref score);
        foreach(var unit in units)unit.fireTimes=score;
        return units;
    }
}

// Full mesh spheres contain the banked/turning models, rather than their smaller damage colliders.
public static class WaveGeometry
{
    public const float Step=.05f,Clearance=.12f;
    static readonly Dictionary<string,float> radii=new Dictionary<string,float>();
    public static float Radius(string enemy)
    {
        if(radii.TryGetValue(enemy,out float value))return value;
        int hull;float scale;
        switch(enemy)
        {
            case "OrbitalScout":hull=41;scale=.46f;break;
            case "LaserSatellite":hull=42;scale=.56f;break;
            case "OrbitalMoonlet":hull=44;scale=.28f;break;
            case "OrbitalCarrier":return radii[enemy]=1.46f; // Includes both orbiting, destroyable escorts.
            case "MeadowTank":hull=39;scale=.68f;break;
            case "FrostAuger":hull=40;scale=.64f;break;
            case "BonbonSkirmisher":hull=35;scale=.38f;break;
            case "WaferDart":hull=36;scale=.39f;break;
            case "MintSpinner":hull=37;scale=.42f;break;
            case "SandEmitter":hull=30;scale=.63f;break;
            case "SandStriker":hull=31;scale=.52f;break;
            // The small worm's trailing body needs its own reserved corridor through a phrase.
            case "SandWorm":return radii[enemy]=2.55f;
            case "ScoutInterceptor":hull=0;scale=.48f;break;case "PatrolGunship":hull=1;scale=.7f;break;
            case "HeavyBomber":hull=2;scale=.9f;break;case "WallTurret":hull=3;scale=.55f;break;
            case "MaintenanceDrone":hull=4;scale=.5f;break;case "ArmoredCarrier":hull=5;scale=.9f;break;
            case "ColdWeatherFighter":hull=6;scale=.62f;break;case "SiegeGunship":hull=7;scale=.9f;break;
            case "ShieldTransport":hull=8;scale=.95f;break;case "BeamDrone":hull=9;scale=.55f;break;
            case "RelayDrone":hull=10;scale=.55f;break;case "StormSkimmer":hull=11;scale=.55f;break;
            case "PursuitFighter":hull=12;scale=.45f;break;case "AttackDrone":hull=13;scale=.38f;break;
            case "CommandGunship":hull=14;scale=.8f;break;case "CourierDiver":hull=15;scale=.34f;break;
            case "AvalancheBomber":hull=17;scale=.85f;break;case "ArcLancer":hull=18;scale=.62f;break;
            default:throw new ArgumentException("Missing wave footprint: "+enemy);
        }
        foreach(var vertex in IndustrialMeshFactory.CombatHull(hull).vertices)value=Mathf.Max(value,vertex.magnitude*scale);
        return radii[enemy]=value+.035f;
    }
    public static Vector2 Position(WaveUnit unit,float clock)
    {
        float parameter=unit.curve.TravelParameter(Mathf.Clamp01(clock/unit.flightDuration));
        var p=unit.curve.Sample(parameter,unit.side,unit.lane);
        p.y+=unit.formationDepth*(unit.fixedDepth ? 1 : Mathf.Sin(Mathf.PI*parameter));
        return new Vector2(p.x*Camera.RightLimit,Mathf.LerpUnclamped(Camera.DownLimit,Camera.UpLimit,p.y));
    }
    public sealed class Track
    {
        public readonly WaveUnit Unit;
        public readonly float Radius;
        public float Start;
        readonly Vector2[] points;
        public readonly float MinX,MaxX,MinY,MaxY;
        public float End=>Start+Unit.flightDuration;
        public Track(Track source)
        {
            Unit=source.Unit;Start=Unit.time;Radius=source.Radius;points=source.points;
            MinX=source.MinX;MinY=source.MinY;MaxX=source.MaxX;MaxY=source.MaxY;
        }
        public Track(WaveUnit unit)
        {
            Unit=unit;Start=unit.time;Radius=WaveGeometry.Radius(unit.enemy);
            int count=Mathf.CeilToInt(unit.flightDuration/Step);points=new Vector2[count+1];
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            for(int i=0;i<points.Length;i++)
            {
                var p=points[i]=Position(unit,i*unit.flightDuration/count);
                if(!Visible(p,Radius))continue;
                minX=Mathf.Min(minX,p.x-Radius-Clearance);maxX=Mathf.Max(maxX,p.x+Radius+Clearance);
                minY=Mathf.Min(minY,p.y-Radius-Clearance);maxY=Mathf.Max(maxY,p.y+Radius+Clearance);
            }
            MinX=minX;MinY=minY;MaxX=maxX;MaxY=maxY;
        }
        public Vector2 At(float time)
        {
            float index=Mathf.Clamp01((time-Start)/Unit.flightDuration)*(points.Length-1);
            int i=Mathf.Min(points.Length-2,Mathf.FloorToInt(index));return Vector2.Lerp(points[i],points[i+1],index-i);
        }
    }
    public static List<Track> Tracks(List<WaveUnit> units)
    {var tracks=new List<Track>();foreach(var unit in units)tracks.Add(new Track(unit));return tracks;}
    public static ulong[] Occupancy(List<Track> tracks)
    {
        float end=0;foreach(var track in tracks)end=Mathf.Max(end,track.End);
        var mask=new ulong[(Mathf.CeilToInt(end/Step)+1)*64];
        float xScale=64/(Camera.RightLimit-Camera.LeftLimit),yScale=64/(Camera.UpLimit-Camera.DownLimit);
        foreach(var track in tracks)
        {
            int first=Mathf.Max(0,Mathf.FloorToInt(track.Start/Step)),last=Mathf.CeilToInt(track.End/Step);
            float r=track.Radius+Clearance+.02f;
            for(int frame=first;frame<=last;frame++)
            {
                Vector2 a=track.At(frame*Step),b=track.At((frame+1)*Step);
                float x0=Mathf.Min(a.x,b.x)-r,x1=Mathf.Max(a.x,b.x)+r,y0=Mathf.Min(a.y,b.y)-r,y1=Mathf.Max(a.y,b.y)+r;
                if(x1<Camera.LeftLimit || x0>Camera.RightLimit || y1<Camera.DownLimit || y0>Camera.UpLimit)continue;
                int left=Mathf.Clamp(Mathf.FloorToInt((x0-Camera.LeftLimit)*xScale),0,63),right=Mathf.Clamp(Mathf.FloorToInt((x1-Camera.LeftLimit)*xScale),0,63);
                int bottom=Mathf.Clamp(Mathf.FloorToInt((y0-Camera.DownLimit)*yScale),0,63),top=Mathf.Clamp(Mathf.FloorToInt((y1-Camera.DownLimit)*yScale),0,63);
                ulong row=(ulong.MaxValue<<left)&(ulong.MaxValue>>(63-right));
                for(int y=bottom;y<=top;y++)mask[frame*64+y]|=row;
            }
        }
        return mask;
    }
    public static bool Fits(ulong[] candidate,int[] filled,float at,ulong[] occupied)
    {
        int offset=Mathf.RoundToInt(at/Step)*64;
        int count=Mathf.Min(candidate.Length,occupied.Length-offset);
        foreach(int i in filled){if(i>=count)break;if((candidate[i]&occupied[i+offset])!=0)return false;}
        return true;
    }
    public static void Reserve(ulong[] candidate,float at,ref ulong[] occupied)
    {
        int offset=Mathf.RoundToInt(at/Step)*64;
        if(occupied.Length<offset+candidate.Length)Array.Resize(ref occupied,offset+candidate.Length);
        for(int i=0;i<candidate.Length;i++)occupied[i+offset]|=candidate[i];
    }
    public static bool Visible(Vector2 p,float r)=>p.x+r>Camera.LeftLimit && p.x-r<Camera.RightLimit && p.y+r>Camera.DownLimit && p.y-r<Camera.UpLimit;
    public static bool Overlap(Track a,Track b)
    {
        float start=Mathf.Max(a.Start,b.Start),end=Mathf.Min(a.End,b.End);
        if(start>=end)return false;
        if(a.MaxX<b.MinX || b.MaxX<a.MinX || a.MaxY<b.MinY || b.MaxY<a.MinY)return false;
        float radius=a.Radius+b.Radius+Clearance;
        Vector2 a0=a.At(start),b0=b.At(start);
        for(float t=start;t<end;t+=Step)
        {
            float next=Mathf.Min(end,t+Step);Vector2 a1=a.At(next),b1=b.At(next);
            if((Visible(a0,a.Radius)||Visible(a1,a.Radius)) && (Visible(b0,b.Radius)||Visible(b1,b.Radius)))
            {
                // Closest approach during the whole interval catches fast crossings between samples.
                Vector2 relative=a0-b0,delta=(a1-b1)-relative;
                float u=delta.sqrMagnitude>.000001f ? Mathf.Clamp01(-Vector2.Dot(relative,delta)/delta.sqrMagnitude) : 0;
                if((relative+delta*u).sqrMagnitude<radius*radius)return true;
            }
            a0=a1;b0=b1;
        }
        return false;
    }
    public static bool Clear(List<Track> tracks)
    {for(int i=0;i<tracks.Count;i++)for(int j=0;j<i;j++)if(Overlap(tracks[i],tracks[j]))return false;return true;}
    public static bool Compatible(List<Track> first,List<Track> second)
    {foreach(var a in first)foreach(var b in second)if(Overlap(a,b))return false;return true;}
}
