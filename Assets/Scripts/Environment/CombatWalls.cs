using System.Collections.Generic;
using UnityEngine;

// A handful of solid scenery rectangles, queried without adding dense projectile
// volleys to the native physics broad phase. Bullet hitboxes against ships are unchanged.
public static class CombatWalls
{
    public static readonly List<Rect> Solid = new List<Rect>(4);
    public const float HullRadius = .34f;
    public static bool Active => Solid.Count > 0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => Solid.Clear();

    public static bool Sweep(Vector2 from, Vector2 to, float radius, out Vector2 point, out Vector2 normal)
    {
        float nearest = 2;
        normal = Vector2.zero;
        Vector2 travel = to - from;
        foreach (var rectangle in Solid)
        {
            Rect r = Rect.MinMaxRect(rectangle.xMin-radius, rectangle.yMin-radius, rectangle.xMax+radius, rectangle.yMax+radius);
            float enter = 0, leave = 1;
            Vector2 face = Vector2.zero;
            bool Slab(float p, float d, float low, float high, Vector2 axis)
            {
                if (Mathf.Abs(d) < .000001f) return p >= low && p <= high;
                float a = (low-p)/d, b = (high-p)/d;
                Vector2 n = d > 0 ? -axis : axis;
                if (a > b) (a,b) = (b,a);
                if (a >= enter) { enter = a; face = n; }
                leave = Mathf.Min(leave,b);
                return enter <= leave;
            }
            if (!Slab(from.x,travel.x,r.xMin,r.xMax,Vector2.right) || !Slab(from.y,travel.y,r.yMin,r.yMax,Vector2.up)) continue;
            if (enter < nearest) { nearest=enter; normal=face; }
        }
        point = from + travel * Mathf.Clamp01(nearest);
        return nearest <= 1;
    }
    public static float BeamLength(Vector2 from, Vector2 axis, float length, float radius = 0)
        => Sweep(from,from+axis*length,radius,out var point,out _) ? Vector2.Distance(from,point) : length;
    public static void ClipPath(Vector3[] path, float radius)
    {
        if (!Active) return;
        for (int i=1;i<path.Length;i++)
            if (Sweep(path[i-1],path[i],radius,out var point,out _))
            {
                var end = new Vector3(point.x,point.y,path[i].z);
                for (int n=i;n<path.Length;n++) path[n]=end;
                return;
            }
    }
    public static Vector3 Constrain(Vector3 from, Vector3 desired)
    {
        if (!Active) return desired;
        Vector2 p=from;
        // A moving wall can reach a stationary pilot. Expel toward the nearest
        // available face, keeping the result inside the flight area.
        for (int pass=0;pass<3;pass++) foreach (var rectangle in Solid)
        {
            var r=Rect.MinMaxRect(rectangle.xMin-HullRadius-.002f,rectangle.yMin-HullRadius-.002f,rectangle.xMax+HullRadius+.002f,rectangle.yMax+HullRadius+.002f);
            if (!r.Contains(p)) continue;
            Vector2 best=p; float distance=float.PositiveInfinity;
            void Candidate(Vector2 q)
            {
                if(q.x<Camera.LeftLimit||q.x>Camera.RightLimit||q.y<Camera.DownLimit||q.y>Camera.UpLimit) return;
                float d=(q-p).sqrMagnitude;
                if(d<distance){distance=d;best=q;}
            }
            Candidate(new Vector2(r.xMin,p.y)); Candidate(new Vector2(r.xMax,p.y));
            Candidate(new Vector2(p.x,r.yMin)); Candidate(new Vector2(p.x,r.yMax));
            p=best;
        }
        Vector2 remaining=(Vector2)(desired-from);
        for(int i=0;i<3&&remaining.sqrMagnitude>.0000001f;i++)
        {
            if(!Sweep(p,p+remaining,HullRadius,out var contact,out var normal)){p+=remaining;break;}
            Vector2 rest=p+remaining-contact;
            p=contact+normal*.002f;
            remaining=rest-normal*Mathf.Min(0,Vector2.Dot(rest,normal));
        }
        return new Vector3(Mathf.Clamp(p.x,Camera.LeftLimit,Camera.RightLimit),Mathf.Clamp(p.y,Camera.DownLimit,Camera.UpLimit),desired.z);
    }
}
