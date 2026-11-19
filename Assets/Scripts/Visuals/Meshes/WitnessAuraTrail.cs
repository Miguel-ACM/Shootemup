using UnityEngine;

// The delayed images read the same black mask as the live aura, captured
// together with its world transform so a turn leaves the old claws behind.
sealed class WitnessAuraTrail
{
    const float Delay = .30f;
    const int Copies = 4;
    const int SamplesPerDelay = 4;
    const float SampleInterval = Delay / SamplesPerDelay;
    static readonly int Mask = Shader.PropertyToID("_Silhouette");
    static readonly int Opacity = Shader.PropertyToID("_Opacity");

    struct Sample
    {
        public RenderTexture mask;
        public Pose pose;
        public float time;
    }

    readonly Sample[] history = new Sample[Copies * SamplesPerDelay + 3];
    readonly MeshRenderer[] echoes = new MeshRenderer[Copies];
    readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    readonly Transform live;
    readonly RenderTexture silhouette;
    int latest = -1, count;
    float clock, nextSample;

    public WitnessAuraTrail(MeshRenderer aura, RenderTexture silhouette)
    {
        live = aura.transform;
        this.silhouette = silhouette;
        for (int i = 0; i < history.Length; i++)
        {
            var texture = new RenderTexture(silhouette.descriptor) {
                name = "Witness afterimage mask", filterMode = silhouette.filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            history[i].mask = texture;
        }
        for (int i = 0; i < echoes.Length; i++)
        {
            var echo = Object.Instantiate(aura, live.parent, false);
            echo.name = "Witness afterimage " + (i + 1);
            echo.sortingOrder = aura.sortingOrder - i - 1;
            echo.gameObject.SetActive(false);
            echoes[i] = echo;
        }
    }

    public void Advance(float dt)
    {
        clock += dt;
        if (clock >= nextSample)
        {
            latest = (latest + 1) % history.Length;
            ref var sample = ref history[latest];
            sample.time = clock;
            sample.pose = new Pose(live.position, live.rotation);
            Graphics.CopyTexture(silhouette, sample.mask);
            count = Mathf.Min(count + 1, history.Length);
            // Drop missed captures after a hitch rather than storing the same pose twice.
            nextSample += (1 + Mathf.Floor((clock - nextSample) / SampleInterval)) * SampleInterval;
        }

        int newer = latest;
        int remaining = count - 1;
        for (int i = 0; i < echoes.Length; i++)
        {
            float target = clock - (i + 1) * Delay;
            int older = (newer + history.Length - 1) % history.Length;
            while (remaining > 1 && history[older].time > target)
            {
                newer = older;
                older = (older + history.Length - 1) % history.Length;
                remaining--;
            }
            if (remaining < 1 || target < history[older].time) break;
            ref var a = ref history[older];
            ref var b = ref history[newer];
            float t = Mathf.InverseLerp(a.time, b.time, target);
            var echo = echoes[i];
            echo.transform.SetPositionAndRotation(Vector3.Lerp(a.pose.position,b.pose.position,t),
                Quaternion.Slerp(a.pose.rotation,b.pose.rotation,t));
            properties.SetTexture(Mask,t < .5f ? a.mask : b.mask);
            properties.SetFloat(Opacity,.64f * Mathf.Pow(.66f,i));
            echo.SetPropertyBlock(properties);
            if (!echo.gameObject.activeSelf) echo.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        foreach (var echo in echoes) echo.gameObject.SetActive(false);
    }

    public void Release()
    {
        foreach (var sample in history) Object.Destroy(sample.mask);
    }
}
