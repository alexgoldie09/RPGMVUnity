using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ability that handles the player's jump behavior, including
/// applying vertical forces, handling air control, and updating jump-related animations.
/// </summary>
public class JumpAbility : BaseAbility
{
    [Header("Jump Ability Settings")]
    [Tooltip("Name of the animator bool parameter used to trigger the jump animation.")]
    [SerializeField] private string jumpAnimParameterName = "Jump";

    [Tooltip("Name of the animator float parameter used for the vertical speed in the blend tree.")]
    [SerializeField] private string ySpeedAnimParameterName = "ySpeed";

    [Tooltip("Initial vertical force applied when the player jumps.")]
    [SerializeField] private float jumpForce = 10f;

    [Tooltip("Horizontal air movement speed while the player is in the air.")]
    [SerializeField] private float jumpSpeed = 5f;

    [Tooltip("Minimum amount of time the player must remain in the air before landing is allowed.")]
    [SerializeField] private float minJumpTime = 0.2f;

    [Tooltip("Input System action reference used to trigger jumps.")]
    [SerializeField] private InputActionReference jumpActionRef;
    
    private float origJumpTime; // Reference to store original minimum for air time
    private int jumpParameterID; // Cached hash for the jump animation parameter to avoid repeated string lookups.
    private int ySpeedParameterID; // Cached hash for the ySpeed animation parameter to avoid repeated string lookups.

    #region Base Class Overrides
    /// <summary>
    /// Performs initialization for the jump ability.
    /// Caches references, original jump time, and animator parameter hashes.
    /// </summary>
    protected override void Initialization()
    {
        // Call the base initialization to set up shared references.
        base.Initialization();

        // Store the original minimum jump time so we can reset it each time we jump.
        origJumpTime = minJumpTime;

        // Cache animation parameter hashes for performance.
        jumpParameterID = Animator.StringToHash(jumpAnimParameterName);
        ySpeedParameterID = Animator.StringToHash(ySpeedAnimParameterName);
    }
    
    /// <summary>
    /// Per-frame logic while in the jump state.
    /// Handles facing direction and transition back to idle when grounded and allowed by minJumpTime.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // Ensure the player sprite faces in the correct direction while jumping.
        player.Flip();

        // Decrease the remaining minimum airtime.
        minJumpTime -= Time.deltaTime;

        // Once grounded and the minimum airtime has elapsed, return to the idle state.
        if (linkedPhysics.IsGrounded && minJumpTime < 0)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
    }

    /// <summary>
    /// Physics step for the jump ability.
    /// Handles horizontal air control while the player is not grounded.
    /// </summary>
    public override void ProcessFixedAbility()
    {
        // Only apply air control when not grounded to avoid overriding grounded movement.
        if (!linkedPhysics.IsGrounded)
        {
            // Preserve the current vertical velocity, but apply horizontal control.
            linkedPhysics.rb.linearVelocity = new Vector2(
                jumpSpeed * linkedInput.HorizontalInput,
                linkedPhysics.rb.linearVelocityY
            );
        }
    }

    /// <summary>
    /// Updates animator parameters related to the jump state and vertical speed.
    /// </summary>
    public override void UpdateAnimator()
    {
        // Enable the jump animation only while the state machine is in the Jump state.
        linkedAnim.SetBool(
            jumpParameterID,
            linkedStateMachine.currentState == PlayerStates.State.Jump
        );

        // Update the vertical speed parameter for the animator blend tree.
        linkedAnim.SetFloat(
            ySpeedParameterID,
            linkedPhysics.rb.linearVelocityY
        );
    }
    #endregion

    #region Unity Events

    /// <summary>
    /// Called when the component becomes enabled.
    /// Subscribes to jump input events for performing and canceling jumps.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to perform jump function.
        jumpActionRef.action.performed += TryToJump;

        // Subscribe to cancel jump function.
        jumpActionRef.action.canceled += StopJump;
    }

    /// <summary>
    /// Called when the component becomes disabled.
    /// Unsubscribes from jump input events to prevent callbacks on disabled objects.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from jump function.
        jumpActionRef.action.performed -= TryToJump;

        // Unsubscribe from cancel jump function.
        jumpActionRef.action.canceled -= StopJump;
    }

    #endregion

    #region Jump Actions

    /// <summary>
    /// Initiates a jump action when the jump binding is performed.
    /// Changes the state to Jump and applies the initial jump force if grounded and permitted.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void TryToJump(InputAction.CallbackContext value)
    {
        // If this ability is not allowed right now, do nothing.
        if (!isPermitted)
            return;

        // Only allow a new jump if the player is grounded.
        if (linkedPhysics.IsGrounded)
        {
            // Enter the Jump state.
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);

            // Apply an initial vertical jump force combined with horizontal movement direction.
            linkedPhysics.rb.linearVelocity = new Vector2(
                jumpSpeed * linkedInput.HorizontalInput,
                jumpForce
            );

            // Reset minimum airtime for this jump.
            minJumpTime = origJumpTime;
        }
    }

    /// <summary>
    /// Cancels the jump action when the jump binding is released.
    /// Currently only logs a message but can be extended for variable jump heights.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void StopJump(InputAction.CallbackContext value)
    {
        Debug.Log("Stop jump");
    }

    #endregion
}
