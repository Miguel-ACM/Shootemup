#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Disposable save files and edit-time UI checks. No live profile or gameplay is touched.
public static class VerifySaveSlots
{
    const BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static string directory;

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/"))
            throw new InvalidOperationException("Run save verification in an isolated Temp project.");
        directory = Path.GetFullPath("ReviewOutput/SaveSlots/" + DateTime.UtcNow.Ticks);
        Directory.CreateDirectory(directory);
        MetaProgress.ReviewSaveDirectory = directory;
        try
        {
            Persistence();
            Presentation();
            File.WriteAllText("ReviewOutput/SaveSlots.txt", "PASS\n" + checks +
                " checks covering original saves, independent slots, summaries, recovery, deletion, menu and confirmation.\n");
            VerifyCleanCompilation.Run();
        }
        catch (Exception e)
        {
            File.WriteAllText("ReviewOutput/SaveSlots.txt", "FAIL\n" + e);
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static string PathFor(int slot) => Path.Combine(directory, slot == 1 ? "hangar.json" : "hangar-" + slot + ".json");
    static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }

    static void Persistence()
    {
        for (int slot = 1; slot <= 3; slot++) Check(!MetaProgress.InspectSlot(slot).Exists, "Empty slot " + slot);
        Check(Directory.GetFiles(directory).Length == 0, "Inspecting empty slots creates no progress");
        File.WriteAllText(PathFor(1), "{\"version\":5,\"coins\":321,\"lastFrame\":1,\"ship\":\"Lancer\",\"difficulty\":1," +
            "\"unlocked\":[\"Popcorn\",\"PolarityEngine\"],\"routes\":[\"Space\"],\"stock\":[],\"loadout\":[\"Popcorn\"]," +
            "\"records\":[{\"clears\":5,\"flights\":8,\"victories\":2,\"seconds\":3661,\"best\":45678}]}");
        string original = File.ReadAllText(PathFor(1));
        var first = MetaProgress.InspectSlot(1);
        Check(first.Coins == 321 && first.Ship == "Lancer" && first.Difficulty == "Hard" && first.Districts == 2 &&
            first.Flights == 8 && first.Victories == 2 && first.Best == 45678 && first.PlayTime == "1h 01m", "Existing save summary");
        Check(File.ReadAllText(PathFor(1)) == original, "Reading cards preserves file contents");
        Check(MetaProgress.SelectSlot(1) && MetaProgress.Coins == 321 && MetaProgress.Outbound.Contains("Popcorn"), "Existing file continues");
        Check(MetaProgress.SelectSlot(2) && MetaProgress.Coins == 0 && MetaProgress.Ship == ShipFrame.GR07, "Second file starts independently");
        Check(MetaProgress.InspectSlot(2).Completion == 0, "Fresh file is zero percent");
        MetaProgress.FlightSetting = RunDifficulty.Easy;
        LevelRun.Reset(LevelId.Snow, 17381);
        MetaProgress.BeginRun();
        Check(!MetaProgress.SelectSlot(1) && !MetaProgress.DeleteSlot(2), "An active flight keeps ownership of its file");
        MetaProgress.EngageBoss();
        MetaProgress.DefeatBoss();
        MetaProgress.EndRun("Lost");
        Check(MetaProgress.RouteOpen(LevelId.Desert), "Second file earns its own route");
        Check(MetaProgress.SelectSlot(1) && MetaProgress.Coins == 321 && MetaProgress.FlightSetting == RunDifficulty.Hard &&
            !MetaProgress.RouteOpen(LevelId.Desert), "Switching restores the first file's bank, difficulty and unlocks");
        Check(MetaProgress.SelectSlot(3), "Third file can be created");
        var saved = typeof(MetaProgress).GetProperty("Data", Private).GetValue(null);
        saved.GetType().GetField("lastFrame").SetValue(saved, ShipFrames.Count - 1);
        saved.GetType().GetField("completed").SetValue(saved, FlightTrials.All.Select(t => t.title).ToList());
        saved.GetType().GetField("records").SetValue(saved, new System.Collections.Generic.List<ServiceRecord>
            { new ServiceRecord { clears = (1 << Enum.GetValues(typeof(LevelId)).Length) - 1 } });
        MetaProgress.UnlockAll();
        Check(MetaProgress.InspectSlot(3).Completion == 100, "All milestones give exactly 100 percent");
        string second = File.ReadAllText(PathFor(2));
        File.Copy(PathFor(2), PathFor(2) + ".bak", true);
        File.WriteAllText(PathFor(2), "{broken");
        Check(MetaProgress.InspectSlot(2).Readable && MetaProgress.SelectSlot(2), "Damaged primary recovers from backup");
        Check(MetaProgress.InspectSlot(2).Difficulty == "Easy", "Recovered data belongs to the selected file");
        Check(MetaProgress.DeleteSlot(3) && !MetaProgress.InspectSlot(3).Exists, "Deletion removes all copies of only the selected file");
        Check(MetaProgress.InspectSlot(1).Coins == 321 && MetaProgress.InspectSlot(2).Readable, "Neighbours survive deletion");
        Check(MetaProgress.SelectSlot(3) && MetaProgress.InspectSlot(3).Completion == 0 && MetaProgress.PendingUnlocks.Count == 0,
            "Reusing a deleted slot has fresh progression and queue");
        MetaProgress.DeleteSlot(3);
        File.WriteAllText(PathFor(3), "{broken");
        Check(MetaProgress.InspectSlot(3).Exists && !MetaProgress.InspectSlot(3).Readable && !MetaProgress.SelectSlot(3),
            "Unreadable progress stays visible and cannot be overwritten by Play");
        Check(File.ReadAllText(PathFor(3)) == "{broken", "Failed selection preserves unreadable data");
        MetaProgress.DeleteSlot(3);
        File.WriteAllText(PathFor(3) + ".tmp", second);
        Check(MetaProgress.InspectSlot(3).Readable, "Interrupted first write is recoverable");
        Check(MetaProgress.DeleteSlot(3) && !File.Exists(PathFor(3) + ".tmp"), "Delete also removes an interrupted first write");
    }

    static void Presentation()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        var entry = Object.FindAnyObjectByType<NewGame>();
        var menu = entry.GetComponentInParent<Menu>();
        Invoke(menu, "Awake");
        Invoke(menu, "Start");
        Invoke(entry, "Start");
        Check(entry.GetComponentInChildren<TextMeshProUGUI>().text == "PLAY", "Title uses PLAY");
        entry.OnAccept();
        var selector = Object.FindAnyObjectByType<SaveSelectMenu>();
        Check(selector != null && menu.Suspended, "PLAY opens a modal save submenu");
        Check(selector.GetComponentsInChildren<SaveSlotButton>().Count(b => !b.Bin && b.Index < 3) == 3, "Exactly three selectable files");
        Check(selector.GetComponentsInChildren<SaveSlotButton>().Count(b => b.Bin) == 2, "Occupied files have their own bin");
        foreach (var bin in selector.GetComponentsInChildren<SaveSlotButton>().Where(b => b.Bin))
        {
            var at = ((RectTransform)bin.transform).anchoredPosition;
            Check(at.x > 280 && at.y > 40, "Bin sits at the card's top right");
        }
        Layout(selector);
        string before = File.ReadAllText(PathFor(1));
        var pad = InputSystem.AddDevice<Gamepad>();
        try
        {
            InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            selector.FocusTarget(0, false);
            Move(selector, pad, new GamepadState());
            Move(selector, pad, new GamepadState { leftStick = Vector2.right });
            Check((bool)typeof(SaveSelectMenu).GetField("binSelected", Instance).GetValue(selector), "Controller reaches the bin");
            Move(selector, pad, new GamepadState().WithButton(GamepadButton.South));
            Check(selector.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == "DELETE FILE 1?"), "Controller opens confirmation");
            Move(selector, pad, new GamepadState());
            Move(selector, pad, new GamepadState { leftStick = Vector2.left });
            Check(!(bool)typeof(SaveSelectMenu).GetField("confirmErase", Instance).GetValue(selector), "Left keeps Cancel selected");
            Move(selector, pad, new GamepadState().WithButton(GamepadButton.East));
            Check(File.ReadAllText(PathFor(1)) == before && selector.GetComponentsInChildren<SaveSlotButton>().Any(b => b.Bin),
                "Controller Back cancels and returns to the files");
        }
        finally
        {
            InputSystem.RemoveDevice(pad);
            InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", false);
        }
        Ready(selector);
        selector.Activate(0, true);
        Check(selector.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == "DELETE FILE 1?"), "Confirmation identifies the file");
        Check(!(bool)typeof(SaveSelectMenu).GetField("confirmErase", Instance).GetValue(selector), "Cancel receives default focus");
        Layout(selector);
        Ready(selector);
        selector.Activate(4, false);
        Check(File.ReadAllText(PathFor(1)) == before, "Cancel preserves progress");
        Ready(selector);
        selector.Activate(0, true);
        Ready(selector);
        selector.Activate(5, false);
        Check(!MetaProgress.InspectSlot(1).Exists && MetaProgress.InspectSlot(2).Readable, "Confirmed bin deletes only its file");
        Ready(selector);
        selector.Activate(3, false);
        Check(!menu.Suspended && menu.CurrentElement == entry, "Back restores title navigation");
    }

    static void Ready(SaveSelectMenu menu) => typeof(SaveSelectMenu).GetField("openedFrame", Instance).SetValue(menu, -1);
    static void Move(SaveSelectMenu menu, Gamepad pad, GamepadState state)
    {
        InputSystem.QueueStateEvent(pad, state);
        InputSystem.Update();
        Ready(menu);
        Invoke(menu, "Update");
    }
    static void Invoke(object target, string method) => target.GetType().GetMethod(method, Instance).Invoke(target, null);
    static void Layout(Component root)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate(true, true);
            Check(!label.isTextOverflowing && label.textBounds.size.x <= label.rectTransform.rect.width + 1,
                "Text fits " + label.text);
        }
    }
}
#endif
