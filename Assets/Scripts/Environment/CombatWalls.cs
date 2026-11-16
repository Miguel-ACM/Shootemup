using System.Collections.Generic;
using UnityEngine;

// Convex slabs: the sloping collision edges match the rendered passage.
public readonly struct CombatWall
{
    public readonly float Bottom, Top, LeftBottom, LeftTop, RightBottom, RightTop;
    readonly Vector2 leftNormal,rightNormal;
    readonly float leftDistance,rightDistance;
    public CombatWall(float bottom,float top,float leftBottom,float leftTop,float rightBottom,float rightTop)
    {
        Bottom=bottom;Top=top;LeftBottom=leftBottom;LeftTop=leftTop;RightBottom=rightBottom;RightTop=rightTop;
        leftNormal=new Vector2(bottom-top,leftTop-leftBottom).normalized;rightNormal=new Vector2(top-bottom,rightBottom-rightTop).normalized;
        leftDistance=Vector2.Dot(leftNormal,new Vector2(leftBottom,bottom));rightDistance=Vector2.Dot(rightNormal,new Vector2(rightBottom,bottom));
    }
    public static implicit operator CombatWall(Rect r) => new CombatWall(r.yMin,r.yMax,r.xMin,r.xMin,r.xMax,r.xMax);
    public CombatWall Translated(float y) => new CombatWall(Bottom+y,Top+y,LeftBottom,LeftTop,RightBottom,RightTop);
    public void Plane(int edge,out Vector2 normal,out float distance)
    {
        if(edge==0){normal=Vector2.down;distance=-Bottom;}
        else if(edge==1){normal=Vector2.up;distance=Top;}
        else if(edge==2){normal=leftNormal;distance=leftDistance;}
        else {normal=rightNormal;distance=rightDistance;}
    }
    public bool Contains(Vector2 p,float radius)
    {
        if(p.y<Bottom-radius||p.y>Top+radius||p.x<Mathf.Min(LeftBottom,LeftTop)-radius*2||p.x>Mathf.Max(RightBottom,RightTop)+radius*2)return false;
        for(int edge=0;edge<4;edge++){Plane(edge,out var normal,out float distance);if(Vector2.Dot(normal,p)>distance+radius)return false;}
        return true;
    }
    public bool Sweep(Vector2 from,Vector2 to,float radius,out float enter,out Vector2 face)
    {
        enter=0;face=Vector2.zero;float leave=1;Vector2 travel=to-from;
        if(Mathf.Max(from.y,to.y)<Bottom-radius||Mathf.Min(from.y,to.y)>Top+radius||
           Mathf.Max(from.x,to.x)<Mathf.Min(LeftBottom,LeftTop)-radius*2||Mathf.Min(from.x,to.x)>Mathf.Max(RightBottom,RightTop)+radius*2)return false;
        for(int edge=0;edge<4;edge++)
        {
            Plane(edge,out var normal,out float distance);
            float outside=Vector2.Dot(normal,from)-distance-radius,speed=Vector2.Dot(normal,travel);
            if(Mathf.Abs(speed)<.000001f){if(outside>0)return false;continue;}
            float t=-outside/speed;
            if(speed<0){if(t>=enter){enter=t;face=normal;}}
            else leave=Mathf.Min(leave,t);
            if(enter>leave)return false;
        }
        return enter<=1;
    }
}

// Publish only nearby slabs; dense rounds never enter native wall physics.
public static class CombatWalls
{
    public static readonly List<CombatWall> Solid = new List<CombatWall>(24);
    public const float HullRadius = .34f;
    public static bool Active => Solid.Count>0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => Solid.Clear();
    public static bool Overlaps(Vector2 point,float radius)
        => Overlaps(Solid, point, radius);
    internal static bool Overlaps(List<CombatWall> walls,Vector2 point,float radius)
    { foreach(var wall in walls)if(wall.Contains(point,radius))return true;return false; }
    public static bool Sweep(Vector2 from,Vector2 to,float radius,out Vector2 point,out Vector2 normal)
        => Sweep(Solid, from, to, radius, out point, out normal);
    static bool Sweep(List<CombatWall> walls,Vector2 from,Vector2 to,float radius,out Vector2 point,out Vector2 normal)
    {
        float nearest=2;normal=Vector2.zero;
        foreach(var wall in walls)
            if(wall.Sweep(from,to,radius,out float t,out var face)&&t<nearest){nearest=t;normal=face;}
        point=Vector2.LerpUnclamped(from,to,Mathf.Clamp01(nearest));return nearest<=1;
    }
    public static float BeamLength(Vector2 from,Vector2 axis,float length,float radius=0)
        => Sweep(from,from+axis*length,radius,out var point,out _) ? Vector2.Distance(from,point) : length;
    public static void ClipPath(Vector3[] path,float radius)
    {
        if(!Active)return;
        for(int i=1;i<path.Length;i++)if(Sweep(path[i-1],path[i],radius,out var point,out _))
        {
            var end=new Vector3(point.x,point.y,path[i].z);
            for(int n=i;n<path.Length;n++)path[n]=end;
            return;
        }
    }
    public static Vector3 Constrain(Vector3 from,Vector3 desired) => Constrain(from,desired,out _);
    public static Vector3 Constrain(Vector3 from,Vector3 desired,out bool struck)
        => Constrain(from, desired, Solid, out struck);
    internal static Vector3 Constrain(Vector3 from,Vector3 desired,List<CombatWall> walls,out bool struck)
    {
        struck=false;if(walls.Count==0)return desired;
        Vector2 p=from;
        if(Overlaps(walls,p,HullRadius))
        {
            struck=true;Vector2 best=p;float nearest=float.PositiveInfinity;
            void Candidate(Vector2 q)
            {
                if(q.x<Camera.LeftLimit||q.x>Camera.RightLimit||q.y<Camera.DownLimit||q.y>Camera.UpLimit||Overlaps(walls,q,HullRadius+.0005f))return;
                float d=(q-p).sqrMagnitude;if(d<nearest){nearest=d;best=q;}
            }
            // Only exposed faces can release the pilot, never internal panel seams.
            foreach(var wall in walls)for(int edge=0;edge<4;edge++)
            {
                wall.Plane(edge,out var n,out float distance);distance+=HullRadius+.003f;
                Candidate(p+n*(distance-Vector2.Dot(n,p)));
                if(Mathf.Abs(n.x)>.00001f)Candidate(new Vector2((distance-n.y*p.y)/n.x,p.y));
                if(Mathf.Abs(n.y)>.00001f)Candidate(new Vector2(p.x,(distance-n.x*p.x)/n.y));
            }
            p=best;
        }
        Vector2 remaining=(Vector2)(desired-from);
        for(int i=0;i<4&&remaining.sqrMagnitude>.0000001f;i++)
        {
            if(!Sweep(walls,p,p+remaining,HullRadius,out var contact,out var normal)){p+=remaining;break;}
            struck=true;Vector2 rest=p+remaining-contact;p=contact+normal*.003f;
            remaining=rest-normal*Mathf.Min(0,Vector2.Dot(rest,normal));
        }
        return new Vector3(Mathf.Clamp(p.x,Camera.LeftLimit,Camera.RightLimit),Mathf.Clamp(p.y,Camera.DownLimit,Camera.UpLimit),desired.z);
    }
}

