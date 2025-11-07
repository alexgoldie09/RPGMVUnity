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
    [Tooltip("Jump actions from Input System")]
    [SerializeField] private InputActionReference jumpActionRef;
    [Tooltip("Move actions from Input System")]
    [SerializeField] private InputActionReference moveActionRef;

    private InputActionMap playerMap; // Represents the player based input action map.
    private InputActionMap uiMap; // Represents the UI based input action map.

    public float HorizontalInput { get; private set; } // Represents the horizontal movement of the player.
    
    #region Unity Events
    private void OnEnable()
    {
        // Subscribe to perform jump function
        jumpActionRef.action.performed += TryToJump;
         
        // Subscribe to cancel jump function
        jumpActionRef.action.canceled += StopJump;
    }

    private void OnDisable()
    {
        // Unsubscribe from jump function
        jumpActionRef.action.performed -= TryToJump;
        
        // Unsubscribe from cancel jump function
        jumpActionRef.action.canceled -= StopJump;
        
        // Disable all actions for player input
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

    #region Input Actions
    /// <summary>
    /// Initiate a jump action after the binding has been pressed.
    /// </summary>
    /// <param name="value"></param>
    private void TryToJump(InputAction.CallbackContext value)
    {
        Debug.Log("Trying to jump");
    }
    
    /// <summary>
    /// Cancels the jump action after the binding has been released.
    /// </summary>
    /// <param name="value"></param>
    private void StopJump(InputAction.CallbackContext value)
    {
        Debug.Log("Stop jump");
    }
    #endregion
}
