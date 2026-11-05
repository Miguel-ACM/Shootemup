using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PromptDevice { Keyboard, PlayStation, Xbox }

// Xelu's authored CC0 icons, shared by world-space controls, the HUD and inline menu text.
// Resolve effective bindings on demand so rebinding and device changes never leave stale hints.
public static class InputPrompts
{
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static TMP_SpriteAsset inline;
    public static TMP_SpriteAsset InlineAsset => inline != null ? inline : inline = Resources.Load<TMP_SpriteAsset>("InputPrompts/Inline");
    public static PromptDevice Device => !DockInstrument.UsingGamepad ? PromptDevice.Keyboard : IsPlayStation(Gamepad.current) ? PromptDevice.PlayStation : PromptDevice.Xbox;

    public static bool IsPlayStation(Gamepad pad) => pad != null &&
        (pad is UnityEngine.InputSystem.DualShock.DualShockGamepad ||
         pad.layout.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0 ||
         (pad.description.manufacturer ?? "").IndexOf("Sony", StringComparison.OrdinalIgnoreCase) >= 0);

    public static string Key(InputAction action, PromptDevice device, string part = null)
    {
        if (action == null) return "key_blank";
        foreach (var binding in action.bindings)
        {
            string path = binding.effectivePath;
            if (binding.isComposite || string.IsNullOrEmpty(path)) continue;
            if (part != null && binding.isPartOfComposite && binding.name != part) continue;
            bool keyboard = path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase);
            bool mouse = path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase);
            if ((device == PromptDevice.Keyboard) != (keyboard || mouse)) continue;
            int end = path.IndexOf('>');
            if (end < 0 || end + 2 >= path.Length) continue;
            return KeyForControl(path.Substring(end + 2), device, mouse);
        }
        return device == PromptDevice.Keyboard ? "key_blank" : device == PromptDevice.PlayStation ? "ps_cross" : "xbox_a";
    }

    public static string KeyForControl(string control, PromptDevice device, bool mouse = false)
    {
        control = control.ToLowerInvariant();
        if (device == PromptDevice.Keyboard)
        {
            if (mouse) return "key_mouse_" + (control == "leftbutton" ? "left" : control == "rightbutton" ? "right" : "middle");
            string key = control switch {
                "escape" => "esc", "leftshift" or "rightshift" => "shift", "leftctrl" or "rightctrl" => "ctrl",
                "leftalt" or "rightalt" => "alt", "leftmeta" or "rightmeta" => "win", "capslock" => "caps_lock",
                "numlock" => "num_lock", "printscreen" => "print_screen", "delete" => "del",
                "uparrow" => "arrow_up", "downarrow" => "arrow_down", "leftarrow" => "arrow_left", "rightarrow" => "arrow_right",
                "pageup" => "page_up", "pagedown" => "page_down", "numpadenter" => "enter", "numpadplus" => "plus",
                "numpadminus" => "minus", "numpadmultiply" => "asterisk", "numpaddivide" => "slash", "backquote" => "tilda",
                "leftbracket" => "bracket_left", "rightbracket" => "bracket_right", _ => control
            };
            if (key.StartsWith("numpad") && key.Length == 7) key = key.Substring(6);
            return "key_" + key;
        }
        bool ps = device == PromptDevice.PlayStation;
        if (control.StartsWith("leftstick/")) control = "leftstick";
        if (control.StartsWith("rightstick/")) control = "rightstick";
        string button = control switch {
            "buttonsouth" => ps ? "cross" : "a", "buttoneast" => ps ? "circle" : "b",
            "buttonwest" => ps ? "square" : "x", "buttonnorth" => ps ? "triangle" : "y",
            "leftshoulder" => ps ? "l1" : "lb", "rightshoulder" => ps ? "r1" : "rb",
            "lefttrigger" => ps ? "l2" : "lt", "righttrigger" => ps ? "r2" : "rt",
            "start" => ps ? "start" : "menu", "select" => ps ? "select" : "view",
            "leftstickpress" => "left_stick_click", "rightstickpress" => "right_stick_click",
            "leftstick" => "left_stick", "rightstick" => "right_stick", _ => control.Replace('/', '_')
        };
        return (ps ? "ps_" : "xbox_") + button;
    }

    public static Sprite Icon(string key)
    {
        if (!sprites.TryGetValue(key, out var sprite))
            sprites[key] = sprite = Resources.Load<Sprite>("InputPrompts/Icons/" + key);
        return sprite != null ? sprite : Resources.Load<Sprite>("InputPrompts/Icons/key_blank");
    }
    public static Sprite For(InputAction action) => Icon(Key(action, Device));
    public static string Inline(InputAction action) => InlineKey(Key(action, Device));
    public static string InlineKey(string key) => "<size=140%><sprite name=\"" + key + "\" tint=0></size>";
    public static string Navigation(bool vertical)
    {
        var move = GameInput.Actions.PlayerControls.Move;
        var device = Device;
        string first = Key(move, device, vertical ? "up" : "left");
        string second = Key(move, device, vertical ? "down" : "right");
        return InlineKey(first) + (first == second ? "" : InlineKey(second));
    }

    public static InputAction Equipment(int slot)
    {
        var controls = GameInput.Actions.PlayerControls;
        return slot switch { 0 => controls.FirePrimary, 1 => controls.FireSecondary, 2 => controls.ActiveLeft, _ => controls.ActiveRight };
    }
}
