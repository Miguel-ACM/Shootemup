using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Temporary: triggers the warning and saves end-of-frame screenshots of it.</summary>
public sealed class TempWarningShots : MonoBehaviour
{
    public Warning warning;
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1f);
        warning.blink();
        Directory.CreateDirectory("ArtSource/PSXv2/WarningCapture");
        for (int i = 0; i < 6; i++)
        {
            yield return new WaitForSecondsRealtime(i == 0 ? .15f : .45f);
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes("ArtSource/PSXv2/WarningCapture/warning_" + i + ".png", shot.EncodeToPNG());
            Destroy(shot);
        }
        File.WriteAllText("ArtSource/PSXv2/WarningCapture/done.txt", "ok");
    }
}
