using UnityEngine;

// Continuous energy with a visible capacitor, ignition expansion and cooling tail.
// The dangerous body stays straight; decorative motion never changes its collision path.
public sealed class BeamRibbon : MonoBehaviour
{
    static Material energy;
    static Sprite emitterGlow;
    LineRenderer body, sheath, tell, collar, intake;
    SpriteRenderer mouth;
    Color hot, cool;
    bool playerBeam;
    public void Build(bool friendly = false)
    {
        playerBeam = friendly;
        hot = friendly ? new Color(.24f, .68f, 1) : LevelProjectilePalette.Accent(LevelRun.Current);
        cool = hot * .45f;
        cool.a = 1;
        if (energy == null)
            energy = new Material(Resources.Load<Shader>("ContinuousBeam")) { name = "Continuous plasma" };
        sheath = CombatVisuals.Line(transform, cool, .5f);
        sheath.sharedMaterial = energy;
        sheath.sortingOrder = 19;
        body = CombatVisuals.Line(transform, hot, .2f);
        body.sharedMaterial = energy;
        body.sortingOrder = 21;
        tell = CombatVisuals.Line(transform, hot, .035f);
        tell.sortingOrder = 20;
        collar = CombatVisuals.Line(transform, hot, .035f);
        collar.sortingOrder = 22;
        intake = CombatVisuals.Line(transform, hot, .025f);
        intake.sortingOrder = 22;
        var go = new GameObject("Emitter plasma");
        go.transform.SetParent(transform, false);
        mouth = go.AddComponent<SpriteRenderer>();
        mouth.sprite = EmitterGlow();
        mouth.sharedMaterial = CombatVisuals.ProjectileMaterial;
        mouth.sortingLayerName = "Bullet";
        mouth.sortingOrder = 23;
    }

    public void Draw(Vector3 p, Vector2 direction, float length, float width, float charge, float power, float release = 0)
    {
        if (!playerBeam)
        {
            hot = LevelProjectilePalette.Accent(LevelRun.Current);
            cool = hot * .45f;
            cool.a = 1;
        }
        p.z = -1.05f;
        var end = p + (Vector3)direction * length;
        float flutter = .96f + .04f * Mathf.Sin(Time.time * 53);
        bool charging = charge < 1 && release <= 0;
        tell.enabled = charging;
        if (charging)
        {
            tell.SetPosition(0, p);
            tell.SetPosition(1, end);
            tell.startColor = tell.endColor = new Color(hot.r, hot.g, hot.b, .46f + charge * .30f);
            tell.startWidth = tell.endWidth = .060f + charge * .015f;
        }
        body.enabled = sheath.enabled = power > .003f;
        float extent = charging ? 0 : Mathf.Clamp01(power * 8 + release) * length;
        body.SetPosition(0, p);
        body.SetPosition(1, p + (Vector3)direction * extent);
        sheath.SetPosition(0, p);
        sheath.SetPosition(1, p + (Vector3)direction * extent);
        body.startWidth = body.endWidth = width * power * flutter;
        body.startColor = body.endColor = new Color(hot.r, hot.g, hot.b, Mathf.Min(1, power * 1.8f));
        sheath.startWidth = sheath.endWidth = width * (1.6f + power * .8f);
        sheath.startColor = sheath.endColor = new Color(cool.r, cool.g, cool.b, power * .18f);
        float heat = charging ? charge * .65f : power;
        mouth.enabled = heat > .015f;
        mouth.transform.position = p;
        mouth.transform.localScale = Vector3.one * Mathf.Min(.36f, .08f + width * heat * .95f);
        var glow = Color.Lerp(hot, Color.white, heat * .18f);
        glow.a = heat * .9f;
        mouth.color = glow;
        collar.enabled = intake.enabled = heat > .01f;
        CombatVisuals.Ring(collar, p, .05f + width * .45f * heat);
        collar.startColor = collar.endColor = new Color(hot.r, hot.g, hot.b, heat * .7f);
        CombatVisuals.Ring(intake, p, charging ? Mathf.Lerp(.42f, .12f, charge) : .12f + width * .8f);
        intake.startColor = intake.endColor = new Color(hot.r, hot.g, hot.b, charging ? Mathf.Sin(charge * Mathf.PI) * .5f : power * .16f);
    }

    static Sprite EmitterGlow()
    {
        if (emitterGlow != null)
            return emitterGlow;
        // Neutral light takes the beam's tint; an ordnance sprite made hostile muzzles cyan.
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Beam emitter light",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - radius), 1.3f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        texture.SetPixels(pixels);
        texture.Apply();
        emitterGlow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        emitterGlow.name = "Compact beam emitter";
        return emitterGlow;
    }

}

