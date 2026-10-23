using UnityEngine;

// An authored continuous route. Shared by a formation; each ship only owns its clock.
public sealed class FlightCurve
{
    public string Name {get;}
    readonly Vector2[] knots;
    readonly Vector2 laneAxis;
    readonly float bloom;
    public FlightCurve(string name,Vector2 laneAxis,float bloom,params float[] xy)
    {
        Name=name;this.laneAxis=laneAxis;this.bloom=bloom;
        if(xy.Length<4 || xy.Length%2!=0)throw new System.ArgumentException("Routes need at least two XY pairs");
        knots=new Vector2[xy.Length/2];
        for(int i=0;i<knots.Length;i++)knots[i]=new Vector2(xy[i*2],xy[i*2+1]);
    }
    public Vector2 Sample(float progress,float side=1,float lane=0)
    {
        float index=Mathf.Clamp01(progress)*(knots.Length-1);
        int i=Mathf.Min(knots.Length-2,Mathf.FloorToInt(index));float t=index-i;
        Vector2 a=knots[i],b=knots[i+1];
        Vector2 before=i>0 ? knots[i-1] : 2*a-b;
        Vector2 after=i+2<knots.Length ? knots[i+2] : 2*b-a;
        // Catmull-Rom tangents join adjacent arcs without a heading snap or stop.
        Vector2 p=(2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*(b-before)*.5f+
            (-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*(after-a)*.5f;
        p+=laneAxis*lane*(1+bloom*Mathf.Sin(Mathf.PI*Mathf.Clamp01(progress)));
        p.x*=side;return p;
    }
}
