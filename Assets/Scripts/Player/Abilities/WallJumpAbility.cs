using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ability that handles the player's wall jump behavior, including
/// applying vertical forces, handling air control, and updating jump-related animations.
/// </summary>
public class WallJumpAbility : BaseAbility
{
    [Header("Wall Jump Ability Settings")]

    [Tooltip("Initial force applied when the player wall jumps.")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(0f, 20f);

    [Tooltip("Maximum amount of time the player can wall jump.")]
    [SerializeField] private float maxWallJumpTime = 0.5f;
    
    [Tooltip("Input System action reference used to trigger jumps.")]
    [SerializeField] private InputActionReference wallJumpActionRef;
   
    private float minWallJumpTime = 0.15f; // Reference to store minimum air time.
    private float wallJumpTimer; // Reference to store wall jump timer.

    #region Base Class Overrides
    /// <summary>
    /// Performs initialization for the wall jump ability.
    /// Caches references, original jump time, and animator parameter hashes.
    /// </summary>
    protected override void Initialization()
    {
        // Call the base initialization to set up shared references.
        base.Initialization();

        // Store the original wall jump timer to the maximum wall jump.
        wallJumpTimer = maxWallJumpTime;
    }
    
    /// <summary>
    /// Per-frame logic while in the wall jump state.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // Decrease the remaining minimum airtime.
        wallJumpTimer -= Time.deltaTime;
        
        // Decreases minimum time to prevent errors in changing states.
        minWallJumpTime -= Time.deltaTime;
        
        // Didn't reach the wall on the other side.
        if (wallJumpTimer <= 0)
        {
            linkedStateMachine.ChangeState(linkedPhysics.IsGrounded ? 
                PlayerStates.State.Idle : 
                PlayerStates.State.Jump);
            return;
        }

        // Once wall detected and the minimum airtime has elapsed, return to the idle state.
        if (linkedPhysics.IsWallDetected && minWallJumpTime <= 0)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            wallJumpTimer = -1;
        }
    }
    #endregion

    #region Unity Events

    /// <summary>
    /// Called when the component becomes enabled.
    /// Subscribes to wall jump input events for performing and canceling wall jumps.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to perform wall jump function.
        wallJumpActionRef.action.performed += TryToWallJump;
    }

    /// <summary>
    /// Called when the component becomes disabled.
    /// Unsubscribes from wall jump input events to prevent callbacks on disabled objects.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from jump function.
        wallJumpActionRef.action.performed -= TryToWallJump;
    }

    #endregion

    #region Wall Jump Actions

    /// <summary>
    /// Initiates a wall jump action when the jump binding is performed.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void TryToWallJump(InputAction.CallbackContext value)
    {
        // If this ability is not allowed right now, do nothing.
        if (!isPermitted)
            return;
        
        // If out of charges, can't wall jump.
        if (!HasAvailableCharges())
            return;

        // Only allow a new wall jump if the player is follows the conditions
        if (EvaluateWallJumpConditions())
        {
            // Consume charge if needed, otherwise exit early.
            if(!TryConsumeCharge())
                return;
            
            // Enter the Wall Jump state.
            linkedStateMachine.ChangeState(PlayerStates.State.WallJump);
            
            // Set timer to max wall jump time.
            wallJumpTimer = maxWallJumpTime;
            minWallJumpTime = 0.15f;

            // Flip the player and then add the velocity.
            player.ForceFlip();
            linkedPhysics.rb.linearVelocity = player.FacingRight ? 
                new Vector2(wallJumpForce.x, wallJumpForce.y) : 
                new Vector2(-wallJumpForce.x, wallJumpForce.y);
        }
    }
    
    /// <summary>
    /// Return if the player is able to wall jump using various condition checks
    /// </summary>
    private bool EvaluateWallJumpConditions()
    {
        if (linkedPhysics.IsGrounded || !linkedPhysics.IsWallDetected || !IsAllowedForCurrentClass())
            return false;

        return true;
    }
    #endregion
}
