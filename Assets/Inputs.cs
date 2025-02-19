//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.3.0
//     from Assets/Inputs.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @Inputs : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @Inputs()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""Inputs"",
    ""maps"": [
        {
            ""name"": ""PlayerControls"",
            ""id"": ""a2b10447-3634-46ab-a100-6f83486212fa"",
            ""actions"": [
                {
                    ""name"": ""FirePrimary"",
                    ""type"": ""Button"",
                    ""id"": ""eab04280-2f3d-4197-852f-62c1bb1aba7b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""FireSecondary"",
                    ""type"": ""Button"",
                    ""id"": ""a44bd104-2a84-4797-804c-e06d68ed9f2f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""d5c2a54e-b84d-4ade-960f-0cf58d514b12"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Focus"",
                    ""type"": ""Button"",
                    ""id"": ""4dc1ce17-70a1-4aa2-af8c-e8413e2098ae"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ActiveRight"",
                    ""type"": ""Button"",
                    ""id"": ""e7a30099-fa65-4fdb-9125-f302b219a0e2"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ActiveLeft"",
                    ""type"": ""Button"",
                    ""id"": ""f6c4f707-ec4d-44f6-b77f-5e0f219632ec"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Start"",
                    ""type"": ""Button"",
                    ""id"": ""2427fd48-6a14-4f31-964a-5fb373db3206"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Accept"",
                    ""type"": ""Button"",
                    ""id"": ""c628de82-5825-4a00-b1f6-12ddb29b6846"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""3127ccbe-65ec-4168-a17e-95da794e1781"",
                    ""path"": ""<Keyboard>/z"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""FirePrimary"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c7ed367d-5650-40d6-880f-ba151484bf75"",
                    ""path"": ""<Gamepad>/rightShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse or Pad"",
                    ""action"": ""FirePrimary"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8d7596e4-71a8-4667-9976-649b493f9af5"",
                    ""path"": ""<Gamepad>/leftShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""FireSecondary"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""acdc5335-e77a-4ebb-bf12-a1b4ae521a52"",
                    ""path"": ""<Keyboard>/x"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""FireSecondary"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""0727da02-cd9d-4d44-a42d-ee3b6b9c28c9"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Focus"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f5e840df-3f38-4240-a9d3-a82d4eea3190"",
                    ""path"": ""<Keyboard>/shift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Focus"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""c4844425-32f4-4d5e-9581-49344f1f6333"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""488396a2-e7f0-4c5a-b7ad-27681ee83d48"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""a0424355-ae8b-4d79-a038-0627585f175b"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""f271411e-5f1d-4996-863d-8717c8dee35a"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""eb1db474-0d1f-458a-b9ab-8ed7fd280d76"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""2D Vector"",
                    ""id"": ""8b62791f-8fde-488f-8b33-4ce949e3b161"",
                    ""path"": ""2DVector(mode=2)"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""fdf4bb63-f93c-4c7c-8a95-227519b3dfea"",
                    ""path"": ""<Gamepad>/leftStick/up"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse or Pad"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""38cdae4b-e0ad-4c35-bf85-a353fa660116"",
                    ""path"": ""<Gamepad>/leftStick/down"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse or Pad"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""bd04a91d-0e12-40e8-8d18-61fe2466deeb"",
                    ""path"": ""<Gamepad>/leftStick/left"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse or Pad"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""07ecaa46-e7e5-4133-9c5d-bbe40e5e72c8"",
                    ""path"": ""<Gamepad>/leftStick/right"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Mouse or Pad"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""d69ddda2-d04d-43bf-906f-fc3401bc50d5"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActiveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""65fbef26-f1ec-4095-8eaf-de991d6f2469"",
                    ""path"": ""<Gamepad>/rightTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActiveRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""a0a27a60-6711-44e0-87da-2dacb9f25a59"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActiveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9b896d52-8511-4306-963c-8fdec5c04371"",
                    ""path"": ""<Gamepad>/leftTrigger"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ActiveLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8acabc22-a853-4ce1-9914-b092d0c72496"",
                    ""path"": ""<Gamepad>/start"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Start"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ccec3ad4-4e12-4729-bf48-27cc2a34a1b9"",
                    ""path"": ""<Keyboard>/escape"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Start"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1bd52ce9-151d-4991-afee-7388c3d460a9"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accept"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""80bf646b-cae7-40a7-a66b-f19d1c8a5aca"",
                    ""path"": ""<Keyboard>/enter"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accept"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6c922fc5-a2b3-4103-a818-1e21e47d07ad"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Accept"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Mouse or Pad"",
            ""bindingGroup"": ""Mouse or Pad"",
            ""devices"": [
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": true,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // PlayerControls
        m_PlayerControls = asset.FindActionMap("PlayerControls", throwIfNotFound: true);
        m_PlayerControls_FirePrimary = m_PlayerControls.FindAction("FirePrimary", throwIfNotFound: true);
        m_PlayerControls_FireSecondary = m_PlayerControls.FindAction("FireSecondary", throwIfNotFound: true);
        m_PlayerControls_Move = m_PlayerControls.FindAction("Move", throwIfNotFound: true);
        m_PlayerControls_Focus = m_PlayerControls.FindAction("Focus", throwIfNotFound: true);
        m_PlayerControls_ActiveRight = m_PlayerControls.FindAction("ActiveRight", throwIfNotFound: true);
        m_PlayerControls_ActiveLeft = m_PlayerControls.FindAction("ActiveLeft", throwIfNotFound: true);
        m_PlayerControls_Start = m_PlayerControls.FindAction("Start", throwIfNotFound: true);
        m_PlayerControls_Accept = m_PlayerControls.FindAction("Accept", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }
    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // PlayerControls
    private readonly InputActionMap m_PlayerControls;
    private IPlayerControlsActions m_PlayerControlsActionsCallbackInterface;
    private readonly InputAction m_PlayerControls_FirePrimary;
    private readonly InputAction m_PlayerControls_FireSecondary;
    private readonly InputAction m_PlayerControls_Move;
    private readonly InputAction m_PlayerControls_Focus;
    private readonly InputAction m_PlayerControls_ActiveRight;
    private readonly InputAction m_PlayerControls_ActiveLeft;
    private readonly InputAction m_PlayerControls_Start;
    private readonly InputAction m_PlayerControls_Accept;
    public struct PlayerControlsActions
    {
        private @Inputs m_Wrapper;
        public PlayerControlsActions(@Inputs wrapper) { m_Wrapper = wrapper; }
        public InputAction @FirePrimary => m_Wrapper.m_PlayerControls_FirePrimary;
        public InputAction @FireSecondary => m_Wrapper.m_PlayerControls_FireSecondary;
        public InputAction @Move => m_Wrapper.m_PlayerControls_Move;
        public InputAction @Focus => m_Wrapper.m_PlayerControls_Focus;
        public InputAction @ActiveRight => m_Wrapper.m_PlayerControls_ActiveRight;
        public InputAction @ActiveLeft => m_Wrapper.m_PlayerControls_ActiveLeft;
        public InputAction @Start => m_Wrapper.m_PlayerControls_Start;
        public InputAction @Accept => m_Wrapper.m_PlayerControls_Accept;
        public InputActionMap Get() { return m_Wrapper.m_PlayerControls; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(PlayerControlsActions set) { return set.Get(); }
        public void SetCallbacks(IPlayerControlsActions instance)
        {
            if (m_Wrapper.m_PlayerControlsActionsCallbackInterface != null)
            {
                @FirePrimary.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFirePrimary;
                @FirePrimary.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFirePrimary;
                @FirePrimary.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFirePrimary;
                @FireSecondary.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFireSecondary;
                @FireSecondary.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFireSecondary;
                @FireSecondary.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFireSecondary;
                @Move.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnMove;
                @Focus.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFocus;
                @Focus.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFocus;
                @Focus.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnFocus;
                @ActiveRight.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnActiveRight;
                @ActiveRight.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnActiveRight;
                @ActiveRight.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnActiveRight;
                @ActiveLeft.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnActiveLeft;
                @ActiveLeft.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnActiveLeft;
                @ActiveLeft.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnActiveLeft;
                @Start.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnStart;
                @Start.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnStart;
                @Start.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnStart;
                @Accept.started -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnAccept;
                @Accept.performed -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnAccept;
                @Accept.canceled -= m_Wrapper.m_PlayerControlsActionsCallbackInterface.OnAccept;
            }
            m_Wrapper.m_PlayerControlsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @FirePrimary.started += instance.OnFirePrimary;
                @FirePrimary.performed += instance.OnFirePrimary;
                @FirePrimary.canceled += instance.OnFirePrimary;
                @FireSecondary.started += instance.OnFireSecondary;
                @FireSecondary.performed += instance.OnFireSecondary;
                @FireSecondary.canceled += instance.OnFireSecondary;
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @Focus.started += instance.OnFocus;
                @Focus.performed += instance.OnFocus;
                @Focus.canceled += instance.OnFocus;
                @ActiveRight.started += instance.OnActiveRight;
                @ActiveRight.performed += instance.OnActiveRight;
                @ActiveRight.canceled += instance.OnActiveRight;
                @ActiveLeft.started += instance.OnActiveLeft;
                @ActiveLeft.performed += instance.OnActiveLeft;
                @ActiveLeft.canceled += instance.OnActiveLeft;
                @Start.started += instance.OnStart;
                @Start.performed += instance.OnStart;
                @Start.canceled += instance.OnStart;
                @Accept.started += instance.OnAccept;
                @Accept.performed += instance.OnAccept;
                @Accept.canceled += instance.OnAccept;
            }
        }
    }
    public PlayerControlsActions @PlayerControls => new PlayerControlsActions(this);
    private int m_MouseorPadSchemeIndex = -1;
    public InputControlScheme MouseorPadScheme
    {
        get
        {
            if (m_MouseorPadSchemeIndex == -1) m_MouseorPadSchemeIndex = asset.FindControlSchemeIndex("Mouse or Pad");
            return asset.controlSchemes[m_MouseorPadSchemeIndex];
        }
    }
    public interface IPlayerControlsActions
    {
        void OnFirePrimary(InputAction.CallbackContext context);
        void OnFireSecondary(InputAction.CallbackContext context);
        void OnMove(InputAction.CallbackContext context);
        void OnFocus(InputAction.CallbackContext context);
        void OnActiveRight(InputAction.CallbackContext context);
        void OnActiveLeft(InputAction.CallbackContext context);
        void OnStart(InputAction.CallbackContext context);
        void OnAccept(InputAction.CallbackContext context);
    }
}
