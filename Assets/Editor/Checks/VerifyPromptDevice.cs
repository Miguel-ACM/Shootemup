#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Actual Input System events/actions in an isolated headless editor. No desktop input.
public static class VerifyPromptDevice
{
    const string Folder = "ReviewOutput/PromptDevice";
    const string EditInput = "RUN_PLAYER_UPDATES_IN_EDIT_MODE";
    static int checks;
    static double eventTime;
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets", StringComparison.Ordinal))
            throw new Exception("Use the isolated UnlockReview checkout");
        Directory.CreateDirectory(Folder);
        try
        {
            CheckInputEvents();
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks.\n" +
                "Reproduced the old timestamp failure with a newer neutral keyboard report. Verified stable controller prompts through idle reports, releases and small stick motion; deliberate buttons/stick/dpad/triggers, rebinding, PlayStation identity despite another pad becoming current, disabled/removed devices and shared HUD selection.\n" +
                "Headless synthetic-device checks only; no desktop input or gameplay session.\n");
            if (Environment.GetCommandLineArgs().Contains("--prompt-build")) Build();
            VerifyCleanCompilation.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    static void Reset() => typeof(InputPrompts).GetMethod("ResetDevice", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    static void CheckInputEvents()
    {
        Keyboard keyboard = null;
        Gamepad xbox = null;
        DualShockGamepad playstation = null;
        Inputs actions = null;
        InputSystem.settings.SetInternalFeatureFlag(EditInput, true);
        try
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            xbox = InputSystem.AddDevice<Gamepad>();
            actions = new Inputs();
            actions.devices = new InputDevice[] { keyboard, xbox };
            actions.Enable();
            eventTime = InputState.currentTime;
            Reset();
            Expect(PromptDevice.Xbox, "Initially connected controller");
            Send(keyboard, new KeyboardState(Key.Z));
            Expect(PromptDevice.Keyboard, "Bound keyboard press switches immediately");
            Send(xbox, new GamepadState().WithButton(GamepadButton.South));
            Expect(PromptDevice.Xbox, "Controller confirm switches immediately");
            Send(keyboard, new KeyboardState());
            Check(keyboard.lastUpdateTime > xbox.lastUpdateTime, "Repro: keyboard release has a newer hardware timestamp");
            Expect(PromptDevice.Xbox, "Keyboard release cannot steal controller prompts");
            for (int i = 0; i < 12; i++)
            {
                Send(xbox, new GamepadState().WithButton(GamepadButton.South));
                Send(keyboard, new KeyboardState());
                Check(!(xbox.lastUpdateTime > keyboard.lastUpdateTime), "Repro: old polling heuristic incorrectly chooses keyboard");
                Expect(PromptDevice.Xbox, "Idle keyboard report retains controller prompts");
            }
            Send(xbox, new GamepadState());
            Expect(PromptDevice.Xbox, "Controller release retains its prompts");
            Send(keyboard, new KeyboardState(Key.Enter));
            Expect(PromptDevice.Keyboard, "Keyboard confirm can deliberately take over");
            Send(keyboard, new KeyboardState());
            foreach (float drift in new[] { .02f, -.04f, .08f, -.11f, .16f, -.18f, 0 })
            {
                Send(xbox, new GamepadState { leftStick = new Vector2(drift, drift * .4f) });
                Expect(PromptDevice.Keyboard, "Idle stick noise cannot steal keyboard prompts");
            }
            Send(xbox, new GamepadState { leftStick = new Vector2(.7f, 0) });
            Expect(PromptDevice.Xbox, "Deliberate composite-stick movement switches");
            Send(xbox, new GamepadState());
            Send(keyboard, new KeyboardState(Key.Escape));
            Expect(PromptDevice.Keyboard, "Keyboard menu action switches");
            Send(keyboard, new KeyboardState());
            Send(xbox, new GamepadState { rightTrigger = .9f });
            Expect(PromptDevice.Xbox, "Pressed trigger switches");
            Send(xbox, new GamepadState());

            playstation = InputSystem.AddDevice<DualShockGamepad>();
            actions.devices = new InputDevice[] { keyboard, xbox, playstation };
            Expect(PromptDevice.Xbox, "Merely connecting a second controller does not steal prompts");
            Send(playstation, new GamepadState().WithButton(GamepadButton.South));
            Expect(PromptDevice.PlayStation, "PlayStation action selects PlayStation glyphs");
            Check(InputPrompts.Inline(actions.PlayerControls.Accept).Contains("ps_cross"), "Actual confirm uses Cross");
            Send(xbox, new GamepadState { rightStick = new Vector2(.8f, .4f) });
            Check(Gamepad.current == xbox, "Repro: inactive controller can become Gamepad.current");
            Expect(PromptDevice.PlayStation, "Inactive pad cannot change the remembered controller family");
            Send(keyboard, new KeyboardState(Key.K));
            Expect(PromptDevice.PlayStation, "Unbound keyboard activity cannot replace game prompts");
            Send(keyboard, new KeyboardState());
            actions.PlayerControls.FirePrimary.ApplyBindingOverride(0, "<Keyboard>/f7");
            Send(keyboard, new KeyboardState(Key.F7));
            Expect(PromptDevice.Keyboard, "Rebound keyboard control switches correctly");
            Check(InputPrompts.Inline(actions.PlayerControls.FirePrimary).Contains("key_f7"), "Rebound key glyph stays correct");
            Send(playstation, new GamepadState());
            Expect(PromptDevice.Keyboard, "PlayStation release does not undo keyboard choice");
            Send(keyboard, new KeyboardState());
            Send(playstation, new GamepadState().WithButton(GamepadButton.DpadDown));
            Expect(PromptDevice.PlayStation, "D-pad movement switches correctly");
            InputSystem.DisableDevice(xbox);
            Expect(PromptDevice.PlayStation, "Disabling an inactive controller preserves prompts");
            InputSystem.EnableDevice(xbox);
            InputSystem.DisableDevice(playstation);
            Expect(PromptDevice.Keyboard, "Disabling the active controller falls back to keyboard");
            InputSystem.EnableDevice(playstation);
            Send(playstation, new GamepadState());
            Expect(PromptDevice.Keyboard, "Idle reconnection waits for deliberate input");
            Send(playstation, new GamepadState().WithButton(GamepadButton.South));
            Expect(PromptDevice.PlayStation, "Reconnected controller resumes on press");
            InputSystem.RemoveDevice(playstation); playstation = null;
            Expect(PromptDevice.Keyboard, "Removing the active controller falls back safely");
            Send(xbox, new GamepadState().WithButton(GamepadButton.RightShoulder));
            Expect(PromptDevice.Xbox, "Another controller can take over normally");
            Check(InputPrompts.Inline(actions.PlayerControls.Accept).Contains("xbox_a"), "Actual Xbox confirm uses A");
            InputSystem.RemoveDevice(xbox); xbox = null;
            Expect(PromptDevice.Keyboard, "Removing the last controller restores keyboard");
        }
        finally
        {
            Reset();
            if (actions != null) { actions.Disable(); Object.DestroyImmediate(actions.asset); }
            if (playstation != null && playstation.added) InputSystem.RemoveDevice(playstation);
            if (xbox != null && xbox.added) InputSystem.RemoveDevice(xbox);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.SetInternalFeatureFlag(EditInput, false);
        }
    }
    static void Send<T>(InputDevice device, T state) where T : struct, IInputStateTypeInfo
    {
        InputSystem.QueueStateEvent(device, state, eventTime += .001);
        InputSystem.Update();
    }
    static void Expect(PromptDevice device, string message)
    {
        Check(InputPrompts.Device == device, message + ": " + InputPrompts.Device);
        Check(DockInstrument.UsingGamepad == (device != PromptDevice.Keyboard), "HUD and prompt device agree");
    }
    static void Check(bool ok, string message)
    {
        checks++;
        if (!ok) throw new Exception(message);
    }
    static void Build()
    {
        string output = Path.GetFullPath("Build/PromptDevice/Shootemup.exe");
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
