using UnityEngine;

/// <summary>
/// Ability that controls the player's idle state,
/// including halting horizontal movement and handling transitions out of idle.
/// </summary>
public class IdleAbility : BaseAbility
{
    [Header("Idle Ability Settings")]
    [Tooltip("Name of the animator bool parameter used to trigger the idle animation.")]
    [SerializeField] private string idleAnimParameterName = "Idle";
    
    private int idleParameterID; // Cached hash for the idle animation parameter to avoid repeated string lookups.

    #region Ability Lifecycle

    /// <summary>
    /// Called when the player enters the idle state.
    /// Stops horizontal movement while keeping any vertical velocity (e.g. gravity).
    /// </summary>
    public override void EnterAbility()
    {
        // Zero out horizontal velocity while preserving vertical motion.
        linkedPhysics.rb.linearVelocityX = 0;
    }

    /// <summary>
    /// Per-frame logic while in the idle state.
    /// Checks for horizontal input and transitions to the run state when needed.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // If the player is not grounded, transition to the Jump state (falling in the blend tree).
        if (!linkedPhysics.IsGrounded)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
            return; // Don't process idle/run logic this frame.
        }

        // If there is any horizontal input and we are grounded, flip and go to Run.
        if (linkedInput.HorizontalInput != 0)
        {
            player.Flip();
            linkedStateMachine.ChangeState(PlayerStates.State.Run);
        }
    }
    
    /// <summary>
    /// Updates animator parameters related to the idle state.
    /// </summary>
    public override void UpdateAnimator()
    {
        // Enable the idle animation only while the state machine is in the Idle state.
        linkedAnim.SetBool(
            idleParameterID,
            linkedStateMachine.currentState == PlayerStates.State.Idle
        );
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Performs initialization specific to the idle ability,
    /// including caching the animator parameter hash.
    /// </summary>
    protected override void Initialization()
    {
        // Run base initialization to cache common references.
        base.Initialization();

        // Convert the idle animation parameter name to a hash for faster access.
        idleParameterID = Animator.StringToHash(idleAnimParameterName);
    }

    #endregion
}
