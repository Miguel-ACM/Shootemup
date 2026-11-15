using UnityEngine;

// Scenery hazard: no enemy registry entry, reward or wave-clear requirement.
// In an ERUPTION the craters glow, charge for longer behind a pulsing ring and aim lines that show
// exactly where the rocks will fly, then throw a wider fan plus rocks lobbed high that fall back.
public sealed class VolcanicVent : MonoBehaviour
{
    public float phase;
    public bool Eruption;
    float readyAt = -1, next, charge, embers;
    bool rumbled;
    ParticleSystem fire;
    LineRenderer ring;
    LineRenderer[] tells;
    float aimSide;
    public int Eruptions
    {
        get; private set;
    }

    float ChargeSeconds => Eruption ? 2.4f : 1.15f;
    int FanCount => Eruption ? (Difficulty.Easy ? 6 : Difficulty.Hard ? 11 : 9) : 5;

    void Start()
    {
        fire = PSXImpactEffects.Create(transform, "Crater fire", Eruption ? 160 : 80, true);
        var renderer = fire.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -34;
        if (!Eruption)
            return;
        ring = CombatVisuals.Line(null, Color.clear, .06f);
        ring.name = "Eruption warning ring";
        ring.useWorldSpace = true;
        ring.sortingLayerName = "Enemy";
        ring.sortingOrder = -3;
        tells = new LineRenderer[FanCount];
        for (int i = 0; i < tells.Length; i++)
        {
            tells[i] = CombatVisuals.Line(null, Color.clear, .05f);
            tells[i].name = "Eruption aim line";
            tells[i].useWorldSpace = true;
            tells[i].sortingLayerName = "Enemy";
            tells[i].sortingOrder = -3;
        }
    }

    void OnDestroy()
    {
        if (ring != null)
            Destroy(ring.gameObject);
        if (tells != null)
            foreach (var tell in tells)
                if (tell != null)
                    Destroy(tell.gameObject);
    }

    Vector2 Axis(Vector3 p) => CombatVisuals.Rotate(Vector2.down, Mathf.Sign(p.x) * -24);
    float Spread => Eruption ? 17 : 25;

    void Update()
    {
        if (fire == null)
            return;
        float dt = Time.deltaTime;
        var p = transform.position;
        bool visible = p.y > Camera.DownLimit + 1.8f && p.y < Camera.UpLimit - 1.2f && Mathf.Abs(p.x) < 5.2f;
        bool combat = visible && !GameRules.playerDisabled && !CloudTransition.Busy && WavePreparation.Ready && LevelRun.Current == LevelId.Hell;
        if (Eruption)
            combat &= DistrictHazards.Open;
        foreach (var enemy in AEnemy.Live)
            if (enemy is DistrictBoss)
            {
                combat = false;
                break;
            }
        if (!combat)
        {
            readyAt = -1;
            charge = 0;
            rumbled = false;
            ShowWarning(p, 0);
            return;
        }
        if (readyAt < 0)
        {
            readyAt = Time.time + WaveOrchestrer.OpeningBreather;
            next = readyAt + .8f + phase * (Eruption ? .45f : .22f) + (Eruption ? ChargeSeconds : 0);
        }
        charge = Mathf.Clamp01(1 - (next - Time.time) / ChargeSeconds);
        if (Eruption && charge > 0 && !rumbled)
        {
            rumbled = true;
            DistrictHazards.Play("eventRumble");
        }
        ShowWarning(p, Eruption ? charge : 0);
        embers += dt * (3 + charge * (Eruption ? 26 : 12));
        while (embers >= 1)
        {
            embers--;
            var e = new ParticleSystem.EmitParams();
            e.position = p + new Vector3(Random.Range(-.14f, .14f), Random.Range(-.12f, .12f), -1.02f);
            e.velocity = new Vector3(Random.Range(-.25f, .25f), .6f + charge * (Eruption ? 2.2f : 1.1f), -.15f);
            float size = Random.Range(.09f, .18f) * (1 + charge * (Eruption ? .8f : .35f));
            e.startSize3D = new Vector3(size, size * Mathf.Lerp(1.2f, 2f, charge), size);
            e.startLifetime = Mathf.Lerp(.55f, .75f, charge);
            e.startColor = Eruption ? Color.Lerp(new Color(.6f, .2f, .05f, .45f), new Color(1, .6f, .16f, .8f), charge) :
                Color.Lerp(new Color(.34f, .095f, .025f, .28f), new Color(.62f, .25f, .055f, .48f), charge);
            fire.Emit(e, 1);
        }
        if (Time.time < next || Time.time < readyAt)
            return;
        next = Time.time + (Eruption ? 6.4f + phase * .5f + ChargeSeconds : 9.5f + phase);
        rumbled = false;
        Eruptions++;
        var axis = Axis(p);
        int count = FanCount;
        // Pooled volcanic rock: dark rounds with a glowing rim.
        CombatRound.LaunchTone = HostileTone.Dark;
        for (int i = 0; i < count; i++)
            CombatRound.Launch<SimpleRound>(p, CombatVisuals.Rotate(axis, (i - (count - 1) * .5f) * Spread), Eruption ? 2.8f : 2.5f, Eruption ? .27f : .24f);
        if (Eruption)
        {
            // Rocks thrown high fall back across the arena under gravity.
            int lobbed = Difficulty.Easy ? 2 : 4;
            for (int i = 0; i < lobbed; i++)
            {
                var up = CombatVisuals.Rotate(Vector2.up, (i - (lobbed - 1) * .5f) * 22 + Mathf.Sign(p.x) * -12);
                CombatRound.Launch<SimpleRound>(p, up, 3.4f + i % 2 * .5f, .3f).Fall(new Vector2(0, -2.4f));
            }
            PSXImpactEffects.Burst(p + Vector3.back, new Color(1, .55f, .15f), true);
            DistrictHazards.Play("eventEruption");
            if (EZCameraShake.CameraShaker.Instance != null)
                EZCameraShake.CameraShaker.Instance.ShakeOnce(1.6f, 10, .03f, .4f);
        }
        else
            SoundManager.PlaySound("enemyExplosion1", .055f);
        CombatRound.LaunchTone = HostileTone.Bright;
    }

    // A ring tightening on the crater and faint lines along each rock's path, brightening to the release.
    void ShowWarning(Vector3 p, float strength)
    {
        if (ring == null)
            return;
        bool show = strength > 0;
        ring.enabled = show;
        foreach (var tell in tells)
            tell.enabled = show;
        if (!show)
            return;
        float flicker = Mathf.Sin(Time.time * Mathf.Lerp(8, 34, strength)) > 0 ? 1 : .55f;
        var hot = Color.Lerp(new Color(1, .45f, .12f), new Color(1, .95f, .7f), strength);
        hot.a = Mathf.Lerp(.25f, .95f, strength) * flicker;
        var centre = new Vector3(p.x, p.y, -1.1f);
        CombatVisuals.Ring(ring, centre, Mathf.Lerp(1.4f, .55f, strength));
        ring.startColor = ring.endColor = hot;
        var axis = Axis(p);
        int count = tells.Length;
        float reach = Mathf.Lerp(.8f, 2.6f, strength);
        for (int i = 0; i < count; i++)
        {
            var direction = (Vector3)CombatVisuals.Rotate(axis, (i - (count - 1) * .5f) * Spread);
            tells[i].positionCount = 2;
            tells[i].SetPosition(0, centre + direction * .6f);
            tells[i].SetPosition(1, centre + direction * (.6f + reach));
            var faint = hot;
            faint.a *= .55f;
            tells[i].startColor = faint;
            tells[i].endColor = new Color(faint.r, faint.g, faint.b, 0);
        }
    }
}
