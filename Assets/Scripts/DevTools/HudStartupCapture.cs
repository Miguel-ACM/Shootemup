#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Called by the combined polish review; renders the real UI and 3D portraits, not a mockup.
public static class HudStartupCapture
{
    public static IEnumerator Run()
    {
        const string directory = "Temp/HudStartupReview";
        Directory.CreateDirectory(directory);
        foreach (string frame in Directory.GetFiles(directory, "????.jpg"))
            File.Delete(frame);
        var hud = UnityEngine.Object.FindAnyObjectByType<IndustrialHUD>();
        if (hud == null)
            throw new Exception("HUD startup review requires the gameplay scene");
        var circuits = UnityEngine.Object.FindObjectsByType<FluorescentStartup>(FindObjectsInactive.Include);
        if (circuits.Count(x => x.gameObject.activeInHierarchy) < 6)
            throw new Exception("Not every cockpit instrument has a power-on circuit");
        // Replay the shared clock once in this disposable verification run.
        typeof(IndustrialHUD).GetField("entranceAge", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, 0f);
        foreach (var circuit in circuits)
        {
            circuit.enabled = true;
            circuit.GetComponent<CanvasGroup>().alpha = 0;
        }
        foreach (var oldTitle in UnityEngine.Object.FindObjectsByType<LevelReveal>())
            UnityEngine.Object.Destroy(oldTitle.gameObject);
        LevelReveal.Show();
        var side = (CanvasGroup)typeof(IndustrialHUD).GetField("sideGroup", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
        var loadout = UnityEngine.Object.FindAnyObjectByType<LoadoutHUD>();
        if (loadout == null || loadout.Panels.Any(x => x == null || x.GetComponent<CanvasGroup>() == null))
            throw new Exception("Equipment portraits do not share the instrument power-on");

        var samples = new List<string> { "frame,seconds,boot_age,health_alpha,left_weapon_alpha,right_weapon_alpha" };
        float started = Time.realtimeSinceStartup, nextFrame = 0, previous = 0, firstAlpha = -1;
        int number = 0, dropouts = 0;
        bool partial = false;
        while (Time.realtimeSinceStartup - started < 3.9f)
        {
            yield return new WaitForEndOfFrame();
            float elapsed = Time.realtimeSinceStartup - started;
            if (elapsed < nextFrame)
                continue;
            nextFrame = elapsed + 1f / 30;
            var picture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(directory, number.ToString("0000") + ".jpg"), picture.EncodeToJPG(86));
            UnityEngine.Object.Destroy(picture);
            float alpha = side.alpha;
            if (firstAlpha < 0)
                firstAlpha = alpha;
            if (previous > .25f && alpha < previous * .7f)
                dropouts++;
            if (alpha > .1f && alpha < .9f)
                partial = true;
            samples.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4}", number, elapsed, hud.EntranceAge, alpha,
                loadout.Panels[0].GetComponent<CanvasGroup>().alpha, loadout.Panels[1].GetComponent<CanvasGroup>().alpha));
            previous = alpha;
            number++;
        }
        File.WriteAllLines(Path.Combine(directory, "timing.csv"), samples);
        if (firstAlpha > .15f)
            throw new Exception("HUD was already bright in its initial captured frame");
        if (!partial || dropouts < 2)
            throw new Exception("Rendered HUD missed the gradual start or irregular dropouts");
        if (!hud.EntranceComplete || circuits.Any(x => x.gameObject.activeInHierarchy && x.GetComponent<CanvasGroup>().alpha != 1))
            throw new Exception("A HUD circuit failed to settle at full visibility");
        File.WriteAllText(Path.Combine(directory, "result.txt"), $"PASS: {number} full-screen frames; dark start, {dropouts} visible dropouts, gradual warming, all active instruments steady. Portraits share their plate alpha.\n");
    }
}
#endif
