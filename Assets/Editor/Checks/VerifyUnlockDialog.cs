#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class VerifyUnlockDialog
{
    const string Folder = "ReviewOutput/UnlockDialog";
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets", StringComparison.Ordinal))
            throw new Exception("Use the isolated UnlockReview checkout");
        Directory.CreateDirectory(Folder);
        string save = Path.GetFullPath(Folder + "/Save-" + DateTime.UtcNow.Ticks);
        Directory.CreateDirectory(save);
        MetaProgress.ReviewSaveDirectory = save;
        AudioListener.pause = true;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int row = 0; row < (ItemCatalog.All.Count + GalleryShelf.Columns - 1) / GalleryShelf.Columns; row++)
                Resources.Load<GalleryShelf>("Gallery/Shelf" + row).Register();
            var queue = (List<ItemUnlockNotice>)MetaProgress.PendingUnlocks;
            queue.AddRange(ItemUnlocks.All.Select(r => new ItemUnlockNotice {
                key = r.Key, title = r.Title, area = r.Area, condition = r.Condition, source = r.Requirement
            }));
            foreach (var trial in FlightTrials.All)
                queue.Add(new ItemUnlockNotice { key = trial.reward, title = ItemCatalog.Get(trial.reward).Key, source = trial.Requirement });
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/prstartk.asset");
            var root = new GameObject("Unlock dialog check", typeof(RectTransform));
            UnlockReveal.Show(root.transform, font);
            var dialog = root.GetComponentInChildren<UnlockReveal>();
            int total = queue.Count;
            for (int item = 0; item < total; item++)
            {
                var notice = queue[0];
                Check(GalleryShelf.TryGet(notice.key, out var exhibit), "Unlocked item has prepared metadata");
                Check(Field<TextMeshProUGUI>(dialog, "description").text == exhibit.description && !string.IsNullOrWhiteSpace(exhibit.description),
                    "Actual description on " + notice.key);
                Check(Field<TextMeshProUGUI>(dialog, "category").text == DecisionOverlay.Category(exhibit.category).ToUpperInvariant(),
                    "Actual category on " + notice.key);
                Check(Field<TextMeshProUGUI>(dialog, "source").text == notice.Requirement, "Unlock reason retained");
                var footer = Field<TextMeshProUGUI>(dialog, "footer");
                Check(footer.text == DockInstrument.MenuKey() && Regex.Matches(footer.text, "<sprite").Count == 1, "Only one confirm sprite at the bottom");
                Check(Field<TextMeshProUGUI>(dialog, "counter").text == (item + 1) + " / " + total, "Separate queue counter");
                var fade = Field<CanvasGroup>(dialog, "fade");
                var card = Field<RectTransform>(dialog, "card");
                Check(fade.alpha == 0 && card.localScale.x < 1 && UnlockReveal.BlocksInput, "Opening begins hidden and blocks the hangar");
                Call(dialog, "Update");
                Check(queue.Count == total - item, "Opening frame cannot acknowledge another item");
                Call(dialog, "Animate", .11f);
                Check(fade.alpha > 0 && fade.alpha < 1 && card.anchoredPosition.y < 0, "Opening interpolates fade, scale and slide");
                Call(dialog, "Animate", .11f);
                Check(fade.alpha == 1 && card.localScale == Vector3.one && card.anchoredPosition == Vector2.zero, "Opening settles fully");
                foreach (var label in dialog.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    label.SendMessage("Awake");
                    label.ForceMeshUpdate(true, true);
                    Check(!label.isTextOverflowing, "Text fits on " + notice.key + ": " + label.text);
                }
                Call(dialog, "Dismiss");
                Call(dialog, "Dismiss");
                Check(queue.Count == total - item - 1 && UnlockReveal.BlocksInput, "Repeated input acknowledges exactly one item and preserves the exit lock");
                Call(dialog, "Animate", .07f);
                Check(fade.alpha > 0 && fade.alpha < 1 && card.localScale.x < 1, "Exit fades and shrinks before disposal");
                if (queue.Count > 0)
                    Call(dialog, "Animate", .07f);
            }
            // Do not run delayed Object.Destroy in edit mode; inspect the final exit halfway through.
            Check(queue.Count == 0 && UnlockReveal.BlocksInput, "Final item remains modal through its disappearing animation");
            var studio = Field<ItemPortraitStudio>(dialog, "studio");
            var atlasField = typeof(ItemPortraitStudio).GetField("atlas", Instance);
            var atlas = (RenderTexture)atlasField.GetValue(studio);
            atlasField.SetValue(studio, null);
            Object.DestroyImmediate(atlas);
            Object.DestroyImmediate(studio.gameObject);
            Object.DestroyImmediate(root);
            Check(!UnlockReveal.BlocksInput, "Destroyed dialog releases its input lock");
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks across " + total + " unlocks.\n" +
                "Actual type/description/reason, single bottom prompt, text fit, opening/closing interpolation and one-item acknowledgements.\nHeadless checks only; no gameplay or visual review.\n");
            if (Environment.GetCommandLineArgs().Contains("--dialog-build")) Build();
            VerifyCleanCompilation.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Instance).GetValue(target);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Instance).Invoke(target, args);
    static void Check(bool ok, string label)
    {
        checks++;
        if (!ok) throw new Exception(label);
    }
    static void Build()
    {
        string output = Path.GetFullPath("Build/UnlockDialog/Shootemup.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\n" + output + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        Check(report.summary.result == BuildResult.Succeeded, "Windows build");
    }
}
#endif
