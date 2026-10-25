using UnityEngine;

// An authored continuous route. Shared by a formation; each ship only owns its clock.
public sealed class FlightCurve
{
    public string Name
    {
        get;
    }

    readonly Vector2[] knots;
    readonly Vector2 laneAxis;
    readonly float bloom;
    const int Samples = 1024;
    public float LaneScale => Vector2.Scale(laneAxis, new Vector2(Camera.RightLimit, Camera.UpLimit - Camera.DownLimit)).magnitude;
    public bool HorizontalLanes => Mathf.Abs(laneAxis.x) > .5f;
    float[] distances;
    float measuredWidth, measuredHeight;
    public float Length
    {
        get
        {
            Measure();
            return distances[Samples];
        }
    }

    void Measure()
    {
        float width = Camera.RightLimit, height = Camera.UpLimit - Camera.DownLimit;
        if (distances != null && Mathf.Approximately(measuredWidth, width) && Mathf.Approximately(measuredHeight, height))
            return;
        measuredWidth = width;
        measuredHeight = height;
        distances = new float[Samples + 1];
        var previous = Sample(0);
        for (int i = 1; i < distances.Length; i++)
        {
            var next = Sample(i / (float)Samples);
            distances[i] = distances[i - 1] + Vector2.Scale(next - previous, new Vector2(width, height)).magnitude;
            previous = next;
        }
    }

    public float TravelParameter(float progress)
    {
        Measure();
        float target = Mathf.Clamp01(progress) * distances[Samples];
        int low = 1, high = Samples;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (distances[mid] < target)
                low = mid + 1;
            else
                high = mid;
        }
        return (low - 1 + Mathf.InverseLerp(distances[low - 1], distances[low], target)) / Samples;
    }

    public FlightCurve(string name, Vector2 laneAxis, float bloom, params float[] xy)
    {
        Name = name;
        this.laneAxis = laneAxis;
        this.bloom = bloom;
        if (xy.Length < 4 || xy.Length % 2 != 0)
            throw new System.ArgumentException("Routes need at least two XY pairs");
        knots = new Vector2[xy.Length / 2];
        for (int i = 0; i < knots.Length; i++)
            knots[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
    }

    public Vector2 Sample(float progress, float side = 1, float lane = 0)
    {
        float index = Mathf.Clamp01(progress) * (knots.Length - 1);
        int i = Mathf.Min(knots.Length - 2, Mathf.FloorToInt(index));
        float t = index - i;
        Vector2 a = knots[i], b = knots[i + 1];
        Vector2 before = i > 0 ? knots[i - 1] : 2 * a - b;
        Vector2 after = i + 2 < knots.Length ? knots[i + 2] : 2 * b - a;
        // A uniform cubic B-spline keeps position, tangent and curvature continuous.
        // Knots guide broad banks instead of forcing a tight reversal through every point.
        float u = 1 - t;
        Vector2 p = (u * u * u * before + (3 * t * t * t - 6 * t * t + 4) * a +
            (-3 * t * t * t + 3 * t * t + 3 * t + 1) * b + t * t * t * after) / 6;
        p += laneAxis * lane * (1 + bloom * Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress)));
        p.x *= side;
        return p;
    }
}
