using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public static bool GameplayPaused
    {
        get; private set;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPause()
    {
        GameplayPaused = false;
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
    public bool Suspended
    {
        get; set;
    }

    public bool IsOpen => menuOpen;
    public void RefreshElements() => menuElements = GetComponentsInChildren<AMenuElement>();

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

    }

    // The dev tools add their title rows on scene load, before Start, so every row exists here.
    void Start()
    {
        MenuLayout.Dress(this, alwaysOpen);
        RefreshElements();
        if (menuElements.Length > 0)
            setCurrentElement(menuElements[0]);
    }

    void ToggleMenu(InputAction.CallbackContext context) { if (!CloudTransition.Busy && !GameOverState.Active) setMenuOpen(!menuOpen); }
    void MoveChanged(InputAction.CallbackContext context) => move = context.ReadValue<Vector2>();
    void MoveCanceled(InputAction.CallbackContext context) { move = Vector2.zero; inputProcessed = false; }

    void Update()
    {
        if (menuOpen && !Suspended)
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
        if (!menuOpen || Suspended || CloudTransition.Busy || element == null || Array.IndexOf(menuElements, element) < 0)
            return;
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
        }
    }
}
