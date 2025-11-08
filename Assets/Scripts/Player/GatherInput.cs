using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GatherInput class is used to store and reference all inputs for the player.
/// </summary>
public class GatherInput : MonoBehaviour
{
    [Header("Input System Actions")]
    [Tooltip("Reference to the player Input Map")]
    [SerializeField] private PlayerInput playerInput;
    [Tooltip("Move actions from Input System")]
    [SerializeField] private InputActionReference moveActionRef;
    
    [Header("Character Switching Actions")]
    [Tooltip("Input action used to switch to the next character (e.g. E).")]
    [SerializeField] private InputActionReference nextCharacterActionRef;

    [Tooltip("Input action used to switch to the previous character (e.g. Q).")]
    [SerializeField] private InputActionReference previousCharacterActionRef;

    private InputActionMap playerMap; // Represents the player based input action map.
    private InputActionMap uiMap; // Represents the UI based input action map.

    /// <summary>
    /// Represents the horizontal movement input value of the player.
    /// Typically ranges from -1 (left) to +1 (right).
    /// </summary>
    public float HorizontalInput { get; private set; }
    
    #region Events

    /// <summary>
    /// Invoked when the NextCharacter input action is performed.
    /// </summary>
    public event Action OnNextCharacter;

    /// <summary>
    /// Invoked when the PreviousCharacter input action is performed.
    /// </summary>
    public event Action OnPreviousCharacter;

    #endregion
    
    #region Unity Events
    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Reserved for enabling input bindings or subscriptions if needed.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to character switching actions if they have been assigned.
        if (nextCharacterActionRef != null)
            nextCharacterActionRef.action.performed += HandleNextCharacter;

        if (previousCharacterActionRef != null)
            previousCharacterActionRef.action.performed += HandlePreviousCharacter;
    }
    
    /// <summary>
    /// Called when the behaviour becomes disabled.
    /// Disables the player input action map to prevent unwanted input while inactive.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from character switching actions.
        if (nextCharacterActionRef != null)
            nextCharacterActionRef.action.performed -= HandleNextCharacter;

        if (previousCharacterActionRef != null)
            previousCharacterActionRef.action.performed -= HandlePreviousCharacter;

        // Disable all actions for player input
        if (playerMap != null)
            playerMap.Disable();
    }
    #endregion
    
    #region Unity Lifecycle
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // Set player input component
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        
        // Set input action maps
        playerMap = playerInput.actions.FindActionMap("Player");
        uiMap = playerInput.actions.FindActionMap("UI");
        
        // Enable all actions for player input
        playerMap.Enable();
    }

    // Update is called once per frame
    private void Update()
    {
        HorizontalInput = moveActionRef.action.ReadValue<float>();
    }
    #endregion
    
    #region Input Callbacks
    private void HandleNextCharacter(InputAction.CallbackContext ctx) => OnNextCharacter?.Invoke();
    private void HandlePreviousCharacter(InputAction.CallbackContext ctx) => OnPreviousCharacter?.Invoke();
    #endregion
}
