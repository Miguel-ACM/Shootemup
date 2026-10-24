#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class FlightSmoothnessAudit
{
    sealed class Legacy
    {
        readonly Vector2[] knots;readonly float[] distances=new float[129];
        public float Length=>distances[128];
        public Legacy(FlightCurve curve)
        {
            knots=(Vector2[])typeof(FlightCurve).GetField("knots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(curve);
            var previous=At(0);for(int i=1;i<distances.Length;i++){var p=At(i/128f);distances[i]=distances[i-1]+Vector2.Distance(previous,p);previous=p;}
        }
        Vector2 At(float progress)
        {
            float index=Mathf.Clamp01(progress)*(knots.Length-1);int i=Mathf.Min(knots.Length-2,Mathf.FloorToInt(index));float t=index-i;
            var a=knots[i];var b=knots[i+1];var before=i>0 ? knots[i-1] : 2*a-b;var after=i+2<knots.Length ? knots[i+2] : 2*b-a;
            return World((2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*(b-before)*.5f+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*(after-a)*.5f);
        }
        public Vector2 Position(float progress)
        {
            float target=progress*Length;
            for(int i=1;i<distances.Length;i++)if(target<=distances[i])return At((i-1+Mathf.InverseLerp(distances[i-1],distances[i],target))/128f);
            return At(1);
        }
    }
    struct Measures {public float SpeedError,Jerk,Turn;}
    static Vector2 World(Vector2 p)=>Vector2.Scale(p,new Vector2(Camera.RightLimit,Camera.UpLimit-Camera.DownLimit));
    static Measures Measure(Func<float,Vector2> position,float length,float duration,int fps)
    {
        int frames=Mathf.CeilToInt(duration*fps);float dt=duration/frames,expected=length/duration;
        var p=position(0);Vector2 velocity=default,acceleration=default;var result=new Measures();
        for(int i=1;i<=frames;i++)
        {
            var next=position(i/(float)frames);var v=(next-p)/dt;
            result.SpeedError=Mathf.Max(result.SpeedError,Mathf.Abs(v.magnitude/expected-1));
            if(i>1)result.Turn=Mathf.Max(result.Turn,Vector2.Angle(v,velocity)/dt);
            var a=(v-velocity)/dt;if(i>2)result.Jerk=Mathf.Max(result.Jerk,(a-acceleration).magnitude/dt);
            p=next;velocity=v;acceleration=a;
        }
        return result;
    }
    public static List<string> Inspect()
    {
        var report=new List<string>();var done=new HashSet<FlightCurve>();float before=0,after=0,worst=0;
        foreach(LevelId level in Enum.GetValues(typeof(LevelId)))foreach(var part in WavePartLibrary.For(level))
        {
            var route=part.Route;if(!done.Add(route))continue;
            float seconds=WavePartLibrary.For(level).Where(x=>x.Route==route).Min(x=>x.Seconds)/PatternMover.FlightTempo;
            var old=new Legacy(route);Measures legacy=default,current=default;
            foreach(int fps in new[]{30,60,144})
            {
                var a=Measure(old.Position,old.Length,seconds,fps);
                var b=Measure(p=>World(route.Sample(route.TravelParameter(p))),route.Length,seconds,fps);
                if(fps==60){legacy=a;current=b;before+=a.Jerk;after+=b.Jerk;}
                worst=Mathf.Max(worst,b.SpeedError);
                if(b.Turn>600)throw new Exception(route.Name+" still whips through its turn at "+b.Turn+" degrees/s");
                if(b.SpeedError>.06f)throw new Exception(route.Name+" still surges at "+fps+"fps: "+b.SpeedError);
            }
            report.Add($"PASS {route.Name}: peak frame jerk {legacy.Jerk:F1} → {current.Jerk:F1}; turn {current.Turn:F1} degrees/s; 30/60/144 FPS constant-distance checks.");
        }
        if(after>=before*.85f)throw new Exception("Curves have not sufficiently reduced abrupt acceleration changes");
        report.Add($"PASS {done.Count} routes: aggregate peak frame jerk reduced {(1-after/before)*100:F1}%; largest frame speed error {worst*100:F2}% at 30/60/144 FPS.");
        return report;
    }
}
#endif
