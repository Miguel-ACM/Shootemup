using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Preserve the last rendered view across scene activation. The new deck dissolves in
// when its surfaces and exhibits are ready, including when returning from the simulator.
public sealed class HangarShutter : MonoBehaviour
{
    public static bool Active
    {
        get; private set;
    }
    RenderTexture previous;

    public static void Enter(RunDifficulty difficulty) => Begin(() => HangarHub.Enter(difficulty), true);
    public static void Depart(Action launch) => Begin(launch, false);
    static void Begin(Action covered, bool entering)
    {
        if (Active)
            return;
        var transfer = new GameObject("Dock cross dissolve", typeof(RectTransform)).AddComponent<HangarShutter>();
        DontDestroyOnLoad(transfer.gameObject);
        transfer.StartCoroutine(transfer.Transfer(covered, entering));
    }
    IEnumerator Transfer(Action covered, bool entering)
    {
        Active = true;
        yield return new WaitForEndOfFrame();
        int width = Screen.width, height = Screen.height;
        previous = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(previous);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32765;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var image = new GameObject("Departing view", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        image.transform.SetParent(transform, false);
        float fraction = Mathf.Min(1, 405f * height / (720 * width));
        image.uvRect = new Rect((1 - fraction) * .5f, 0, fraction, 1);
        image.rectTransform.sizeDelta = new Vector2(Mathf.Min(405, width * 720f / height), 720);
        image.texture = previous;
        image.raycastTarget = false;
        covered();
        yield return null;
        if (!entering)
            while (HangarHub.Active || HangarHub.Launching)
                yield return null;
        while (CloudTransition.Busy || entering && HangarHub.PreparingDeck)
            yield return null;
        for (float t = 0; t < .8f; t += Mathf.Min(Time.unscaledDeltaTime, .05f))
        {
            image.color = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, t / .8f));
            yield return null;
        }
        Active = false;
        Destroy(gameObject);
    }
    void OnDestroy()
    {
        if (previous != null)
            RenderTexture.ReleaseTemporary(previous);
        Active = false;
    }
}
