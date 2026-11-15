using UnityEngine;

// District events that act on their own during ordinary combat waves: lightning in a THUNDERSTORM,
// mines rising from the depths in a MINEFIELD, falling sweets in CANDY RAIN and spears in SPEAR RAIN.
// Every hazard is telegraphed first and pauses with the waves: never during decisions, minibosses
// or bosses. Wave-based events (rush hour, deep passages, eruptions) live with their own systems.
public sealed class DistrictHazards : MonoBehaviour
{
    WaveOrchestrer waves;
    float next = -1;
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
        if (now < next)
            return;
        float rest = Difficulty.Easy ? 1.35f : Difficulty.Hard ? .8f : 1;
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
