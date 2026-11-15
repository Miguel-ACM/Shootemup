using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DamageKind
{
    Direct, Burn, Poison
}

// Damage numbers over struck enemies (Miguel, 15 November 2026): a settings option, on by default.
// Hits on one target gather into a single number for a moment (it pops with each addition), then
// it rises and fades, so rapid fire reads as a few clean figures instead of a spray. Burn and
// poison gather into their own coloured numbers. Values show at ten times the internal damage,
// rounded, so a burn tick never reads as .06. The labels are pooled and clipped to the combat area.
public sealed class DamageNumbers : MonoBehaviour
{
    public const float Scale = 10;
    const int Capacity = 48;
    // A number gathers hits for at most Gather seconds, or until Quiet seconds pass without one.
    const float Gather = .5f, Quiet = .22f, Rise = .65f, RiseHeight = .55f;
    static readonly Color DirectInk = new Color(1, .94f, .8f);
    static readonly Color BurnInk = Color.Lerp(PlayerShotPalette.Fire, Color.white, .25f);
    static readonly Color PoisonInk = Color.Lerp(PlayerShotPalette.Poison, Color.white, .2f);

    // Set by CombatHit.Affliction around each burn or poison tick.
    public static DamageKind TickKind = DamageKind.Burn;

    sealed class Number
    {
        public TextMeshProUGUI label;
        public AEnemy target;
        public DamageKind kind;
        public float total, born, lastHit, released = -1, pulse;
        public Vector3 offset, world;
        public bool live;
    }

    static DamageNumbers instance;
    readonly Number[] numbers = new Number[Capacity];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance() => instance = null;

    public static void Report(AEnemy target, float applied, Vector3 point, DamageKind kind)
    {
        if (!GameOptions.DamageNumbers || applied <= 0 || target == null || !Application.isPlaying)
            return;
        if (instance == null)
            instance = new GameObject("Damage numbers").AddComponent<DamageNumbers>();
        instance.Add(target, applied, point, kind);
    }

    void Awake()
    {
        var dialog = FindAnyObjectByType<Dialog>();
        var font = dialog != null ? dialog.font : null;
        if (font == null)
            foreach (var label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
                if (label.font != null)
                {
                    font = label.font;
                    break;
                }
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the scenery and ships, beneath the route choice, title reveal and pause menu.
        canvas.sortingOrder = 3;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var frame = new GameObject("Combat area", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        frame.SetParent(transform, false);
        frame.sizeDelta = new Vector2(405, 720);
        // One shared outlined material keeps the figures readable over bright scenery.
        Material outlined = null;
        if (font != null)
        {
            outlined = new Material(font.material) { name = "Damage number outline" };
            outlined.EnableKeyword(ShaderUtilities.Keyword_Outline);
            outlined.SetFloat(ShaderUtilities.ID_OutlineWidth, .28f);
            outlined.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0, 0, 0, .9f));
        }
        for (int i = 0; i < Capacity; i++)
        {
            var go = new GameObject("Damage number", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(frame, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                label.font = font;
                label.fontSharedMaterial = outlined;
            }
            label.fontSize = 15;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(90, 24);
            go.SetActive(false);
            numbers[i] = new Number { label = label };
        }
    }

    void Add(AEnemy target, float applied, Vector3 point, DamageKind kind)
    {
        float now = Time.time;
        Number free = null, oldest = null;
        foreach (var n in numbers)
        {
            if (!n.live)
            {
                free ??= n;
                continue;
            }
            if (n.target == target && n.kind == kind && n.released < 0)
            {
                n.total += applied;
                n.lastHit = now;
                n.pulse = 1;
                Show(n);
                return;
            }
            if (oldest == null || n.born < oldest.born)
                oldest = n;
        }
        var number = free ?? oldest;
        number.live = true;
        number.target = target;
        number.kind = kind;
        number.total = applied;
        number.born = number.lastHit = now;
        number.released = -1;
        number.pulse = 1;
        // Ticks carry no impact point; scatter them a little around the hull's centre.
        var jitter = kind == DamageKind.Direct ? Vector3.zero : new Vector3(Random.Range(-.25f, .25f), Random.Range(-.1f, .2f));
        number.offset = point + jitter - target.transform.position;
        number.world = point + jitter;
        number.label.color = kind == DamageKind.Burn ? BurnInk : kind == DamageKind.Poison ? PoisonInk : DirectInk;
        number.label.gameObject.SetActive(true);
        Show(number);
    }

    static void Show(Number n) => n.label.SetText("{0:0}", Mathf.Max(1, Mathf.Round(n.total * Scale)));

    void LateUpdate()
    {
        var view = UnityEngine.Camera.main;
        float now = Time.time, dt = Time.deltaTime;
        foreach (var n in numbers)
        {
            if (!n.live)
                continue;
            if (!GameOptions.DamageNumbers || view == null)
            {
                Free(n);
                continue;
            }
            bool targetGone = n.target == null || !n.target.gameObject.activeInHierarchy;
            if (n.released < 0)
            {
                if (!targetGone)
                    n.world = n.target.transform.position + n.offset;
                if (targetGone || now - n.lastHit >= Quiet || now - n.born >= Gather)
                    n.released = now;
            }
            float t = n.released < 0 ? 0 : (now - n.released) / Rise;
            if (t >= 1)
            {
                Free(n);
                continue;
            }
            n.pulse = Mathf.Max(0, n.pulse - dt * 7);
            float lift = RiseHeight * (1 - (1 - t) * (1 - t));
            var color = n.label.color;
            color.a = 1 - Mathf.SmoothStep(0, 1, (t - .45f) / .55f);
            n.label.color = color;
            n.label.rectTransform.localScale = Vector3.one * (1 + .35f * n.pulse);
            // The camera renders into the low-resolution scene texture, so go through the viewport.
            var viewport = view.WorldToViewportPoint(n.world + Vector3.up * (.12f + lift));
            n.label.rectTransform.position = new Vector3(viewport.x * Screen.width, viewport.y * Screen.height);
        }
    }

    static void Free(Number n)
    {
        n.live = false;
        n.target = null;
        n.label.gameObject.SetActive(false);
    }
}
