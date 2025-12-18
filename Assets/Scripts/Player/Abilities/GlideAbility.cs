using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Ability that handles the player's glide behaviour, including
/// applying slowing Y velocity motion, horizontal input and updating glide-related animations.
/// </summary>
public class GlideAbility : BaseAbility
{
    [FormerlySerializedAs("maxGlideSpeed")]
    [Header("Glide Ability Settings")]
    
    [Tooltip("Name of the animator bool parameter used to trigger the glide animation.")]
    [SerializeField] private string glideAnimParameterName = "Glide";
    
    [Tooltip("Max speed for downwards gliding.")]
    [SerializeField] private float maxGlideVerticalSpeed = 0.5f;
    
    [Tooltip("Horizontal movement speed while gliding.")]
    [SerializeField] private float glideHorizontalSpeed = 5f;
    
    private int glideParameterID; // Cached hash for the jump animation parameter to avoid repeated string lookups.
    
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
        glideParameterID = Animator.StringToHash(glideAnimParameterName);
    }
    
    /// <summary>
    /// Actions to perform once entering the ability.
    /// </summary>
    public override void EnterAbility()
    {
        linkedPhysics.ResetVelocity();
    }

    /// <summary>
    /// Per-frame logic while in the glide state.
    /// </summary>
    public override void ProcessUpdateAbility()
    {
        // Ensure the player sprite faces in the correct direction while jumping.
        player.Flip();
        
        // Once grounded, exit
        if (linkedPhysics.IsGrounded)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
        
        // Only run glide state for allowed class (Mage, given your inspector settings).
        if (!IsAllowedForCurrentClass())
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
        }
    }
    
    /// <summary>
    /// Physics step for the glide ability.
    /// Handles horizontal air control while the player is not grounded,
    /// and clamps the fall speed.
    /// </summary>
    public override void ProcessFixedAbility()
    {
        // Only apply glide control while in the air.
        if (!linkedPhysics.IsGrounded)
        {
            // Clamp vertical speed for the glide.
            float clampedY = Mathf.Clamp(
                linkedPhysics.rb.linearVelocityY,
                -maxGlideVerticalSpeed,
                1f
            );

            // Apply horizontal input like in jump falling, but using glideHorizontalSpeed.
            linkedPhysics.rb.linearVelocity = new Vector2(
                glideHorizontalSpeed * linkedInput.HorizontalInput,
                clampedY
            );
        }
    }
    
    /// <summary>
    /// Updates animator parameters related to the jump state and vertical speed.
    /// </summary>
    public override void UpdateAnimator()
    {
        if (IsAllowedForCurrentClass())
        {
            // Enable the jump animation only while the state machine is in the Glide state.
            linkedAnim.SetBool(
                glideParameterID,
                linkedStateMachine.currentState == PlayerStates.State.Glide
            );
        }
    }
    #endregion
}
