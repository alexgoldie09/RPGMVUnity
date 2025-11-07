using UnityEngine;

/// <summary>
/// Ability that handles horizontal movement logic for the player,
/// including transitioning between idle and run states and driving movement physics.
/// </summary>
public class MoveAbility : BaseAbility
{
    [Header("Move Ability Settings")]
    [Tooltip("Name of the animator bool parameter used to trigger the run animation.")]
    [SerializeField] private string runAnimParameterName = "Run";

    [Tooltip("Horizontal move speed applied while this ability is active.")]
    [SerializeField] private float moveSpeed;

    private int runParameterID; // Cached hash for the run animation parameter to avoid repeated string lookups.

    #region Initialization

    /// <summary>
    /// Performs initialization specific to the move ability,
    /// including caching the animator parameter hash.
    /// </summary>
    protected override void Initialization()
    {
        // Run base initialization to cache shared references.
        base.Initialization();

        // Convert the run animation parameter name to a hash for faster access.
        runParameterID = Animator.StringToHash(runAnimParameterName);
    }

    #endregion

    #region Ability Lifecycle

    /// <summary>
    /// Per-frame logic while in the move/run state.
    /// Handles transitions back to idle when input stops.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // If there is horizontal input while running,
        // ensure the player is facing the correct direction.
        if (linkedInput.HorizontalInput != 0)
        {
            player.Flip();
        }
        
        // If there is no horizontal input, transition back to the idle state.
        if (linkedInput.HorizontalInput == 0)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
    }

    /// <summary>
    /// Physics step for the move ability.
    /// Applies horizontal velocity based on input and configured move speed.
    /// </summary>
    public override void ProcessFixedAbility()
    {
        // Apply horizontal velocity based on input while preserving vertical velocity.
        linkedPhysics.rb.linearVelocity = new Vector2(
            moveSpeed * linkedInput.HorizontalInput,
            linkedPhysics.rb.linearVelocityY
        );
    }

    /// <summary>
    /// Updates animator parameters related to the run state.
    /// </summary>
    public override void UpdateAnimator()
    {
        // Enable the run animation only while the state machine is in the Run state.
        linkedAnim.SetBool(
            runParameterID,
            linkedStateMachine.currentState == PlayerStates.State.Run
        );
    }

    #endregion
}
