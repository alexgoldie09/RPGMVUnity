using UnityEngine;

/// <summary>
/// Base class for all player abilities, providing shared references and lifecycle hooks.
/// </summary>
public class BaseAbility : MonoBehaviour
{
    protected PlayerController player; // Reference to the player.
    protected GatherInput linkedInput; // Reference to the player's input handler.
    protected PhysicsControl linkedPhysics; // Reference to the player's physics controller.
    protected StateMachine linkedStateMachine; // Reference to the player's state machine.
    protected Animator linkedAnim; // Reference to the player's animator component.

    [Header("Ability Settings")]
    [Tooltip("The player state that this ability is responsible for handling.")]
    public PlayerStates.State thisAbilityState;

    [Tooltip("If false, this ability will be ignored when attempting to enter this state.")]
    public bool isPermitted = true;

    #region Unity Lifecycle

    /// <summary>
    /// Start is called before the first frame update.
    /// Initializes all core references for the ability.
    /// </summary>
    protected virtual void Start()
    {
        Initialization(); // Cache component references on Start.
    }

    #endregion

    #region Ability Lifecycle

    /// <summary>
    /// Called when this ability becomes the active state.
    /// Override this in derived abilities to implement enter logic.
    /// </summary>
    public virtual void EnterAbility() { }

    /// <summary>
    /// Called when this ability stops being the active state.
    /// Override this in derived abilities to implement exit logic.
    /// </summary>
    public virtual void ExitAbility() { }

    /// <summary>
    /// Called every frame while this ability is the active state.
    /// Override this in derived abilities to handle Update logic.
    /// </summary>
    public virtual void ProcessUpdateAbility() { }

    /// <summary>
    /// Called every fixed timestep while this ability is the active state.
    /// Override this in derived abilities to handle FixedUpdate logic.
    /// </summary>
    public virtual void ProcessFixedAbility() { }
    
    /// <summary>
    /// Called when this ability needs to update the animator state.
    /// </summary>
    public virtual void UpdateAnimator() { }
    #endregion

    #region Initialization

    /// <summary>
    /// Caches references to the PlayerController, GatherInput, and StateMachine components.
    /// </summary>
    protected virtual void Initialization()
    {
        // Get PlayerController attached to the same GameObject.
        player = GetComponentInParent<PlayerController>();

        if (player != null)
        {
            // Cache references from the PlayerController.
            linkedInput = player.gatherInput;          // Input handler used by the player.
            linkedPhysics = player.physicsControl;     // Physics controller used by the player.
            linkedStateMachine = player.stateMachine;  // State machine managing player states.
            linkedAnim = player.anim;                  // Animator handling player animations.
        }
    }

    #endregion
}
