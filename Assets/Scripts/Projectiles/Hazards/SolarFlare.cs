using UnityEngine;

// SOLAR FLARE (Space): the sun sets one half of the arena on fire. First the outer edge of the
// doomed half glows and breathes, flames lick in and embers rise while the half pulses a warm tint
// (Miguel wants no line drawn down the middle); then the fire erupts in a rolling chain of
// fireballs, fills that half up to the centre, burns for a moment and recedes. Anywhere in the
// burning half costs a hit; the other half is safe.
public sealed class SolarFlare : MonoBehaviour
{
    public const float Warning = 1.7f, Erupt = .35f, Burn = 2.1f, Recede = .55f;
    static readonly Color Hot = new Color(1, .62f, .18f), Fireball = new Color(1, .8f, .55f);
    static Material fireTemplate;
    int side;
    float age, emberTimer, fireballAt;
    bool erupted;
    Material fire;
    Mesh sheet;

    static SolarFlare current;
    // Unity's null check also covers a destroyed flare, in edit mode as well as play mode.
    public static bool Active => current != null;
    public int Side => side;
    static float Line => (Camera.LeftLimit + Camera.RightLimit) * .5f;
    static float Boundary => (Camera.RightLimit - Camera.LeftLimit) * .5f + 1;
    float Outer => side < 0 ? Camera.LeftLimit : Camera.RightLimit;
    bool Burning => age >= Warning && age < Warning + Erupt + Burn;

    // side: -1 the left half burns, +1 the right half.
    public static SolarFlare Begin(int side)
    {
        var flare = new GameObject("Solar flare").AddComponent<SolarFlare>();
        flare.side = side < 0 ? -1 : 1;
        flare.Build();
        current = flare;
        return flare;
    }

    // True while the fire covers this point: past the line, on the burning side.
    public bool Burns(Vector3 point) => age >= Warning + Erupt * .8f && Burning && (point.x - Line) * side > .1f;

    void Build()
    {
        if (fireTemplate == null)
            fireTemplate = new Material(Resources.Load<Shader>("SolarFire")) { name = "Solar fire" };
        fire = new Material(fireTemplate);
        fire.SetFloat("_Boundary", Boundary);
        // One sheet from beyond the outer edge to the dividing line, mirrored for the right half.
        float top = Camera.UpLimit + 3, bottom = Camera.DownLimit - 3;
        sheet = new Mesh { name = "Solar fire sheet" };
        sheet.vertices = new[] { new Vector3(-.5f, bottom), new Vector3(Boundary + .5f, bottom), new Vector3(Boundary + .5f, top), new Vector3(-.5f, top) };
        sheet.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        sheet.RecalculateBounds();
        var wall = new GameObject("Fire wall").transform;
        wall.SetParent(transform, false);
        wall.position = new Vector3(Outer + side, 0, 1.5f);
        wall.localScale = new Vector3(-side, 1, 1);
        wall.gameObject.AddComponent<MeshFilter>().sharedMesh = sheet;
        var renderer = wall.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = fire;
        // Under every ship and round: enemies in the fire stay readable.
        renderer.sortingLayerName = "Enemy";
        renderer.sortingOrder = -5;
        DistrictHazards.Play("eventRumble");
        Pose();
    }

    void Update()
    {
        if (WorldTimeStop.Frozen || !GameClock.ActivePlay)
            return;
        age += Time.deltaTime;
        if (!DistrictHazards.Open && age < Warning + Erupt + Burn)
            age = Warning + Erupt + Burn; // The wave ended: recede without harm.
        Pose();
        if (age < Warning)
            Embers(age / Warning);
        else if (!erupted && Burning)
            Eruption();
        if (Burning)
        {
            RollingFire();
            Hurt();
        }
        if (age >= Warning + Erupt + Burn + Recede)
            Destroy(gameObject);
    }

    // Exposed for the review renders: pose the flare at any moment of its life.
    public void PoseAt(float seconds)
    {
        age = seconds;
        Pose();
    }

    void Pose()
    {
        float heat = Mathf.Clamp01(age / Warning);
        float ending = Mathf.Clamp01((age - Warning - Erupt - Burn) / Recede);
        // While it warns, flames lick in from the outer edge (harmless); then the front erupts.
        float licking = (1 + 1.05f * heat) / (Boundary + 1);
        float fill = age < Warning ? licking * heat : age < Warning + Erupt ? Mathf.Lerp(licking, 1, Mathf.SmoothStep(0, 1, (age - Warning) / Erupt)) : 1 - ending;
        float fade = 1 - ending;
        fire.SetFloat("_Clock", age);
        fire.SetFloat("_Heat", heat * fade);
        fire.SetFloat("_Warn", age < Warning ? heat : 0);
        fire.SetFloat("_Fill", fill);
        fire.SetFloat("_Fade", fade);
    }

    // Embers drift up out of the doomed half, thicker as the eruption nears.
    void Embers(float heat)
    {
        emberTimer -= Time.deltaTime;
        if (emberTimer > 0)
            return;
        emberTimer = Mathf.Lerp(.16f, .05f, heat);
        float x = Mathf.Lerp(Outer, Line, Random.Range(0f, .85f));
        PSXImpactEffects.Burst(new Vector3(x, Random.Range(Camera.DownLimit, Camera.UpLimit), -1), Hot, false, Vector2.up);
    }

    // The flare bursts out of the outer edge in a column of fireballs.
    void Eruption()
    {
        erupted = true;
        DistrictHazards.Play("eventEruption");
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(3.2f, 12, .05f, .7f);
        for (int i = 0; i < 5; i++)
            PSXFireballs.Spawn(new Vector3(Outer - side * .3f, Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, (i + .5f) / 5), -1), 1.5f, 3, .6f, .12f, Fireball);
    }

    // A chain of fireballs rides the advancing front, then the burning half keeps flaring.
    void RollingFire()
    {
        if (age < fireballAt)
            return;
        bool advancing = age < Warning + Erupt;
        fireballAt = age + (advancing ? .06f : .4f);
        float progress = advancing ? Mathf.SmoothStep(0, 1, (age - Warning) / Erupt) : Random.Range(.15f, .8f);
        float x = Mathf.Lerp(Outer, Line + side * .9f, progress);
        float y = Random.Range(Camera.DownLimit + .5f, Camera.UpLimit - .5f);
        PSXFireballs.Spawn(new Vector3(x, y, -1), advancing ? 1.25f : .8f, advancing ? 2 : 1, .5f, .08f, Fireball);
    }

    void Hurt()
    {
        var pilot = CombatVisuals.Player;
        if (pilot != null && Burns(pilot.position))
            pilot.SendMessage("ReceiveHazardDamage", 1f, SendMessageOptions.DontRequireReceiver);
    }

    static void Release(Object asset)
    {
        if (asset == null)
            return;
        if (Application.isPlaying)
            Destroy(asset);
        else
            DestroyImmediate(asset);
    }

    void OnDestroy()
    {
        Release(fire);
        Release(sheet);
    }
}
