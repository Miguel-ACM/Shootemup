using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed partial class SnowSiegeBoss : DistrictBoss
{
    readonly Transform[] comb = new Transform[13];
    readonly SpriteRenderer[] apertures = new SpriteRenderer[13];
    readonly Vector3[] launch = new Vector3[13], lanes = new Vector3[13];
    readonly float[] releases = new float[13];
    float deployment;
    int combCount;
    bool deployed, returning;

    protected override void Start()
    {
        base.Start();
        for (int i = 0; i < comb.Length; i++)
        {
            var go = new GameObject("Snow comb emitter");
            comb[i] = go.transform;
            comb[i].SetParent(transform, false);
            comb[i].localScale = Vector3.one * .32f;
            go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.SnowCombEmitter();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CombatMaterials.Fleet;
            renderer.sortingLayerName = "Enemy";
            apertures[i] = ItemSpark.Sprite(comb[i], "Charging comb aperture", "standard", LevelProjectilePalette.Accent(LevelRun.Current), .35f);
            apertures[i].transform.localPosition = new Vector3(0, -.26f, -.55f);
            firingCue.Register(comb[i], new Vector3(0, -.26f, -.55f));
            go.SetActive(false);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!deployed || GameRules.playerDisabled)
            return;
        deployment += Time.deltaTime * LevelRun.EnemyTempoScale;
        float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(deployment / .32f));
        Vector3 muzzle = Hull.TransformPoint(new Vector3(0, -.65f, -.25f));
        for (int i = 0; i < combCount; i++)
        {
            releases[i] = Mathf.Max(0, releases[i] - Time.deltaTime);
            comb[i].position = returning ? Vector3.Lerp(launch[i], muzzle, t) : Vector3.Lerp(launch[i], lanes[i], t);
            comb[i].rotation = Quaternion.identity;
            var color = LevelProjectilePalette.Accent(LevelRun.Current);
            color.a = returning ? 1 - t : .3f + .7f * t;
            apertures[i].color = color;
            apertures[i].transform.localScale = Vector3.one * (.35f + .3f * releases[i] / .1f);
            if (returning && t >= 1)
                comb[i].gameObject.SetActive(false);
        }
        if (returning && t >= 1)
            deployed = false;
    }

    void DeployComb()
    {
        combCount = (phaseTwo ? 11 : 9);
        deployment = 0;
        deployed = true;
        returning = false;
        for (int i = 0; i < combCount; i++)
        {
            launch[i] = Hull.TransformPoint(new Vector3((i % 2 == 0 ? -1 : 1) * .38f, -.65f, -.25f));
            float x = Mathf.Lerp(Camera.LeftLimit + .35f, Camera.RightLimit - .35f, (i + .5f) / combCount);
            // The comb lines the top edge of the screen, so its rain covers the whole arena.
            lanes[i] = new Vector3(x, Camera.UpLimit - .35f, 0);
            comb[i].position = launch[i];
            comb[i].gameObject.SetActive(true);
        }
    }

    protected override void Configure()
    {
        hull = 21;
        hullScale = 1.6f;
        health = 2100;
        attackInterval = (.85f);
        shellTravelDistance = 6.6f;
    }

    protected override void MoveBoss(float dt)
    {
        station.x = Mathf.Sin(motionAge * .45f) * 1.8f;
        base.MoveBoss(dt);
    }

    protected override void PressureAttack()
    {
    } // The score below owns the rests as well as the volleys.
    protected override void OnPhaseTwo()
    {
        deployed = false;
        foreach (var emitter in comb)
            if (emitter != null)
                emitter.gameObject.SetActive(false);
        volley = 0;
        nextAttack = age + 1.1f;
    }
}
