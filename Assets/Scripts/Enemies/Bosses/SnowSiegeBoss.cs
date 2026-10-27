using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SnowSiegeBoss : DistrictBoss
{
    Vector2 shellAxis = Vector2.down;
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
            lanes[i] = new Vector3(x, Hull.GetComponent<MeshRenderer>().bounds.min.y - .25f, 0);
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
    protected override void Attack()
    {
        int beat = (volley - 1) % 14;
        if (beat == 0)
            shellAxis = Aim;
        if (beat == 0 || beat == 2 || beat == 4)
        {
            // Three broad crowns leave room for complete rings instead of queueing
            // oversized shell clusters against the shared projectile cap.
            int count = (phaseTwo ? 5 : 4);
            float distance = beat == 2 ? shellTravelDistance - 2 : shellTravelDistance;
            for (int i = 0; i < count; i++)
            {
                var shot = Fire<BurstShell>(Hull.TransformPoint(new Vector3(0, -.65f, -.25f)),
                    CombatVisuals.Rotate(shellAxis, (i - (count - 1) * .5f) * 30 + (beat == 2 ? 7.5f : -7.5f)), 3.0f, .48f);
                if (shot != null)
                    shot.SetTravelDistance(distance);
            }
        }
        else if (beat == 5)
            DeployComb();
        else if (beat == 6 || beat == 8)
        {
            // Two straight combs answer the shell crowns; the second fills the first's lanes.
            for (int i = 0; i < combCount; i++)
            {
                var port = comb[i].TransformPoint(new Vector3(0, -.26f, -.55f));
                if (beat == 6)
                    Fire<SuspendedRound>(port, Vector2.down, 2.8f, .23f);
                else
                    Fire<SimpleRound>(port, Vector2.down, 2.0f, .28f);
                releases[i] = .1f;
            }
        }
        else if (beat == 7)
        {
            deployment = 0;
            for (int i = 0; i < combCount; i++)
            {
                launch[i] = comb[i].position;
                lanes[i].x += (Camera.RightLimit - Camera.LeftLimit - .7f) / combCount * .5f;
            }
        }
        else if (beat == 9)
        {
            deployment = 0;
            returning = true;
            for (int i = 0; i < combCount; i++)
                launch[i] = comb[i].position;
        }
        else if (beat == 11 || beat == 12 && phaseTwo)
        {
            int sign = beat == 11 ? -1 : 1;
            int count = (phaseTwo ? 11 : 9);
            PetalFan<NeedleRound>(Hull.TransformPoint(new Vector3(0, -.65f, -.25f)),
                CombatVisuals.Rotate(Vector2.down, sign * 8), count, 15, 3.7f, .19f, .18f);
        }
        // Beats 3, 5, 7, 10 and 11 are intentional rests while existing patterns unfold.
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        int beat = volley % 14;
        if (beat == 6 || beat == 8)
        {
            for (int i = 0; i < combCount; i++)
                if (comb[i] != null)
                    Cue(comb[i].TransformPoint(new Vector3(0, -.26f, -.55f)), seconds);
        }
        else if (beat == 0 || beat == 2 || beat == 4 || beat == 11 || beat == 12 && phaseTwo)
            Cue(Muzzle(), seconds);
    }

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
