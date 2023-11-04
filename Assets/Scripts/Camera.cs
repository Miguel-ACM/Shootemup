using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{

    public static float UpLimit = 7.87f;
    public static float DownLimit = -9.1f;
    public static float RightLimit = 4.86f;
    public static float LeftLimit = -4.86f;
    [SerializeField] AnimationCurve whiteScreenJitter;
    [SerializeField] UnityEngine.UI.Image whiteOverlay;
    private static float whiteScreenOverlayT = 0;
    private static float whiteScreenOverlayDuration;
    private static bool overlayWhite;

    public static float[] getLimits() {
        return new float[4] {UpLimit, DownLimit, LeftLimit, RightLimit};
    }

    public static void WhiteScreenJitter(float duration)
    {
        whiteScreenOverlayT = 0;
        whiteScreenOverlayDuration = duration;
        overlayWhite = true;
    }

    public void Update()
    {
        if (overlayWhite)
        {
            whiteScreenOverlayT += Time.deltaTime;
            float a = whiteScreenJitter.Evaluate(whiteScreenOverlayT / whiteScreenOverlayDuration);
            if (whiteScreenOverlayT > whiteScreenOverlayDuration)
            {
                overlayWhite = false;
                a = 0;
            }
            whiteOverlay.color = new Color(1, 1, 1, a);

        }
    }
}
