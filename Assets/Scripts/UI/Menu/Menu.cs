using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    static int handledFrame = -1;
    public static bool HandledInputThisFrame => handledFrame == Time.frameCount;
    SettingsEntry settings;
    public static bool GameplayPaused
    {
        get; private set;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPause()
    {
        GameplayPaused = false;
        handledFrame = -1;
    }

    private AMenuElement[] menuElements;
    private int currentIndex = 0;
    public AMenuElement CurrentElement
    {
        get; private set;
    }

    private Inputs input;
    private Vector2 move;
    private Image panel;
    private bool inputProcessed = false;
    private bool menuOpen = false;
    private CanvasGroup canvasGroup;
    const float InputDelay = .45f;
    float inputReadyAt;
    public bool InputReady { get; private set; }
    public bool Suspended
    {
        get; set;
    }

    public bool IsOpen => menuOpen;
    public void RefreshElements() => menuElements = GetComponentsInChildren<AMenuElement>();

    // Shared with modal pages, so one press cannot carry across a page change.
    public void LockInput()
    {
        InputReady = false;
        inputReadyAt = Time.unscaledTime + InputDelay;
        inputProcessed = true;
    }

    void ReleaseInputLock(float now, bool pressed, Vector2 steering)
    {
        if (!InputReady && now >= inputReadyAt && !pressed && steering.sqrMagnitude < .24f)
        {
            InputReady = true;
            inputProcessed = false;
        }
    }

    [SerializeField] private bool alwaysOpen = false;


    void Awake()
    {
        input = GameInput.Actions;
        menuElements = GetComponentsInChildren<AMenuElement>();
        panel = transform.parent.GetComponent<Image>();
        panel.color = Color.clear;

        if (menuElements.Length > 0)
        {
            CurrentElement = menuElements[0];
        }

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (!alwaysOpen)
        {
            input.PlayerControls.Start.performed += ToggleMenu;
            setMenuOpen(false);
        }
        else
        {
            setMenuOpen(true);
        }

        input.PlayerControls.Move.performed += MoveChanged;
        input.PlayerControls.Move.canceled += MoveCanceled;

        input.PlayerControls.Accept.performed += OnAccept;
        input.PlayerControls.Back.performed += OnBack;

    }

    // The dev tools add their title rows on scene load, before Start, so every row exists here.
    void Start()
    {
        MenuLayout.Dress(this, alwaysOpen);
        settings = GetComponentInChildren<SettingsEntry>(true);
        RefreshElements();
        if (menuElements.Length > 0)
            setCurrentElement(menuElements[0]);
    }

    void ToggleMenu(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled || !InputReady || Suspended || CloudTransition.Busy || GameOverState.Active || UnlockReveal.BlocksInput) return;
        handledFrame = Time.frameCount;
        LockInput();
        if (menuOpen && context.control.device is Keyboard && settings != null && settings.TryBack()) return;
        setMenuOpen(!menuOpen);
    }
    void OnBack(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled || !menuOpen || !InputReady || Suspended || CloudTransition.Busy || GameOverState.Active || UnlockReveal.BlocksInput) return;
        handledFrame = Time.frameCount;
        LockInput();
        if (settings != null && settings.TryBack()) return;
        if (!alwaysOpen)
            setMenuOpen(false);
        else if (GetComponentInChildren<QuitGame>() is QuitGame quit)
            setCurrentElement(quit);
    }
    void MoveChanged(InputAction.CallbackContext context) => move = context.ReadValue<Vector2>();
    void MoveCanceled(InputAction.CallbackContext context) { move = Vector2.zero; inputProcessed = false; }

    void Update()
    {
        // Keep polling while a modal page owns the menu. A short tap wholly
        // inside this frame must not arm the next page either.
        if (!InputReady)
        {
            var controls = input.PlayerControls;
            bool pressed = controls.Accept.IsPressed() || controls.Accept.WasPressedThisFrame() ||
                controls.Back.IsPressed() || controls.Back.WasPressedThisFrame() ||
                controls.Start.IsPressed() || controls.Start.WasPressedThisFrame() ||
                Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame);
            ReleaseInputLock(Time.unscaledTime, pressed, controls.Move.ReadValue<Vector2>());
        }
        if (menuOpen && !Suspended && InputReady)
        {
            if (move.y != 0 && !inputProcessed)
            {
                if (move.y > 0)
                {
                    currentIndex--;
                    if (currentIndex < 0)
                        currentIndex = menuElements.Length - 1;
                }
                else if (move.y < 0)
                {
                    currentIndex++;
                    if (currentIndex >= menuElements.Length)
                        currentIndex = 0;
                }
                CurrentElement = menuElements[currentIndex];
                inputProcessed = true;
            }
            else if (move.y == 0)
            {
                inputProcessed = false;
            }
        }
    }

    public void setCurrentElement(AMenuElement element)
    {
        CurrentElement = element;
        currentIndex = Array.IndexOf(menuElements, element);
    }

    public void setMenuOpen(bool open)
    {
        LockInput();
        if (!alwaysOpen)
            GameplayPaused = open;
        menuOpen = open;
        panel.color = menuOpen ? new Color(.035f, .045f, .045f, .91f) : Color.clear;
        if (menuOpen)
        {
            CurrentElement = menuElements[0];
            currentIndex = 0;
            if (!CloudTransition.Busy)
                Time.timeScale = 0f;
        }
        else
        {
            if (!CloudTransition.Busy)
                Time.timeScale = WorldTimeStop.Frozen || BossDestruction.HoldingFrame ? 0 : 1;
        }
        canvasGroup.alpha = menuOpen ? 1 : 0;
        canvasGroup.interactable = menuOpen;
        canvasGroup.blocksRaycasts = menuOpen;

    }

    void OnAccept(InputAction.CallbackContext context)
    {
        TryAccept(CurrentElement);
    }

    public void TryAccept(AMenuElement element)
    {
        if (!isActiveAndEnabled || !menuOpen || !InputReady || Suspended || CloudTransition.Busy || GameOverState.Active || UnlockReveal.BlocksInput || element == null || Array.IndexOf(menuElements, element) < 0)
            return;
        handledFrame = Time.frameCount;
        LockInput();
        setCurrentElement(element);
        element.OnAccept();
    }

    // Inputs instances stay enabled after their scene unloads, and their callbacks then run
    // against this destroyed object, so release it with the object.
    void OnDestroy()
    {
        if (!alwaysOpen)
            GameplayPaused = false;
        if (input != null)
        {
            input.PlayerControls.Start.performed -= ToggleMenu;
            input.PlayerControls.Move.performed -= MoveChanged;
            input.PlayerControls.Move.canceled -= MoveCanceled;
            input.PlayerControls.Accept.performed -= OnAccept;
            input.PlayerControls.Back.performed -= OnBack;
        }
    }
}
