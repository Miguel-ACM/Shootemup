using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// A pressure door covers the portrait aperture while the title shaft becomes the dock.
public sealed class HangarShutter : MonoBehaviour
{
    RectTransform upper, lower;
    public static bool Active
    {
        get; private set;
    }

    public static void Enter(RunDifficulty difficulty)
    {
        if (Active)
            return;
        var door = new GameObject("Hangar pressure door", typeof(RectTransform)).AddComponent<HangarShutter>();
        DontDestroyOnLoad(door.gameObject);
        door.StartCoroutine(door.Transfer(difficulty, null));
    }

    public static void Depart(System.Action launch)
    {
        if (Active)
            return;
        var door = new GameObject("Simulator pressure door", typeof(RectTransform)).AddComponent<HangarShutter>();
        DontDestroyOnLoad(door.gameObject);
        door.StartCoroutine(door.Transfer(null, launch));
    }

    IEnumerator Transfer(RunDifficulty? difficulty, System.Action launch)
    {
        Active = true;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32765;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var aperture = DockInstrument.Block(transform, "Pressure door aperture", Vector2.zero, new Vector2(405, 720), Color.clear);
        aperture.gameObject.AddComponent<RectMask2D>();
        upper = Leaf(aperture, "Upper leaf");
        lower = Leaf(aperture, "Lower leaf");
        for (float t = 0; t < .65f; t += Time.unscaledDeltaTime)
        {
            Place(Mathf.SmoothStep(0, 1, t / .65f));
            yield return null;
        }
        Place(1);
        if (difficulty.HasValue)
            HangarHub.Enter(difficulty.Value);
        else
            launch();
        yield return null;
        if (!difficulty.HasValue)
            while (HangarHub.Active || HangarHub.Launching)
                yield return null;
        while (CloudTransition.Busy)
            yield return null;
        // The docks finish their arrival behind the closed bulkhead.
        yield return new WaitForSecondsRealtime(1.6f);
        for (float t = 0; t < .85f; t += Time.unscaledDeltaTime)
        {
            Place(1 - Mathf.SmoothStep(0, 1, t / .85f));
            yield return null;
        }
        Active = false;
        Destroy(gameObject);
    }

    RectTransform Leaf(Transform parent, string title)
    {
        var image = new GameObject(title, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(parent, false);
        image.texture = Resources.Load<Texture2D>("DockArt/PressureDoor");
        image.uvRect = new Rect(0, 0, 1, 1);
        image.color = new Color(.65f, .72f, .73f);
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(405, 362);
        return image.rectTransform;
    }

    void Place(float closed)
    {
        upper.anchoredPosition = new Vector2(0, Mathf.Lerp(542, 180, closed));
        lower.anchoredPosition = new Vector2(0, -Mathf.Lerp(542, 180, closed));
    }

    void OnDestroy()
    {
        Active = false;
    }
}
