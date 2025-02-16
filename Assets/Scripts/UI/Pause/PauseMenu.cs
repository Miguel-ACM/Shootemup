using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private APauseElement[] menuElements;
    private int currentIndex = 0;
    public APauseElement CurrentElement { get; private set; }
    
    private Inputs input;
    private Vector2 move;
    private Image panel;
    private bool inputProcessed = false;
    private bool menuOpen = false;
    private CanvasGroup canvasGroup;


    void Awake()
    {
        input = new Inputs();
        menuElements = GetComponentsInChildren<APauseElement>();
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
        setMenuOpen(false);
        
        input.PlayerControls.Start.performed += ctx => setMenuOpen(!menuOpen);

        input.PlayerControls.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        input.PlayerControls.Move.canceled += ctx => { move = Vector2.zero; inputProcessed = false; };
        
        input.PlayerControls.Accept.performed += OnAccept;
        input.Enable();
    }

    void Update()
    {
        if (menuOpen) {
            if (move.y != 0 && !inputProcessed) {
                if (move.y > 0) {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = menuElements.Length - 1;
                }
                else if (move.y < 0) {
                    currentIndex++;
                    if (currentIndex >= menuElements.Length) currentIndex = 0;
                }
                CurrentElement = menuElements[currentIndex];
                inputProcessed = true;
            }
            else if (move.y == 0) {
                inputProcessed = false;
            }
        }
    }

    public void setCurrentElement(APauseElement element)
    {
        Debug.Log(element);
        CurrentElement = element;
        currentIndex = Array.IndexOf(menuElements, element);
    }

    public void setMenuOpen(bool open)
    {
        menuOpen = open;
        panel.color = menuOpen ? new Color(0, 0, 0, 0.4f) : Color.clear;
        if (menuOpen) {
            CurrentElement = menuElements[0];
            currentIndex = 0;
            Time.timeScale = 0f;
        } else {
            Time.timeScale = 1;
        }
        canvasGroup.alpha = menuOpen ? 1 : 0;
        canvasGroup.interactable = menuOpen;
        canvasGroup.blocksRaycasts = menuOpen;

    }

    void OnAccept(InputAction.CallbackContext context)
    {
        if (CurrentElement != null)
        {
            CurrentElement.OnAccept();
        }
    }
}