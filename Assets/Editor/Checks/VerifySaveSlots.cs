#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
            PressEdges();
            Presentation();
            Departure();
            File.WriteAllText("ReviewOutput/SaveSlots.txt", "PASS\n" + checks +
                " checks covering saves, deletion, per-press menu dispatch, immediate fresh input, confirmation and departure fade state.\n");
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
        var unlocked = (System.Collections.Generic.List<string>)saved.GetType().GetField("unlocked").GetValue(saved);
        unlocked.AddRange(FlightTrials.All.Where(tape => tape.Advanced && tape.reward != null).Select(tape => tape.reward));
        MetaProgress.FlightSetting = RunDifficulty.Normal;
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
        var doubleClick = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 2 };
        entry.OnPointerClick(doubleClick);
        Check(!menu.Suspended, "The second click of a mouse double-click does not activate a title row");
        menu.TryAccept(entry);
        var selector = menu.SavePicker;
        Check(selector != null && menu.Suspended, "PLAY responds immediately and gives its save page ownership of input");
        selector.GetComponentsInChildren<SaveSlotButton>().First(b => b.Bin && b.Index == 0).OnPointerClick(doubleClick);
        Check(selector.GetComponentsInChildren<TextMeshProUGUI>().All(t => t.text != "DELETE FILE 1?"),
            "The second click of a mouse double-click does not open a delete dialog");
        Check(selector.GetComponentsInChildren<SaveSlotButton>().Count(b => !b.Bin && b.Index < 3) == 3, "Exactly three selectable files");
        Check(selector.GetComponentsInChildren<SaveSlotButton>().Count(b => b.Bin) == 2, "Occupied files have their own bin");
        var cards = selector.GetComponentsInChildren<SaveSlotButton>().Where(b => !b.Bin && b.Index < 3).OrderBy(b => b.Index).ToArray();
        for (int i = 0; i < cards.Length; i++)
        {
            var card = (RectTransform)cards[i].transform;
            Check(card.rect.height > card.rect.width && card.anchoredPosition.y == 0 &&
                (i == 0 || card.anchoredPosition.x > ((RectTransform)cards[i - 1].transform).anchoredPosition.x + card.rect.width),
                "Portrait cards sit side by side with a clear gap");
            var artwork = cards[i].GetComponentsInChildren<UnityEngine.UI.RawImage>();
            Check(artwork.Any(image => image.name == "SaveHangar" && image.texture != null), "Each file has a drawn berth");
            Check(artwork.Any(image => image.name.StartsWith("Portrait") && image.texture != null) == (i < 2),
                "Occupied berths show the saved hull; the empty berth is vacant");
        }
        foreach (var bin in selector.GetComponentsInChildren<SaveSlotButton>().Where(b => b.Bin))
        {
            var at = ((RectTransform)bin.transform).anchoredPosition;
            Check(at.x > 90 && at.y > 220, "Bin sits at the card's top right");
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
            Check((int)typeof(SaveSelectMenu).GetField("selected", Instance).GetValue(selector) == 1, "Right moves to the neighbouring portrait card");
            Move(selector, pad, new GamepadState());
            Move(selector, pad, new GamepadState { leftStick = Vector2.left });
            Move(selector, pad, new GamepadState());
            Move(selector, pad, new GamepadState { leftStick = Vector2.up });
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
        selector.Activate(0, true);
        Check(selector.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == "DELETE FILE 1?"), "Confirmation identifies the file");
        Check(!(bool)typeof(SaveSelectMenu).GetField("confirmErase", Instance).GetValue(selector), "Cancel receives default focus");
        Layout(selector);
        selector.Activate(4, false);
        Check(File.ReadAllText(PathFor(1)) == before, "Cancel preserves progress");
        selector.Activate(0, true);
        selector.Activate(5, false);
        Check(!MetaProgress.InspectSlot(1).Exists && MetaProgress.InspectSlot(2).Readable, "Confirmed bin deletes only its file");
        selector.Activate(3, false);
        Check(!menu.Suspended && menu.CurrentElement == entry, "Back restores title navigation");
        menu.TryAccept(entry);
        Check(menu.Suspended && menu.SavePicker != null, "A fresh click can immediately reopen the save picker after Back");
        menu.SavePicker.Back();
        Check(!menu.Suspended && menu.SavePicker == null, "Back immediately returns ownership to the title");
    }

    static void PressEdges()
    {
        var pad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var first = new InputAction("First page", InputActionType.Button, "<Gamepad>/buttonWest");
        using var second = new InputAction("Second page", InputActionType.Button, "<Gamepad>/buttonWest");
        first.AddBinding("<Keyboard>/escape");
        second.AddBinding("<Keyboard>/escape");
        int accepted = 0, deliveries = 0;
        bool duplicatesIgnored = true;
        void Receive(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            deliveries++;
            if (Menu.ConsumePress(context)) accepted++;
            duplicatesIgnored &= !Menu.ConsumePress(context);
        }
        first.performed += Receive;
        second.performed += Receive;
        try
        {
            InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            first.Enable();
            second.Enable();
            double time = InputState.currentTime;
            void Pad(GamepadState state, double after)
            {
                InputSystem.QueueStateEvent(pad, state, time + after);
                InputSystem.Update();
            }
            var heldStick = new GamepadState { leftStick = Vector2.up };
            Pad(heldStick.WithButton(GamepadButton.West), 0);
            Check(accepted == 1 && deliveries == 2, "Shared bindings consume one controller press only once");
            Pad(heldStick.WithButton(GamepadButton.West), .005);
            Check(accepted == 1, "Holding confirm never repeats on the next page");
            Pad(heldStick.WithButton(GamepadButton.West, false), .01);
            Pad(heldStick.WithButton(GamepadButton.West), .02);
            Check(accepted == 2 && deliveries == 4, "A fresh press 20 ms later works while the stick is still held");
            Pad(heldStick.WithButton(GamepadButton.West, false), .025);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape), time + .03);
            InputSystem.Update();
            Check(accepted == 3 && deliveries == 6, "Shared keyboard Back/Start binding cannot act twice");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(), time + .04);
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape), time + .05);
            InputSystem.Update();
            Check(accepted == 4 && deliveries == 8, "A fresh keyboard press is accepted without a cooldown");
            Check(duplicatesIgnored, "Repeated delivery of each physical press was ignored");
        }
        finally
        {
            first.Disable();
            second.Disable();
            InputSystem.RemoveDevice(pad);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", false);
        }
    }

    static void Departure()
    {
        foreach (var old in Object.FindObjectsByType<SaveSelectMenu>())
            Object.DestroyImmediate(old.gameObject);
        var entry = Object.FindAnyObjectByType<NewGame>();
        var menu = entry.GetComponentInParent<Menu>();
        entry.OnAccept();
        var selector = Object.FindAnyObjectByType<SaveSelectMenu>();
        var title = menu.GetComponent<CanvasGroup>();
        var files = selector.GetComponent<CanvasGroup>();
        files.alpha = 1;
        selector.DismissForDeparture();
        Check(title.alpha == 0 && !title.interactable && !title.blocksRaycasts && menu.Suspended,
            "Departure keeps the main menu hidden and noninteractive");
        Check(!files.interactable && !files.blocksRaycasts, "Departing save cards cannot accept input");
        files.alpha = .37f;
        Invoke(selector, "Update");
        Check(files.alpha == .37f, "Entry animation cannot overwrite the departure fade");
        var flight = (System.Collections.IEnumerator)typeof(NewGame).GetMethod("IntoHangar", Instance)
            .Invoke(entry, new object[] { RunDifficulty.Normal });
        Check(flight.MoveNext(), "Departure begins its fade before scenery readiness");
        Check(flight.MoveNext() && title.alpha == 0, "Fading a hidden title never flashes its rows back on");
        selector.StopAllCoroutines();
        Object.DestroyImmediate(selector.gameObject);
    }

    static void Move(SaveSelectMenu menu, Gamepad pad, GamepadState state)
    {
        InputSystem.QueueStateEvent(pad, state);
        InputSystem.Update();
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
