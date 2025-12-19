using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ability that handles the player's dash behavior, including
/// applying horizontal forces, and updating dash-related animations.
/// </summary>
public class DashAbility: BaseAbility
{
    [Header("Dash Ability Settings")]
    [Tooltip("Name of the animator bool parameter used to trigger the dash animation.")]
    [SerializeField] private string dashAnimParameterName = "Dash";

    [Tooltip("Initial horizontal force applied when the player dashes.")]
    [SerializeField] private float dashForce = 10f;

    [Tooltip("Time for how long the player can dash for.")]
    [SerializeField] private float maxDashDuration = 1f;

    [Tooltip("Input System action reference used to trigger jumps.")]
    [SerializeField] private InputActionReference dashActionRef;
    
    private float dashTimer; // Reference to store dash timer
    private int dashParameterID; // Cached hash for the jump animation parameter to avoid repeated string lookups.
    
    #region Base Class Overrides
    /// <summary>
    /// Performs initialization for the jump ability.
    /// Caches references, original jump time, and animator parameter hashes.
    /// </summary>
    protected override void Initialization()
    {
        // Call the base initialization to set up shared references.
        base.Initialization();
    
        // Cache animation parameter hashes for performance.
        dashParameterID = Animator.StringToHash(dashAnimParameterName);
    }
    
    /// <summary>
    /// Actions to perform once leaving the ability.
    /// </summary>
    public override void ExitAbility()
    {
        linkedPhysics.EnableGravity();
        linkedPhysics.ResetVelocity();
    }

    /// <summary>
    /// Per-frame logic while in the dash state.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // Decrease the remaining minimum airtime.
        dashTimer -= Time.deltaTime;
        
        // If touched a wall, exit ability
        if (linkedPhysics.IsWallDetected)
            dashTimer = -1;
    
        // Once the dash timer has elapsed
        if (dashTimer <= 0)
        {
            // If grounded go to idle, else go to jump
            linkedStateMachine.ChangeState(linkedPhysics.IsGrounded
                ? PlayerStates.State.Idle
                : PlayerStates.State.Jump);
        }
    }

    /// <summary>
    /// Updates animator parameters related to the dash state.
    /// </summary>
    public override void UpdateAnimator()
    {
        // Animator logic
        if (IsAllowedForCurrentClass())
        {
            linkedAnim.SetBool(
                dashParameterID,
                linkedStateMachine.currentState == PlayerStates.State.Dash
            );
        }
    }
    #endregion
    
    #region Unity Events
    /// <summary>
    /// Called when the component becomes enabled.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to perform dash function.
        dashActionRef.action.performed += TryToDash;
    }
    
    /// <summary>
    /// Called when the component becomes disabled.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from jump function.
        dashActionRef.action.performed -= TryToDash;
    }
    #endregion
    
    #region Dash Actions
    /// <summary>
    /// Initiates a dash action when the dash binding is performed.
    /// Changes the state to dash and applies the initial dash force if permitted.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void TryToDash(InputAction.CallbackContext value)
    {
        // If this ability is not allowed right now, do nothing.
        if (!isPermitted)
            return;
        
        // If out of charges, can't dash.
        if (!HasAvailableCharges())
            return;
        
        // If this ability is the same state as dash, idle, or a wall is met, do nothing.
        if (linkedStateMachine.currentState == PlayerStates.State.Dash || 
            linkedStateMachine.currentState == PlayerStates.State.Idle ||
            linkedPhysics.IsWallDetected)
            return;

        // Only allow a new dash if the player is grounded AND they are permitted by class
        if (IsAllowedForCurrentClass())
        {
            // Consume charge if needed, otherwise exit early.
            if(!TryConsumeCharge())
                return;
            
            // Enter the Jump state and disable gravity
            linkedStateMachine.ChangeState(PlayerStates.State.Dash);
            linkedPhysics.DisableGravity();
            linkedPhysics.ResetVelocity();
            
            // Apply an initial horizontal dash force.
            if (player.FacingRight)
                linkedPhysics.rb.linearVelocityX = dashForce;
            else
                linkedPhysics.rb.linearVelocityX = -dashForce;

            // Set dash timer.
            dashTimer = maxDashDuration;
        }
    }
    #endregion
}
