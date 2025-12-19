using UnityEngine;
using UnityEngine.InputSystem;
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
    
    [Tooltip("Input System action reference used to trigger jumps.")]
    [SerializeField] private InputActionReference glideActionRef;
    
    private int glideParameterID; // Cached hash for the glide animation parameter to avoid repeated string lookups.
    
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
    
    #region Unity Events
    /// <summary>
    /// Called when the component becomes enabled.
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to perform glide function.
        glideActionRef.action.performed += TryToGlide;

        // Subscribe to cancel/stop glide when the binding is released.
        glideActionRef.action.canceled += StopGlide;
    }

    /// <summary>
    /// Called when the component becomes disabled.
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from glide function.
        glideActionRef.action.performed -= TryToGlide;

        // Unsubscribe from stop glide function.
        glideActionRef.action.canceled -= StopGlide;
    }
    #endregion
    
    #region Glide Actions
    /// <summary>
    /// Initiates a glide action when the glide binding is performed.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void TryToGlide(InputAction.CallbackContext value)
    {
        // If this ability is not allowed right now, do nothing.
        if (!isPermitted)
            return;
        
        // If out of charges, can't glide.
        if (!HasAvailableCharges())
            return;

        // Only allow gliding if the player is follows the conditions
        if (EvaluateGlideConditions())
        {
            // Consume charge if needed, otherwise exit early.
            if(!TryConsumeCharge())
                return;
            
            // Enter the Wall Jump state.
            linkedStateMachine.ChangeState(PlayerStates.State.Glide);
        }
    }
    
    /// <summary>
    /// Stops gliding when the glide binding is released.
    /// </summary>
    /// <param name="value">Callback context from the input system.</param>
    private void StopGlide(InputAction.CallbackContext value)
    {
        // Only do anything if we're actually in the Glide state.
        if (linkedStateMachine.currentState != PlayerStates.State.Glide)
            return;

        // If grounded, go back to idle; otherwise fall as part of the jump state.
        if (linkedPhysics.IsGrounded)
        {
            linkedStateMachine.ChangeState(PlayerStates.State.Idle);
        }
        else
        {
            // Go back to Jump so normal falling physics take over.
            linkedStateMachine.ChangeState(PlayerStates.State.Jump);
        }
    }
    
    /// <summary>
    /// Return if the player is able to glide using various condition checks
    /// </summary>
    private bool EvaluateGlideConditions()
    {
        if (linkedPhysics.IsGrounded || linkedPhysics.rb.linearVelocityY > 0 || !IsAllowedForCurrentClass())
            return false;

        return true;
    }
    #endregion
}
