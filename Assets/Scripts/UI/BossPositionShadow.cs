using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A quiet bottom-edge shadow for each encounter hull, including both City partners.
public sealed class BossPositionShadow : MonoBehaviour
{
    readonly Dictionary<AEnemy, RectTransform> markers = new Dictionary<AEnemy, RectTransform>();
    readonly List<AEnemy> expired = new List<AEnemy>();
    Canvas canvas;
    float scan;

    void Start()
    {
        var root = new GameObject("Boss position shadows", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
    }

    void LateUpdate()
    {
        var camera = UnityEngine.Camera.main;
        if (canvas == null || camera == null)
            return;
        if (Time.unscaledTime >= scan)
        {
            scan = Time.unscaledTime + .15f;
            foreach (var enemy in AEnemy.Live)
            {
                if (!(enemy is DistrictBoss) && !(enemy is DistrictMiniboss) && !(enemy is Boss))
                    continue;
                if (markers.ContainsKey(enemy))
                    continue;
                var go = new GameObject("Encounter hull shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(BossShadowGraphic));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
                rect.sizeDelta = new Vector2(enemy is DistrictMiniboss ? 29 : 42, 20);
                markers.Add(enemy, rect);
            }
        }
        expired.Clear();
        foreach (var pair in markers)
        {
            var enemy = pair.Key;
            if (enemy == null || enemy.health <= 0)
            {
                Destroy(pair.Value.gameObject);
                expired.Add(enemy);
                continue;
            }
            var point = camera.WorldToViewportPoint(enemy.transform.position);
            bool visible = point.y >= 0 && point.y <= 1 && !GameOverState.Active &&
                !(enemy is DistrictMiniboss mini && mini.Retreating);
            pair.Value.gameObject.SetActive(visible);
            float width = Screen.width * 720f / Mathf.Max(1, Screen.height);
            float halfArena = camera.orthographicSize * 405f / 720f;
            float arenaX = Mathf.Clamp(enemy.transform.position.x - camera.transform.position.x, -halfArena, halfArena);
            pair.Value.anchoredPosition = new Vector2(arenaX / (camera.orthographicSize * camera.aspect * 2) * width, 10);
        }
        foreach (var enemy in expired)
            markers.Remove(enemy);
    }
}

public sealed class BossShadowGraphic : MaskableGraphic
{
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float half = rectTransform.rect.width * .5f;
        for (int ring = 0; ring < 4; ring++)
        {
            float spread = 1 - ring * .15f;
            Color ink = new Color(.015f, .025f, .035f, .13f + ring * .08f);
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(0, -7 + ring, 0), ink, Vector2.zero);
            for (int i = 0; i <= 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20;
                vh.AddVert(new Vector3(Mathf.Cos(a) * half * spread, -7 + ring + Mathf.Sin(a) * 8 * spread), ink, Vector2.zero);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }
        int arrow = vh.currentVertCount;
        Color edge = new Color(.73f, .78f, .75f, .85f);
        vh.AddVert(new Vector3(-5, 0, 0), edge, Vector2.zero);
        vh.AddVert(new Vector3(0, 7, 0), edge, Vector2.zero);
        vh.AddVert(new Vector3(5, 0, 0), edge, Vector2.zero);
        vh.AddTriangle(arrow, arrow + 1, arrow + 2);
    }
}

