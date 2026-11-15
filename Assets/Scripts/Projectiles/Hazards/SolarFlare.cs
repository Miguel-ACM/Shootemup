using System.Collections.Generic;
using UnityEngine;

// SOLAR FLARE (Space): slow tumbling asteroids drift through the arena. Now and then the sun flares
// from one edge: the edge glows for two seconds while each asteroid's shadow appears as a dark lane
// stretching away from the sun, then a band of light sweeps across the screen. Being in the band
// outside every shadow costs a hit. Hiding is the answer, not dodging.
public sealed class SolarFlare : MonoBehaviour
{
    const float Warning = 2f, Sweep = 1.05f, Afterglow = .45f, BandWidth = 1.9f;
    static readonly Color Sun = new Color(1, .88f, .58f);
    static Material light;
    static Sprite square;
    int side;
    float age;
    bool struck;
    LineRenderer edge, band;
    SpriteRenderer wash;
    readonly List<SpriteRenderer> shadows = new List<SpriteRenderer>();
    readonly List<LineRenderer> lanes = new List<LineRenderer>();

    public static bool Active { get; private set; }

    // side: -1 the sun is on the left, +1 on the right.
    public static void Begin(int side)
    {
        var flare = new GameObject("Solar flare").AddComponent<SolarFlare>();
        flare.side = side;
        flare.Build();
        Active = true;
    }

    void Build()
    {
        if (light == null)
            light = new Material(Resources.Load<Shader>("HazardLine")) { name = "Solar flare light" };
        if (square == null)
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4);
        edge = Line(2.6f);
        band = Line(BandWidth);
        // Sunlight washes the arena warm, so the shadow lanes read dark even against black space.
        wash = new GameObject("Flare sunlight").AddComponent<SpriteRenderer>();
        wash.transform.SetParent(transform, false);
        wash.sprite = square;
        wash.sortingLayerName = "Enemy";
        wash.sortingOrder = -8;
        wash.color = Color.clear;
        foreach (var asteroid in SpaceAsteroid.Live)
        {
            var shadow = new GameObject("Asteroid shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, false);
            shadow.sprite = square;
            shadow.sortingLayerName = "Enemy";
            shadow.sortingOrder = -6;
            shadow.color = Color.clear;
            shadows.Add(shadow);
            // Pale edges mark each safe lane.
            for (int edgeLine = 0; edgeLine < 2; edgeLine++)
            {
                var lane = CombatVisuals.Line(transform, Color.clear, .04f);
                lane.useWorldSpace = true;
                lane.sortingLayerName = "Enemy";
                lane.sortingOrder = -5;
                lane.positionCount = 2;
                lanes.Add(lane);
            }
        }
        DistrictHazards.Play("eventRumble");
    }

    LineRenderer Line(float width)
    {
        var line = CombatVisuals.Line(transform, Color.clear, width);
        line.useWorldSpace = true;
        line.sharedMaterial = light;
        line.sortingLayerName = "Enemy";
        // Under the shadow lanes, so the band visibly stops at each one.
        line.sortingOrder = -7;
        line.positionCount = 2;
        return line;
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || !GameClock.ActivePlay)
            return;
        age += Time.deltaTime;
        if (!DistrictHazards.Open && age < Warning + Sweep)
        {
            // The wave ended mid-flare: let it fade without harm.
            age = Mathf.Max(age, Warning + Sweep);
            struck = true;
        }
        float left = Camera.LeftLimit, right = Camera.RightLimit;
        float sunX = side < 0 ? left : right, farX = side < 0 ? right : left;
        float top = Camera.UpLimit + 1, bottom = Camera.DownLimit - 1;
        float warm = Mathf.Clamp01(age / Warning);
        float fade = age < Warning + Sweep ? 1 : 1 - Mathf.Clamp01((age - Warning - Sweep) / Afterglow);
        // The sun's edge glows ever brighter, flickering faster toward the release.
        float flicker = .8f + .2f * Mathf.Sin(age * Mathf.Lerp(8, 30, warm));
        SetLine(edge, sunX, top, bottom, new Color(Sun.r, Sun.g, Sun.b, Mathf.Lerp(.15f, .9f, warm) * flicker * fade));
        var bandColor = new Color(1, .95f, .8f, 0);
        float bandX = sunX;
        if (age >= Warning)
        {
            float t = Mathf.Clamp01((age - Warning) / Sweep);
            bandX = Mathf.Lerp(sunX, farX, t * t * (3 - 2 * t));
            bandColor.a = fade;
            if (!struck && t > 0)
                SweepFirst();
        }
        SetLine(band, bandX, top, bottom, bandColor);
        var view = UnityEngine.Camera.main;
        var centre = view != null ? view.transform.position : Vector3.zero;
        wash.transform.position = new Vector3(centre.x, centre.y, 3);
        wash.transform.localScale = new Vector3(Camera.RightLimit - Camera.LeftLimit + 2, Camera.UpLimit - Camera.DownLimit + 4, 1);
        float lit = age < Warning ? Mathf.Lerp(.04f, .2f, warm) : .32f;
        wash.color = new Color(1, .82f, .5f, lit * fade);
        UpdateShadows(farX, Mathf.Lerp(.35f, .8f, warm) * fade);
        if (age >= Warning && age < Warning + Sweep && DistrictHazards.Open)
            Hurt(bandX);
        if (age >= Warning + Sweep + Afterglow)
            Destroy(gameObject);
    }

    void SweepFirst()
    {
        struck = true;
        DistrictHazards.Play("eventSpear");
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(1.2f, 10, .05f, .5f);
    }

    static void SetLine(LineRenderer line, float x, float top, float bottom, Color color)
    {
        line.SetPosition(0, new Vector3(x, top, -1));
        line.SetPosition(1, new Vector3(x, bottom, -1));
        line.startColor = line.endColor = color;
    }

    // Each shadow runs from the asteroid to the far edge, slightly narrower than the rock.
    void UpdateShadows(float farX, float alpha)
    {
        int i = 0;
        foreach (var asteroid in SpaceAsteroid.Live)
        {
            if (i >= shadows.Count)
                break;
            var shadow = shadows[i];
            var top = lanes[i * 2];
            var bottom = lanes[i * 2 + 1];
            i++;
            if (asteroid == null)
            {
                shadow.color = Color.clear;
                top.startColor = top.endColor = bottom.startColor = bottom.endColor = Color.clear;
                continue;
            }
            var p = asteroid.transform.position;
            float width = Mathf.Abs(farX - p.x) + .2f, height = asteroid.Radius * 1.7f;
            shadow.transform.position = new Vector3((p.x + farX) * .5f, p.y, 1.5f);
            shadow.transform.localScale = new Vector3(width, height, 1);
            shadow.color = new Color(.01f, .015f, .04f, alpha);
            var edgeColour = new Color(1, .9f, .65f, alpha * .9f);
            foreach (var (lane, y) in new[] { (top, p.y + height * .5f), (bottom, p.y - height * .5f) })
            {
                lane.SetPosition(0, new Vector3(p.x, y, 1.2f));
                lane.SetPosition(1, new Vector3(farX, y, 1.2f));
                lane.startColor = edgeColour;
                lane.endColor = new Color(edgeColour.r, edgeColour.g, edgeColour.b, edgeColour.a * .3f);
            }
        }
    }

    void Hurt(float bandX)
    {
        var pilot = CombatVisuals.Player;
        if (pilot == null)
            return;
        var p = pilot.position;
        if (Mathf.Abs(p.x - bandX) > BandWidth * .45f)
            return;
        foreach (var asteroid in SpaceAsteroid.Live)
            if (asteroid != null && asteroid.Shades(p, side))
                return;
        pilot.SendMessage("ReceiveHazardDamage", 1f, SendMessageOptions.DontRequireReceiver);
    }

    void OnDestroy()
    {
        Active = false;
    }
}

// A slow tumbling asteroid that casts a SOLAR FLARE shadow. Scenery on the arena layer:
// it has no collider and never blocks fire.
public sealed class SpaceAsteroid : MonoBehaviour
{
    public static readonly List<SpaceAsteroid> Live = new List<SpaceAsteroid>();
    public float Radius { get; private set; }
    Vector3 drift, spin;

    public static SpaceAsteroid Spawn(float x, float radius, int seed)
    {
        var go = new GameObject("Drifting asteroid");
        var view = UnityEngine.Camera.main;
        float top = view != null ? view.transform.position.y + view.orthographicSize : Camera.UpLimit;
        go.transform.position = new Vector3(x, Mathf.Max(top, Camera.UpLimit) + radius + .6f, 2);
        go.transform.localScale = Vector3.one * radius;
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.Asteroid(seed % 3);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        renderer.sortingOrder = -4;
        var rock = go.AddComponent<SpaceAsteroid>();
        var random = new System.Random(seed);
        rock.Radius = radius;
        rock.drift = new Vector3((float)(random.NextDouble() - .5) * .35f, -.75f - (float)random.NextDouble() * .3f, 0);
        rock.spin = new Vector3((float)random.NextDouble() * 30, (float)random.NextDouble() * 40, (float)(random.NextDouble() - .5) * 50);
        return rock;
    }

    // In the shadow when downstream of the rock (away from the sun) and within its height.
    public bool Shades(Vector3 point, int sunSide)
    {
        var p = transform.position;
        bool downstream = sunSide < 0 ? point.x > p.x - Radius * .3f : point.x < p.x + Radius * .3f;
        return downstream && Mathf.Abs(point.y - p.y) < Radius * .85f;
    }

    void OnEnable() => Live.Add(this);
    void OnDisable() => Live.Remove(this);

    void Update()
    {
        if (WorldTimeStop.Frozen || !GameClock.ActivePlay)
            return;
        transform.position += drift * Time.deltaTime;
        transform.Rotate(spin * Time.deltaTime, Space.Self);
        if (transform.position.y < Camera.DownLimit - Radius - 1.5f || LevelRun.Current != LevelId.Space)
            Destroy(gameObject);
    }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] asteroids = new Mesh[3];

    // A lumpy faceted rock of overlapping boulders, unit radius.
    public static Mesh Asteroid(int variant)
    {
        if (asteroids[variant] != null)
            return asteroids[variant];
        var b = new Builder();
        var random = new System.Random(4111 + variant * 977);
        float R(float a, float c) => Mathf.Lerp(a, c, (float)random.NextDouble());
        // Pale grey-brown rock with darker craters, bright enough to read against space.
        b.Ball(Vector3.zero, new Vector3(R(.82f, .95f), R(.72f, .88f), .7f), 6);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3 + R(-.3f, .3f);
            var at = new Vector3(Mathf.Cos(a) * R(.35f, .55f), Mathf.Sin(a) * R(.3f, .5f), R(-.25f, .1f));
            b.Ball(at, Vector3.one * R(.28f, .42f), i % 3 == 0 ? 13 : i % 3 == 1 ? 6 : 14);
        }
        for (int i = 0; i < 3; i++)
            b.Ball(new Vector3(R(-.4f, .4f), R(-.35f, .35f), -.55f), new Vector3(.16f, .14f, .05f), 11);
        return asteroids[variant] = b.Finish("Drifting asteroid " + variant);
    }
}
