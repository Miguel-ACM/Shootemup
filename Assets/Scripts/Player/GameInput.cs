using System;
using UnityEngine;
using UnityEngine.InputSystem;

public readonly struct PlayerInputSnapshot
{
    public readonly Vector2 Move;
    public readonly bool Focus, Primary, Secondary;
    public PlayerInputSnapshot(Vector2 move, bool focus, bool primary, bool secondary)
    { Move = move; Focus = focus; Primary = primary; Secondary = secondary; }
}

// One action asset for the application. Equipment selection always gets first refusal,
// regardless of the creation order of ships, weapons, companions and UI.
public static class GameInput
{
    static Inputs actions;
    public static Inputs Actions { get { Ensure(); return actions; } }
    public static PlayerInputSnapshot Snapshot { get; private set; }
    public static Func<bool, int, bool> EquipmentSelection;
    public static event Action<InputAction.CallbackContext> PrimaryPressed, PrimaryReleased, SecondaryPressed, SecondaryReleased,
        ActiveLeftPressed, ActiveLeftReleased, ActiveRightPressed, ActiveRightReleased;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        InputSystem.onAfterUpdate -= Capture;
        actions?.Dispose();
        actions = null;
        Snapshot = default;
        EquipmentSelection = null;
        PrimaryPressed = PrimaryReleased = SecondaryPressed = SecondaryReleased = null;
        ActiveLeftPressed = ActiveLeftReleased = ActiveRightPressed = ActiveRightReleased = null;
        Application.quitting -= Reset;
    }
    static void Ensure()
    {
        if (actions != null) return;
        actions = new Inputs();
        var controls = actions.PlayerControls;
        controls.FirePrimary.started += ctx => Dispatch(false, 0, ctx);
        controls.FireSecondary.started += ctx => Dispatch(false, 1, ctx);
        controls.ActiveLeft.started += ctx => Dispatch(true, 0, ctx);
        controls.ActiveRight.started += ctx => Dispatch(true, 1, ctx);
        controls.FirePrimary.canceled += ctx => PrimaryReleased?.Invoke(ctx);
        controls.FireSecondary.canceled += ctx => SecondaryReleased?.Invoke(ctx);
        controls.ActiveLeft.canceled += ctx => ActiveLeftReleased?.Invoke(ctx);
        controls.ActiveRight.canceled += ctx => ActiveRightReleased?.Invoke(ctx);
        InputSystem.onAfterUpdate += Capture;
        Application.quitting += Reset;
        actions.Enable();
        Capture();
    }
    public static bool TrySelectEquipment(bool active, int slot) => EquipmentSelection?.Invoke(active, slot) ?? false;
    static void Dispatch(bool active, int slot, InputAction.CallbackContext context)
    {
        if (TrySelectEquipment(active, slot)) return;
        if (active)
        {
            if (slot == 0) ActiveLeftPressed?.Invoke(context); else ActiveRightPressed?.Invoke(context);
        }
        else
        {
            if (slot == 0) PrimaryPressed?.Invoke(context); else SecondaryPressed?.Invoke(context);
        }
    }
    static void Capture()
    {
        if (actions == null) return;
        var controls = actions.PlayerControls;
        Snapshot = new PlayerInputSnapshot(controls.Move.ReadValue<Vector2>(), controls.Focus.IsPressed(),
            controls.FirePrimary.IsPressed(), controls.FireSecondary.IsPressed());
    }
}
