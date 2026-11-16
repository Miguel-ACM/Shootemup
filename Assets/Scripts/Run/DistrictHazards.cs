using UnityEngine;

// District events that act on their own during ordinary combat waves: lightning in a THUNDERSTORM,
// mines rising from the depths in a MINEFIELD, falling sweets in CANDY RAIN, spears in SPEAR RAIN,
// half the arena on fire in a SOLAR FLARE, Thicket Lurkers in the Plains trees and a steady light
// snowfall in a BLIZZARD.
// Every hazard is telegraphed first and pauses with the waves: never during decisions, minibosses
// or bosses. Wave-based events (rush hour, deep passages, eruptions) live with their own systems.
public sealed class DistrictHazards : MonoBehaviour
{
    WaveOrchestrer waves;
    PSXGrassField valley;
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
            next = now + (LevelRun.Current == LevelId.Mushroom ? 2.1f : 3.5f);
            return;
        }
        float rest = Difficulty.Easy ? 1.35f : Difficulty.Hard ? .8f : 1;
        if (LevelRun.Current == LevelId.Fields)
        {
            Lurkers(now, rest, condition == DistrictCondition.Forest);
            return;
        }
        if (now < next)
            return;
        if (LevelRun.Current == LevelId.Mushroom)
        {
            var entry = new Vector3(R(Camera.LeftLimit + 1.6f, Camera.RightLimit - 1.6f), Camera.UpLimit + 1.28f);
            SporeCloud.Drop(entry, R(0, 6.28f));
            next = now + R(5.8f, 7.6f) * rest;
            return;
        }
        switch (condition)
        {
            // Spaced well apart (Miguel found the first pass far too busy, 15 November 2026).
            case DistrictCondition.Thunderstorm:
                Lightning();
                next = now + R(4.8f, 7.2f) * rest;
                break;
            case DistrictCondition.Minefield:
                if (CountRising() < 1)
                    DepthMine.Rise(new Vector3(R(Camera.LeftLimit + .8f, Camera.RightLimit - .8f), Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, R(.42f, .74f)), 1));
                next = now + R(7.5f, 10.5f) * rest;
                break;
            case DistrictCondition.Plague:
                int swarm = Difficulty.Easy ? 7 : Difficulty.Hard ? 14 : 10;
                int side = random.Next(2) == 0 ? -1 : 1;
                PlagueMote.Spill(new Vector2(side * R(1.25f, 3.45f), Camera.UpLimit + .65f), swarm);
                next = now + R(3.8f, 5.6f) * rest;
                break;
            case DistrictCondition.CandyRain:
                // More candy (Miguel, 15 November 2026): up to six sweets, twice as often.
                if (CandyDrop.Count < 6)
                    CandyDrop.Drop(R(Camera.LeftLimit + .6f, Camera.RightLimit - .6f), random.Next(5));
                next = now + R(2.4f, 3.6f) * rest;
                break;
            // A blizzard keeps a light snowfall coming between the shells' own snow.
            case DistrictCondition.Blizzard:
                Snowflake();
                next = now + R(.32f, .5f) * rest;
                break;
            case DistrictCondition.SolarFlare:
                SolarFlares();
                next = now + R(13, 18) * rest;
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

    // One hostile snowflake born above the visible top edge, swaying as it falls and carried
    // downwind; it starts a little upwind so the drift spreads the fall over the whole arena.
    void Snowflake()
    {
        var wind = DistrictConditions.BlizzardWind;
        float x = R(Camera.LeftLimit + .3f, Camera.RightLimit - .3f) - Mathf.Sign(wind.x) * R(0, 1.2f);
        CombatRound.LaunchTone = HostileTone.Bright;
        var flake = CombatRound.Launch<SnowflakeRound>(new Vector3(x, CombatRound.Border.yMax + .5f), Vector2.down, R(1.4f, 1.9f), .19f);
        if (flake == null)
            return;
        flake.Sway(random.NextDouble() < .5 ? .3f : -.3f, .45f).Drift(wind * R(.85f, 1.15f));
        flake.generation = 1;
    }

    // SOLAR FLARE: one half of the arena burns, usually the half the pilot is in.
    void SolarFlares()
    {
        if (SolarFlare.Active)
            return;
        var pilot = CombatVisuals.Player;
        int side = random.NextDouble() < .5 ? -1 : 1;
        if (pilot != null && random.NextDouble() < .6)
            side = pilot.position.x < (Camera.LeftLimit + Camera.RightLimit) * .5f ? -1 : 1;
        SolarFlare.Begin(side);
    }

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
