using UnityEngine;

// District events that act on their own during ordinary combat waves: lightning in a THUNDERSTORM,
// mines rising from the depths in a MINEFIELD, falling sweets in CANDY RAIN and spears in SPEAR RAIN.
// Every hazard is telegraphed first and pauses with the waves: never during decisions, minibosses
// or bosses. Wave-based events (rush hour, deep passages, eruptions) live with their own systems.
public sealed class DistrictHazards : MonoBehaviour
{
    WaveOrchestrer waves;
    PSXGrassField valley;
    float next = -1, nextRock = -1;
    System.Random random;
    int seed = -1;

    public static bool Open => WaveOrchestrer.Current != null && WaveOrchestrer.Current.HazardWindow;

    public static void Play(string cue)
    {
        var definition = SoundCueCatalog.Find(cue);
        if (definition != null)
            SoundManager.PlaySoundPitch(cue, definition.Volume, definition.Pitch);
    }

    void Start()
    {
        waves = GetComponent<WaveOrchestrer>();
    }

    void Update()
    {
        if (waves == null || !waves.HazardWindow)
        {
            // Each combat wave opens with a short breather before the first hazard.
            next = -1;
            nextRock = -1;
            return;
        }
        if (seed != LevelRun.Seed)
        {
            seed = LevelRun.Seed;
            random = new System.Random(seed ^ 0x1f3d5b79);
        }
        var condition = DistrictConditions.Current;
        float now = Time.time;
        if (next < 0)
        {
            next = now + 3.5f;
            return;
        }
        float rest = Difficulty.Easy ? 1.35f : Difficulty.Hard ? .8f : 1;
        // These keep their own clocks: asteroids drift in between flares.
        if (condition == DistrictCondition.SolarFlare)
        {
            SolarFlares(now, rest);
            return;
        }
        if (LevelRun.Current == LevelId.Fields)
        {
            Lurkers(now, rest, condition == DistrictCondition.Forest);
            return;
        }
        if (now < next)
            return;
        switch (condition)
        {
            // Spaced well apart (Miguel found the first pass far too busy, 15 November 2026).
            case DistrictCondition.Thunderstorm:
                Lightning();
                next = now + R(4.8f, 7.2f) * rest;
                break;
            case DistrictCondition.Minefield:
                if (CountRising() < 2)
                    DepthMine.Rise(new Vector3(R(Camera.LeftLimit + .8f, Camera.RightLimit - .8f), Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, R(.42f, .74f)), 1));
                next = now + R(4.6f, 6.8f) * rest;
                break;
            case DistrictCondition.CandyRain:
                if (CandyDrop.Count < 3)
                    CandyDrop.Drop(R(Camera.LeftLimit + .6f, Camera.RightLimit - .6f), random.Next(5));
                next = now + R(5.2f, 7.6f) * rest;
                break;
            case DistrictCondition.SpearRain:
                Spears();
                next = now + R(5.8f, 8.4f) * rest;
                break;
            default:
                next = now + 5;
                break;
        }
    }

    float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());

    // SOLAR FLARE: keep two asteroids drifting through; flare only when one is fully in view,
    // from the side that gives its shadow the most room.
    void SolarFlares(float now, float rest)
    {
        if (nextRock < 0 || now >= nextRock)
        {
            nextRock = now + R(3.5f, 5.5f);
            if (SpaceAsteroid.Live.Count < 2)
                SpaceAsteroid.Spawn(R(Camera.LeftLimit + 1.4f, Camera.RightLimit - 1.4f), R(.75f, 1.15f), random.Next());
        }
        if (now < next || SolarFlare.Active)
            return;
        foreach (var rock in SpaceAsteroid.Live)
        {
            var p = rock.transform.position;
            if (p.y > Camera.DownLimit + 2.5f && p.y < Camera.UpLimit - 1.5f)
            {
                SolarFlare.Begin(p.x > 0 ? 1 : -1);
                next = now + R(11, 15) * rest;
                return;
            }
        }
    }

    // Thicket Lurkers ambush from the trees: now and then in the open valley, often in the forest.
    void Lurkers(float now, float rest, bool forest)
    {
        if (now < next)
            return;
        next = now + (forest ? R(6, 9) : R(16, 24)) * rest;
        int alive = 0;
        foreach (var enemy in AEnemy.Live)
            if (enemy is ThicketLurker)
                alive++;
        if (alive >= (forest ? 2 : 1))
            return;
        if (valley == null)
            valley = FindAnyObjectByType<PSXGrassField>();
        ThicketLurker.Ambush(valley, random);
    }

    static int CountRising()
    {
        int count = 0;
        foreach (var enemy in AEnemy.Live)
            if (enemy is DepthMine mine && mine.FromDepths)
                count++;
        return count;
    }

    // One or two strikes; one lands near the pilot so standing still is never safe.
    void Lightning()
    {
        var pilot = CombatVisuals.Player;
        float x = pilot != null && random.NextDouble() < .6 ? pilot.position.x + R(-.6f, .6f) : R(Camera.LeftLimit + .5f, Camera.RightLimit - .5f);
        LightningStrike.Begin(Mathf.Clamp(x, Camera.LeftLimit + .4f, Camera.RightLimit - .4f), random.Next());
        if (Difficulty.Hard && random.NextDouble() < .3)
        {
            float other = x + (random.Next(2) == 0 ? -1 : 1) * R(1.8f, 3.2f);
            if (other > Camera.LeftLimit + .4f && other < Camera.RightLimit - .4f)
                LightningStrike.Begin(other, random.Next(), .35f);
        }
    }

    // A volley of spears in spaced columns, one column over the pilot.
    void Spears()
    {
        int count = Difficulty.Easy ? 1 : Difficulty.Hard ? 3 : 2;
        var pilot = CombatVisuals.Player;
        float width = Camera.RightLimit - Camera.LeftLimit - 1;
        float offset = R(0, 1);
        for (int i = 0; i < count; i++)
        {
            float x = Camera.LeftLimit + .5f + Mathf.Repeat((i + offset) / count, 1) * width;
            if (i == 0 && pilot != null)
                x = Mathf.Clamp(pilot.position.x, Camera.LeftLimit + .4f, Camera.RightLimit - .4f);
            SpearStrike.Begin(x, i * .16f);
        }
    }
}
